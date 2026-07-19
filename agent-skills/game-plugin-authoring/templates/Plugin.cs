using UnityExplorer.Plugins;

namespace {{PluginNamespace}}
{
    public sealed class {{PluginClassName}} : IUnityExplorerPlugin
    {
        public string Id => "{{PluginId}}";
        public string Name => "{{PluginDisplayName}}";
        public string Version => "1.0.0";

        public bool IsAvailable(IUnityExplorerPluginContext context)
            => context.Runtime.FindType("{{AvailabilityTypeFullName}}") != null;

        public void Initialize(IUnityExplorerPluginContext context)
        {
            {{PluginClassName}}Config.Register(context.Config, Id);
            {{PluginClassName}}McpRegistration.Register(context.Mcp);
            context.Panels.RegisterPanel({{PluginClassName}}Panel.CreateDescriptor());
        }

        public void Update() { }
        public void Shutdown() { }
    }
}
