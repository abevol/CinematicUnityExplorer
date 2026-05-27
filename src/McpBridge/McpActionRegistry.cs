namespace UnityExplorer.McpBridge
{
    internal static class McpActionRegistry
    {
        private static readonly Dictionary<string, Func<Dictionary<string, object>, object>> actions = BuildActions();

        public static Dictionary<string, Func<Dictionary<string, object>, object>> Actions => actions;

        private static Dictionary<string, Func<Dictionary<string, object>, object>> BuildActions()
        {
            Dictionary<string, Func<Dictionary<string, object>, object>> registry = new();
            Register(registry, UnityObjectService.Actions);
            Register(registry, UnityComponentService.Actions);
            Register(registry, UnityRuntimeService.Actions);
#if MONO
            Register(registry, Paralives.ParalivesStateService.Actions);
            Register(registry, Paralives.ParalivesMenuService.Actions);
            Register(registry, Paralives.ParalivesSaveService.Actions);
            Register(registry, Paralives.ParalivesContentModService.Actions);
            Register(registry, Paralives.ParalivesCollectionService.Actions);
            Register(registry, Paralives.ParalivesNeedService.Actions);
            Register(registry, Paralives.ParalivesCheatService.Actions);
            Register(registry, Paralives.ParalivesRuntimeService.Actions);
            Register(registry, Paralives.ParalivesLogService.Actions);
            Register(registry, Paralives.ParalivesPerformanceCountersService.Actions);
#endif
            return registry;
        }

        private static void Register(
            Dictionary<string, Func<Dictionary<string, object>, object>> registry,
            Dictionary<string, Func<Dictionary<string, object>, object>> serviceActions)
        {
            foreach (KeyValuePair<string, Func<Dictionary<string, object>, object>> action in serviceActions)
            {
                if (registry.ContainsKey(action.Key))
                    throw new McpBridgeException("invalid_request", $"Duplicate MCP action registration for '{action.Key}'.");

                registry[action.Key] = action.Value;
            }
        }
    }
}
