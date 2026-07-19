using UnityExplorer.Plugins;
using UnityExplorer.UI.Widgets;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    internal sealed class PluginPanelAdapter : UEPanel, IPluginPanelHost
    {
        private readonly PluginPanelDescriptor descriptor;
        private IPluginPanel pluginPanel;

        public PluginPanelAdapter(UIBase owner, PluginPanelDescriptor descriptor) : base(owner)
        {
            this.descriptor = descriptor;
        }

        public override string Name => descriptor.Title;
        public override UIManager.Panels PanelType => UIManager.Panels.Plugin;
        public override string PanelSaveKey => "plugin:" + descriptor.Id;
        public override string NavButtonId => "Plugin_" + descriptor.Id.Replace('.', '_').Replace(':', '_');
        public override int MinWidth => descriptor.MinWidth;
        public override int MinHeight => descriptor.MinHeight;
        public override bool ShowByDefault => descriptor.ShowByDefault;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);

        protected override void ConstructPanelContent()
        {
            pluginPanel = descriptor.Create(this);
            pluginPanel.Construct(this);
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            pluginPanel?.SetActive(active);
        }

        protected override void OnNavButtonClicked()
        {
            SetActive(!Enabled);
        }

        public Text CreateStatus(string name, string text) => UEUI.CreateStatus(ContentRoot, name, text);
        public ButtonRef CreateButton(GameObject parent, string name, string text) => UIFactory.CreateButton(parent, name, text);
        public Text CreateLabel(GameObject parent, string name, string text, TextAnchor anchor) => UIFactory.CreateLabel(parent, name, text, anchor);

        public GameObject CreateHorizontalGroup(GameObject parent, string name, int spacing, TextAnchor alignment)
            => UIFactory.CreateHorizontalGroup(parent, name, false, false, true, true, spacing, default, default, alignment);

        public GameObject CreateVerticalGroup(GameObject parent, string name, int spacing, TextAnchor alignment)
            => UIFactory.CreateVerticalGroup(parent, name, true, false, true, true, spacing, default, default, alignment);

        public void CreateScrollView(GameObject parent, string name, out GameObject content, out AutoSliderScrollbar scrollbar, Color background)
            => UIFactory.CreateScrollView(parent, name, out content, out scrollbar, background);

        public void SetLayoutElement(GameObject target, int? minWidth = null, int? minHeight = null, int? flexibleWidth = null, int? flexibleHeight = null)
            => UIFactory.SetLayoutElement(target, minWidth: minWidth, minHeight: minHeight, flexibleWidth: flexibleWidth, flexibleHeight: flexibleHeight);

        public void ClearChildren(GameObject target) => UEUI.ClearChildren(target);
    }
}
