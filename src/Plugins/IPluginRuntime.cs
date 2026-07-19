namespace UnityExplorer.Plugins
{
    public interface IPluginRuntime
    {
        string ExplorerFolder { get; }
        string PluginFolder { get; }
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
        Assembly[] GetAssemblies();
        Type FindType(string fullName);
    }
}
