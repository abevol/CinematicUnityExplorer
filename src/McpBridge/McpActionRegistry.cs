using UnityExplorer.Plugins;

namespace UnityExplorer.McpBridge
{
    internal static class McpActionRegistry
    {
        private static readonly Dictionary<string, Func<Dictionary<string, object>, object>> actions = BuildActions();
        private static readonly List<PluginMcpToolDescriptor> pluginTools = new();
        private static readonly List<PluginMcpResourceDescriptor> pluginResources = new();

        public static Dictionary<string, Func<Dictionary<string, object>, object>> Actions => actions;
        public static List<PluginMcpToolDescriptor> PluginTools => pluginTools;
        public static List<PluginMcpResourceDescriptor> PluginResources => pluginResources;

        public static void RegisterPluginAction(string action, Func<Dictionary<string, object>, object> handler)
        {
            if (actions.ContainsKey(action))
                throw new McpBridgeException("invalid_request", $"Duplicate MCP action registration for '{action}'.");
            actions[action] = handler;
        }

        public static void RegisterPluginTool(PluginMcpToolDescriptor descriptor)
        {
            if (pluginTools.Any(tool => tool.Name == descriptor.Name))
                throw new McpBridgeException("invalid_request", $"Duplicate MCP tool registration for '{descriptor.Name}'.");
            pluginTools.Add(descriptor);
        }

        public static void RegisterPluginResource(PluginMcpResourceDescriptor descriptor)
        {
            if (pluginResources.Any(resource => resource.Uri == descriptor.Uri))
                throw new McpBridgeException("invalid_request", $"Duplicate MCP resource registration for '{descriptor.Uri}'.");
            pluginResources.Add(descriptor);
        }

        private static Dictionary<string, Func<Dictionary<string, object>, object>> BuildActions()
        {
            Dictionary<string, Func<Dictionary<string, object>, object>> registry = new();
            Register(registry, UnityObjectService.Actions);
            Register(registry, UnityComponentService.Actions);
            Register(registry, UnityRuntimeService.Actions);
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
