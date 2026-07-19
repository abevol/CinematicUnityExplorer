namespace UnityExplorer.Plugins
{
    public sealed class PluginMcpToolDescriptor
    {
        public PluginMcpToolDescriptor(string name, string action, string description, string inputSchemaJson, string group, string risk)
        {
            Name = name;
            Action = action;
            Description = description;
            InputSchemaJson = inputSchemaJson;
            Group = group;
            Risk = risk;
        }

        public string Name { get; }
        public string Action { get; }
        public string Description { get; }
        public string InputSchemaJson { get; }
        public string Group { get; }
        public string Risk { get; }
    }
}
