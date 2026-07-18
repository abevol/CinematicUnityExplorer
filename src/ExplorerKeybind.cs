using UnityExplorer.UI;
using UnityExplorer.UI.Widgets;
using UniverseLib.Input;

namespace UnityExplorer;

public static class ExplorerKeybind
{
    public static void Update()
    {
        UpdateTimeScaleKeybinds();
    }

    private static void UpdateTimeScaleKeybinds()
    {
        TimeScaleWidget widget = TimeScaleWidget.Instance;
        if (widget == null)
            return;

if (IInputManager.GetKeyDown(Config.ConfigManager.Pause.Value))
            widget.PauseToggle();
    }
}
