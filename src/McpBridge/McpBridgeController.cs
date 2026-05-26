using System.Threading;

namespace UnityExplorer.McpBridge
{
    internal static class McpBridgeController
    {
        private sealed class PendingRequest
        {
            public string Payload;
            public string Response;
            public ManualResetEvent Complete = new(false);
        }

        private static readonly Queue<PendingRequest> pendingRequests = new();
        private static McpWebSocketServer server;

        public static void Init()
        {
            if (!Config.ConfigManager.McpBridge_Enabled.Value)
                return;

            server = new McpWebSocketServer(Config.ConfigManager.McpBridge_Port.Value);
            server.Start(SubmitFromTransport);
        }

        public static void Shutdown()
        {
            server?.Stop();
            server = null;

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

            try
            {
                if (!(McpJson.Parse(payload) is Dictionary<string, object> request))
                    throw new McpBridgeException("invalid_request", "Request must be a JSON object.");

                request.TryGetValue("id", out id);
                string action = GetRequiredString(request, "action");
                Dictionary<string, object> parameters = GetParameters(request);
                object result = McpBridgeService.Handle(action, parameters);

                return McpJson.Stringify(new Dictionary<string, object>
                {
                    ["id"] = id,
                    ["ok"] = true,
                    ["result"] = result
                });
            }
            catch (McpBridgeException ex)
            {
                return BuildErrorResponse(id, ex.Code, ex.Message);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"MCP bridge request failed: {ex}");
                return BuildErrorResponse(id, "execution_failed", ex.GetInnerMostException().Message);
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
