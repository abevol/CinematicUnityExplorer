using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace UnityExplorer.McpBridge
{
    internal sealed class McpWebSocketServer
    {
        private readonly int port;
        private readonly object writeLock = new();
        private Func<string, string> messageHandler;
        private TcpListener listener;
        private Thread listenerThread;
        private bool running;

        public McpWebSocketServer(int port)
        {
            this.port = port;
        }

        public void Start(Func<string, string> messageHandler)
        {
            if (running)
                return;

            this.messageHandler = messageHandler;
            running = true;
            listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "UnityExplorer MCP WebSocket"
            };
            listenerThread.Start();
        }

        public void Stop()
        {
            running = false;
            try { listener?.Stop(); } catch { }
        }

        private void ListenLoop()
        {
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                ExplorerCore.Log($"MCP bridge listening on ws://127.0.0.1:{port}");

                while (running)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread clientThread = new(() => HandleClient(client))
                    {
                        IsBackground = true,
                        Name = "UnityExplorer MCP Client"
                    };
                    clientThread.Start();
                }
            }
            catch (SocketException ex)
            {
                if (running)
                    ExplorerCore.LogWarning($"MCP bridge socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                if (running)
                    ExplorerCore.LogWarning($"MCP bridge failed: {ex}");
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    if (!PerformHandshake(stream))
                        return;

                    while (running && client.Connected)
                    {
                        WebSocketFrame frame = ReadFrame(stream);
                        if (frame == null)
                            return;

                        if (frame.OpCode == 8)
                            return;

                        if (frame.OpCode == 9)
                        {
                            WriteFrame(stream, frame.Payload, 10);
                            continue;
                        }

                        if (frame.OpCode != 1)
                            continue;

                        string request = Encoding.UTF8.GetString(frame.Payload);
                        string response = messageHandler(request);
                        WriteFrame(stream, Encoding.UTF8.GetBytes(response), 1);
                    }
                }
                catch (IOException)
                {
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning($"MCP bridge client error: {ex.Message}");
                }
            }
        }

        private bool PerformHandshake(NetworkStream stream)
        {
            string headers = ReadHttpHeaders(stream);
            if (string.IsNullOrEmpty(headers))
                return false;

            string key = null;
            foreach (string line in headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = line.IndexOf(':');
                if (colon <= 0)
                    continue;

                string name = line.Substring(0, colon).Trim();
                if (string.Equals(name, "Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                {
                    key = line.Substring(colon + 1).Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(key))
                return false;

            string accept = CreateWebSocketAccept(key);
            string response =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                $"Sec-WebSocket-Accept: {accept}\r\n\r\n";

            byte[] bytes = Encoding.ASCII.GetBytes(response);
            stream.Write(bytes, 0, bytes.Length);
            return true;
        }

        private static string ReadHttpHeaders(NetworkStream stream)
        {
            List<byte> bytes = new();
            byte[] buffer = new byte[1];

            while (bytes.Count < 8192)
            {
                int read = stream.Read(buffer, 0, 1);
                if (read <= 0)
                    break;

                bytes.Add(buffer[0]);
                int count = bytes.Count;
                if (count >= 4
                    && bytes[count - 4] == '\r'
                    && bytes[count - 3] == '\n'
                    && bytes[count - 2] == '\r'
                    && bytes[count - 1] == '\n')
                    break;
            }

            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        private static string CreateWebSocketAccept(string key)
        {
            using SHA1 sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            return Convert.ToBase64String(hash);
        }

        private static WebSocketFrame ReadFrame(NetworkStream stream)
        {
            int first = stream.ReadByte();
            int second = stream.ReadByte();
            if (first < 0 || second < 0)
                return null;

            int opCode = first & 0x0F;
            bool masked = (second & 0x80) != 0;
            ulong length = (ulong)(second & 0x7F);

            if (length == 126)
            {
                byte[] extended = ReadExact(stream, 2);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(extended);
                length = BitConverter.ToUInt16(extended, 0);
            }
            else if (length == 127)
            {
                byte[] extended = ReadExact(stream, 8);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(extended);
                length = BitConverter.ToUInt64(extended, 0);
            }

            byte[] mask = masked ? ReadExact(stream, 4) : null;
            byte[] payload = ReadExact(stream, checked((int)length));

            if (masked)
            {
                for (int i = 0; i < payload.Length; i++)
                    payload[i] = (byte)(payload[i] ^ mask[i % 4]);
            }

            return new WebSocketFrame { OpCode = opCode, Payload = payload };
        }

        private void WriteFrame(NetworkStream stream, byte[] payload, int opCode)
        {
            lock (writeLock)
            {
                List<byte> frame = new();
                frame.Add((byte)(0x80 | opCode));

                if (payload.Length < 126)
                {
                    frame.Add((byte)payload.Length);
                }
                else if (payload.Length <= ushort.MaxValue)
                {
                    frame.Add(126);
                    byte[] length = BitConverter.GetBytes((ushort)payload.Length);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(length);
                    frame.AddRange(length);
                }
                else
                {
                    frame.Add(127);
                    byte[] length = BitConverter.GetBytes((ulong)payload.Length);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(length);
                    frame.AddRange(length);
                }

                frame.AddRange(payload);
                byte[] bytes = frame.ToArray();
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        private static byte[] ReadExact(NetworkStream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                    throw new IOException("Unexpected end of WebSocket stream.");
                offset += read;
            }
            return buffer;
        }

        private sealed class WebSocketFrame
        {
            public int OpCode;
            public byte[] Payload;
        }
    }
}
