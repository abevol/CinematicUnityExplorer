using UnityExplorer.Config;
using UnityExplorer.Plugins;

namespace CinematicUnityExplorer.Plugins.Paralives
{
    internal static class ParalivesPluginConfig
    {
        public enum SafeActionMode
        {
            ConfirmRequired,
            OneClickInUI
        }

        public static ConfigElement<SafeActionMode> SafeActionModeSetting;
        public static ConfigElement<int> SavedGameListLimit;
        public static ConfigElement<int> LoadingWaitTimeoutMs;
        public static ConfigElement<bool> PreferUiFlowForSaveLoad;

        public static void Register(IPluginConfigRegistry config, string pluginId)
        {
            SafeActionModeSetting = config.Create(pluginId + ".safeActionMode", "Controls whether Paralives UI actions require a second click confirmation.", SafeActionMode.ConfirmRequired, "Plugin:Paralives.Safety");
            SavedGameListLimit = config.Create(pluginId + ".savedGameListLimit", "Maximum saved games to display in the Paralives panel.", 50, "Plugin:Paralives.UI");
            LoadingWaitTimeoutMs = config.Create(pluginId + ".loadingWaitTimeoutMs", "Maximum time to wait for Paralives loading actions before treating them as timed out.", 30000, "Plugin:Paralives.MCP", advanced: true);
            PreferUiFlowForSaveLoad = config.Create(pluginId + ".preferUiFlowForSaveLoad", "Prefer visible Paralives UI flows for save loading when available.", true, "Plugin:Paralives.Safety");
        }
    }
}
