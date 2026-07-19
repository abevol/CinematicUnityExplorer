namespace UnityExplorer.Plugins
{
    internal static class PluginManager
    {
        private sealed class LoadedPlugin
        {
            public IUnityExplorerPlugin Plugin;
            public PluginStatusEntry Status;
        }

        private static readonly List<LoadedPlugin> loadedPlugins = new();
        private static readonly List<PluginStatusEntry> statuses = new();
        private static readonly List<PluginPanelDescriptor> panels = new();

        public static List<PluginPanelDescriptor> RegisteredPanels => panels;

        public static void LoadPlugins()
        {
            string root = ExplorerCore.ExplorerFolder;
            string pluginFolder = Path.Combine(root, "plugins");
            List<string> candidates = new();

            if (Directory.Exists(root))
                candidates.AddRange(Directory.GetFiles(root, "CinematicUnityExplorer.*Plugin.dll"));
            if (Directory.Exists(pluginFolder))
                candidates.AddRange(Directory.GetFiles(pluginFolder, "CinematicUnityExplorer.*Plugin.dll"));

            foreach (string path in candidates.Distinct().OrderBy(it => it))
                LoadPluginAssembly(path);
        }

        public static void RegisterPanel(PluginPanelDescriptor descriptor)
        {
            if (panels.Any(panel => panel.Id == descriptor.Id))
                throw new InvalidOperationException("Duplicate plugin panel id '" + descriptor.Id + "'.");
            panels.Add(descriptor);
        }

        public static List<object> GetStatusSnapshot()
        {
            return statuses.Select(status => status.ToDictionary()).Cast<object>().ToList();
        }

        public static void UpdatePlugins()
        {
            foreach (LoadedPlugin entry in loadedPlugins)
            {
                try
                {
                    entry.Plugin.Update();
                }
                catch (Exception ex)
                {
                    entry.Status.State = "error";
                    entry.Status.Error = ex.GetInnerMostException().Message;
                    ExplorerCore.LogWarning("Plugin update failed for " + entry.Status.Id + ": " + ex);
                }
            }
        }

        public static void ShutdownPlugins()
        {
            for (int i = loadedPlugins.Count - 1; i >= 0; i--)
            {
                LoadedPlugin entry = loadedPlugins[i];
                try
                {
                    entry.Plugin.Shutdown();
                    entry.Status.State = "shutdown";
                }
                catch (Exception ex)
                {
                    entry.Status.State = "error";
                    entry.Status.Error = ex.GetInnerMostException().Message;
                    ExplorerCore.LogWarning("Plugin shutdown failed for " + entry.Status.Id + ": " + ex);
                }
            }
        }

        private static void LoadPluginAssembly(string path)
        {
            PluginStatusEntry status = new() { AssemblyPath = Path.GetFileName(path), State = "discovered" };
            statuses.Add(status);

            try
            {
                Assembly assembly = Assembly.LoadFrom(path);
                foreach (Type type in assembly.GetTypes().Where(IsPluginType))
                    LoadPluginType(type, path, status);
            }
            catch (Exception ex)
            {
                status.State = "error";
                status.Error = ex.GetInnerMostException().Message;
                ExplorerCore.LogWarning("Plugin assembly load failed for " + path + ": " + ex);
            }
        }

        private static void LoadPluginType(Type type, string path, PluginStatusEntry status)
        {
            IUnityExplorerPlugin plugin = null;
            try
            {
                plugin = (IUnityExplorerPlugin)Activator.CreateInstance(type);
                status.Id = plugin.Id;
                status.Name = plugin.Name;
                status.Version = plugin.Version;

                PluginContext context = new(Path.GetDirectoryName(path));
                status.Available = plugin.IsAvailable(context);
                if (!status.Available)
                {
                    status.State = "unavailable";
                    return;
                }

                plugin.Initialize(context);
                status.State = "loaded";
                loadedPlugins.Add(new LoadedPlugin { Plugin = plugin, Status = status });
            }
            catch (Exception ex)
            {
                status.State = "error";
                status.Error = ex.GetInnerMostException().Message;
                ExplorerCore.LogWarning("Plugin initialization failed for " + (plugin?.Id ?? type.FullName) + ": " + ex);
            }
        }

        private static bool IsPluginType(Type type)
        {
            return typeof(IUnityExplorerPlugin).IsAssignableFrom(type)
                && type.IsClass
                && !type.IsAbstract
                && type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}
