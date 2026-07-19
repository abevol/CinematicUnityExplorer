using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;

namespace UnityExplorer.Plugins
{
    public interface IPluginPanelHost
    {
        GameObject ContentRoot { get; }
        Text CreateStatus(string name, string text);
        ButtonRef CreateButton(GameObject parent, string name, string text);
        Text CreateLabel(GameObject parent, string name, string text, TextAnchor anchor);
        GameObject CreateHorizontalGroup(GameObject parent, string name, int spacing, TextAnchor alignment);
        GameObject CreateVerticalGroup(GameObject parent, string name, int spacing, TextAnchor alignment);
        void CreateScrollView(GameObject parent, string name, out GameObject content, out AutoSliderScrollbar scrollbar, Color background);
        void SetLayoutElement(GameObject target, int? minWidth = null, int? minHeight = null, int? flexibleWidth = null, int? flexibleHeight = null);
        void ClearChildren(GameObject target);
    }
}
