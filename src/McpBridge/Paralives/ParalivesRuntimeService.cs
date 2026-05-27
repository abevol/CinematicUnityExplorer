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
        // 日志缓冲区
        private static readonly List<LogEntry> logBuffer = new();
        private static readonly object logLock = new();
        private const int MaxLogBufferSize = 1000;

        // 日志订阅
        private static readonly Dictionary<string, LogSubscription> subscriptions = new();
        private static bool isLogCallbackRegistered;

        // 日志条目结构
        private class LogEntry
        {
            public int Id;
            public string Type;
            public string Message;
            public string StackTrace;
            public DateTime Timestamp;
            public int CollapseCount;
        }

        // 日志订阅结构
        private class LogSubscription
        {
            public string Id;
            public HashSet<string> Types;
            public List<LogEntry> Buffer;
            public int MaxSize;
            public DateTime CreatedAt;
        }

        /// <summary>
        /// 处理运行时状态和日志相关的 action
        /// </summary>
        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            return action switch
            {
                "paralives_get_runtime_summary" => GetRuntimeSummary(),
                "paralives_get_game_time" => GetGameTime(),
                "paralives_get_economy" => GetEconomy(),
                "paralives_get_selection" => GetSelection(),
                "get_game_logs" => GetGameLogs(parameters),
                "subscribe_logs" => SubscribeLogs(parameters),
                "poll_logs" => PollLogs(parameters),
                _ => throw new McpBridgeException("invalid_request", $"Unknown runtime action '{action}'.")
            };
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
            GameObject uiTime = FindGameObjectByName("UITime(Clone)");
            if (uiTime == null)
                uiTime = FindGameObjectByName("UITime");

            bool isPaused = true;
            int timeSpeed = 0;
            int minutes = 0;

            if (uiTime != null)
            {
                Component timeComponent = FindComponentByName(uiTime, "UITime");
                if (timeComponent != null)
                {
                    Type type = timeComponent.GetActualType();
                    isPaused = ReadMemberBool(timeComponent, type, "LastIsPaused", true);
                    timeSpeed = ReadMemberInt(timeComponent, type, "LastTimeSpeed", 0);
                    minutes = ReadMemberInt(timeComponent, type, "_lastMinute", 0);
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
            GameObject uiGameBar = FindGameObjectByName("UIGameBar(Clone)");
            if (uiGameBar == null)
                uiGameBar = FindGameObjectByName("UIGameBar");

            int funds = 0;

            if (uiGameBar != null)
            {
                Component gameBarComponent = FindComponentByName(uiGameBar, "UIGameBar");
                if (gameBarComponent != null)
                {
                    Type type = gameBarComponent.GetActualType();
                    funds = ReadMemberInt(gameBarComponent, type, "LastMoneyBalance", 0);
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
            GameObject uiSelected = FindGameObjectByName("UISelected(Clone)");
            if (uiSelected == null)
                uiSelected = FindGameObjectByName("UISelected");

            bool hasSelection = false;
            int selectedSubEntity = -1;

            if (uiSelected != null)
            {
                Component selectedComponent = FindComponentByName(uiSelected, "UISelected");
                if (selectedComponent != null)
                {
                    Type type = selectedComponent.GetActualType();
                    hasSelection = ReadMemberBool(selectedComponent, type, "IsVisible", false);
                    selectedSubEntity = ReadMemberInt(selectedComponent, type, "_selectedSubEntity", -1);
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
                GameObject panel = FindGameObjectByName(panelName);
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
            GameObject mainMenu = FindGameObjectByName("UIMainMenu(Clone)");
            if (mainMenu == null)
                mainMenu = FindGameObjectByName("UIMainMenu");

            if (mainMenu != null)
            {
                Component mainMenuComponent = FindComponentByName(mainMenu, "UIMainMenu");
                if (mainMenuComponent != null)
                {
                    Type type = mainMenuComponent.GetActualType();
                    isMainMenuVisible = ReadMemberBool(mainMenuComponent, type, "IsVisible", false);
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
                object manager = GetSingletonInstance(householdManagerType);
                if (manager != null)
                {
                    // 尝试读取当前家庭
                    foreach (string memberName in new[] { "CurrentHousehold", "ActiveHousehold", "PlayerHousehold" })
                    {
                        try
                        {
                            object household = ReadMemberSafe(manager, householdManagerType, memberName);
                            if (household != null)
                            {
                                Type householdType = household.GetActualType();
                                string name = ReadMemberString(household, householdType, "Name", "Unknown");
                                int memberCount = 0;

                                // 尝试获取成员数量
                                foreach (string countMember in new[] { "MemberCount", "CharacterCount", "Members" })
                                {
                                    object countValue = ReadMemberSafe(household, householdType, countMember);
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
            GameObject mainMenu = FindGameObjectByName("UIMainMenu(Clone)");
            if (mainMenu == null)
                mainMenu = FindGameObjectByName("UIMainMenu");
            
            if (mainMenu != null)
            {
                Component mainMenuComponent = FindComponentByName(mainMenu, "UIMainMenu");
                if (mainMenuComponent != null)
                {
                    Type type = mainMenuComponent.GetActualType();
                    bool isVisible = ReadMemberBool(mainMenuComponent, type, "IsVisible", false);
                    if (isVisible)
                        return "main_menu";
                }
            }

            // 检查是否在建造模式
            GameObject buildMode = FindGameObjectByName("UIBuildModeModes(Clone)");
            if (buildMode != null && buildMode.activeInHierarchy)
            {
                // 检查建造模式 UI 是否可见
                Component buildComponent = FindComponentByName(buildMode, "UIBuildModeModes");
                if (buildComponent != null)
                {
                    Type type = buildComponent.GetActualType();
                    bool isVisible = ReadMemberBool(buildComponent, type, "IsVisible", false);
                    if (isVisible)
                        return "build_mode";
                }
            }

            // 检查是否在角色创建
            GameObject characterCreation = FindGameObjectByName("UICharacterCreation(Clone)");
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

        #region 日志功能

        /// <summary>
        /// 注册 Unity 日志回调
        /// </summary>
        private static void EnsureLogCallbackRegistered()
        {
            if (isLogCallbackRegistered)
                return;

            Application.logMessageReceived += OnLogMessageReceived;
            isLogCallbackRegistered = true;
            ExplorerCore.Log("ParalivesRuntimeService: Unity log callback registered.");
        }

        /// <summary>
        /// Unity 日志回调处理
        /// </summary>
        private static void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            lock (logLock)
            {
                string typeStr = type.ToString().ToLower();
                
                // 检查是否可以合并（相同消息）
                if (logBuffer.Count > 0)
                {
                    LogEntry lastEntry = logBuffer[logBuffer.Count - 1];
                    if (lastEntry.Message == message && lastEntry.Type == typeStr)
                    {
                        lastEntry.CollapseCount++;
                        // 推送给订阅者
                        PushToSubscribers(lastEntry, typeStr);
                        return;
                    }
                }

                LogEntry newEntry = new LogEntry
                {
                    Id = logBuffer.Count + 1,
                    Type = typeStr,
                    Message = message,
                    StackTrace = stackTrace,
                    Timestamp = DateTime.UtcNow,
                    CollapseCount = 1
                };
                
                logBuffer.Add(newEntry);

                // 限制缓冲区大小
                while (logBuffer.Count > MaxLogBufferSize)
                {
                    logBuffer.RemoveAt(0);
                }

                // 推送给订阅者
                PushToSubscribers(newEntry, typeStr);
            }
        }

        private static void PushToSubscribers(LogEntry entry, string typeStr)
        {
            foreach (LogSubscription subscription in subscriptions.Values)
            {
                if (subscription.Types.Contains(typeStr))
                {
                    subscription.Buffer.Add(entry);
                    while (subscription.Buffer.Count > subscription.MaxSize)
                    {
                        subscription.Buffer.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// 获取游戏日志
        /// </summary>
        private static object GetGameLogs(Dictionary<string, object> parameters)
        {
            EnsureLogCallbackRegistered();

            int limit = GetOptionalInt(parameters, "limit", 50);
            string type = GetOptionalString(parameters, "type") ?? "all";
            bool includeCollapsed = GetOptionalBool(parameters, "includeCollapsed", true);

            List<object> logs = new();
            int logCount = 0;
            int warningCount = 0;
            int exceptionCount = 0;

            lock (logLock)
            {
                IEnumerable<LogEntry> filteredLogs = type == "all"
                    ? logBuffer
                    : logBuffer.Where(l => l.Type == type);

                // .NET 3.5 兼容：手动实现 TakeLast
                List<LogEntry> logsList = new List<LogEntry>(filteredLogs);
                int startIndex = Math.Max(0, logsList.Count - limit);
                for (int i = startIndex; i < logsList.Count; i++)
                {
                    LogEntry entry = logsList[i];
                    logs.Add(new Dictionary<string, object>
                    {
                        ["id"] = entry.Id,
                        ["type"] = entry.Type,
                        ["message"] = entry.Message,
                        ["timestamp"] = entry.Timestamp.ToString("O"),
                        ["collapseCount"] = includeCollapsed ? entry.CollapseCount : 1,
                        ["stackTrace"] = string.IsNullOrEmpty(entry.StackTrace) ? null : entry.StackTrace
                    });
                }

                logCount = logBuffer.Count(l => l.Type == "log");
                warningCount = logBuffer.Count(l => l.Type == "warning");
                exceptionCount = logBuffer.Count(l => l.Type == "exception");
            }

            return new Dictionary<string, object>
            {
                ["logs"] = logs,
                ["totalCount"] = logCount + warningCount + exceptionCount,
                ["logCount"] = logCount,
                ["warningCount"] = warningCount,
                ["exceptionCount"] = exceptionCount,
                ["limit"] = limit,
                ["type"] = type
            };
        }

        /// <summary>
        /// 订阅日志
        /// </summary>
        private static object SubscribeLogs(Dictionary<string, object> parameters)
        {
            EnsureLogCallbackRegistered();

            int bufferSize = GetOptionalInt(parameters, "bufferSize", 100);
            List<object> typesArray = GetOptionalArray(parameters, "types");
            HashSet<string> types = new(StringComparer.OrdinalIgnoreCase);

            if (typesArray.Count > 0)
            {
                foreach (object typeObj in typesArray)
                {
                    if (typeObj != null)
                        types.Add(typeObj.ToString().ToLower());
                }
            }
            else
            {
                types.Add("log");
                types.Add("warning");
                types.Add("exception");
            }

            string subscriptionId = $"sub_{Guid.NewGuid():N}";

            lock (logLock)
            {
                subscriptions[subscriptionId] = new LogSubscription
                {
                    Id = subscriptionId,
                    Types = types,
                    Buffer = new List<LogEntry>(),
                    MaxSize = bufferSize,
                    CreatedAt = DateTime.UtcNow
                };
            }

            return new Dictionary<string, object>
            {
                ["subscriptionId"] = subscriptionId,
                ["status"] = "active",
                ["bufferSize"] = bufferSize,
                ["subscribedTypes"] = types.ToList()
            };
        }

        /// <summary>
        /// 轮询日志
        /// </summary>
        private static object PollLogs(Dictionary<string, object> parameters)
        {
            string subscriptionId = GetRequiredString(parameters, "subscriptionId");
            int limit = GetOptionalInt(parameters, "limit", 50);

            LogSubscription subscription;
            lock (logLock)
            {
                if (!subscriptions.TryGetValue(subscriptionId, out subscription))
                {
                    throw new McpBridgeException("not_found", $"Subscription '{subscriptionId}' not found.");
                }
            }

            List<object> logs = new();
            bool hasMore = false;

            lock (logLock)
            {
                var bufferCopy = subscription.Buffer.ToList();
                subscription.Buffer.Clear();

                foreach (var entry in bufferCopy.Take(limit))
                {
                    logs.Add(new Dictionary<string, object>
                    {
                        ["id"] = entry.Id,
                        ["type"] = entry.Type,
                        ["message"] = entry.Message,
                        ["timestamp"] = entry.Timestamp.ToString("O"),
                        ["collapseCount"] = entry.CollapseCount,
                        ["stackTrace"] = string.IsNullOrEmpty(entry.StackTrace) ? null : entry.StackTrace
                    });
                }

                hasMore = bufferCopy.Count > limit;
            }

            return new Dictionary<string, object>
            {
                ["logs"] = logs,
                ["hasMore"] = hasMore,
                ["nextPollToken"] = DateTime.UtcNow.Ticks.ToString()
            };
        }

        #endregion

        #region 辅助方法

        private static GameObject FindGameObjectByName(string name)
        {
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (go && go.name == name)
                    return go;
            }
            return null;
        }

        private static Component FindComponentByName(GameObject go, string componentName)
        {
            foreach (Component component in go.GetComponents<Component>())
            {
                if (!component)
                    continue;

                Type type = component.GetActualType();
                if (type.Name == componentName || type.FullName == componentName)
                    return component;
            }
            return null;
        }

        private static bool ReadMemberBool(object owner, Type type, string memberName, bool defaultValue)
        {
            try
            {
                object value = ReadMemberSafe(owner, type, memberName);
                if (value is bool boolValue)
                    return boolValue;
            }
            catch
            {
            }
            return defaultValue;
        }

        private static int ReadMemberInt(object owner, Type type, string memberName, int defaultValue)
        {
            try
            {
                object value = ReadMemberSafe(owner, type, memberName);
                if (value is int intValue)
                    return intValue;
                if (value != null)
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
            }
            return defaultValue;
        }

        private static string ReadMemberString(object owner, Type type, string memberName, string defaultValue)
        {
            try
            {
                object value = ReadMemberSafe(owner, type, memberName);
                if (value != null)
                    return value.ToString();
            }
            catch
            {
            }
            return defaultValue;
        }

        private static object ReadMemberSafe(object owner, Type type, string memberName)
        {
            PropertyInfo property = type.GetProperty(memberName, ReflectionUtility.FLAGS);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(owner, null);

            FieldInfo field = type.GetField(memberName, ReflectionUtility.FLAGS);
            if (field != null)
                return field.GetValue(owner);

            return null;
        }

        private static object GetSingletonInstance(Type type)
        {
            if (type == null)
                return null;

            foreach (string memberName in new[] { "Instance", "_instance", "instance", "<Instance>k__BackingField" })
            {
                try
                {
                    object value = ReadMemberSafe(null, type, memberName);
                    if (value != null)
                        return value;
                }
                catch
                {
                }
            }

            try
            {
                object lazy = ReadMemberSafe(null, type, "lazy");
                if (lazy != null)
                {
                    PropertyInfo valueProperty = lazy.GetType().GetProperty("Value", ReflectionUtility.FLAGS);
                    object value = valueProperty?.GetValue(lazy, null);
                    if (value != null)
                        return value;
                }
            }
            catch
            {
            }

            return UnityEngine.Object.FindObjectOfType(type);
        }

        private static string GetRequiredString(Dictionary<string, object> parameters, string name)
        {
            if (!parameters.TryGetValue(name, out object value) || value == null)
                throw new McpBridgeException("invalid_request", $"'{name}' is required.");
            return value.ToString();
        }

        private static string GetOptionalString(Dictionary<string, object> parameters, string name)
        {
            return parameters.TryGetValue(name, out object value) && value != null ? value.ToString() : null;
        }

        private static int GetOptionalInt(Dictionary<string, object> parameters, string name, int fallback)
        {
            return parameters.TryGetValue(name, out object value) && value != null
                ? Convert.ToInt32(value, CultureInfo.InvariantCulture)
                : fallback;
        }

        private static bool GetOptionalBool(Dictionary<string, object> parameters, string name, bool fallback)
        {
            return parameters.TryGetValue(name, out object value) && value != null
                ? Convert.ToBoolean(value, CultureInfo.InvariantCulture)
                : fallback;
        }

        private static List<object> GetOptionalArray(Dictionary<string, object> parameters, string name)
        {
            return parameters.TryGetValue(name, out object value) && value is List<object> list
                ? list
                : new List<object>();
        }

        #endregion
    }
}
#endif
