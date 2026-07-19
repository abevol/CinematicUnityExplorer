using UnityExplorer.Plugins;

namespace {{PluginNamespace}}
{
    internal static class {{PluginClassName}}McpRegistration
    {
        private const string EmptySchema = "{\"type\":\"object\",\"properties\":{}}";

        public static void Register(IPluginMcpRegistry registry)
        {
            registry.RegisterAction("{{action_name}}", {{PluginClassName}}Service.Handle{{ActionPascalName}});
            registry.RegisterTool(new PluginMcpToolDescriptor("{{PluginDisplayName}}:{{tool_name}}", "{{action_name}}", "{{tool_description}}", EmptySchema, "diagnostics/read-only", "read-only"));
        }
    }
}
