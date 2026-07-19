using UnityExplorer.Config;

namespace UnityExplorer.Plugins
{
    internal sealed class PluginContext : IUnityExplorerPluginContext, IPluginPanelRegistry, IPluginMcpRegistry, IPluginConfigRegistry, IPluginRuntime
    {
        private readonly string pluginFolder;

        public PluginContext(string pluginFolder)
        {
            this.pluginFolder = pluginFolder;
        }

        public IPluginPanelRegistry Panels => this;
        public IPluginMcpRegistry Mcp => this;
        public IPluginConfigRegistry Config => this;
        public IPluginRuntime Runtime => this;

        public string ExplorerFolder => ExplorerCore.ExplorerFolder;
        public string PluginFolder => pluginFolder;

        public void RegisterPanel(PluginPanelDescriptor descriptor)
            => PluginManager.RegisterPanel(descriptor);

        public void RegisterAction(string action, Func<Dictionary<string, object>, object> handler)
            => UnityExplorer.McpBridge.McpActionRegistry.RegisterPluginAction(action, handler);

        public void RegisterTool(PluginMcpToolDescriptor descriptor)
            => UnityExplorer.McpBridge.McpActionRegistry.RegisterPluginTool(descriptor);

        public void RegisterResource(PluginMcpResourceDescriptor descriptor)
            => UnityExplorer.McpBridge.McpActionRegistry.RegisterPluginResource(descriptor);

        public ConfigElement<T> Create<T>(string name, string description, T defaultValue, string category, bool requiresRestart = false, bool advanced = false)
            => ConfigManager.CreatePluginConfig(name, description, defaultValue, category, requiresRestart, advanced);

        public void Log(string message) => ExplorerCore.Log(message);
        public void LogWarning(string message) => ExplorerCore.LogWarning(message);
        public void LogError(string message) => ExplorerCore.LogError(message);
        public Assembly[] GetAssemblies() => AppDomain.CurrentDomain.GetAssemblies();
        public Type FindType(string fullName) => GetAssemblies().Select(assembly => assembly.GetType(fullName, false)).FirstOrDefault(type => type != null);
    }
}
