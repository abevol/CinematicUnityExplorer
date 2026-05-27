namespace UnityExplorer.McpBridge
{
    internal static class McpBridgeService
    {
        public static object Handle(string action, Dictionary<string, object> parameters)
        {
#if !MONO
            if (action.StartsWith("paralives_", StringComparison.Ordinal) || IsParalivesLogAction(action))
                throw new McpBridgeException("not_available", "Paralives MCP actions are only available in Mono builds.");
#endif

            Dictionary<string, Func<Dictionary<string, object>, object>> actions = McpActionRegistry.Actions;
            if (actions.TryGetValue(action, out Func<Dictionary<string, object>, object> handler))
                return handler(parameters);

            throw new McpBridgeException("invalid_request", $"Unknown MCP bridge action '{action}'.");
        }

        private static bool IsParalivesLogAction(string action)
        {
            return action == "get_game_logs"
                || action == "subscribe_logs"
                || action == "poll_logs";
        }
    }
}
