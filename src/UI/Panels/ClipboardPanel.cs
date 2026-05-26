using UnityExplorer.Localization;
using UnityExplorer.UI.Widgets;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    public class ClipboardPanel : UEPanel
    {
        private sealed class ClipboardEntry
        {
            public object Value;
            public string TypeName;
            public string Source;
            public DateTime CreatedAt;
            public bool IsPinned;
        }

        private const int MaxHistory = 30;
        private static readonly List<ClipboardEntry> History = new();

        public static object Current { get; private set; }

        public override string Name => Localizer.Get("PANEL_CLIPBOARD", "Clipboard");
        public override UIManager.Panels PanelType => UIManager.Panels.Clipboard;

        public override int MinWidth => 560;
        public override int MinHeight => 190;
        public override Vector2 DefaultAnchorMin => new(0.1f, 0.05f);
        public override Vector2 DefaultAnchorMax => new(0.45f, 0.22f);

        public override bool CanDragAndResize => true;
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;
        public override bool ShowByDefault => true;

        private static Text CurrentPasteLabel;
        private static Text StatusLabel;
        private static GameObject HistoryContent;

        public ClipboardPanel(UIBase owner) : base(owner)
        {
        }

        public static void Copy(object obj)
        {
            Current = obj;

            if (obj != null)
                AddHistory(obj, "Copy");

            Notification.ShowMessage(Localizer.Get("MSG_COPIED", "Copied!"));
            UpdateCurrentPasteInfo();
        }

        public static bool TryPaste(Type targetType, out object paste)
        {
            paste = Current;
            Type pasteType = Current?.GetActualType();

            if (Current != null && !targetType.IsAssignableFrom(pasteType))
            {
                Notification.ShowMessage(string.Format(Localizer.Get("MSG_CANNOT_ASSIGN", "Cannot assign '{0}' to '{1}'!"), pasteType.Name, targetType.Name));
                return false;
            }

            Notification.ShowMessage(Localizer.Get("MSG_PASTED", "Pasted!"));
            return true;
        }

        public static void ClearClipboard()
        {
            Current = null;
            UpdateCurrentPasteInfo();
        }

        private static void AddHistory(object obj, string source)
        {
            Type type = obj.GetActualType();
            History.RemoveAll(entry => ReferenceEquals(entry.Value, obj));
            History.Insert(0, new ClipboardEntry
            {
                Value = obj,
                TypeName = type?.FullName ?? "null",
                Source = source,
                CreatedAt = DateTime.Now
            });

            for (int i = History.Count - 1; i >= MaxHistory; i--)
            {
                if (!History[i].IsPinned)
                    History.RemoveAt(i);
            }

            RefreshHistory();
        }

        private static void UpdateCurrentPasteInfo()
        {
            if (CurrentPasteLabel)
                CurrentPasteLabel.text = ToStringUtility.ToStringWithType(Current, typeof(object), false);

            if (StatusLabel)
                StatusLabel.text = Current == null ? "Clipboard is empty." : $"Current type: {Current.GetActualType().FullName}";

            RefreshHistory();
        }

        private static void InspectClipboard()
        {
            if (Current.IsNullOrDestroyed())
            {
                Notification.ShowMessage(Localizer.Get("MSG_CANNOT_INSPECT_NULL", "Cannot inspect a null or destroyed object!"));
                return;
            }

            InspectorManager.Inspect(Current);
        }

        private static void RefreshHistory()
        {
            if (!HistoryContent)
                return;

            UEUI.ClearChildren(HistoryContent);

            if (History.Count == 0)
            {
                Text empty = UIFactory.CreateLabel(HistoryContent, "EmptyHistory", "No clipboard history yet.", TextAnchor.MiddleLeft, color: Color.grey);
                UIFactory.SetLayoutElement(empty.gameObject, minHeight: 24, flexibleWidth: 9999);
                return;
            }

            for (int i = 0; i < History.Count; i++)
                AddHistoryRow(i);
        }

        private static void AddHistoryRow(int index)
        {
            ClipboardEntry entry = History[index];
            GameObject row = UIFactory.CreateHorizontalGroup(HistoryContent, "HistoryRow", false, false, true, true, 5, new Vector4(3, 3, 3, 3), new Color(0.14f, 0.14f, 0.14f), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(row, minHeight: 34, flexibleHeight: 0, flexibleWidth: 9999);

            Text value = UIFactory.CreateLabel(row, "Value", BuildEntryText(entry), TextAnchor.MiddleLeft);
            value.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(value.gameObject, minHeight: 30, flexibleHeight: 200, flexibleWidth: 9999);

            ButtonRef use = UEUI.CreateActionButton(row, "Use", "Use", UEUI.Good, 55);
            use.OnClick += () =>
            {
                Current = entry.Value;
                UpdateCurrentPasteInfo();
                Notification.ShowMessage(Localizer.Get("MSG_PASTED", "Pasted!"));
            };

            ButtonRef inspect = UEUI.CreateActionButton(row, "Inspect", Localizer.Get("BTN_INSPECT", "Inspect"), null, 70);
            inspect.OnClick += () =>
            {
                if (!entry.Value.IsNullOrDestroyed())
                    InspectorManager.Inspect(entry.Value);
            };

            ButtonRef pin = UEUI.CreateActionButton(row, "Pin", entry.IsPinned ? "Unpin" : "Pin", entry.IsPinned ? UEUI.Warning : null, 62);
            pin.OnClick += () =>
            {
                entry.IsPinned = !entry.IsPinned;
                RefreshHistory();
            };
        }

        private static string BuildEntryText(ClipboardEntry entry)
        {
            string summary = ToStringUtility.ToStringWithType(entry.Value, entry.Value?.GetActualType(), false);
            return $"{summary}\n<color=grey><i>{entry.TypeName} | {entry.Source} | {entry.CreatedAt:HH:mm:ss}</i></color>";
        }

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, MinWidth);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, MinHeight);
        }

        protected override void ConstructPanelContent()
        {
            UIRoot.GetComponent<Image>().color = UEUI.PanelBg;

            GameObject firstRow = UEUI.CreateToolbar(ContentRoot, "ClipboardToolbar");

            Text currentPasteTitle = UIFactory.CreateLabel(firstRow, "CurrentPasteTitle", Localizer.Get("LBL_CURRENT_PASTE", "Current paste:"), TextAnchor.MiddleLeft, color: Color.grey);
            UIFactory.SetLayoutElement(currentPasteTitle.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);

            ButtonRef inspectButton = UEUI.CreateActionButton(firstRow, "InspectButton", Localizer.Get("BTN_INSPECT", "Inspect"), null, 80);
            inspectButton.OnClick += InspectClipboard;

            ButtonRef clearButton = UEUI.CreateActionButton(firstRow, "ClearPasteButton", Localizer.Get("BTN_CLEAR_CLIPBOARD", "Clear Clipboard"), UEUI.Warning, 125);
            clearButton.OnClick += ClearClipboard;

            GameObject currentPasteHolder = UIFactory.CreateHorizontalGroup(ContentRoot, "CurrentRow", false, false, true, true, 0, new Vector4(2, 2, 2, 2), childAlignment: TextAnchor.UpperCenter);
            UIFactory.SetLayoutElement(currentPasteHolder, minHeight: 28, flexibleHeight: 0, flexibleWidth: 9999);

            CurrentPasteLabel = UIFactory.CreateLabel(currentPasteHolder, "CurrentPasteInfo", Localizer.Get("LBL_NOT_SET", "not set"), TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(CurrentPasteLabel.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 999, flexibleHeight: 0);

            StatusLabel = UEUI.CreateStatus(ContentRoot, "ClipboardStatus");

            UIFactory.CreateScrollView(ContentRoot, "ClipboardHistory", out HistoryContent, out AutoSliderScrollbar _, new Color(0.1f, 0.1f, 0.1f));
            UpdateCurrentPasteInfo();
        }
    }
}
