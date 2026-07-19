namespace CinematicUnityExplorer.Plugins.Paralives
{
    internal static class ParalivesSchemas
    {
        internal const string InvokeMainMenuAction = "{\"type\":\"object\",\"properties\":{\"action\":{\"type\":\"string\",\"description\":\"Main menu action to invoke\"},\"dryRun\":{\"type\":\"boolean\",\"description\":\"Simulate without executing\",\"default\":true},\"confirm\":{\"type\":\"string\"}},\"required\":[\"action\"]}";
        internal const string ListSavedGames = "{\"type\":\"object\",\"properties\":{\"limit\":{\"type\":\"integer\",\"description\":\"Maximum saved games to list\",\"default\":50}},\"required\":[]}";
        internal const string LoadSavedGame = "{\"type\":\"object\",\"properties\":{\"savePath\":{\"type\":\"string\",\"description\":\"Path to the save file\"},\"saveName\":{\"type\":\"string\",\"description\":\"Name of the save\"},\"saveId\":{\"type\":\"string\",\"description\":\"ID of the save\"},\"dryRun\":{\"type\":\"boolean\",\"default\":true},\"confirm\":{\"type\":\"string\"}},\"required\":[]}";
        internal const string DryRunConfirm = "{\"type\":\"object\",\"properties\":{\"dryRun\":{\"type\":\"boolean\",\"default\":true},\"confirm\":{\"type\":\"string\"}},\"required\":[]}";
        internal const string ModPath = "{\"type\":\"object\",\"properties\":{\"modPath\":{\"type\":\"string\",\"description\":\"Path to the content mod folder\"}},\"required\":[\"modPath\"]}";
        internal const string CreateContentMod = "{\"type\":\"object\",\"properties\":{\"modName\":{\"type\":\"string\",\"description\":\"Name for the new content mod\"},\"dryRun\":{\"type\":\"boolean\",\"default\":true},\"confirm\":{\"type\":\"string\"}},\"required\":[\"modName\"]}";
        internal const string ImportAsset = "{\"type\":\"object\",\"properties\":{\"sourcePath\":{\"type\":\"string\",\"description\":\"Source file path\"},\"modPath\":{\"type\":\"string\",\"description\":\"Target mod path\"},\"subFolder\":{\"type\":\"string\",\"description\":\"Subfolder within mod\"},\"dryRun\":{\"type\":\"boolean\",\"default\":true},\"confirm\":{\"type\":\"string\"}},\"required\":[\"sourcePath\",\"modPath\"]}";
        internal const string SetNeedValue = "{\"type\":\"object\",\"properties\":{\"characterGuid\":{\"type\":\"string\",\"description\":\"Character GUID\"},\"needGuid\":{\"type\":\"string\",\"description\":\"Need GUID\"},\"value\":{\"type\":\"string\",\"description\":\"Value to set\"},\"dryRun\":{\"type\":\"boolean\",\"default\":true},\"confirm\":{\"type\":\"string\"}},\"required\":[\"characterGuid\",\"needGuid\",\"value\"]}";
        internal const string RunCheat = "{\"type\":\"object\",\"properties\":{\"command\":{\"type\":\"string\",\"description\":\"Cheat command to run\"},\"dryRun\":{\"type\":\"boolean\",\"default\":true},\"confirm\":{\"type\":\"string\"}},\"required\":[\"command\"]}";
        internal const string CharacterGuid = "{\"type\":\"object\",\"properties\":{\"characterGuid\":{\"type\":\"string\",\"description\":\"Character GUID\"}},\"required\":[]}";
        internal const string PerformanceHistory = "{\"type\":\"object\",\"properties\":{\"limit\":{\"type\":\"integer\",\"description\":\"Number of FPS history samples\",\"default\":50}},\"required\":[]}";
        internal const string SceneStats = "{\"type\":\"object\",\"properties\":{\"forceRefresh\":{\"type\":\"boolean\",\"description\":\"Force a scene-wide scan\",\"default\":false}},\"required\":[]}";
        internal const string ListProfilerCounters = "{\"type\":\"object\",\"properties\":{\"query\":{\"type\":\"string\",\"description\":\"Name filter\"},\"category\":{\"type\":\"string\",\"description\":\"Category filter\"},\"limit\":{\"type\":\"integer\",\"default\":100}},\"required\":[]}";
        internal const string ProfilerCounterSamples = "{\"type\":\"object\",\"properties\":{\"counters\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"description\":\"Profiler counter names\"}},\"required\":[\"counters\"]}";
    }
}
