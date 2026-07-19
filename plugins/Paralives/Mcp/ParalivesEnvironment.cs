#if MONO
namespace CinematicUnityExplorer.Plugins.Paralives.Mcp
{
    internal static class ParalivesEnvironment
    {
        private static bool initialized;
        private static string managedPath;
        private static string rootPath;
        private static string mainModPath;
        private static string paralivesAssemblyPath;
        private static ParalivesTypeIndex typeIndex;

        public static string RootPath
        {
            get
            {
                EnsureInitialized();
                return rootPath;
            }
        }

        public static string MainModPath
        {
            get
            {
                EnsureInitialized();
                return mainModPath;
            }
        }

        public static ParalivesTypeIndex TypeIndex
        {
            get
            {
                EnsureInitialized();
                return typeIndex;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                EnsureInitialized();
                return File.Exists(paralivesAssemblyPath);
            }
        }

        public static void EnsureAvailable()
        {
            EnsureInitialized();
            if (!IsAvailable)
                throw new McpBridgeException("not_available", "Paralives.dll was not found; ParalivesBridge is disabled.");
        }

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            managedPath = Path.Combine(Application.dataPath, "Managed");
            rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            mainModPath = Path.Combine(rootPath, "Main.mod");
            paralivesAssemblyPath = Path.Combine(managedPath, "Paralives.dll");

            try
            {
                typeIndex = ParalivesTypeIndex.Build(paralivesAssemblyPath);
                if (File.Exists(paralivesAssemblyPath))
                    ExplorerCore.Log($"ParalivesBridge indexed {typeIndex.Managers.Count} managers, {typeIndex.Settings.Count} settings, {typeIndex.Cheats.Count} cheat types.");
            }
            catch (Exception ex)
            {
                typeIndex = new ParalivesTypeIndex();
                ExplorerCore.LogWarning($"ParalivesBridge failed to index Paralives.dll: {ex}");
            }
        }
    }
}
#endif
