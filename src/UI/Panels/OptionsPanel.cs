using UnityExplorer.CacheObject;
using UnityExplorer.CacheObject.Views;
using UnityExplorer.Config;
using UnityExplorer.Localization;
using UnityExplorer.UI.Widgets;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.UI.Panels
{
    public class OptionsPanel : UEPanel, ICacheObjectController, ICellPoolDataSource<ConfigEntryCell>
    {
        private static readonly string[] CategoryOptions =
        {
            "All",
            "General",
            "UI",
            "MCP",
            "Paralives",
            "Console",
            "Inspector",
            "Export",
            "Advanced"
        };

        public override string Name => Localizer.Get("PANEL_OPTIONS", "Options");
        public override UIManager.Panels PanelType => UIManager.Panels.Options;

        public override int MinWidth => 680;
        public override int MinHeight => 240;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.1f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.85f);

        public override bool ShouldSaveActiveState => false;
        public override bool ShowByDefault => false;

        private readonly List<CacheConfigEntry> allConfigEntries = new();
        private readonly List<CacheConfigEntry> configEntries = new();

        private ScrollPool<ConfigEntryCell> scrollPool;
        private InputFieldRef searchInput;
        private Dropdown categoryDropdown;
        private Text resultLabel;
        private Text statusLabel;

        public CacheObjectBase ParentCacheObject => null;
        public object Target => null;
        public Type TargetType => null;
        public bool CanWrite => true;

        public int ItemCount => configEntries.Count;

        public OptionsPanel(UIBase owner) : base(owner)
        {
            foreach (KeyValuePair<string, IConfigElement> entry in ConfigManager.ConfigElements
                .OrderBy(entry => GetCategoryOrder(entry.Value.Category))
                .ThenBy(entry => entry.Value.Name))
            {
                CacheConfigEntry cache = new(entry.Value)
                {
                    Owner = this
                };
                allConfigEntries.Add(cache);
            }

            RefreshFilteredEntries(false);

            foreach (CacheConfigEntry config in allConfigEntries)
                config.UpdateValueFromSource();
        }

        public void OnCellBorrowed(ConfigEntryCell cell)
        {
        }

        public void SetCell(ConfigEntryCell cell, int index)
        {
            CacheObjectControllerHelper.SetCell(cell, index, configEntries, null);
        }

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 680f);
        }

        protected override void ConstructPanelContent()
        {
            GameObject actionRow = UIFactory.CreateHorizontalGroup(ContentRoot, "ActionRow", false, false, true, true, 5, default, default, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(actionRow, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);

            ButtonRef saveBtn = UIFactory.CreateButton(actionRow, "Save", Localizer.Get("BTN_SAVE_OPTIONS", "Save Options"), new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(saveBtn.Component.gameObject, minWidth: 130, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            saveBtn.OnClick += () =>
            {
                ConfigManager.Handler.SaveConfig();
                SetStatus(Localizer.Get("STATUS_OPTIONS_SAVED", "Options saved."));
            };

            ButtonRef resetBtn = UIFactory.CreateButton(actionRow, "ResetDefaults", Localizer.Get("BTN_RESET_DEFAULTS", "Reset to Default"), new Color(0.25f, 0.2f, 0.12f));
            UIFactory.SetLayoutElement(resetBtn.Component.gameObject, minWidth: 130, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            resetBtn.OnClick += ResetVisibleToDefault;

            resultLabel = UIFactory.CreateLabel(actionRow, "ResultLabel", "", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(resultLabel.gameObject, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            GameObject filterRow = UIFactory.CreateHorizontalGroup(ContentRoot, "FilterRow", false, false, true, true, 5, default, default, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(filterRow, minHeight: 30, flexibleHeight: 0, flexibleWidth: 9999);

            searchInput = UIFactory.CreateInputField(filterRow, "Search", Localizer.Get("TXT_SEARCH_OPTIONS", "Search options..."));
            UIFactory.SetLayoutElement(searchInput.UIRoot, minWidth: 260, minHeight: 25, flexibleWidth: 9999, flexibleHeight: 0);
            searchInput.OnValueChanged += _ => RefreshFilteredEntries();

            UIFactory.CreateDropdown(filterRow, "Category", out categoryDropdown, Localizer.Get("CATEGORY_ALL", "All"), 14, _ => RefreshFilteredEntries(), GetLocalizedCategoryOptions());
            UIFactory.SetLayoutElement(categoryDropdown.gameObject, minWidth: 150, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);

            statusLabel = UEUI.CreateStatus(ContentRoot, "OptionsStatus", "");

            scrollPool = UIFactory.CreateScrollPool<ConfigEntryCell>(
                ContentRoot,
                "ConfigEntries",
                out GameObject scrollObj,
                out GameObject scrollContent);

            scrollPool.Initialize(this);
            RefreshFilteredEntries();
        }

        private void ResetVisibleToDefault()
        {
            foreach (CacheConfigEntry entry in configEntries)
                entry.RefConfigElement.RevertToDefaultValue();

            RefreshFilteredEntries(false);
            SetStatus(Localizer.Get("STATUS_OPTIONS_RESET", "Visible options reset to defaults."));
        }

        private void RefreshFilteredEntries(bool jumpToTop = true)
        {
            configEntries.Clear();

            string query = searchInput?.Text ?? "";
            string selectedCategory = GetSelectedCategory();
            foreach (CacheConfigEntry entry in allConfigEntries)
            {
                IConfigElement config = entry.RefConfigElement;
                bool categoryMatch = selectedCategory == "All"
                    || string.Equals(config.Category, selectedCategory, StringComparison.OrdinalIgnoreCase)
                    || selectedCategory == "Advanced" && config.Advanced;
                bool queryMatch = string.IsNullOrEmpty(query)
                    || config.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    || config.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    || config.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

                if (categoryMatch && queryMatch)
                    configEntries.Add(entry);
            }

            if (resultLabel)
                resultLabel.text = string.Format(Localizer.Get("STATUS_OPTIONS_COUNT", "{0} option(s)"), configEntries.Count);

            SetStatus(string.Format(Localizer.Get("STATUS_OPTIONS_FILTERED", "{0} option(s) shown."), configEntries.Count));

            scrollPool?.Refresh(true, jumpToTop);
        }

        private string GetSelectedCategory()
        {
            if (!categoryDropdown || categoryDropdown.value < 0 || categoryDropdown.value >= CategoryOptions.Length)
                return "All";
            return CategoryOptions[categoryDropdown.value];
        }

        private static int GetCategoryOrder(string category)
        {
            for (int i = 0; i < CategoryOptions.Length; i++)
            {
                if (string.Equals(CategoryOptions[i], category, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return CategoryOptions.Length;
        }

        private static string[] GetLocalizedCategoryOptions()
        {
            return CategoryOptions
                .Select(category => Localizer.Get("CATEGORY_" + category.ToUpperInvariant(), category))
                .ToArray();
        }

        private void SetStatus(string text)
        {
            if (statusLabel)
                statusLabel.text = text ?? "";
        }
    }
}
