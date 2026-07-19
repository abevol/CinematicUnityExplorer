using UnityEngine.SceneManagement;
using UnityExplorer.Config;
using UnityExplorer.Plugins;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.McpBridge
{
    internal static class UnityRuntimeService
    {
        private const int DefaultLimit = 50;
        private const int MaxLimit = 200;

        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["get_runtime_status"] = GetRuntimeStatus,
            ["get_recent_logs"] = GetRecentLogs,
            ["list_config"] = ListConfig,
            ["get_mcp_status"] = GetMcpStatus,
            ["get_plugin_status"] = GetPluginStatus,
            ["get_mcp_tool_definitions"] = GetMcpToolDefinitions
        };

        private static object GetRuntimeStatus(Dictionary<string, object> parameters)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            List<object> panels = new();

            foreach (KeyValuePair<UIManager.Panels, UEPanel> entry in UIManager.UIPanels)
            {
                UEPanel panel = entry.Value;
                panels.Add(new Dictionary<string, object>
                {
                    ["id"] = entry.Key.ToString(),
                    ["name"] = panel.Name,
                    ["active"] = panel.Enabled,
                    ["showByDefault"] = panel.ShowByDefault,
                    ["minWidth"] = panel.MinWidth,
                    ["minHeight"] = panel.MinHeight
                });
            }

            foreach (UEPanel panel in UIManager.PluginPanels)
            {
                panels.Add(new Dictionary<string, object>
                {
                    ["id"] = panel.PanelSaveKey,
                    ["name"] = panel.Name,
                    ["active"] = panel.Enabled,
                    ["showByDefault"] = panel.ShowByDefault,
                    ["minWidth"] = panel.MinWidth,
                    ["minHeight"] = panel.MinHeight,
                    ["plugin"] = true
                });
            }

            return new Dictionary<string, object>
            {
                ["name"] = ExplorerCore.NAME,
                ["version"] = ExplorerCore.VERSION,
                ["author"] = ExplorerCore.AUTHOR,
                ["guid"] = ExplorerCore.GUID,
                ["universeContext"] = Universe.Context.ToString(),
                ["unityVersion"] = Application.unityVersion,
                ["platform"] = Application.platform.ToString(),
                ["isEditor"] = Application.isEditor,
                ["isPlaying"] = Application.isPlaying,
                ["timeScale"] = Time.timeScale,
                ["realtimeSinceStartup"] = Time.realtimeSinceStartup,
                ["menuVisible"] = UIManager.ShowMenu,
                ["uiInitialized"] = !UIManager.Initializing,
                ["activeScene"] = new Dictionary<string, object>
                {
                    ["name"] = activeScene.IsValid() ? activeScene.name : "",
                    ["path"] = activeScene.IsValid() ? activeScene.path : "",
                    ["buildIndex"] = activeScene.IsValid() ? activeScene.buildIndex : -1,
                    ["isLoaded"] = activeScene.IsValid() && activeScene.isLoaded
                },
                ["panels"] = panels,
                ["mcp"] = McpBridgeController.GetStatusSnapshot()
            };
        }

        private static object GetRecentLogs(Dictionary<string, object> parameters)
        {
            int limit = McpParameters.Clamp(McpParameters.OptionalInt(parameters, "limit", DefaultLimit), 1, MaxLimit);
            return LogPanel.GetLogSnapshot(limit);
        }

        private static object ListConfig(Dictionary<string, object> parameters)
        {
            string category = McpParameters.OptionalString(parameters, "category");
            bool includeAdvanced = McpParameters.OptionalBool(parameters, "includeAdvanced", true);
            int limit = McpParameters.Clamp(McpParameters.OptionalInt(parameters, "limit", MaxLimit), 1, MaxLimit);

            List<object> entries = new();
            foreach (IConfigElement element in ConfigManager.ConfigElements.Values
                .OrderBy(it => it.Category)
                .ThenBy(it => it.Name))
            {
                if (!string.IsNullOrEmpty(category) && !string.Equals(element.Category, category, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!includeAdvanced && element.Advanced)
                    continue;

                entries.Add(new Dictionary<string, object>
                {
                    ["name"] = element.Name,
                    ["description"] = element.Description,
                    ["category"] = element.Category,
                    ["type"] = element.ElementType.FullName,
                    ["value"] = FormatConfigValue(element.BoxedValue),
                    ["defaultValue"] = FormatConfigValue(element.DefaultValue),
                    ["requiresRestart"] = element.RequiresRestart,
                    ["advanced"] = element.Advanced
                });

                if (entries.Count >= limit)
                    break;
            }

            return new Dictionary<string, object>
            {
                ["entries"] = entries,
                ["limit"] = limit,
                ["truncated"] = entries.Count >= limit,
                ["category"] = category,
                ["includeAdvanced"] = includeAdvanced
            };
        }

        private static object GetMcpStatus(Dictionary<string, object> parameters)
        {
            return McpBridgeController.GetStatusSnapshot();
        }

        private static object GetPluginStatus(Dictionary<string, object> parameters)
        {
            return new Dictionary<string, object>
            {
                ["plugins"] = PluginManager.GetStatusSnapshot()
            };
        }

        private static object GetMcpToolDefinitions(Dictionary<string, object> parameters)
        {
            List<object> tools = McpActionRegistry.PluginTools.Select(tool => new Dictionary<string, object>
            {
                ["name"] = tool.Name,
                ["action"] = tool.Action,
                ["description"] = tool.Description,
                ["inputSchema"] = McpJson.Parse(tool.InputSchemaJson),
                ["group"] = tool.Group,
                ["risk"] = tool.Risk
            }).Cast<object>().ToList();

            List<object> resources = McpActionRegistry.PluginResources.Select(resource => new Dictionary<string, object>
            {
                ["uri"] = resource.Uri,
                ["name"] = resource.Name,
                ["description"] = resource.Description,
                ["mimeType"] = resource.MimeType,
                ["action"] = resource.Action,
                ["params"] = resource.Parameters
            }).Cast<object>().ToList();

            return new Dictionary<string, object>
            {
                ["tools"] = tools,
                ["resources"] = resources
            };
        }

        private static string FormatConfigValue(object value)
        {
            return value == null ? null : value.ToString();
        }
    }
}
