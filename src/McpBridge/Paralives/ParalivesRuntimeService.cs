#if MONO
using System.Globalization;

namespace UnityExplorer.McpBridge.Paralives
{
    /// <summary>
    /// 提供 Paralives 运行时状态查询和日志读取功能
    /// </summary>
    internal static class ParalivesRuntimeService
    {
        private static readonly Dictionary<string, Func<Dictionary<string, object>, object>> actionHandlers = new()
        {
            ["paralives_get_runtime_summary"] = _ => GetRuntimeSummary(),
            ["paralives_get_game_time"] = _ => GetGameTime(),
            ["paralives_get_economy"] = _ => GetEconomy(),
            ["paralives_get_selection"] = _ => GetSelection()
        };
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = BuildActions();

        /// <summary>
        /// 处理运行时状态和日志相关的 action
        /// </summary>
        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            if (actionHandlers.TryGetValue(action, out Func<Dictionary<string, object>, object> handler))
                return handler(parameters);

            throw new McpBridgeException("invalid_request", $"Unknown runtime action '{action}'.");
        }

        private static Dictionary<string, Func<Dictionary<string, object>, object>> BuildActions()
        {
            Dictionary<string, Func<Dictionary<string, object>, object>> actions = new();
            foreach (string action in actionHandlers.Keys)
            {
                string registeredAction = action;
                actions[registeredAction] = parameters => Handle(registeredAction, parameters);
            }
            return actions;
        }

        /// <summary>
        /// 获取综合运行时状态摘要
        /// </summary>
        private static object GetRuntimeSummary()
        {
            // 获取游戏状态
            Dictionary<string, object> gameState = ParalivesStateService.GetGameStateSnapshot();
            Dictionary<string, object> loadingState = ParalivesStateService.GetLoadingStateSnapshot();

            // 获取时间状态
            Dictionary<string, object> timeState = GetGameTimeState();

            // 获取经济状态
            Dictionary<string, object> economyState = GetEconomyState();

            // 获取选中对象状态
            Dictionary<string, object> selectionState = GetSelectionState();

            // 获取 UI 状态
            Dictionary<string, object> uiState = GetUiState();

            // 推断游戏模式
            string mode = InferGameMode(gameState, loadingState);
            bool isLoading = mode == "loading";

            return new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["isLoading"] = isLoading,
                ["dataReliability"] = isLoading ? "partial" : "full",
                ["gameState"] = new Dictionary<string, object>
                {
                    ["mode"] = mode,
                    ["isPaused"] = timeState.ContainsKey("isPaused") && (bool)timeState["isPaused"],
                    ["timeSpeed"] = timeState.ContainsKey("timeSpeed") ? timeState["timeSpeed"] : 0,
                    ["gameTime"] = timeState
                },
                ["economy"] = economyState,
                ["selection"] = selectionState,
                ["currentFamily"] = GetCurrentFamilyInfo(),
                ["uiState"] = uiState,
                ["loading"] = loadingState,
                ["advice"] = isLoading 
                    ? "Game is loading. Some data may be incomplete. Wait for loading to finish for accurate state." 
                    : null
            };
        }

        /// <summary>
        /// 获取游戏时间状态
        /// </summary>
        private static object GetGameTime()
        {
            return GetGameTimeState();
        }

        /// <summary>
        /// 获取经济状态
        /// </summary>
        private static object GetEconomy()
        {
            return GetEconomyState();
        }

        /// <summary>
        /// 获取选中对象状态
        /// </summary>
        private static object GetSelection()
        {
            return GetSelectionState();
        }

        /// <summary>
        /// 获取游戏时间状态（内部实现）
        /// </summary>
        private static Dictionary<string, object> GetGameTimeState()
        {
            // 查找 UITime 组件
            GameObject uiTime = UnityReflectionUtility.FindGameObjectByName("UITime(Clone)");
            if (uiTime == null)
                uiTime = UnityReflectionUtility.FindGameObjectByName("UITime");

            bool isPaused = true;
            int timeSpeed = 0;
            int minutes = 0;

            if (uiTime != null)
            {
                Component timeComponent = UnityReflectionUtility.FindComponentByName(uiTime, "UITime");
                if (timeComponent != null)
                {
                    Type type = timeComponent.GetActualType();
                    isPaused = UnityReflectionUtility.ReadMemberBool(timeComponent, type, "LastIsPaused", true);
                    timeSpeed = UnityReflectionUtility.ReadMemberInt(timeComponent, type, "LastTimeSpeed", 0);
                    minutes = UnityReflectionUtility.ReadMemberInt(timeComponent, type, "_lastMinute", 0);
                }
            }

            // 计算时间
            int hour = minutes / 60;
            int minute = minutes % 60;
            int day = hour / 24;
            hour = hour % 24;

            return new Dictionary<string, object>
            {
                ["isPaused"] = isPaused,
                ["timeSpeed"] = timeSpeed,
                ["minutes"] = minutes,
                ["formatted"] = $"{hour:D2}:{minute:D2}",
                ["day"] = day + 1,
                ["hour"] = hour,
                ["minute"] = minute,
                ["realtimeSinceStartup"] = Time.realtimeSinceStartup
            };
        }

        /// <summary>
        /// 获取经济状态（内部实现）
        /// </summary>
        private static Dictionary<string, object> GetEconomyState()
        {
            // 查找 UIGameBar 组件
            GameObject uiGameBar = UnityReflectionUtility.FindGameObjectByName("UIGameBar(Clone)");
            if (uiGameBar == null)
                uiGameBar = UnityReflectionUtility.FindGameObjectByName("UIGameBar");

            int funds = 0;

            if (uiGameBar != null)
            {
                Component gameBarComponent = UnityReflectionUtility.FindComponentByName(uiGameBar, "UIGameBar");
                if (gameBarComponent != null)
                {
                    Type type = gameBarComponent.GetActualType();
                    funds = UnityReflectionUtility.ReadMemberInt(gameBarComponent, type, "LastMoneyBalance", 0);
                }
            }

            return new Dictionary<string, object>
            {
                ["householdFunds"] = funds,
                ["formatted"] = funds.ToString("C0", CultureInfo.GetCultureInfo("en-US"))
            };
        }

        /// <summary>
        /// 获取选中对象状态（内部实现）
        /// </summary>
        private static Dictionary<string, object> GetSelectionState()
        {
            // 查找 UISelected 组件
            GameObject uiSelected = UnityReflectionUtility.FindGameObjectByName("UISelected(Clone)");
            if (uiSelected == null)
                uiSelected = UnityReflectionUtility.FindGameObjectByName("UISelected");

            bool hasSelection = false;
            int selectedSubEntity = -1;

            if (uiSelected != null)
            {
                Component selectedComponent = UnityReflectionUtility.FindComponentByName(uiSelected, "UISelected");
                if (selectedComponent != null)
                {
                    Type type = selectedComponent.GetActualType();
                    hasSelection = UnityReflectionUtility.ReadMemberBool(selectedComponent, type, "IsVisible", false);
                    selectedSubEntity = UnityReflectionUtility.ReadMemberInt(selectedComponent, type, "_selectedSubEntity", -1);
                }
            }

            return new Dictionary<string, object>
            {
                ["hasSelection"] = hasSelection,
                ["selectedSubEntity"] = selectedSubEntity,
                ["selectedObject"] = hasSelection ? "Use UnityExplorer:get_object_detail for details" : null
            };
        }

        /// <summary>
        /// 获取 UI 状态（内部实现）
        /// </summary>
        private static Dictionary<string, object> GetUiState()
        {
            List<string> visiblePanels = new();

            // 检查各个 UI 面板
            string[] panelNames = new[]
            {
                "UIGameBar(Clone)", "UITime(Clone)", "UICharacters(Clone)",
                "UIBuildModeModes(Clone)", "UIBuildModeCatalog(Clone)",
                "UITownMap(Clone)", "UINewspaper(Clone)", "UIOfferedWants(Clone)",
                "UIDeveloperTools(Clone)", "UISelected(Clone)"
            };

            foreach (string panelName in panelNames)
            {
                GameObject panel = UnityReflectionUtility.FindGameObjectByName(panelName);
                if (panel != null && panel.activeInHierarchy)
                {
                    // 移除 (Clone) 后缀
                    string cleanName = panelName.Replace("(Clone)", "");
                    if (!visiblePanels.Contains(cleanName))
                        visiblePanels.Add(cleanName);
                }
            }

            // 检查主菜单是否可见
            bool isMainMenuVisible = false;
            GameObject mainMenu = UnityReflectionUtility.FindGameObjectByName("UIMainMenu(Clone)");
            if (mainMenu == null)
                mainMenu = UnityReflectionUtility.FindGameObjectByName("UIMainMenu");

            if (mainMenu != null)
            {
                Component mainMenuComponent = UnityReflectionUtility.FindComponentByName(mainMenu, "UIMainMenu");
                if (mainMenuComponent != null)
                {
                    Type type = mainMenuComponent.GetActualType();
                    isMainMenuVisible = UnityReflectionUtility.ReadMemberBool(mainMenuComponent, type, "IsVisible", false);
                }
            }

            return new Dictionary<string, object>
            {
                ["visiblePanels"] = visiblePanels,
                ["isMainMenuVisible"] = isMainMenuVisible
            };
        }

        /// <summary>
        /// 获取当前家庭信息
        /// </summary>
        private static Dictionary<string, object> GetCurrentFamilyInfo()
        {
            // 尝试从 HouseholdManager 获取当前家庭
            Type householdManagerType = ReflectionUtility.GetTypeByName("HouseholdManager");
            if (householdManagerType != null)
            {
                object manager = UnityReflectionUtility.GetSingletonInstance(householdManagerType);
                if (manager != null)
                {
                    // 尝试读取当前家庭
                    foreach (string memberName in new[] { "CurrentHousehold", "ActiveHousehold", "PlayerHousehold" })
                    {
                        try
                        {
                            object household = UnityReflectionUtility.ReadMemberSafe(manager, householdManagerType, memberName);
                            if (household != null)
                            {
                                Type householdType = household.GetActualType();
                                string name = UnityReflectionUtility.ReadMemberString(household, householdType, "Name", "Unknown");
                                int memberCount = 0;

                                // 尝试获取成员数量
                                foreach (string countMember in new[] { "MemberCount", "CharacterCount", "Members" })
                                {
                                    object countValue = UnityReflectionUtility.ReadMemberSafe(household, householdType, countMember);
                                    if (countValue is int intCount)
                                    {
                                        memberCount = intCount;
                                        break;
                                    }
                                    else if (countValue is System.Collections.IEnumerable enumerable)
                                    {
                                        foreach (object _ in enumerable)
                                            memberCount++;
                                        break;
                                    }
                                }

                                return new Dictionary<string, object>
                                {
                                    ["name"] = name,
                                    ["memberCount"] = memberCount,
                                    ["available"] = true
                                };
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["name"] = "Unknown",
                ["memberCount"] = 0,
                ["available"] = false
            };
        }

        /// <summary>
        /// 推断游戏模式
        /// </summary>
        private static string InferGameMode(Dictionary<string, object> gameState, Dictionary<string, object> loadingState)
        {
            // 检查 GameLoadingManager.State - 最可靠的来源
            if (loadingState.TryGetValue("selectedMembers", out object membersObj) 
                && membersObj is Dictionary<string, object> members)
            {
                // 如果 State 是 "Loading"，则正在加载
                if (members.TryGetValue("State", out object stateValue))
                {
                    string state = stateValue?.ToString();
                    if (state == "Loading" || state == "LoadingGame" || state == "LoadingScene")
                        return "loading";
                }
            }

            // 检查 isLoadingInferred
            if (loadingState.TryGetValue("isLoadingInferred", out object loadingValue) && loadingValue is bool isLoading && isLoading)
                return "loading";

            // 检查主菜单是否可见（不只是存在）
            GameObject mainMenu = UnityReflectionUtility.FindGameObjectByName("UIMainMenu(Clone)");
            if (mainMenu == null)
                mainMenu = UnityReflectionUtility.FindGameObjectByName("UIMainMenu");
            
            if (mainMenu != null)
            {
                Component mainMenuComponent = UnityReflectionUtility.FindComponentByName(mainMenu, "UIMainMenu");
                if (mainMenuComponent != null)
                {
                    Type type = mainMenuComponent.GetActualType();
                    bool isVisible = UnityReflectionUtility.ReadMemberBool(mainMenuComponent, type, "IsVisible", false);
                    if (isVisible)
                        return "main_menu";
                }
            }

            // 检查是否在建造模式
            GameObject buildMode = UnityReflectionUtility.FindGameObjectByName("UIBuildModeModes(Clone)");
            if (buildMode != null && buildMode.activeInHierarchy)
            {
                // 检查建造模式 UI 是否可见
                Component buildComponent = UnityReflectionUtility.FindComponentByName(buildMode, "UIBuildModeModes");
                if (buildComponent != null)
                {
                    Type type = buildComponent.GetActualType();
                    bool isVisible = UnityReflectionUtility.ReadMemberBool(buildComponent, type, "IsVisible", false);
                    if (isVisible)
                        return "build_mode";
                }
            }

            // 检查是否在角色创建
            GameObject characterCreation = UnityReflectionUtility.FindGameObjectByName("UICharacterCreation(Clone)");
            if (characterCreation != null && characterCreation.activeInHierarchy)
                return "character_creation";

            // 检查游戏是否已加载（GameLoadingManager.State == "Game"）
            if (loadingState.TryGetValue("selectedMembers", out object gameMembersObj) 
                && gameMembersObj is Dictionary<string, object> gameMembers)
            {
                if (gameMembers.TryGetValue("State", out object gameStateValue))
                {
                    string state = gameStateValue?.ToString();
                    if (state == "Game")
                        return "game";
                }
            }

            // 默认为未知
            return "unknown";
        }

    }
}
#endif
