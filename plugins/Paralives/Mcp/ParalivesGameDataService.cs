#if MONO
namespace CinematicUnityExplorer.Plugins.Paralives.Mcp
{
    /// <summary>
    /// 提供 Paralives 游戏数据查询服务（技能、情绪、记忆、目标）
    /// </summary>
    internal static class ParalivesGameDataService
    {
        private static readonly Dictionary<string, Func<Dictionary<string, object>, object>> actionHandlers = new()
        {
            ["paralives_get_skill_data"] = GetSkillData,
            ["paralives_get_emotion_data"] = GetEmotionData,
            ["paralives_get_memory_data"] = GetMemoryData,
            ["paralives_get_goals_data"] = GetGoalsData
        };

        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = BuildActions();

        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            if (actionHandlers.TryGetValue(action, out Func<Dictionary<string, object>, object> handler))
                return handler(parameters);

            throw new McpBridgeException("invalid_request", $"Unknown game data action '{action}'.");
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

        #region 技能数据

        /// <summary>
        /// 获取技能数据
        /// </summary>
        private static object GetSkillData(Dictionary<string, object> parameters)
        {
            // 从 UISkillsInProgressAndUpcomingEvents 面板读取技能数据
            GameObject uiSkills = ParalivesUiQuery.FindUiRoot("UISkillsInProgressAndUpcomingEvents");
            if (uiSkills == null || !uiSkills.activeInHierarchy)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "Skills UI not visible" };

            List<object> skills = new();
            FindSkillsInChildren(uiSkills.transform, skills);

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["source"] = "UISkillsInProgressAndUpcomingEvents",
                ["skills"] = skills,
                ["count"] = skills.Count
            };
        }

        /// <summary>
        /// 递归查找技能项
        /// </summary>
        private static void FindSkillsInChildren(Transform parent, List<object> skills)
        {
            ParalivesUiQuery.VisitDescendants(parent, child =>
            {
                if (child.name.Contains("SkillItem") && !child.name.Contains("Clone"))
                {
                    Dictionary<string, object> skillInfo = new()
                    {
                        ["name"] = child.name,
                        ["path"] = UnityObjectSummary.GetPath(child.gameObject),
                        ["active"] = child.gameObject.activeInHierarchy
                    };

                    TryExtractSkillDetails(child.gameObject, skillInfo);
                    skills.Add(skillInfo);
                }
            }, () => skills.Count >= 20, 10);
        }

        /// <summary>
        /// 提取技能详情
        /// </summary>
        private static void TryExtractSkillDetails(GameObject go, Dictionary<string, object> skillInfo)
        {
            foreach (Component component in go.GetComponentsInChildren<Component>(true))
            {
                if (!component)
                    continue;

                Type type = component.GetActualType();

                // 读取技能名称
                if (type.Name == "TextMeshProUGUI")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "text", out object textValue))
                    {
                        string text = textValue?.ToString();
                        if (!string.IsNullOrEmpty(text) && text.Length < 50)
                        {
                            string path = UnityObjectSummary.GetPath(component.gameObject);
                            if (path.Contains("LabelSkillName"))
                                skillInfo["skillName"] = text;
                            else if (path.Contains("LabelSkillLevel"))
                                skillInfo["skillLevel"] = text;
                        }
                    }
                }

                // 读取技能翻译键
                if (type.Name == "TranslatedText")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "Key", out object keyValue))
                    {
                        string key = keyValue?.ToString();
                        if (!string.IsNullOrEmpty(key) && key.Contains("Skill"))
                            skillInfo["translationKey"] = key;
                    }
                }

                // 读取进度条
                if (type.Name == "UISkillItemProgressBar")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "FillAmount", out object fillValue))
                    {
                        float fill = Convert.ToSingle(fillValue);
                        skillInfo["progress"] = Math.Round(fill, 2);
                        skillInfo["progressPercent"] = Math.Round(fill * 100, 0);
                    }
                }
            }
        }

        #endregion

        #region 情绪数据

        /// <summary>
        /// 获取情绪数据
        /// </summary>
        private static object GetEmotionData(Dictionary<string, object> parameters)
        {
            // 从 UIThoughts 面板的 UIEmotions2 部分读取情绪数据
            GameObject uiThoughts = ParalivesUiQuery.FindUiRoot("UIThoughts");
            if (uiThoughts == null || !uiThoughts.activeInHierarchy)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "UIThoughts not visible" };

            List<object> emotions = new();
            FindEmotionsInChildren(uiThoughts.transform, emotions);

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["source"] = "UIThoughts/Emotions",
                ["emotions"] = emotions,
                ["count"] = emotions.Count
            };
        }

        /// <summary>
        /// 递归查找情绪项
        /// </summary>
        private static void FindEmotionsInChildren(Transform parent, List<object> emotions)
        {
            ParalivesUiQuery.VisitDescendants(parent, child =>
            {
                if (child.name.Contains("EmotionsItem") && child.gameObject.activeInHierarchy)
                {
                    Dictionary<string, object> emotionInfo = new()
                    {
                        ["name"] = child.name,
                        ["path"] = UnityObjectSummary.GetPath(child.gameObject),
                        ["active"] = child.gameObject.activeInHierarchy
                    };

                    TryExtractEmotionDetails(child.gameObject, emotionInfo);
                    
                    // 只添加有实际数据的情绪项
                    if (emotionInfo.ContainsKey("emotionName") || emotionInfo.ContainsKey("translationKey"))
                        emotions.Add(emotionInfo);
                }
            }, () => emotions.Count >= 10, 10);
        }

        /// <summary>
        /// 提取情绪详情
        /// </summary>
        private static void TryExtractEmotionDetails(GameObject go, Dictionary<string, object> emotionInfo)
        {
            foreach (Component component in go.GetComponentsInChildren<Component>(true))
            {
                if (!component)
                    continue;

                Type type = component.GetActualType();

                // 读取情绪名称
                if (type.Name == "TextMeshProUGUI")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "text", out object textValue))
                    {
                        string text = textValue?.ToString();
                        if (!string.IsNullOrEmpty(text) && text.Length < 30)
                        {
                            string path = UnityObjectSummary.GetPath(component.gameObject);
                            if (path.Contains("LabelName"))
                                emotionInfo["emotionName"] = text;
                            else if (path.Contains("LabelValue"))
                                emotionInfo["emotionValue"] = text;
                        }
                    }
                }

                // 读取情绪翻译键
                if (type.Name == "TranslatedText")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "Key", out object keyValue))
                    {
                        string key = keyValue?.ToString();
                        if (!string.IsNullOrEmpty(key) && key.Contains("Emotion"))
                        {
                            emotionInfo["translationKey"] = key;
                            emotionInfo["emotionType"] = InferEmotionType(key);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 推断情绪类型
        /// </summary>
        private static string InferEmotionType(string key)
        {
            if (key.Contains("Happy") || key.Contains("Joy"))
                return "positive";
            if (key.Contains("Sad") || key.Contains("Unhappy"))
                return "negative";
            if (key.Contains("Angry") || key.Contains("Mad"))
                return "negative";
            if (key.Contains("Scared") || key.Contains("Fear"))
                return "negative";
            if (key.Contains("Excited"))
                return "positive";
            if (key.Contains("Calm") || key.Contains("Relax"))
                return "neutral";
            return "unknown";
        }

        #endregion

        #region 记忆数据

        /// <summary>
        /// 获取记忆数据
        /// </summary>
        private static object GetMemoryData(Dictionary<string, object> parameters)
        {
            // 记忆数据通常通过 MemoryManager 管理
            Type memoryManagerType = ReflectionUtility.GetTypeByName("MemoryManager");
            if (memoryManagerType == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "MemoryManager type not found" };

            object memoryManager = UnityReflectionUtility.GetSingletonInstance(memoryManagerType);
            if (memoryManager == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "MemoryManager instance not found" };

            // 尝试读取记忆列表
            List<object> memories = new();
            TryReadMemoryCollection(memoryManager, memoryManagerType, memories);

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["source"] = "MemoryManager",
                ["memories"] = memories,
                ["count"] = memories.Count,
                ["note"] = "Memory data is limited in Release build"
            };
        }

        /// <summary>
        /// 尝试读取记忆集合
        /// </summary>
        private static void TryReadMemoryCollection(object manager, Type type, List<object> memories)
        {
            // 尝试常见的记忆集合属性名
            string[] collectionNames = { "Memories", "AllMemories", "MemoryList", "ActiveMemories" };
            
            foreach (string memberName in collectionNames)
            {
                if (UnityReflectionUtility.TryReadMember(manager, type, memberName, out object collection))
                {
                    if (collection is System.Collections.IEnumerable enumerable)
                    {
                        foreach (object item in enumerable)
                        {
                            if (item == null) continue;
                            
                            Type itemType = item.GetActualType();
                            Dictionary<string, object> memoryInfo = new()
                            {
                                ["type"] = itemType.FullName,
                                ["display"] = item.ToString()
                            };

                            // 尝试读取记忆属性
                            TryReadMemoryProperties(item, itemType, memoryInfo);
                            memories.Add(memoryInfo);
                            
                            if (memories.Count >= 10) return;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 尝试读取记忆属性
        /// </summary>
        private static void TryReadMemoryProperties(object item, Type type, Dictionary<string, object> memoryInfo)
        {
            string[] properties = { "Name", "Description", "Type", "Timestamp", "Importance" };
            
            foreach (string prop in properties)
            {
                if (UnityReflectionUtility.TryReadMember(item, type, prop, out object value))
                {
                    memoryInfo[prop.ToLower()] = value?.ToString();
                }
            }
        }

        #endregion

        #region 目标数据

        /// <summary>
        /// 获取目标数据
        /// </summary>
        private static object GetGoalsData(Dictionary<string, object> parameters)
        {
            // 目标数据通常通过 GoalsManager 管理
            Type goalsManagerType = ReflectionUtility.GetTypeByName("GoalsManager");
            if (goalsManagerType == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "GoalsManager type not found" };

            object goalsManager = UnityReflectionUtility.GetSingletonInstance(goalsManagerType);
            if (goalsManager == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "GoalsManager instance not found" };

            // 尝试读取目标列表
            List<object> goals = new();
            TryReadGoalsCollection(goalsManager, goalsManagerType, goals);

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["source"] = "GoalsManager",
                ["goals"] = goals,
                ["count"] = goals.Count,
                ["note"] = "Goals data is limited in Release build"
            };
        }

        /// <summary>
        /// 尝试读取目标集合
        /// </summary>
        private static void TryReadGoalsCollection(object manager, Type type, List<object> goals)
        {
            // 尝试常见的目标集合属性名
            string[] collectionNames = { "Goals", "AllGoals", "GoalList", "ActiveGoals", "Wants" };
            
            foreach (string memberName in collectionNames)
            {
                if (UnityReflectionUtility.TryReadMember(manager, type, memberName, out object collection))
                {
                    if (collection is System.Collections.IEnumerable enumerable)
                    {
                        foreach (object item in enumerable)
                        {
                            if (item == null) continue;
                            
                            Type itemType = item.GetActualType();
                            Dictionary<string, object> goalInfo = new()
                            {
                                ["type"] = itemType.FullName,
                                ["display"] = item.ToString()
                            };

                            // 尝试读取目标属性
                            TryReadGoalProperties(item, itemType, goalInfo);
                            goals.Add(goalInfo);
                            
                            if (goals.Count >= 10) return;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 尝试读取目标属性
        /// </summary>
        private static void TryReadGoalProperties(object item, Type type, Dictionary<string, object> goalInfo)
        {
            string[] properties = { "Name", "Description", "Priority", "Status", "Progress" };
            
            foreach (string prop in properties)
            {
                if (UnityReflectionUtility.TryReadMember(item, type, prop, out object value))
                {
                    goalInfo[prop.ToLower()] = value?.ToString();
                }
            }
        }

        #endregion
    }
}
#endif
