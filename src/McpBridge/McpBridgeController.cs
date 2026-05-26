using System.Threading;

namespace UnityExplorer.McpBridge
{
    internal static class McpBridgeController
    {
        internal sealed class RequestLogEntry
        {
            public DateTime Time;
            public string Action;
            public bool Ok;
            public string Error;
            public long DurationMs;
        }

        private sealed class PendingRequest
        {
            public string Payload;
            public string Response;
            public ManualResetEvent Complete = new(false);
        }

        private static readonly Queue<PendingRequest> pendingRequests = new();
        private static readonly List<RequestLogEntry> requestLog = new();
        private static McpWebSocketServer server;
        private static bool listening;
        private static string lastError;
        private static string lastAction;
        private static DateTime? lastRequestTime;
        private static long lastDurationMs;

        public static void Init()
        {
            if (!Config.ConfigManager.McpBridge_Enabled.Value)
                return;

            server = new McpWebSocketServer(Config.ConfigManager.McpBridge_Port.Value);
            server.Start(SubmitFromTransport);
            listening = true;
            lastError = null;
        }

        public static void Shutdown()
        {
            server?.Stop();
            server = null;
            listening = false;

            lock (pendingRequests)
            {
                while (pendingRequests.Count > 0)
                {
                    PendingRequest request = pendingRequests.Dequeue();
                    request.Response = BuildErrorResponse(null, "execution_failed", "UnityExplorer MCP bridge is shutting down.");
                    request.Complete.Set();
                }
            }
        }

        public static Dictionary<string, object> GetStatusSnapshot()
        {
            int pending;
            lock (pendingRequests)
                pending = pendingRequests.Count;

            lock (requestLog)
            {
                return new Dictionary<string, object>
                {
                    ["enabled"] = Config.ConfigManager.McpBridge_Enabled.Value,
                    ["listening"] = listening,
                    ["port"] = Config.ConfigManager.McpBridge_Port.Value,
                    ["pendingRequests"] = pending,
                    ["lastAction"] = lastAction,
                    ["lastError"] = lastError,
                    ["lastRequestTime"] = lastRequestTime?.ToString("HH:mm:ss") ?? "",
                    ["lastDurationMs"] = lastDurationMs,
                    ["requests"] = requestLog
                        .Take(25)
                        .Select(entry => new Dictionary<string, object>
                        {
                            ["time"] = entry.Time.ToString("HH:mm:ss"),
                            ["action"] = entry.Action,
                            ["ok"] = entry.Ok,
                            ["error"] = entry.Error,
                            ["durationMs"] = entry.DurationMs
                        })
                        .Cast<object>()
                        .ToList()
                };
            }
        }

        public static void Update()
        {
            while (true)
            {
                PendingRequest request = null;
                lock (pendingRequests)
                {
                    if (pendingRequests.Count == 0)
                        return;
                    request = pendingRequests.Dequeue();
                }

                request.Response = HandlePayload(request.Payload);
                request.Complete.Set();
            }
        }

        private static string SubmitFromTransport(string payload)
        {
            PendingRequest request = new() { Payload = payload };
            lock (pendingRequests)
            {
                pendingRequests.Enqueue(request);
            }

            int timeoutMs = Math.Max(1000, Config.ConfigManager.McpBridge_RequestTimeoutMs.Value);
            if (!request.Complete.WaitOne(timeoutMs))
                return BuildErrorResponse(null, "timeout", $"Unity MCP bridge request timed out after {timeoutMs}ms.");

            return request.Response;
        }

        private static string HandlePayload(string payload)
        {
            object id = null;
            string action = "";
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (!(McpJson.Parse(payload) is Dictionary<string, object> request))
                    throw new McpBridgeException("invalid_request", "Request must be a JSON object.");

                request.TryGetValue("id", out id);
                action = GetRequiredString(request, "action");
                Dictionary<string, object> parameters = GetParameters(request);
                object result = McpBridgeService.Handle(action, parameters);
                sw.Stop();
                RecordRequest(action, true, null, sw.ElapsedMilliseconds);

                return McpJson.Stringify(new Dictionary<string, object>
                {
                    ["id"] = id,
                    ["ok"] = true,
                    ["result"] = result
                });
            }
            catch (McpBridgeException ex)
            {
                sw.Stop();
                RecordRequest(action, false, ex.Code, sw.ElapsedMilliseconds);
                return BuildErrorResponse(id, ex.Code, ex.Message);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"MCP bridge request failed: {ex}");
                sw.Stop();
                RecordRequest(action, false, "execution_failed", sw.ElapsedMilliseconds);
                return BuildErrorResponse(id, "execution_failed", ex.GetInnerMostException().Message);
            }
        }

        private static void RecordRequest(string action, bool ok, string error, long durationMs)
        {
            lastAction = string.IsNullOrEmpty(action) ? "<invalid>" : action;
            lastError = ok ? null : error;
            lastRequestTime = DateTime.Now;
            lastDurationMs = durationMs;

            lock (requestLog)
            {
                requestLog.Insert(0, new RequestLogEntry
                {
                    Time = DateTime.Now,
                    Action = lastAction,
                    Ok = ok,
                    Error = error,
                    DurationMs = durationMs
                });

                if (requestLog.Count > 50)
                    requestLog.RemoveRange(50, requestLog.Count - 50);
            }
        }

        private static string BuildErrorResponse(object id, string code, string message)
        {
            return McpJson.Stringify(new Dictionary<string, object>
            {
                ["id"] = id,
                ["ok"] = false,
                ["error"] = new Dictionary<string, object>
                {
                    ["code"] = code,
                    ["message"] = message
                }
            });
        }

        private static string GetRequiredString(Dictionary<string, object> request, string name)
        {
            if (!request.TryGetValue(name, out object value) || value == null)
                throw new McpBridgeException("invalid_request", $"'{name}' is required.");
            return value.ToString();
        }

        private static Dictionary<string, object> GetParameters(Dictionary<string, object> request)
        {
            if (!request.TryGetValue("params", out object value) || value == null)
                return new Dictionary<string, object>();

            if (value is Dictionary<string, object> parameters)
                return parameters;

            throw new McpBridgeException("invalid_request", "'params' must be a JSON object.");
        }
    }
}
