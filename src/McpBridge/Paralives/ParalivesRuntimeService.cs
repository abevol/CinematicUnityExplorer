#if MONO
using System.Globalization;
using System.Text;
using UnityEngine.SceneManagement;

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
            ["paralives_get_selection"] = _ => GetSelection(),
            ["paralives_get_active_context"] = _ => GetActiveContext(),
            ["paralives_get_character_needs"] = GetCharacterNeeds,
            ["paralives_get_character_actions"] = GetCharacterActions
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
            Dictionary<string, object> gameState = ParalivesBridgeService.GetGameStateSnapshot();
            Dictionary<string, object> loadingState = ParalivesBridgeService.GetLoadingStateSnapshot();

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

        #region 主控上下文功能

        /// <summary>
        /// 获取当前活跃上下文（家庭、角色、地段）
        /// </summary>
        private static object GetActiveContext()
        {
            // 获取当前家庭信息
            Dictionary<string, object> householdInfo = GetActiveHouseholdInfo();
            
            // 获取当前活跃角色（从 UICharacters 面板推断）
            Dictionary<string, object> characterInfo = GetActiveCharacterInfo();
            
            // 获取当前地段信息
            Dictionary<string, object> lotInfo = GetCurrentLotInfo();

            return new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["activeHousehold"] = householdInfo,
                ["activeCharacter"] = characterInfo,
                ["currentLot"] = lotInfo
            };
        }

        /// <summary>
        /// 获取当前家庭信息
        /// </summary>
        private static Dictionary<string, object> GetActiveHouseholdInfo()
        {
            Type householdManagerType = ReflectionUtility.GetTypeByName("HouseholdManager");
            if (householdManagerType == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "HouseholdManager type not found" };

            object manager = UnityReflectionUtility.GetSingletonInstance(householdManagerType);
            if (manager == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "HouseholdManager instance not found" };

            ulong guid = UnityReflectionUtility.TryReadMember(manager, householdManagerType, "CurrentHouseholdGUID", out object guidValue) 
                ? Convert.ToUInt64(guidValue) : 0;
            bool hasHousehold = UnityReflectionUtility.TryReadMember(manager, householdManagerType, "HasCurrentHousehold", out object hasValue) 
                && (bool)hasValue;

            if (!hasHousehold || guid == 0)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "No active household" };

            // 尝试获取家庭名称和成员
            string name = "Unknown";
            int memberCount = 0;
            List<object> members = new();

            // 从 HouseholdManager 获取当前家庭对象
            UnityReflectionUtility.TryReadMember(manager, householdManagerType, "CurrentHousehold", out object householdObj);
            if (householdObj != null)
            {
                Type householdType = householdObj.GetActualType();
                name = UnityReflectionUtility.TryReadMember(householdObj, householdType, "Name", out object nameValue) ? nameValue?.ToString() : "Unknown";
                
                // 获取成员列表
                if (UnityReflectionUtility.TryReadMember(householdObj, householdType, "Members", out object membersObj) && membersObj is System.Collections.IEnumerable enumerable)
                {
                    foreach (object member in enumerable)
                    {
                        if (member == null) continue;
                        memberCount++;
                        Type memberType = member.GetActualType();
                        members.Add(new Dictionary<string, object>
                        {
                            ["type"] = memberType.FullName,
                            ["display"] = member.ToString(),
                            ["guid"] = UnityReflectionUtility.TryReadMember(member, memberType, "GUID", out object memberGuid) ? memberGuid?.ToString() : null
                        });
                        if (members.Count >= 10) break;
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["guid"] = guid.ToString(),
                ["name"] = name,
                ["memberCount"] = memberCount,
                ["members"] = members
            };
        }

        /// <summary>
        /// 获取当前活跃角色信息
        /// </summary>
        private static Dictionary<string, object> GetActiveCharacterInfo()
        {
            // 从 UICharacters 面板推断当前选中角色
            GameObject uiCharacters = UnityReflectionUtility.FindGameObjectByName("UICharacters(Clone)");
            if (uiCharacters == null)
                uiCharacters = UnityReflectionUtility.FindGameObjectByName("UICharacters");

            if (uiCharacters == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "UICharacters not found" };

            // 查找选中的角色
            GameObject selectedCharacter = FindChildByName(uiCharacters, "SelectedCharacter");
            if (selectedCharacter != null && selectedCharacter.activeInHierarchy)
            {
                // 找到父级 Character 对象
                Transform parent = selectedCharacter.transform.parent;
                if (parent != null)
                {
                    // 查找 CharacterThumbnail 中的 ImageCharacterIcon
                    GameObject thumbnail = FindChildByName(parent.gameObject, "CharacterThumbnail");
                    if (thumbnail != null)
                    {
                        return new Dictionary<string, object>
                        {
                            ["available"] = true,
                            ["source"] = "UICharacters selection",
                            ["parentPath"] = UnityObjectSummary.GetPath(parent.gameObject)
                        };
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["available"] = false,
                ["reason"] = "No character selected in UICharacters"
            };
        }

        /// <summary>
        /// 获取当前地段信息
        /// </summary>
        private static Dictionary<string, object> GetCurrentLotInfo()
        {
            // 从场景对象推断当前地段
            // 查找 NavMeshSurface 对象，它们通常属于当前地段
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (!go || !go.name.StartsWith("NavMeshSurface lot "))
                    continue;

                // 提取 lot GUID
                string lotName = go.name.Replace("NavMeshSurface lot ", "");
                if (ulong.TryParse(lotName.Split('/')[0], out ulong lotGuid))
                {
                    return new Dictionary<string, object>
                    {
                        ["available"] = true,
                        ["guid"] = lotGuid.ToString(),
                        ["name"] = go.name,
                        ["path"] = UnityObjectSummary.GetPath(go),
                        ["isActive"] = go.activeInHierarchy
                    };
                }
            }

            return new Dictionary<string, object>
            {
                ["available"] = false,
                ["reason"] = "Could not determine current lot"
            };
        }

        /// <summary>
        /// 获取角色需求状态
        /// </summary>
        private static object GetCharacterNeeds(Dictionary<string, object> parameters)
        {
            string characterGuid = McpParameters.OptionalString(parameters, "characterGuid");
            
            // 如果没有指定角色，尝试获取当前选中角色
            if (string.IsNullOrEmpty(characterGuid))
            {
                Dictionary<string, object> activeChar = GetActiveCharacterInfo();
                if (activeChar.TryGetValue("available", out object avail) && (bool)avail)
                {
                    // 从 UI 获取需求信息
                    return GetNeedsFromUI();
                }
                return new Dictionary<string, object> 
                { 
                    ["available"] = false, 
                    ["reason"] = "No character specified and no active character found" 
                };
            }

            // 指定角色的需求获取
            return GetCharacterNeedsByGuid(characterGuid);
        }

        /// <summary>
        /// 从 UI 获取需求信息
        /// </summary>
        private static Dictionary<string, object> GetNeedsFromUI()
        {
            // 查找 UIThoughts 面板
            GameObject uiThoughts = UnityReflectionUtility.FindGameObjectByName("UIThoughts(Clone)");
            if (uiThoughts == null)
                uiThoughts = UnityReflectionUtility.FindGameObjectByName("UIThoughts");

            if (uiThoughts == null || !uiThoughts.activeInHierarchy)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "UIThoughts not visible" };

            // 查找需求项
            List<object> needs = new();
            FindNeedsInChildren(uiThoughts.transform, needs, 0);

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["source"] = "UIThoughts panel",
                ["needs"] = needs
            };
        }

        /// <summary>
        /// 递归查找需求项
        /// </summary>
        private static void FindNeedsInChildren(Transform parent, List<object> needs, int depth)
        {
            if (depth > 10 || needs.Count >= 20) return;

            foreach (Transform child in parent)
            {
                if (child.name.Contains("NeedItem") || child.name.Contains("UINeed") || child.name.Contains("EmotionsItem"))
                {
                    Dictionary<string, object> needInfo = new()
                    {
                        ["name"] = child.name,
                        ["path"] = UnityObjectSummary.GetPath(child.gameObject),
                        ["active"] = child.gameObject.activeInHierarchy
                    };

                    // 尝试获取需求/情绪名称和值
                    TryExtractNeedDetails(child.gameObject, needInfo);
                    
                    needs.Add(needInfo);
                }
                FindNeedsInChildren(child, needs, depth + 1);
            }
        }

        /// <summary>
        /// 提取需求/情绪详情
        /// </summary>
        private static void TryExtractNeedDetails(GameObject go, Dictionary<string, object> needInfo)
        {
            // 查找 TranslatedText 组件获取翻译键
            foreach (Component component in go.GetComponentsInChildren<Component>(true))
            {
                if (!component) continue;
                Type type = component.GetActualType();
                
                // TranslatedText 组件包含 Key
                if (type.Name == "TranslatedText")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "Key", out object keyValue))
                    {
                        string key = keyValue?.ToString();
                        if (!string.IsNullOrEmpty(key))
                        {
                            needInfo["translationKey"] = key;
                            // 推断需求类型
                            needInfo["needType"] = InferNeedTypeFromKey(key);
                        }
                    }
                }
                
                // TextMeshProUGUI 组件包含显示文本
                if (type.Name == "TextMeshProUGUI")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "text", out object textValue))
                    {
                        string text = textValue?.ToString();
                        if (!string.IsNullOrEmpty(text) && text.Length < 20)
                        {
                            needInfo["displayText"] = text;
                        }
                    }
                }

                // Image 组件可能包含 FillAmount（需求值）
                if (type.Name == "Image")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "fillAmount", out object fillValue))
                    {
                        float fill = Convert.ToSingle(fillValue);
                        if (fill > 0 && fill <= 1)
                        {
                            needInfo["fillAmount"] = Math.Round(fill, 2);
                            needInfo["fillPercent"] = Math.Round(fill * 100, 0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从翻译键推断需求类型
        /// </summary>
        private static string InferNeedTypeFromKey(string key)
        {
            if (key.Contains("Hunger") || key.Contains("Food"))
                return "hunger";
            if (key.Contains("Hygiene") || key.Contains("Clean"))
                return "hygiene";
            if (key.Contains("Energy") || key.Contains("Sleep") || key.Contains("Tired"))
                return "energy";
            if (key.Contains("Fun") || key.Contains("Entertainment"))
                return "fun";
            if (key.Contains("Social"))
                return "social";
            if (key.Contains("Bladder") || key.Contains("Toilet"))
                return "bladder";
            if (key.Contains("Happy") || key.Contains("Emotion"))
                return "emotion";
            if (key.Contains("Comfort"))
                return "comfort";
            return key;
        }

        /// <summary>
        /// 通过 GUID 获取角色需求
        /// </summary>
        private static Dictionary<string, object> GetCharacterNeedsByGuid(string characterGuid)
        {
            // 尝试通过 NeedManager 获取
            Type needManagerType = ReflectionUtility.GetTypeByName("NeedManager");
            if (needManagerType == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "NeedManager type not found" };

            object needManager = UnityReflectionUtility.GetSingletonInstance(needManagerType);
            if (needManager == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "NeedManager instance not found" };

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["characterGuid"] = characterGuid,
                ["managerAvailable"] = true,
                ["note"] = "Use Paralives:set_need_value to modify needs"
            };
        }

        /// <summary>
        /// 获取角色当前/排队动作
        /// </summary>
        private static object GetCharacterActions(Dictionary<string, object> parameters)
        {
            string characterGuid = McpParameters.OptionalString(parameters, "characterGuid");
            
            // 从 UIInteractionQueue 获取动作信息
            return GetActionsFromUI();
        }

        /// <summary>
        /// 从 UI 获取动作队列
        /// </summary>
        private static Dictionary<string, object> GetActionsFromUI()
        {
            // 查找 UIInteractionQueue 面板
            GameObject uiInteractionQueue = UnityReflectionUtility.FindGameObjectByName("UIInteractionQueue(Clone)");
            if (uiInteractionQueue == null)
                uiInteractionQueue = UnityReflectionUtility.FindGameObjectByName("UIInteractionQueue");

            if (uiInteractionQueue == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "UIInteractionQueue not found" };

            // 查找队列项
            List<object> actions = new();
            FindActionsInChildren(uiInteractionQueue.transform, actions, 0);

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["source"] = "UIInteractionQueue",
                ["actions"] = actions
            };
        }

        /// <summary>
        /// 递归查找动作项
        /// </summary>
        private static void FindActionsInChildren(Transform parent, List<object> actions, int depth)
        {
            if (depth > 10 || actions.Count >= 20) return;

            foreach (Transform child in parent)
            {
                if (child.name.Contains("QueueItem") || child.name.Contains("Interaction"))
                {
                    Dictionary<string, object> actionInfo = new()
                    {
                        ["name"] = child.name,
                        ["path"] = UnityObjectSummary.GetPath(child.gameObject),
                        ["active"] = child.gameObject.activeInHierarchy
                    };

                    // 尝试获取动作详情
                    TryExtractActionDetails(child.gameObject, actionInfo);
                    
                    actions.Add(actionInfo);
                }
                FindActionsInChildren(child, actions, depth + 1);
            }
        }

        /// <summary>
        /// 提取动作详情
        /// </summary>
        private static void TryExtractActionDetails(GameObject go, Dictionary<string, object> actionInfo)
        {
            // 查找子对象中的文本组件
            foreach (Component component in go.GetComponentsInChildren<Component>(true))
            {
                if (!component) continue;
                Type type = component.GetActualType();
                
                // TextMeshProUGUI 组件包含显示文本
                if (type.Name == "TextMeshProUGUI")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "text", out object textValue))
                    {
                        string text = textValue?.ToString();
                        if (!string.IsNullOrEmpty(text) && text.Length < 50)
                        {
                            // 检查是否是动作名称
                            string path = UnityObjectSummary.GetPath(component.gameObject);
                            if (path.Contains("LabelInteractionName") || path.Contains("LabelName"))
                            {
                                actionInfo["interactionName"] = text;
                            }
                        }
                    }
                }

                // TranslatedText 组件包含翻译键
                if (type.Name == "TranslatedText")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "Key", out object keyValue))
                    {
                        string key = keyValue?.ToString();
                        if (!string.IsNullOrEmpty(key) && key.Contains("Interaction"))
                        {
                            actionInfo["translationKey"] = key;
                        }
                    }
                }

                // 检查是否正在运行
                if (component.gameObject.name == "ImageIsInteractionRunning")
                {
                    actionInfo["isRunning"] = component.gameObject.activeInHierarchy;
                }
            }
        }

        /// <summary>
        /// 查找子对象
        /// </summary>
        private static GameObject FindChildByName(GameObject parent, string name)
        {
            foreach (Transform child in parent.transform)
            {
                if (child.name == name)
                    return child.gameObject;
                GameObject found = FindChildByName(child.gameObject, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        #endregion

    }
}
#endif
