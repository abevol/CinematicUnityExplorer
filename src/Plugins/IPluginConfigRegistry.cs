using UnityExplorer.Config;

namespace UnityExplorer.Plugins
{
    public interface IPluginConfigRegistry
    {
        ConfigElement<T> Create<T>(string name, string description, T defaultValue, string category, bool requiresRestart = false, bool advanced = false);
    }
}
