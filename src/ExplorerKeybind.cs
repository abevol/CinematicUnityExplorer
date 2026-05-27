using UnityExplorer.Config;
using UnityExplorer.UI;
using UnityExplorer.UI.Widgets;
using UniverseLib.Input;

namespace UnityExplorer;

public static class ExplorerKeybind
{
    public static void Update()
    {
        if (InputManager.GetKeyDown(ConfigManager.Master_Toggle.Value))
            UIManager.ShowMenu = !UIManager.ShowMenu;

        UpdateTimeScaleKeybinds();
    }

    private static void UpdateTimeScaleKeybinds()
    {
        TimeScaleWidget widget = TimeScaleWidget.Instance;
        if (widget == null)
            return;

        if (InputManager.GetKeyDown(ConfigManager.TimeScale_Toggle_Keybind.Value))
            widget.ToggleLock();

        if (InputManager.GetKeyDown(ConfigManager.TimeScale_Zero_Keybind.Value))
            widget.LockTo(0f);

        if (InputManager.GetKeyDown(ConfigManager.TimeScale_Normal_Keybind.Value))
            widget.LockTo(1f);

        if (InputManager.GetKeyDown(ConfigManager.TimeScale_Half_Keybind.Value))
            widget.LockTo(widget.DesiredTime * 0.5f);

        if (InputManager.GetKeyDown(ConfigManager.TimeScale_Double_Keybind.Value))
            widget.LockTo(widget.DesiredTime * 2f);
    }
}
