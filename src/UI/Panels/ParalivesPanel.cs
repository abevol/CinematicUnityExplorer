#if MONO
using UnityExplorer.Config;
using UnityExplorer.Localization;
using UnityExplorer.McpBridge.Paralives;
using UnityExplorer.UI.Widgets;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    internal class ParalivesPanel : UEPanel
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

        public override string Name => Localizer.Get("PANEL_PARALIVES", "Paralives");
        public override UIManager.Panels PanelType => UIManager.Panels.Paralives;

        public override int MinWidth => 680;
        public override int MinHeight => 320;
        public override Vector2 DefaultAnchorMin => new(0.2f, 0.15f);
        public override Vector2 DefaultAnchorMax => new(0.8f, 0.85f);
        public override bool ShowByDefault => false;

        private GameObject scrollContent;
        private Text statusLabel;
        private Tab activeTab = Tab.State;
        private string pendingMainMenuAction;
        private string pendingSaveArgumentName;
        private string pendingSaveArgumentValue;

        public ParalivesPanel(UIBase owner) : base(owner)
        {
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            if (active && scrollContent)
                BuildActiveTab();
        }

        protected override void ConstructPanelContent()
        {
            GameObject tabRow = UIFactory.CreateHorizontalGroup(ContentRoot, "Tabs", false, false, true, true, 5, default, default, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(tabRow, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);

            CreateTabButton(tabRow, Tab.State, "State");
            CreateTabButton(tabRow, Tab.MainMenu, "Main Menu");
            CreateTabButton(tabRow, Tab.Saves, "Saves");
            CreateTabButton(tabRow, Tab.Settings, "Settings");

            statusLabel = UEUI.CreateStatus(ContentRoot, "Status", "");

            UIFactory.CreateScrollView(ContentRoot, "ParalivesScroll", out scrollContent, out AutoSliderScrollbar _, new Color(0.12f, 0.12f, 0.12f, 1f));
            BuildActiveTab();
        }

        private void CreateTabButton(GameObject parent, Tab tab, string label)
        {
            ButtonRef button = UIFactory.CreateButton(parent, $"Tab_{tab}", label);
            UIFactory.SetLayoutElement(button.Component.gameObject, minWidth: 105, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
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

            ButtonRef refresh = UIFactory.CreateButton(scrollContent, "RefreshState", "Refresh");
            UIFactory.SetLayoutElement(refresh.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
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

                GameObject row = UIFactory.CreateHorizontalGroup(scrollContent, $"Action_{actionName}", false, false, true, true, 5, default, default, TextAnchor.MiddleLeft);
                UIFactory.SetLayoutElement(row, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);

                ButtonRef button = UIFactory.CreateButton(row, $"Button_{actionName}", label);
                UIFactory.SetLayoutElement(button.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
                button.Component.interactable = available && interactable;
                button.OnClick += () => InvokeMainMenuAction(actionName, label);

                string status = available ? interactable ? "Ready" : "Not interactable" : "Unavailable";
                Text text = UIFactory.CreateLabel(row, $"Status_{actionName}", status, TextAnchor.MiddleLeft);
                UIFactory.SetLayoutElement(text.gameObject, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);
            }

            AddMessage(scrollContent, $"Confirmation: {ConfigManager.Paralives_SafeActionMode.Value}");
        }

        private void BuildSavesTab()
        {
            GameObject topRow = UIFactory.CreateHorizontalGroup(scrollContent, "SavesTopRow", false, false, true, true, 5, default, default, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(topRow, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);

            ButtonRef refresh = UIFactory.CreateButton(topRow, "RefreshSaves", "Refresh");
            UIFactory.SetLayoutElement(refresh.Component.gameObject, minWidth: 100, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            refresh.OnClick += BuildActiveTab;

            int limit = Clamp(ConfigManager.Paralives_SavedGameListLimit.Value, 1, 100);
            Text countLabel = UIFactory.CreateLabel(topRow, "SavesLimit", $"Limit: {limit}", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(countLabel.gameObject, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

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

            CreateEnumDropdown(scrollContent, "Safe action mode", ConfigManager.Paralives_SafeActionMode);
            CreateBoolToggle(scrollContent, "Prefer UI save/load flow", ConfigManager.Paralives_PreferUiFlowForSaveLoad);
            CreateIntInput(scrollContent, "Saved game list limit", ConfigManager.Paralives_SavedGameListLimit, 1, 100);
            CreateIntInput(scrollContent, "Loading wait timeout ms", ConfigManager.Paralives_LoadingWaitTimeoutMs, 1000, 300000);
            CreateBoolToggle(scrollContent, "MCP bridge enabled", ConfigManager.McpBridge_Enabled);
            CreateIntInput(scrollContent, "MCP bridge port", ConfigManager.McpBridge_Port, 1, 65535);
            CreateIntInput(scrollContent, "MCP request timeout ms", ConfigManager.McpBridge_RequestTimeoutMs, 1000, 300000);

            ButtonRef save = UIFactory.CreateButton(scrollContent, "SaveSettings", "Save Options");
            UIFactory.SetLayoutElement(save.Component.gameObject, minWidth: 130, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
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
            GameObject group = UIFactory.CreateVerticalGroup(scrollContent, $"Save_{source}_{scrollContent.transform.childCount}", true, false, true, true, 3, new Vector4(4, 4, 4, 4), new Color(0.16f, 0.16f, 0.16f, 1f), TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(group, minHeight: 72, flexibleHeight: 0, flexibleWidth: 9999);

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

            ButtonRef load = UIFactory.CreateButton(group, "Load", "Load");
            UIFactory.SetLayoutElement(load.Component.gameObject, minWidth: 90, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
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
            if (ConfigManager.Paralives_SafeActionMode.Value == ConfigManager.ParalivesSafeActionMode.OneClickInUI)
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
            UIFactory.SetLayoutElement(row, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);
            text.text = label;
            toggle.isOn = config.Value;
            toggle.onValueChanged.AddListener(value => config.Value = value);
        }

        private void CreateIntInput(GameObject parent, string label, ConfigElement<int> config, int min, int max)
        {
            GameObject row = UIFactory.CreateHorizontalGroup(parent, $"Input_{config.Name}", false, false, true, true, 5, default, default, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(row, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);
            Text text = UIFactory.CreateLabel(row, "Label", label, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(text.gameObject, minWidth: 190, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            InputFieldRef input = UIFactory.CreateInputField(row, "Value", config.Value.ToString());
            input.Text = config.Value.ToString();
            UIFactory.SetLayoutElement(input.UIRoot, minWidth: 100, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            ButtonRef apply = UIFactory.CreateButton(row, "Apply", "Apply");
            UIFactory.SetLayoutElement(apply.Component.gameObject, minWidth: 70, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
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

        private void CreateEnumDropdown(GameObject parent, string label, ConfigElement<ConfigManager.ParalivesSafeActionMode> config)
        {
            GameObject row = UIFactory.CreateHorizontalGroup(parent, "SafeModeRow", false, false, true, true, 5, default, default, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(row, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);
            Text text = UIFactory.CreateLabel(row, "Label", label, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(text.gameObject, minWidth: 190, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            UIFactory.CreateDropdown(row, "SafeMode", out Dropdown dropdown, config.Value.ToString(), 14, value =>
            {
                config.Value = value == 1
                    ? ConfigManager.ParalivesSafeActionMode.OneClickInUI
                    : ConfigManager.ParalivesSafeActionMode.ConfirmRequired;
                ClearPending();
            }, new[] { "ConfirmRequired", "OneClickInUI" });
            dropdown.value = config.Value == ConfigManager.ParalivesSafeActionMode.OneClickInUI ? 1 : 0;
            dropdown.RefreshShownValue();
            UIFactory.SetLayoutElement(dropdown.gameObject, minWidth: 180, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
        }

        private void AddInfoRow(GameObject parent, string label, string value)
        {
            GameObject row = UIFactory.CreateHorizontalGroup(parent, $"Info_{label}", false, false, true, true, 5, default, default, TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(row, minHeight: 24, flexibleHeight: 0, flexibleWidth: 9999);
            Text labelText = UIFactory.CreateLabel(row, "Label", $"<color=#9fb8d8>{label}</color>", TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(labelText.gameObject, minWidth: 150, minHeight: 22, flexibleWidth: 0, flexibleHeight: 0);
            Text valueText = UIFactory.CreateLabel(row, "Value", value ?? "", TextAnchor.UpperLeft);
            valueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(valueText.gameObject, minHeight: 22, flexibleHeight: 100, flexibleWidth: 9999);
        }

        private void AddMessage(GameObject parent, string message)
        {
            Text text = UIFactory.CreateLabel(parent, $"Message_{parent.transform.childCount}", message, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(text.gameObject, minHeight: 24, flexibleHeight: 0, flexibleWidth: 9999);
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

            for (int i = scrollContent.transform.childCount - 1; i >= 0; i--)
                GameObject.Destroy(scrollContent.transform.GetChild(i).gameObject);
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
#endif
