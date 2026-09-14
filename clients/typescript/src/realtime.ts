// Hand-written, unlike everything under `src/generated/` — OpenAPI has no
// concept of a push channel (see `contract/openapi.yaml`'s
// `x-realtime-channels` extension), so there is nothing for the generator
// to produce here. This module still imports its message *types* from the
// generated output rather than redeclaring them, so a future contract
// change to `RealtimeMessage` is caught by `tsc`, not discovered at
// runtime.
//
// Connects to whichever realtime transport the target backend actually
// speaks — a SignalR hub (.NET) or a native WebSocket (Python) — the same
// two documented shapes `contract/conformance/src/realtime.mjs` probes.
// SignalR's own wire format (the JSON Hub Protocol) is a handful of JSON
// objects separated by the ASCII record separator (`\x1e`); hand-rolling
// the small part of it a client needs here keeps this package's own
// zero-runtime-dependency rule intact — no `@microsoft/signalr` package,
// just the platform's own `WebSocket`.

import type { RealtimeMessage } from './generated/types.gen.js';

export type { RealtimeMessage };

const RECORD_SEPARATOR = '\x1e';
const OPEN_TIMEOUT_MS = 4_000;

export type RealtimeChannel = 'notifications' | 'jobs';
export type RealtimeTransport = 'signalr' | 'native-websocket';

export interface RealtimeConnection {
  /** Which transport this connection turned out to be — informational only; callers never need to branch on it. */
  readonly transport: RealtimeTransport;
  /** Notifications channel: none. Jobs channel: starts the connected job (or the shared demo job — see `docs/SPEC.md` §13). */
  start(): void;
  /** Registers a handler for every `RealtimeMessage` this connection receives, in arrival order. */
  onMessage(handler: (message: RealtimeMessage) => void): void;
  close(): void;
}

function toWsUrl(baseUrl: string, path: string, accessToken: string): string {
  const url = new URL(path, baseUrl);
  url.protocol = url.protocol === 'https:' ? 'wss:' : 'ws:';
  url.searchParams.set('access_token', accessToken);
  return url.toString();
}

function openSocket(url: string): Promise<WebSocket> {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(url);
    const timer = setTimeout(() => {
      cleanup();
      try {
        ws.close();
      } catch {
        // already closing
      }
      reject(new Error(`timed out opening ${url}`));
    }, OPEN_TIMEOUT_MS);

    function cleanup() {
      clearTimeout(timer);
      ws.removeEventListener('open', onOpen);
      ws.removeEventListener('error', onError);
      ws.removeEventListener('close', onClose);
    }
    function onOpen() {
      cleanup();
      resolve(ws);
    }
    function onError() {
      cleanup();
      reject(new Error(`could not open ${url}`));
    }
    function onClose(event: CloseEvent) {
      cleanup();
      reject(new Error(`closed before opening ${url} (code ${event.code})`));
    }
    ws.addEventListener('open', onOpen);
    ws.addEventListener('error', onError);
    ws.addEventListener('close', onClose);
  });
}

function performSignalRHandshake(ws: WebSocket): Promise<void> {
  ws.send(JSON.stringify({ protocol: 'json', version: 1 }) + RECORD_SEPARATOR);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      ws.removeEventListener('message', onMessage);
      reject(new Error('SignalR handshake timed out'));
    }, OPEN_TIMEOUT_MS);

    function onMessage(event: MessageEvent) {
      const text = typeof event.data === 'string' ? event.data : String(event.data);
      const idx = text.indexOf(RECORD_SEPARATOR);
      if (idx === -1) return;
      clearTimeout(timer);
      ws.removeEventListener('message', onMessage);
      const raw = text.slice(0, idx);
      const parsed = raw.length ? JSON.parse(raw) : {};
      if (parsed.error) reject(new Error(`SignalR handshake rejected: ${parsed.error}`));
      else resolve();
    }
    ws.addEventListener('message', onMessage);
  });
}

/**
 * @param httpBaseUrl The backend's own HTTP(S) base URL — the same one every other generated call in this package targets.
 * @param channel `'notifications'` (per-user) or `'jobs'` (per-job — pass `entityId`).
 * @param accessToken The bearer access token this connection authenticates as (a browser `WebSocket` cannot set an `Authorization` header, so both backends accept it as a query parameter instead — see `contract/openapi.yaml`'s `x-realtime-channels`).
 * @param entityId The job id, required for the `'jobs'` channel; ignored for `'notifications'`.
 */
export async function connectRealtimeChannel(
  httpBaseUrl: string,
  channel: RealtimeChannel,
  accessToken: string,
  entityId?: string,
): Promise<RealtimeConnection> {
  const hubPath = channel === 'notifications' ? '/v1/hubs/notifications' : '/v1/hubs/jobs';
  const wsPath = channel === 'notifications' ? '/v1/ws/notifications' : `/v1/ws/jobs/${entityId}`;

  let transport: RealtimeTransport;
  let ws: WebSocket;
  try {
    ws = await openSocket(toWsUrl(httpBaseUrl, hubPath, accessToken));
    await performSignalRHandshake(ws);
    transport = 'signalr';
  } catch {
    ws = await openSocket(toWsUrl(httpBaseUrl, wsPath, accessToken));
    transport = 'native-websocket';
  }

  if (transport === 'signalr' && channel === 'jobs') {
    ws.send(JSON.stringify({ type: 1, target: 'Subscribe', arguments: [entityId] }) + RECORD_SEPARATOR);
  }

  const handlers: Array<(message: RealtimeMessage) => void> = [];
  let buffer = '';

  ws.addEventListener('message', (event: MessageEvent) => {
    const text = typeof event.data === 'string' ? event.data : String(event.data);
    if (transport === 'signalr') {
      buffer += text;
      let idx: number;
      // eslint-disable-next-line no-cond-assign
      while ((idx = buffer.indexOf(RECORD_SEPARATOR)) !== -1) {
        const raw = buffer.slice(0, idx);
        buffer = buffer.slice(idx + 1);
        if (!raw) continue;
        const parsed = JSON.parse(raw);
        if (parsed.type === 1 && parsed.target === 'message' && Array.isArray(parsed.arguments)) {
          for (const handler of handlers) handler(parsed.arguments[0] as RealtimeMessage);
        }
      }
    } else {
      const message = JSON.parse(text) as RealtimeMessage;
      for (const handler of handlers) handler(message);
    }
  });

  return {
    transport,
    start() {
      if (transport === 'signalr') {
        ws.send(JSON.stringify({ type: 1, target: 'StartDemoJob', arguments: [entityId] }) + RECORD_SEPARATOR);
      } else {
        ws.send(JSON.stringify({ action: 'start_demo_job' }));
      }
    },
    onMessage(handler) {
      handlers.push(handler);
    },
    close() {
      try {
        ws.close();
      } catch {
        // already closed
      }
    },
  };
}
