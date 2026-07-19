namespace UnityExplorer.McpBridge
{
    internal static class McpBridgeService
    {
        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            Dictionary<string, Func<Dictionary<string, object>, object>> actions = McpActionRegistry.Actions;
            if (actions.TryGetValue(action, out Func<Dictionary<string, object>, object> handler))
                return handler(parameters);

            throw new McpBridgeException("invalid_request", $"Unknown MCP bridge action '{action}'.");
        }
    }
}
