using UnityEngine.SceneManagement;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UnityExplorer.UI.Widgets.AutoComplete;
using UnityExplorer.Localization;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ButtonList;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.ObjectExplorer
{
    public class ObjectSearch : UIModel
    {
        public ObjectExplorerPanel Parent { get; }

        private SearchContext context = SearchContext.GameObject;
        private SceneFilter sceneFilter = SceneFilter.Any;
        private ChildFilter childFilter = ChildFilter.Any;
        private string desiredTypeInput;
        private string lastCheckedTypeInput;

        public ButtonListHandler<object, ButtonCell> dataHandler;
        private ScrollPool<ButtonCell> resultsScrollPool;
        private List<object> currentResults = new();

        public TypeCompleter unityObjectTypeCompleter;
        public TypeCompleter componentTypeCompleter;
        public TypeCompleter allTypesCompleter;

        public override GameObject UIRoot => uiRoot;
        private GameObject uiRoot;
        private GameObject sceneFilterRow;
        private GameObject childFilterRow;
        private GameObject classInputRow;
        private GameObject nameInputRow;
        private InputFieldRef nameInputField;
        private InputFieldRef classInputField;
        private Text resultsLabel;
        private Text statusLabel;

        public ObjectSearch(ObjectExplorerPanel parent)
        {
            Parent = parent;
        }

        public List<object> GetEntries() => currentResults;

        public void DoSearch()
        {
            cachedCellTexts.Clear();
            SetStatus("Searching...");

            try
            {
                switch (context)
                {
                    case SearchContext.Singleton:
                        currentResults = SearchProvider.InstanceSearch(desiredTypeInput).ToList();
                        break;
                    case SearchContext.Class:
                        currentResults = SearchProvider.ClassSearch(desiredTypeInput);
                        break;
                    case SearchContext.StaticClass:
                        currentResults = SearchProvider.ClassSearch(desiredTypeInput, true);
                        break;
                    case SearchContext.Component:
                        currentResults = SearchProvider.UnityObjectSearch(nameInputField.Text, GetComponentTypeInput(), childFilter, sceneFilter);
                        break;
                    default:
                        currentResults = SearchProvider.UnityObjectSearch(nameInputField.Text, GetGameObjectTypeInput(), childFilter, sceneFilter);
                        break;
                }

                dataHandler.RefreshData();
                resultsScrollPool.Refresh(true, true);

                string mode = context.ToString();
                resultsLabel.text = string.Format(Localizer.Get("LBL_RESULTS_COUNT", "{0} results"), currentResults.Count);
                SetStatus(currentResults.Count == 0 ? $"No {mode} results matched the current filters." : $"{mode}: {currentResults.Count} result(s)");
            }
            catch (Exception ex)
            {
                currentResults.Clear();
                dataHandler.RefreshData();
                resultsScrollPool.Refresh(true, true);
                resultsLabel.text = string.Format(Localizer.Get("LBL_RESULTS_COUNT", "{0} results"), 0);
                SetStatus($"Search failed: {ex.GetInnerMostException().Message}");
            }
        }

        public void Update()
        {
            if ((context == SearchContext.GameObject || context == SearchContext.Component) && lastCheckedTypeInput != desiredTypeInput)
            {
                lastCheckedTypeInput = desiredTypeInput;
                Type type = ReflectionUtility.GetTypeByName(desiredTypeInput);
                bool canFilterByScene = string.IsNullOrEmpty(desiredTypeInput)
                    || type == typeof(GameObject)
                    || type != null && typeof(Component).IsAssignableFrom(type);

                sceneFilterRow.SetActive(canFilterByScene);
                childFilterRow.SetActive(canFilterByScene);
            }
        }

        private void OnContextDropdownChanged(int value)
        {
            context = (SearchContext)value;
            lastCheckedTypeInput = null;

            bool objectMode = context == SearchContext.GameObject || context == SearchContext.Component;
            nameInputRow.SetActive(objectMode);
            sceneFilterRow.SetActive(objectMode);
            childFilterRow.SetActive(objectMode);

            unityObjectTypeCompleter.Enabled = context == SearchContext.GameObject;
            componentTypeCompleter.Enabled = context == SearchContext.Component;
            allTypesCompleter.Enabled = context == SearchContext.Singleton || context == SearchContext.Class || context == SearchContext.StaticClass;

            if (classInputField != null)
                classInputField.PlaceholderText.text = context switch
                {
                    SearchContext.GameObject => "GameObject or UnityEngine.Object type...",
                    SearchContext.Component => "Component type...",
                    SearchContext.Singleton => "Singleton type filter...",
                    SearchContext.StaticClass => "Static class filter...",
                    _ => "Class filter..."
                };

            SetStatus($"Mode: {context}");
        }

        private void OnSceneFilterDropChanged(int value) => sceneFilter = (SceneFilter)value;

        private void OnChildFilterDropChanged(int value) => childFilter = (ChildFilter)value;

        private void OnTypeInputChanged(string val)
        {
            desiredTypeInput = val;
            if (string.IsNullOrEmpty(val))
                lastCheckedTypeInput = val;
        }

        private static readonly Dictionary<int, string> cachedCellTexts = new();

        public void SetCell(ButtonCell cell, int index)
        {
            if (!cachedCellTexts.ContainsKey(index))
                cachedCellTexts.Add(index, BuildResultText(currentResults[index]));

            cell.Button.ButtonText.text = cachedCellTexts[index];
        }

        private void OnCellClicked(int dataIndex)
        {
            if (context == SearchContext.Class || context == SearchContext.StaticClass)
                InspectorManager.Inspect(currentResults[dataIndex] as Type);
            else
                InspectorManager.Inspect(currentResults[dataIndex]);
        }

        private bool ShouldDisplayCell(object arg1, string arg2) => true;

        public override void ConstructUI(GameObject parent)
        {
            uiRoot = UIFactory.CreateVerticalGroup(parent, "ObjectSearch", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(uiRoot, flexibleHeight: 9999);

            GameObject contextGroup = UEUI.CreateToolbar(uiRoot, "SearchContextRow");
            Text contextLbl = UIFactory.CreateLabel(contextGroup, "SearchContextLabel", Localizer.Get("LBL_SEARCHING_FOR", "Searching for:"), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(contextLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            GameObject contextDropObj = UIFactory.CreateDropdown(contextGroup, "ContextDropdown", out Dropdown contextDrop, null, 14, OnContextDropdownChanged);
            foreach (string name in Enum.GetNames(typeof(SearchContext)))
                contextDrop.options.Add(new Dropdown.OptionData(Localizer.Get("CONTEXT_" + name.ToUpper(), name)));
            UIFactory.SetLayoutElement(contextDropObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            classInputRow = UIFactory.CreateHorizontalGroup(uiRoot, "ClassRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(classInputRow, minHeight: 25, flexibleHeight: 0);
            Text unityClassLbl = UIFactory.CreateLabel(classInputRow, "ClassLabel", Localizer.Get("LBL_CLASS_FILTER", "Type filter:"), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(unityClassLbl.gameObject, minWidth: 110, flexibleWidth: 0);
            classInputField = UIFactory.CreateInputField(classInputRow, "ClassInput", "GameObject or UnityEngine.Object type...");
            UIFactory.SetLayoutElement(classInputField.UIRoot, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            unityObjectTypeCompleter = new(typeof(UnityEngine.Object), classInputField, true, false, true);
            componentTypeCompleter = new(typeof(Component), classInputField, true, false, true);
            allTypesCompleter = new(null, classInputField, true, false, true);
            componentTypeCompleter.Enabled = false;
            allTypesCompleter.Enabled = false;
            classInputField.OnValueChanged += OnTypeInputChanged;

            childFilterRow = UIFactory.CreateHorizontalGroup(uiRoot, "ChildFilterRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(childFilterRow, minHeight: 25, flexibleHeight: 0);
            Text childLbl = UIFactory.CreateLabel(childFilterRow, "ChildLabel", Localizer.Get("LBL_CHILD_FILTER", "Child filter:"), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(childLbl.gameObject, minWidth: 110, flexibleWidth: 0);
            GameObject childDropObj = UIFactory.CreateDropdown(childFilterRow, "ChildFilterDropdown", out Dropdown childDrop, null, 14, OnChildFilterDropChanged);
            foreach (string name in Enum.GetNames(typeof(ChildFilter)))
                childDrop.options.Add(new Dropdown.OptionData(Localizer.Get("FILTER_" + name.ToUpper(), name)));
            UIFactory.SetLayoutElement(childDropObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            sceneFilterRow = UIFactory.CreateHorizontalGroup(uiRoot, "SceneFilterRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(sceneFilterRow, minHeight: 25, flexibleHeight: 0);
            Text sceneLbl = UIFactory.CreateLabel(sceneFilterRow, "SceneLabel", Localizer.Get("LBL_SCENE_FILTER", "Scene filter:"), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(sceneLbl.gameObject, minWidth: 110, flexibleWidth: 0);
            GameObject sceneDropObj = UIFactory.CreateDropdown(sceneFilterRow, "SceneFilterDropdown", out Dropdown sceneDrop, null, 14, OnSceneFilterDropChanged);
            foreach (string name in Enum.GetNames(typeof(SceneFilter)))
            {
                if (!SceneHandler.DontDestroyExists && name == "DontDestroyOnLoad")
                    continue;
                sceneDrop.options.Add(new Dropdown.OptionData(Localizer.Get("FILTER_" + name.ToUpper(), name)));
            }
            UIFactory.SetLayoutElement(sceneDropObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            nameInputRow = UIFactory.CreateHorizontalGroup(uiRoot, "NameRow", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(nameInputRow, minHeight: 25, flexibleHeight: 0);
            Text nameLbl = UIFactory.CreateLabel(nameInputRow, "NameFilterLabel", Localizer.Get("LBL_NAME_CONTAINS", "Name contains:"), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(nameLbl.gameObject, minWidth: 110, flexibleWidth: 0);
            nameInputField = UIFactory.CreateInputField(nameInputRow, "NameFilterInput", "Name substring...");
            UIFactory.SetLayoutElement(nameInputField.UIRoot, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            GameObject searchRow = UEUI.CreateToolbar(uiRoot, "SearchRow");
            ButtonRef searchButton = UEUI.CreateActionButton(searchRow, "SearchButton", Localizer.Get("BTN_SEARCH", "Search"), UEUI.Good, 110);
            searchButton.OnClick += DoSearch;
            resultsLabel = UIFactory.CreateLabel(searchRow, "ResultsLabel", string.Format(Localizer.Get("LBL_RESULTS_COUNT", "{0} results"), 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(resultsLabel.gameObject, minHeight: 25, flexibleWidth: 9999);

            statusLabel = UEUI.CreateStatus(uiRoot, "SearchStatus", "Mode: GameObject");

            resultsScrollPool = UIFactory.CreateScrollPool<ButtonCell>(uiRoot, "ResultsList", out GameObject scrollObj, out GameObject scrollContent);
            dataHandler = new ButtonListHandler<object, ButtonCell>(resultsScrollPool, GetEntries, SetCell, ShouldDisplayCell, OnCellClicked);
            resultsScrollPool.Initialize(dataHandler);
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
        }

        private string GetGameObjectTypeInput()
        {
            return string.IsNullOrEmpty(desiredTypeInput) ? typeof(GameObject).FullName : desiredTypeInput;
        }

        private string GetComponentTypeInput()
        {
            return string.IsNullOrEmpty(desiredTypeInput) ? typeof(Component).FullName : desiredTypeInput;
        }

        private void SetStatus(string text)
        {
            if (statusLabel)
                statusLabel.text = text ?? "";
        }

        private string BuildResultText(object result)
        {
            if (result == null)
                return "<color=grey>null</color>";

            if (result is Type type)
                return $"{SignatureHighlighter.Parse(type, true)} <color=grey><i>({type.Assembly.GetName().Name})</i></color>";

            UnityEngine.Object unityObject = result.TryCast<UnityEngine.Object>();
            GameObject go = null;
            if (unityObject is GameObject gameObject)
                go = gameObject;
            else if (unityObject is Component component)
                go = component.gameObject;

            if (go)
            {
                Scene scene = go.scene;
                string sceneName = scene.IsValid() ? scene.name : "HideAndDontSave";
                string active = go.activeInHierarchy ? "active" : "inactive";
                return $"{ToStringUtility.ToStringWithType(result, result.GetActualType())} <color=grey><i>{sceneName} | {active} | {GetPath(go)}</i></color>";
            }

            return ToStringUtility.ToStringWithType(result, result.GetActualType());
        }

        private static string GetPath(GameObject go)
        {
            List<string> names = new();
            Transform current = go.transform;
            while (current)
            {
                names.Add(current.name);
                current = current.parent;
            }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }
    }
}
