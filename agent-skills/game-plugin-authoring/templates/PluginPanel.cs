using UnityExplorer.Plugins;

namespace {{PluginNamespace}}
{
    internal sealed class {{PluginClassName}}Panel : IPluginPanel
    {
        private IPluginPanelHost host;
        private GameObject content;

        public static PluginPanelDescriptor CreateDescriptor()
        {
            return new PluginPanelDescriptor("{{PluginId}}.panel", "{{PluginDisplayName}}", panelHost => new {{PluginClassName}}Panel(), 640, 320);
        }

        public void Construct(IPluginPanelHost host)
        {
            this.host = host;
            host.CreateScrollView(host.ContentRoot, "{{PluginClassName}}Scroll", out content, out _, new Color(0.12f, 0.12f, 0.12f, 1f));
            Refresh();
        }

        public void SetActive(bool active)
        {
            if (active)
                Refresh();
        }

        private void Refresh()
        {
            if (content == null)
                return;
            host.ClearChildren(content);
            host.CreateLabel(content, "Status", "{{PluginDisplayName}} plugin loaded.", TextAnchor.MiddleLeft);
        }
    }
}
