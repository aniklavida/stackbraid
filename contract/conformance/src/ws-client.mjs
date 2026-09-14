// A minimal RFC 6455 WebSocket client — hand-rolled, not `ws` or any other
// package. Node's own global `WebSocket` is stable only from v22.4.0
// onward and does not exist at all (not even behind a flag reachable
// without editing this suite's own invocation) on Node 20, which is what
// this repository's CI runs and what package.json's own `engines` field
// (">=18.17.0") commits to supporting. This keeps the conformance suite's
// zero-runtime-dependency rule intact for realtime the same way
// hand-rolling the SignalR JSON Hub Protocol handshake in `realtime.mjs`
// does — both are small, stable, documented wire formats.
//
// Implements exactly what this suite needs: a client-initiated handshake,
// masked outgoing text frames, unmasked incoming frames (server frames are
// never masked, but a mask is honored if present), fragmentation
// reassembly, and answering a ping with a pong. Binary frames are not
// used by anything realtime pushes here and are not implemented.

import { randomBytes } from 'node:crypto';
import { EventEmitter } from 'node:events';
import { connect as netConnect } from 'node:net';
import { connect as tlsConnect } from 'node:tls';

const OPCODE_CONTINUATION = 0x0;
const OPCODE_TEXT = 0x1;
const OPCODE_CLOSE = 0x8;
const OPCODE_PING = 0x9;
const OPCODE_PONG = 0xa;

/**
 * Events: `open`, `message` (string), `error` (Error), `close`.
 */
export class MinimalWebSocket extends EventEmitter {
  constructor(url) {
    super();
    this._url = new URL(url);
    this._buffer = Buffer.alloc(0);
    this._handshakeDone = false;
    this._fragments = [];
    this._fragmentOpcode = null;
    this._connect();
  }

  _connect() {
    const isTls = this._url.protocol === 'wss:';
    const port = Number(this._url.port || (isTls ? 443 : 80));
    const connectFn = isTls ? tlsConnect : netConnect;
    const key = randomBytes(16).toString('base64');

    this._socket = connectFn({ host: this._url.hostname, port }, () => {
      const path = this._url.pathname + (this._url.search || '');
      const request =
        `GET ${path} HTTP/1.1\r\n` +
        `Host: ${this._url.host}\r\n` +
        `Upgrade: websocket\r\n` +
        `Connection: Upgrade\r\n` +
        `Sec-WebSocket-Key: ${key}\r\n` +
        `Sec-WebSocket-Version: 13\r\n` +
        `\r\n`;
      this._socket.write(request);
    });

    this._socket.on('data', (chunk) => this._onData(chunk));
    this._socket.on('error', (err) => this.emit('error', err));
    this._socket.on('close', () => this.emit('close'));
  }

  _onData(chunk) {
    this._buffer = Buffer.concat([this._buffer, chunk]);

    if (!this._handshakeDone) {
      const headerEnd = this._buffer.indexOf('\r\n\r\n');
      if (headerEnd === -1) return; // wait for the rest of the response headers
      const header = this._buffer.subarray(0, headerEnd).toString('utf8');
      this._buffer = this._buffer.subarray(headerEnd + 4);

      const statusLine = header.split('\r\n')[0] || '';
      const statusMatch = statusLine.match(/^HTTP\/1\.1 (\d+)/);
      const status = statusMatch ? Number(statusMatch[1]) : 0;
      if (status !== 101) {
        this.emit('error', new Error(`WebSocket handshake failed: HTTP ${status || '(no status line)'}`));
        this._socket.destroy();
        return;
      }
      this._handshakeDone = true;
      this.emit('open');
    }

    this._parseFrames();
  }

  _parseFrames() {
    // eslint-disable-next-line no-constant-condition
    while (true) {
      if (this._buffer.length < 2) return;
      const byte0 = this._buffer[0];
      const byte1 = this._buffer[1];
      const fin = (byte0 & 0x80) !== 0;
      const opcode = byte0 & 0x0f;
      const masked = (byte1 & 0x80) !== 0;
      let payloadLen = byte1 & 0x7f;
      let offset = 2;

      if (payloadLen === 126) {
        if (this._buffer.length < offset + 2) return;
        payloadLen = this._buffer.readUInt16BE(offset);
        offset += 2;
      } else if (payloadLen === 127) {
        if (this._buffer.length < offset + 8) return;
        payloadLen = Number(this._buffer.readBigUInt64BE(offset));
        offset += 8;
      }

      let maskKey = null;
      if (masked) {
        if (this._buffer.length < offset + 4) return;
        maskKey = this._buffer.subarray(offset, offset + 4);
        offset += 4;
      }

      if (this._buffer.length < offset + payloadLen) return; // wait for the full frame

      let payload = this._buffer.subarray(offset, offset + payloadLen);
      if (masked) {
        const unmasked = Buffer.alloc(payload.length);
        for (let i = 0; i < payload.length; i += 1) unmasked[i] = payload[i] ^ maskKey[i % 4];
        payload = unmasked;
      }
      this._buffer = this._buffer.subarray(offset + payloadLen);

      this._handleFrame(fin, opcode, payload);
    }
  }

  _handleFrame(fin, opcode, payload) {
    if (opcode === OPCODE_CLOSE) {
      this._socket.end();
      return;
    }
    if (opcode === OPCODE_PING) {
      this._sendFrame(OPCODE_PONG, payload);
      return;
    }
    if (opcode === OPCODE_PONG) {
      return;
    }

    if (opcode === OPCODE_CONTINUATION) {
      this._fragments.push(payload);
    } else {
      this._fragments = [payload];
      this._fragmentOpcode = opcode;
    }

    if (fin) {
      const full = Buffer.concat(this._fragments);
      this._fragments = [];
      if (this._fragmentOpcode === OPCODE_TEXT) {
        this.emit('message', full.toString('utf8'));
      }
    }
  }

  send(text) {
    this._sendFrame(OPCODE_TEXT, Buffer.from(text, 'utf8'));
  }

  _sendFrame(opcode, payload) {
    const maskKey = randomBytes(4);
    const masked = Buffer.alloc(payload.length);
    for (let i = 0; i < payload.length; i += 1) masked[i] = payload[i] ^ maskKey[i % 4];

    const len = payload.length;
    let header;
    if (len < 126) {
      header = Buffer.from([0x80 | opcode, 0x80 | len]);
    } else if (len < 65536) {
      header = Buffer.alloc(4);
      header[0] = 0x80 | opcode;
      header[1] = 0x80 | 126;
      header.writeUInt16BE(len, 2);
    } else {
      header = Buffer.alloc(10);
      header[0] = 0x80 | opcode;
      header[1] = 0x80 | 127;
      header.writeBigUInt64BE(BigInt(len), 2);
    }
    this._socket.write(Buffer.concat([header, maskKey, masked]));
  }

  close() {
    try {
      this._sendFrame(OPCODE_CLOSE, Buffer.alloc(0));
    } catch {
      // socket may already be gone
    }
    this._socket.destroy();
  }
}
