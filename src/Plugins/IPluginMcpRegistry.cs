namespace UnityExplorer.Plugins
{
    public interface IPluginMcpRegistry
    {
        void RegisterAction(string action, Func<Dictionary<string, object>, object> handler);
        void RegisterTool(PluginMcpToolDescriptor descriptor);
        void RegisterResource(PluginMcpResourceDescriptor descriptor);
    }
}
