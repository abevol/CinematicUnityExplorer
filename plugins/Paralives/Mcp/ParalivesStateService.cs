#if MONO
namespace CinematicUnityExplorer.Plugins.Paralives.Mcp
{
    internal static class ParalivesStateService
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_get_type_index"] = _ => GetTypeIndex(),
            ["paralives_get_game_state"] = _ => GetGameState(),
            ["paralives_get_loading_state"] = _ => GetLoadingState(),
            ["paralives_read_resource"] = parameters => ReadResource(McpParameters.RequiredString(parameters, "uri"), parameters)
        };

        public static object ReadResource(string uri, Dictionary<string, object> parameters)
        {
            ParalivesShared.EnsureAvailable();
            return uri switch
            {
                "paralives://types/managers" => new Dictionary<string, object> { ["types"] = ParalivesEnvironment.TypeIndex.Managers },
                "paralives://types/settings" => new Dictionary<string, object> { ["types"] = ParalivesEnvironment.TypeIndex.Settings },
                "paralives://types/cheats" => new Dictionary<string, object> { ["types"] = ParalivesEnvironment.TypeIndex.Cheats },
                _ => throw new McpBridgeException("invalid_request", $"Unknown Paralives resource '{uri}'.")
            };
        }

        internal static Dictionary<string, object> GetGameStateSnapshot()
        {
            return (Dictionary<string, object>)GetGameState();
        }

        internal static Dictionary<string, object> GetLoadingStateSnapshot()
        {
            return (Dictionary<string, object>)GetLoadingState();
        }

        internal static Dictionary<string, object> BuildLoadingState()
        {
            Dictionary<string, object> result = ParalivesShared.SummarizeManager("GameLoadingManager", new[]
            {
                "State",
                "CurrentState",
                "LoadingState",
                "IsLoading",
                "Loading",
                "Progress",
                "CurrentStep",
                "CurrentLoadingStep"
            });

            bool isLoading = false;
            if (result.TryGetValue("selectedMembers", out object selectedObj) && selectedObj is Dictionary<string, object> selected)
            {
                foreach (KeyValuePair<string, object> pair in selected)
                {
                    if ((pair.Key == "IsLoading" || pair.Key == "Loading") && pair.Value is bool boolValue && boolValue)
                        isLoading = true;

                    string text = pair.Value?.ToString();
                    if (!string.IsNullOrEmpty(text) && text.IndexOf("loading", StringComparison.OrdinalIgnoreCase) >= 0)
                        isLoading = true;
                }
            }

            result["isLoadingInferred"] = isLoading;
            result["activeScene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return result;
        }

        private static object GetTypeIndex()
        {
            ParalivesShared.EnsureAvailable();
            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["rootPath"] = ParalivesEnvironment.RootPath,
                ["mainModPath"] = ParalivesEnvironment.MainModPath,
                ["index"] = ParalivesEnvironment.TypeIndex.ToSummary()
            };
        }

        private static object GetGameState()
        {
            ParalivesShared.EnsureAvailable();
            GameObject mainMenu = ParalivesShared.FindMainMenuRoot();
            Dictionary<string, object> loadingState = BuildLoadingState();
            List<object> scenes = new();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                scenes.Add(new Dictionary<string, object>
                {
                    ["name"] = scene.name,
                    ["path"] = scene.path,
                    ["isLoaded"] = scene.isLoaded,
                    ["rootCount"] = scene.IsValid() ? RuntimeHelper.GetRootCount(scene) : 0
                });
            }

            string inferredMode = InferGameModeFromState(loadingState);
            bool isMainMenuVisible = false;
            if (mainMenu)
            {
                Component mainMenuComponent = UnityReflectionUtility.FindComponentByName(mainMenu, "UIMainMenu");
                if (mainMenuComponent != null)
                {
                    Type type = mainMenuComponent.GetActualType();
                    isMainMenuVisible = UnityReflectionUtility.TryReadMember(mainMenuComponent, type, "IsVisible", out object isVisibleValue)
                        ? (bool)isVisibleValue
                        : false;
                }
            }

            return new Dictionary<string, object>
            {
                ["mode"] = inferredMode,
                ["isMainMenu"] = isMainMenuVisible,
                ["isMainMenuObjectPresent"] = mainMenu && mainMenu.activeInHierarchy,
                ["mainMenu"] = mainMenu ? UnityObjectSummary.FromGameObject(mainMenu) : null,
                ["scenes"] = scenes,
                ["activeUiRoots"] = GetActiveUiRoots(30),
                ["loading"] = loadingState,
                ["savedGameManager"] = ParalivesShared.SummarizeManager("SavedGameManager", new[] { "CurrentSavedGame", "CurrentSave", "LoadedGame", "IsGameLoaded", "HasLoadedGame" }),
                ["gameLoadingManager"] = ParalivesShared.SummarizeManager("GameLoadingManager", new[] { "State", "CurrentState", "IsLoading", "Progress" })
            };
        }

        private static object GetLoadingState()
        {
            ParalivesShared.EnsureAvailable();
            return BuildLoadingState();
        }

        private static string InferGameModeFromState(Dictionary<string, object> loadingState)
        {
            if (loadingState.TryGetValue("selectedMembers", out object membersObj)
                && membersObj is Dictionary<string, object> members)
            {
                if (members.TryGetValue("State", out object stateValue))
                {
                    string state = stateValue?.ToString();
                    if (state == "Loading" || state == "LoadingGame" || state == "LoadingScene")
                        return "loading";
                    if (state == "MainMenu")
                        return "main_menu";
                    if (state == "Game")
                        return "game";
                }
            }

            bool isLoading = loadingState.TryGetValue("isLoadingInferred", out object loadingValue)
                && loadingValue is bool loadingBool
                && loadingBool;

            if (isLoading)
                return "loading";

            return "unknown";
        }

        private static List<object> GetActiveUiRoots(int limit)
        {
            List<object> results = new();
            HashSet<int> seen = new();

            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (!go || !go.activeInHierarchy)
                    continue;

                string path = UnityObjectSummary.GetPath(go);
                bool looksLikeUi = go.name.StartsWith("UI", StringComparison.OrdinalIgnoreCase)
                    || go.name.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("/UI", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!looksLikeUi || !seen.Add(go.GetInstanceID()))
                    continue;

                results.Add(UnityObjectSummary.FromGameObject(go));
                if (results.Count >= limit)
                    break;
            }

            return results;
        }
    }
}
#endif
