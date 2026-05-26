using UnityExplorer.Config;
using UnityExplorer.McpBridge;
using UnityExplorer.Localization;
using UnityExplorer.UI.Widgets;
using UniverseLib.UI;
using UniverseLib.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    internal class McpPanel : UEPanel
    {
        public override string Name => Localizer.Get("PANEL_MCP", "MCP");
        public override UIManager.Panels PanelType => UIManager.Panels.MCP;

        public override int MinWidth => 520;
        public override int MinHeight => 260;
        public override Vector2 DefaultAnchorMin => new(0.55f, 0.15f);
        public override Vector2 DefaultAnchorMax => new(0.9f, 0.55f);
        public override bool ShowByDefault => false;

        private GameObject content;
        private Text statusLabel;

        public McpPanel(UIBase owner) : base(owner) { }

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            if (active && content)
                Refresh();
        }

        protected override void ConstructPanelContent()
        {
            GameObject toolbar = UEUI.CreateToolbar(ContentRoot, "Toolbar");
            UEUI.CreateActionButton(toolbar, "Refresh", Localizer.Get("BTN_UPDATE", "Update"), UEUI.Good, 95).OnClick += Refresh;
            UEUI.CreateActionButton(toolbar, "Options", Localizer.Get("PANEL_OPTIONS", "Options"), null, 95).OnClick += () => UIManager.SetPanelActive(UIManager.Panels.Options, true);

            statusLabel = UEUI.CreateStatus(ContentRoot, "Status", "");
            UIFactory.CreateScrollView(ContentRoot, "Requests", out content, out AutoSliderScrollbar _, new Color(0.1f, 0.1f, 0.1f));
            Refresh();
        }

        private void Refresh()
        {
            if (!content)
                return;

            UEUI.ClearChildren(content);
            Dictionary<string, object> status = McpBridgeController.GetStatusSnapshot();

            bool enabled = GetBool(status, "enabled");
            bool listening = GetBool(status, "listening");
            statusLabel.text = enabled
                ? listening ? $"Listening on ws://127.0.0.1:{GetText(status, "port")}" : "Enabled, not listening. Restart may be required."
                : "MCP bridge disabled.";

            GameObject summary = UEUI.CreateSection(content, "Summary", "Bridge");
            UEUI.AddInfoRow(summary, "Enabled", enabled.ToString());
            UEUI.AddInfoRow(summary, "Listening", listening.ToString());
            UEUI.AddInfoRow(summary, "Port", GetText(status, "port"));
            UEUI.AddInfoRow(summary, "Timeout", $"{ConfigManager.McpBridge_RequestTimeoutMs.Value} ms");
            UEUI.AddInfoRow(summary, "Last action", GetText(status, "lastAction"));
            UEUI.AddInfoRow(summary, "Last error", GetText(status, "lastError"));
            UEUI.AddInfoRow(summary, "Last duration", $"{GetText(status, "lastDurationMs")} ms");

            GameObject tools = UEUI.CreateSection(content, "Tools", "Tool Groups");
            UEUI.AddInfoRow(tools, "UnityExplorer", "find_game_objects, get_object_detail, set_component_property, call_component_method, get_scene_hierarchy, get_object_components");
            UEUI.AddInfoRow(tools, "Paralives", "Available in Paralives only; write tools keep dry-run/confirm policy.");

            GameObject requests = UEUI.CreateSection(content, "RequestLog", "Recent Requests");
            List<object> logs = GetList(status, "requests");
            if (logs.Count == 0)
            {
                Text empty = UIFactory.CreateLabel(requests, "Empty", "No MCP requests recorded yet.", TextAnchor.MiddleLeft, color: Color.grey);
                UIFactory.SetLayoutElement(empty.gameObject, minHeight: 24, flexibleWidth: 9999);
                return;
            }

            foreach (object item in logs)
            {
                if (item is Dictionary<string, object> entry)
                {
                    string state = GetBool(entry, "ok") ? "<color=#8fd18f>ok</color>" : "<color=#e08a80>error</color>";
                    Text row = UIFactory.CreateLabel(requests, "Request", $"{GetText(entry, "time")}  {state}  {GetText(entry, "durationMs")}ms  {GetText(entry, "action")} {GetText(entry, "error")}", TextAnchor.MiddleLeft);
                    row.horizontalOverflow = HorizontalWrapMode.Wrap;
                    UIFactory.SetLayoutElement(row.gameObject, minHeight: 24, flexibleHeight: 50, flexibleWidth: 9999);
                }
            }
        }

        private static string GetText(Dictionary<string, object> source, string key)
        {
            return source != null && source.TryGetValue(key, out object value) && value != null ? value.ToString() : "";
        }

        private static bool GetBool(Dictionary<string, object> source, string key)
        {
            return source != null && source.TryGetValue(key, out object value) && value is bool boolValue && boolValue;
        }

        private static List<object> GetList(Dictionary<string, object> source, string key)
        {
            return source != null && source.TryGetValue(key, out object value) && value is List<object> list ? list : new List<object>();
        }
    }
}
