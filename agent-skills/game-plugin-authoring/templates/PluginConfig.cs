using UnityExplorer.Config;
using UnityExplorer.Plugins;

namespace {{PluginNamespace}}
{
    internal static class {{PluginClassName}}Config
    {
        public static ConfigElement<int> ResultLimit;

        public static void Register(IPluginConfigRegistry config, string pluginId)
        {
            ResultLimit = config.Create(pluginId + ".resultLimit", "Maximum results returned by {{PluginDisplayName}} tools.", 50, "Plugin:{{PluginDisplayName}}.MCP");
        }
    }
}
