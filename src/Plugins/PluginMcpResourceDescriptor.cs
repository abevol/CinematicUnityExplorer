namespace UnityExplorer.Plugins
{
    public sealed class PluginMcpResourceDescriptor
    {
        public PluginMcpResourceDescriptor(string uri, string name, string description, string mimeType, string action, Dictionary<string, object> parameters)
        {
            Uri = uri;
            Name = name;
            Description = description;
            MimeType = mimeType;
            Action = action;
            Parameters = parameters ?? new Dictionary<string, object>();
        }

        public string Uri { get; }
        public string Name { get; }
        public string Description { get; }
        public string MimeType { get; }
        public string Action { get; }
        public Dictionary<string, object> Parameters { get; }
    }
}
