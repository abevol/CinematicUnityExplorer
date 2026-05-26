import { createHash, randomBytes } from "node:crypto";
import { Socket } from "node:net";

export type UnityBridgeResponse =
  | { id: string; ok: true; result: unknown }
  | { id: string; ok: false; error: { code: string; message: string } };

export class UnityBridgeClient {
  private readonly host: string;
  private readonly port: number;
  private readonly timeoutMs: number;
  private socket: Socket | null = null;
  private connected = false;
  private buffer = Buffer.alloc(0);
  private connecting: Promise<void> | null = null;
  private readonly pending = new Map<
    string,
    {
      resolve: (response: UnityBridgeResponse) => void;
      reject: (error: Error) => void;
      timeout: NodeJS.Timeout;
    }
  >();

  constructor(options: { host?: string; port?: number; timeoutMs?: number } = {}) {
    this.host = options.host ?? "127.0.0.1";
    this.port = options.port ?? 8765;
    this.timeoutMs = options.timeoutMs ?? 5000;
  }

  async request(action: string, params: Record<string, unknown>): Promise<UnityBridgeResponse> {
    await this.ensureConnected();

    const id = randomBytes(12).toString("hex");
    const payload = JSON.stringify({ id, action, params });

    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        this.pending.delete(id);
        reject(new Error(`Unity bridge request timed out after ${this.timeoutMs}ms.`));
      }, this.timeoutMs);

      this.pending.set(id, { resolve, reject, timeout });

      try {
        this.socket?.write(encodeFrame(Buffer.from(payload, "utf8")));
      } catch (error) {
        clearTimeout(timeout);
        this.pending.delete(id);
        reject(error instanceof Error ? error : new Error(String(error)));
      }
    });
  }

  private async ensureConnected(): Promise<void> {
    if (this.connected && this.socket && !this.socket.destroyed) {
      return;
    }

    if (!this.connecting) {
      this.connecting = this.connect().finally(() => {
        this.connecting = null;
      });
    }

    await this.connecting;
  }

  private connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      const socket = new Socket();
      const key = randomBytes(16).toString("base64");
      const request =
        `GET / HTTP/1.1\r\n` +
        `Host: ${this.host}:${this.port}\r\n` +
        `Upgrade: websocket\r\n` +
        `Connection: Upgrade\r\n` +
        `Sec-WebSocket-Key: ${key}\r\n` +
        `Sec-WebSocket-Version: 13\r\n\r\n`;

      let handshakeBuffer = Buffer.alloc(0);
      const timeout = setTimeout(() => {
        socket.destroy();
        reject(new Error("Unity bridge connection timed out."));
      }, this.timeoutMs);

      const fail = (error: Error) => {
        clearTimeout(timeout);
        socket.destroy();
        reject(error);
      };

      socket.once("error", fail);
      socket.connect(this.port, this.host, () => {
        socket.write(request);
      });

      socket.on("data", (chunk) => {
        if (!this.connected) {
          handshakeBuffer = Buffer.concat([handshakeBuffer, chunk]);
          const end = handshakeBuffer.indexOf("\r\n\r\n");
          if (end < 0) {
            return;
          }

          const header = handshakeBuffer.subarray(0, end).toString("ascii");
          const expected = createHash("sha1")
            .update(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")
            .digest("base64");

          if (!header.startsWith("HTTP/1.1 101") || !header.includes(`Sec-WebSocket-Accept: ${expected}`)) {
            fail(new Error("Unity bridge WebSocket handshake failed."));
            return;
          }

          clearTimeout(timeout);
          socket.removeListener("error", fail);
          this.socket = socket;
          this.connected = true;
          socket.on("error", () => this.disconnectPending("Unity bridge socket error."));
          socket.on("close", () => this.disconnectPending("Unity bridge disconnected."));

          const remaining = handshakeBuffer.subarray(end + 4);
          if (remaining.length > 0) {
            this.onFrameData(remaining);
          }

          resolve();
          return;
        }

        this.onFrameData(chunk);
      });
    });
  }

  private onFrameData(chunk: Buffer): void {
    this.buffer = Buffer.concat([this.buffer, chunk]);

    while (true) {
      const decoded = decodeFrame(this.buffer);
      if (!decoded) {
        return;
      }

      this.buffer = this.buffer.subarray(decoded.bytesRead);

      if (decoded.opCode === 8) {
        this.disconnectPending("Unity bridge closed the WebSocket.");
        return;
      }

      if (decoded.opCode !== 1) {
        continue;
      }

      const response = JSON.parse(decoded.payload.toString("utf8")) as UnityBridgeResponse;
      const pending = this.pending.get(response.id);
      if (!pending) {
        continue;
      }

      clearTimeout(pending.timeout);
      this.pending.delete(response.id);
      pending.resolve(response);
    }
  }

  private disconnectPending(message: string): void {
    this.connected = false;
    this.socket = null;
    for (const [id, pending] of this.pending) {
      clearTimeout(pending.timeout);
      pending.reject(new Error(message));
      this.pending.delete(id);
    }
  }
}

function encodeFrame(payload: Buffer): Buffer {
  const mask = randomBytes(4);
  const header: number[] = [0x81];

  if (payload.length < 126) {
    header.push(0x80 | payload.length);
  } else if (payload.length <= 0xffff) {
    header.push(0x80 | 126, (payload.length >> 8) & 0xff, payload.length & 0xff);
  } else {
    const length = Buffer.alloc(8);
    length.writeBigUInt64BE(BigInt(payload.length), 0);
    header.push(0x80 | 127, ...length);
  }

  const masked = Buffer.alloc(payload.length);
  for (let i = 0; i < payload.length; i++) {
    masked[i] = payload[i] ^ mask[i % 4];
  }

  return Buffer.concat([Buffer.from(header), mask, masked]);
}

function decodeFrame(buffer: Buffer): { opCode: number; payload: Buffer; bytesRead: number } | null {
  if (buffer.length < 2) {
    return null;
  }

  const opCode = buffer[0] & 0x0f;
  let offset = 2;
  let length = buffer[1] & 0x7f;

  if (length === 126) {
    if (buffer.length < offset + 2) {
      return null;
    }
    length = buffer.readUInt16BE(offset);
    offset += 2;
  } else if (length === 127) {
    if (buffer.length < offset + 8) {
      return null;
    }
    const bigLength = buffer.readBigUInt64BE(offset);
    if (bigLength > BigInt(Number.MAX_SAFE_INTEGER)) {
      throw new Error("Unity bridge frame is too large.");
    }
    length = Number(bigLength);
    offset += 8;
  }

  const masked = (buffer[1] & 0x80) !== 0;
  let mask: Buffer | null = null;
  if (masked) {
    if (buffer.length < offset + 4) {
      return null;
    }
    mask = buffer.subarray(offset, offset + 4);
    offset += 4;
  }

  if (buffer.length < offset + length) {
    return null;
  }

  const payload = Buffer.from(buffer.subarray(offset, offset + length));
  if (mask) {
    for (let i = 0; i < payload.length; i++) {
      payload[i] = payload[i] ^ mask[i % 4];
    }
  }

  return { opCode, payload, bytesRead: offset + length };
}
