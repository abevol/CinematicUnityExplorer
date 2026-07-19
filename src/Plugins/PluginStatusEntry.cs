namespace UnityExplorer.Plugins
{
    internal sealed class PluginStatusEntry
    {
        public string Id;
        public string Name;
        public string Version;
        public string AssemblyPath;
        public string State;
        public bool Available;
        public string Error;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["id"] = Id,
                ["name"] = Name,
                ["version"] = Version,
                ["assembly"] = AssemblyPath,
                ["state"] = State,
                ["available"] = Available,
                ["error"] = Error
            };
        }
    }
}
