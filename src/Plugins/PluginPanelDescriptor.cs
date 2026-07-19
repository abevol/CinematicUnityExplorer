namespace UnityExplorer.Plugins
{
    public sealed class PluginPanelDescriptor
    {
        public PluginPanelDescriptor(string id, string title, Func<IPluginPanelHost, IPluginPanel> create, int minWidth, int minHeight, bool showByDefault = false)
        {
            Id = id;
            Title = title;
            Create = create;
            MinWidth = minWidth;
            MinHeight = minHeight;
            ShowByDefault = showByDefault;
        }

        public string Id { get; }
        public string Title { get; }
        public Func<IPluginPanelHost, IPluginPanel> Create { get; }
        public int MinWidth { get; }
        public int MinHeight { get; }
        public bool ShowByDefault { get; }
    }
}
