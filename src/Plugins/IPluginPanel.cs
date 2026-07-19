namespace UnityExplorer.Plugins
{
    public interface IPluginPanel
    {
        void Construct(IPluginPanelHost host);
        void SetActive(bool active);
    }
}
