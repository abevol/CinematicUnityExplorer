namespace UnityExplorer.Plugins
{
    public interface IUnityExplorerPluginContext
    {
        IPluginPanelRegistry Panels { get; }
        IPluginMcpRegistry Mcp { get; }
        IPluginConfigRegistry Config { get; }
        IPluginRuntime Runtime { get; }
    }
}
