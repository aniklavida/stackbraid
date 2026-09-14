// Connects to whichever realtime transport the target backend actually
// speaks. `contract/openapi.yaml`'s `x-realtime-channels` documents exactly
// two shapes — a SignalR hub (.NET) or a native WebSocket (Python) — and
// this probes both rather than being told which one to expect: the suite
// still takes one positional argument (a base URL) and nothing else, the
// same rule the rest of the runner follows (see `contract/README.md`).
//
// Uses this package's own `ws-client.mjs` (a minimal, hand-rolled RFC 6455
// client — no `ws` package; see that file for why Node's global
// `WebSocket` cannot be relied on at package.json's own declared engine
// floor). SignalR's own wire format (the JSON Hub Protocol) is a handful
// of JSON objects separated by the ASCII record separator (`\x1e`),
// documented and stable; hand-rolling the handshake and the one message
// shape this suite needs keeps the suite's own zero-runtime-dependency
// rule intact for its realtime coverage too.

import { MinimalWebSocket } from './ws-client.mjs';

const RECORD_SEPARATOR = '\x1e';
const OPEN_TIMEOUT_MS = 4_000;

function toWsUrl(baseUrl, path, accessToken) {
  const url = new URL(path, baseUrl);
  url.protocol = url.protocol === 'https:' ? 'wss:' : 'ws:';
  url.searchParams.set('access_token', accessToken);
  return url.toString();
}

function openSocket(url) {
  return new Promise((resolve, reject) => {
    const ws = new MinimalWebSocket(url);
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
      ws.off('open', onOpen);
      ws.off('error', onError);
      ws.off('close', onClose);
    }
    function onOpen() {
      cleanup();
      resolve(ws);
    }
    function onError(err) {
      cleanup();
      reject(err instanceof Error ? err : new Error(`could not open ${url}`));
    }
    function onClose() {
      cleanup();
      reject(new Error(`closed before opening ${url}`));
    }
    ws.on('open', onOpen);
    ws.on('error', onError);
    ws.on('close', onClose);
  });
}

function performSignalRHandshake(ws) {
  ws.send(JSON.stringify({ protocol: 'json', version: 1 }) + RECORD_SEPARATOR);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      ws.off('message', onMessage);
      reject(new Error('SignalR handshake timed out'));
    }, OPEN_TIMEOUT_MS);

    function onMessage(text) {
      const idx = text.indexOf(RECORD_SEPARATOR);
      if (idx === -1) return;
      clearTimeout(timer);
      ws.off('message', onMessage);
      const raw = text.slice(0, idx);
      const parsed = raw.length ? JSON.parse(raw) : {};
      if (parsed.error) reject(new Error(`SignalR handshake rejected: ${parsed.error}`));
      else resolve();
    }
    ws.on('message', onMessage);
  });
}

/**
 * @param {string} httpBaseUrl
 * @param {'notifications'|'jobs'} channel
 * @param {string} accessToken
 * @param {string} [jobId] required for the `jobs` channel
 * @returns {Promise<{transport: 'signalr'|'native-websocket', start: () => void, waitForMessage: (timeoutMs?: number) => Promise<unknown>, close: () => void}>}
 */
export async function connectRealtimeChannel(httpBaseUrl, channel, accessToken, jobId) {
  const hubPath = channel === 'notifications' ? '/v1/hubs/notifications' : '/v1/hubs/jobs';
  const wsPath = channel === 'notifications' ? '/v1/ws/notifications' : `/v1/ws/jobs/${jobId}`;

  let transport;
  let ws;
  try {
    ws = await openSocket(toWsUrl(httpBaseUrl, hubPath, accessToken));
    await performSignalRHandshake(ws);
    transport = 'signalr';
  } catch {
    ws = await openSocket(toWsUrl(httpBaseUrl, wsPath, accessToken));
    transport = 'native-websocket';
  }

  const messageQueue = [];
  const waiters = [];
  let buffer = '';

  function deliver(payload) {
    if (waiters.length) waiters.shift()(payload);
    else messageQueue.push(payload);
  }

  ws.on('message', (text) => {
    if (transport === 'signalr') {
      buffer += text;
      let idx;
      while ((idx = buffer.indexOf(RECORD_SEPARATOR)) !== -1) {
        const raw = buffer.slice(0, idx);
        buffer = buffer.slice(idx + 1);
        if (!raw) continue;
        const parsed = JSON.parse(raw);
        // type 1 = Invocation. Every send in this suite's own hubs
        // (`Host/Realtime/SignalRRealtimePublisher.cs`) targets "message";
        // ignore anything else (type 6 = ping, type 3 = completion, ...).
        if (parsed.type === 1 && parsed.target === 'message' && Array.isArray(parsed.arguments)) {
          deliver(parsed.arguments[0]);
        }
      }
    } else {
      deliver(JSON.parse(text));
    }
  });

  if (transport === 'signalr' && channel === 'jobs') {
    // Join the job's group before the caller can start it — a plain WS
    // connection to a job id in its own URL path has no equivalent race.
    ws.send(JSON.stringify({ type: 1, target: 'Subscribe', arguments: [jobId] }) + RECORD_SEPARATOR);
  }

  return {
    transport,
    start() {
      if (transport === 'signalr') {
        ws.send(JSON.stringify({ type: 1, target: 'StartDemoJob', arguments: [jobId] }) + RECORD_SEPARATOR);
      } else {
        ws.send(JSON.stringify({ action: 'start_demo_job' }));
      }
    },
    waitForMessage(timeoutMs = 5_000) {
      if (messageQueue.length) return Promise.resolve(messageQueue.shift());
      return new Promise((resolve, reject) => {
        const timer = setTimeout(
          () => reject(new Error(`no realtime message arrived within ${timeoutMs}ms (transport: ${transport})`)),
          timeoutMs,
        );
        waiters.push((payload) => {
          clearTimeout(timer);
          resolve(payload);
        });
      });
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
