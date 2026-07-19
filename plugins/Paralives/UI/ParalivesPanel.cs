using System.Collections.Generic;
using CinematicUnityExplorer.Plugins.Paralives.Mcp;
using UnityExplorer.Config;
using UnityExplorer.Plugins;
using UnityExplorer.McpBridge;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace CinematicUnityExplorer.Plugins.Paralives.UI
{
    internal class ParalivesPanel : IPluginPanel
    {
        private enum Tab
        {
            State,
            MainMenu,
            Saves,
            Settings
        }

        private sealed class MainMenuActionSpec
        {
            public MainMenuActionSpec(string action, string label)
            {
                Action = action;
                Label = label;
            }

            public string Action { get; }
            public string Label { get; }
        }

        private static readonly MainMenuActionSpec[] MainMenuActions =
        {
            new("continue_game", "Continue"),
            new("new_game", "New"),
            new("load_game_menu", "Load"),
            new("mod_editor", "Mod Editor"),
            new("options", "Options")
        };

        private IPluginPanelHost host;
        private GameObject scrollContent;
        private Text statusLabel;
        private Tab activeTab = Tab.State;
        private string pendingMainMenuAction;
        private string pendingSaveArgumentName;
        private string pendingSaveArgumentValue;

        public static PluginPanelDescriptor CreateDescriptor()
        {
            return new PluginPanelDescriptor("cinematic-unity-explorer.paralives.panel", "Paralives", host => new ParalivesPanel(), 680, 320);
        }

        public void Construct(IPluginPanelHost panelHost)
        {
            host = panelHost;

            GameObject tabRow = host.CreateHorizontalGroup(host.ContentRoot, "Tabs", 5, TextAnchor.MiddleLeft);
            host.SetLayoutElement(tabRow, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);

            CreateTabButton(tabRow, Tab.State, "State");
            CreateTabButton(tabRow, Tab.MainMenu, "Main Menu");
            CreateTabButton(tabRow, Tab.Saves, "Saves");
            CreateTabButton(tabRow, Tab.Settings, "Settings");

            statusLabel = host.CreateStatus("Status", "");

            host.CreateScrollView(host.ContentRoot, "ParalivesScroll", out scrollContent, out AutoSliderScrollbar _, new Color(0.12f, 0.12f, 0.12f, 1f));
            BuildActiveTab();
        }

        public void SetActive(bool active)
        {
            if (active && scrollContent)
                BuildActiveTab();
        }

        private void CreateTabButton(GameObject parent, Tab tab, string label)
        {
            ButtonRef button = host.CreateButton(parent, $"Tab_{tab}", label);
            host.SetLayoutElement(button.Component.gameObject, minWidth: 105, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            button.OnClick += () =>
            {
                activeTab = tab;
                ClearPending();
                BuildActiveTab();
            };
        }

        private void BuildActiveTab()
        {
            ClearScrollContent();
            SetStatus("");

            try
            {
                if (!ParalivesControlService.IsAvailable)
                {
                    AddMessage(scrollContent, "Paralives.dll was not found. This panel is only available in Paralives.");
                    return;
                }

                switch (activeTab)
                {
                    case Tab.State:
                        BuildStateTab();
                        break;
                    case Tab.MainMenu:
                        BuildMainMenuTab();
                        break;
                    case Tab.Saves:
                        BuildSavesTab();
                        break;
                    case Tab.Settings:
                        BuildSettingsTab();
                        break;
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
            }
        }

        private void BuildStateTab()
        {
            Dictionary<string, object> state = ParalivesControlService.GetGameState();
            Dictionary<string, object> loading = GetDict(state, "loading");

            AddInfoRow(scrollContent, "Mode", GetText(state, "mode"));
            AddInfoRow(scrollContent, "Active scene", GetText(loading, "activeScene"));
            AddInfoRow(scrollContent, "Main menu visible", GetText(state, "isMainMenu"));
            AddInfoRow(scrollContent, "Loading inferred", GetText(loading, "isLoadingInferred"));
            AddInfoRow(scrollContent, "SavedGameManager", GetAvailability(GetDict(state, "savedGameManager")));
            AddInfoRow(scrollContent, "GameLoadingManager", GetAvailability(GetDict(state, "gameLoadingManager")));

            ButtonRef refresh = host.CreateButton(scrollContent, "RefreshState", "Refresh");
            host.SetLayoutElement(refresh.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            refresh.OnClick += BuildActiveTab;
        }

        private void BuildMainMenuTab()
        {
            Dictionary<string, object> data = ParalivesControlService.ListMainMenuActions();
            Dictionary<string, Dictionary<string, object>> byAction = new();
            foreach (object item in GetList(data, "actions"))
            {
                if (item is Dictionary<string, object> action)
                    byAction[GetText(action, "action")] = action;
            }

            foreach (MainMenuActionSpec spec in MainMenuActions)
            {
                string actionName = spec.Action;
                string label = spec.Label;
                Dictionary<string, object> action = byAction.TryGetValue(actionName, out Dictionary<string, object> found) ? found : null;
                bool available = GetBool(action, "available");
                bool interactable = GetBool(action, "interactable");

                GameObject row = host.CreateHorizontalGroup(scrollContent, $"Action_{actionName}", 5, TextAnchor.MiddleLeft);
                host.SetLayoutElement(row, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);

                ButtonRef button = host.CreateButton(row, $"Button_{actionName}", label);
                host.SetLayoutElement(button.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
                button.Component.interactable = available && interactable;
                button.OnClick += () => InvokeMainMenuAction(actionName, label);

                string status = available ? interactable ? "Ready" : "Not interactable" : "Unavailable";
                Text text = host.CreateLabel(row, $"Status_{actionName}", status, TextAnchor.MiddleLeft);
                host.SetLayoutElement(text.gameObject, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);
            }

            AddMessage(scrollContent, $"Confirmation: {ParalivesPluginConfig.SafeActionModeSetting.Value}");
        }

        private void BuildSavesTab()
        {
            GameObject topRow = host.CreateHorizontalGroup(scrollContent, "SavesTopRow", 5, TextAnchor.MiddleLeft);
            host.SetLayoutElement(topRow, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);

            ButtonRef refresh = host.CreateButton(topRow, "RefreshSaves", "Refresh");
            host.SetLayoutElement(refresh.Component.gameObject, minWidth: 100, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            refresh.OnClick += BuildActiveTab;

            int limit = Clamp(ParalivesPluginConfig.SavedGameListLimit.Value, 1, 100);
            Text countLabel = host.CreateLabel(topRow, "SavesLimit", $"Limit: {limit}", TextAnchor.MiddleLeft);
            host.SetLayoutElement(countLabel.gameObject, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            Dictionary<string, object> data = ParalivesControlService.ListSavedGames(limit);
            List<object> managerItems = GetList(data, "managerItems");
            List<object> files = GetList(data, "files");

            AddMessage(scrollContent, $"Persistent data: {GetText(data, "persistentDataPath")}");
            AddMessage(scrollContent, $"Manager items: {managerItems.Count}, files: {files.Count}, truncated: {GetText(data, "truncated")}");

            if (managerItems.Count == 0 && files.Count == 0)
            {
                AddMessage(scrollContent, "No saved games were found from the manager or known save directories.");
                return;
            }

            foreach (object item in managerItems)
            {
                if (item is Dictionary<string, object> save)
                    AddSaveEntry("Manager", save, ResolveSaveId(save), null);
            }

            foreach (object item in files)
            {
                if (item is Dictionary<string, object> file)
                    AddSaveEntry("File", file, null, GetText(file, "path"));
            }
        }

        private void BuildSettingsTab()
        {
            AddMessage(scrollContent, "Paralives settings are also available in Options under the Paralives and MCP categories.");

            CreateEnumDropdown(scrollContent, "Safe action mode", ParalivesPluginConfig.SafeActionModeSetting);
            CreateBoolToggle(scrollContent, "Prefer UI save/load flow", ParalivesPluginConfig.PreferUiFlowForSaveLoad);
            CreateIntInput(scrollContent, "Saved game list limit", ParalivesPluginConfig.SavedGameListLimit, 1, 100);
            CreateIntInput(scrollContent, "Loading wait timeout ms", ParalivesPluginConfig.LoadingWaitTimeoutMs, 1000, 300000);

            ButtonRef save = host.CreateButton(scrollContent, "SaveSettings", "Save Options");
            host.SetLayoutElement(save.Component.gameObject, minWidth: 130, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            save.OnClick += () =>
            {
                ConfigManager.Handler.SaveConfig();
                SetStatus("Options saved.");
            };
        }

        private void InvokeMainMenuAction(string actionName, string label)
        {
            bool confirmed = ShouldExecuteNow("main menu action", actionName, label);
            if (!confirmed)
                return;

            try
            {
                Dictionary<string, object> result = ParalivesControlService.InvokeMainMenuAction(actionName, true);
                SetStatus(GetBool(result, "invoked") ? $"{label} invoked." : $"{label} prepared.");
                ClearPending();
            }
            catch (Exception ex)
            {
                SetStatus($"Action failed: {ex.Message}");
            }
        }

        private void AddSaveEntry(string source, Dictionary<string, object> save, string saveId, string savePath)
        {
            GameObject group = host.CreateVerticalGroup(scrollContent, $"Save_{source}_{scrollContent.transform.childCount}", 3, TextAnchor.UpperLeft);
            host.SetLayoutElement(group, minHeight: 72, flexibleHeight: 0, flexibleWidth: 9999);

            AddInfoRow(group, "Source", source);
            AddInfoRow(group, "Name", GetFirstText(save, "name", "Name", "display"));
            AddInfoRow(group, "Modified", GetText(save, "lastWriteTimeUtc"));
            AddInfoRow(group, "Path", savePath ?? "");

            string argumentName = !string.IsNullOrEmpty(savePath) ? "savePath" : !string.IsNullOrEmpty(saveId) ? "saveId" : null;
            string argumentValue = savePath ?? saveId;
            if (string.IsNullOrEmpty(argumentName))
            {
                AddMessage(group, "No loadable save id or path was detected for this manager item.");
                return;
            }

            ButtonRef load = host.CreateButton(group, "Load", "Load");
            host.SetLayoutElement(load.Component.gameObject, minWidth: 90, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            load.OnClick += () => LoadSave(argumentName, argumentValue);
        }

        private void LoadSave(string argumentName, string argumentValue)
        {
            bool confirmed = ShouldExecuteNow("save load", $"{argumentName}:{argumentValue}", "Load save");
            if (!confirmed)
                return;

            try
            {
                Dictionary<string, object> result = ParalivesControlService.LoadSavedGame(argumentName, argumentValue, true);
                SetStatus(GetBool(result, "invoked") ? "Save load invoked." : "Save load prepared.");
                ClearPending();
            }
            catch (Exception ex)
            {
                SetStatus($"Load failed: {ex.Message}");
            }
        }

        private bool ShouldExecuteNow(string operation, string key, string label)
        {
            if (ParalivesPluginConfig.SafeActionModeSetting.Value == ParalivesPluginConfig.SafeActionMode.OneClickInUI)
                return true;

            if (operation == "main menu action")
            {
                if (pendingMainMenuAction == key)
                    return true;

                try
                {
                    ParalivesControlService.InvokeMainMenuAction(key, false);
                }
                catch (Exception ex)
                {
                    SetStatus($"Dry run failed: {ex.Message}");
                    return false;
                }

                pendingMainMenuAction = key;
                pendingSaveArgumentName = null;
                pendingSaveArgumentValue = null;
            }
            else
            {
                if (pendingSaveArgumentName == key.Split(':')[0] && pendingSaveArgumentValue == key.Substring(key.IndexOf(':') + 1))
                    return true;

                int separator = key.IndexOf(':');
                string argumentName = key.Substring(0, separator);
                string argumentValue = key.Substring(separator + 1);
                try
                {
                    ParalivesControlService.LoadSavedGame(argumentName, argumentValue, false);
                }
                catch (Exception ex)
                {
                    SetStatus($"Dry run failed: {ex.Message}");
                    return false;
                }

                pendingMainMenuAction = null;
                pendingSaveArgumentName = argumentName;
                pendingSaveArgumentValue = argumentValue;
            }

            SetStatus($"{label}: click again to confirm. MCP confirmation phrase remains {ParalivesControlService.RequiredConfirmPhrase}.");
            return false;
        }

        private void CreateBoolToggle(GameObject parent, string label, ConfigElement<bool> config)
        {
            GameObject row = UIFactory.CreateToggle(parent, $"Toggle_{config.Name}", out Toggle toggle, out Text text);
            host.SetLayoutElement(row, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);
            text.text = label;
            toggle.isOn = config.Value;
            toggle.onValueChanged.AddListener(value => config.Value = value);
        }

        private void CreateIntInput(GameObject parent, string label, ConfigElement<int> config, int min, int max)
        {
            GameObject row = host.CreateHorizontalGroup(parent, $"Input_{config.Name}", 5, TextAnchor.MiddleLeft);
            host.SetLayoutElement(row, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);
            Text text = host.CreateLabel(row, "Label", label, TextAnchor.MiddleLeft);
            host.SetLayoutElement(text.gameObject, minWidth: 190, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            InputFieldRef input = UIFactory.CreateInputField(row, "Value", config.Value.ToString());
            input.Text = config.Value.ToString();
            host.SetLayoutElement(input.UIRoot, minWidth: 100, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            ButtonRef apply = host.CreateButton(row, "Apply", "Apply");
            host.SetLayoutElement(apply.Component.gameObject, minWidth: 70, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            apply.OnClick += () =>
            {
                if (int.TryParse(input.Text, out int value))
                {
                    config.Value = Clamp(value, min, max);
                    input.Text = config.Value.ToString();
                    SetStatus($"{label} updated.");
                }
                else
                {
                    SetStatus($"{label} must be an integer.");
                }
            };
        }

        private void CreateEnumDropdown(GameObject parent, string label, ConfigElement<ParalivesPluginConfig.SafeActionMode> config)
        {
            GameObject row = host.CreateHorizontalGroup(parent, "SafeModeRow", 5, TextAnchor.MiddleLeft);
            host.SetLayoutElement(row, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);
            Text text = host.CreateLabel(row, "Label", label, TextAnchor.MiddleLeft);
            host.SetLayoutElement(text.gameObject, minWidth: 190, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            UIFactory.CreateDropdown(row, "SafeMode", out Dropdown dropdown, config.Value.ToString(), 14, value =>
            {
                config.Value = value == 1
                    ? ParalivesPluginConfig.SafeActionMode.OneClickInUI
                    : ParalivesPluginConfig.SafeActionMode.ConfirmRequired;
                ClearPending();
            }, new[] { "ConfirmRequired", "OneClickInUI" });
            dropdown.value = config.Value == ParalivesPluginConfig.SafeActionMode.OneClickInUI ? 1 : 0;
            dropdown.RefreshShownValue();
            host.SetLayoutElement(dropdown.gameObject, minWidth: 180, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
        }

        private void AddInfoRow(GameObject parent, string label, string value)
        {
            GameObject row = host.CreateHorizontalGroup(parent, $"Info_{label}", 5, TextAnchor.UpperLeft);
            host.SetLayoutElement(row, minHeight: 24, flexibleHeight: 0, flexibleWidth: 9999);
            Text labelText = host.CreateLabel(row, "Label", $"<color=#9fb8d8>{label}</color>", TextAnchor.UpperLeft);
            host.SetLayoutElement(labelText.gameObject, minWidth: 150, minHeight: 22, flexibleWidth: 0, flexibleHeight: 0);
            Text valueText = host.CreateLabel(row, "Value", value ?? "", TextAnchor.UpperLeft);
            valueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            host.SetLayoutElement(valueText.gameObject, minHeight: 22, flexibleHeight: 100, flexibleWidth: 9999);
        }

        private void AddMessage(GameObject parent, string message)
        {
            Text text = host.CreateLabel(parent, $"Message_{parent.transform.childCount}", message, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            host.SetLayoutElement(text.gameObject, minHeight: 24, flexibleHeight: 0, flexibleWidth: 9999);
        }

        private void SetStatus(string text)
        {
            if (statusLabel)
                statusLabel.text = text ?? "";
        }

        private void ClearPending()
        {
            pendingMainMenuAction = null;
            pendingSaveArgumentName = null;
            pendingSaveArgumentValue = null;
        }

        private void ClearScrollContent()
        {
            if (!scrollContent)
                return;

            host.ClearChildren(scrollContent);
        }

        private static Dictionary<string, object> GetDict(Dictionary<string, object> source, string key)
        {
            return source != null && source.TryGetValue(key, out object value) ? value as Dictionary<string, object> : null;
        }

        private static List<object> GetList(Dictionary<string, object> source, string key)
        {
            return source != null && source.TryGetValue(key, out object value) && value is List<object> list ? list : new List<object>();
        }

        private static string GetText(Dictionary<string, object> source, string key)
        {
            return source != null && source.TryGetValue(key, out object value) && value != null ? value.ToString() : "";
        }

        private static string GetFirstText(Dictionary<string, object> source, params string[] keys)
        {
            foreach (string key in keys)
            {
                string text = GetText(source, key);
                if (!string.IsNullOrEmpty(text))
                    return text;
            }

            return "";
        }

        private static bool GetBool(Dictionary<string, object> source, string key)
        {
            return source != null && source.TryGetValue(key, out object value) && value is bool boolValue && boolValue;
        }

        private static string GetAvailability(Dictionary<string, object> manager)
        {
            if (manager == null)
                return "Unknown";
            return GetBool(manager, "available") ? $"Available ({GetText(manager, "type")})" : $"Unavailable ({GetText(manager, "type")})";
        }

        private static string ResolveSaveId(Dictionary<string, object> save)
        {
            return GetFirstText(save, "GUID", "guid", "Id", "ID", "id", "SaveId", "saveId");
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
