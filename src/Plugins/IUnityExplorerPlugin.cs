namespace UnityExplorer.Plugins
{
    public interface IUnityExplorerPlugin
    {
        string Id { get; }
        string Name { get; }
        string Version { get; }

        bool IsAvailable(IUnityExplorerPluginContext context);
        void Initialize(IUnityExplorerPluginContext context);
        void Update();
        void Shutdown();
    }
}
