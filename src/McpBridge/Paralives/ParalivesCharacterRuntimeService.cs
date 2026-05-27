#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesCharacterRuntimeService
    {
        private static readonly Dictionary<string, Func<Dictionary<string, object>, object>> actionHandlers = new()
        {
            ["paralives_get_character_needs"] = GetCharacterNeeds,
            ["paralives_get_character_actions"] = GetCharacterActions
        };
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = BuildActions();

        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            if (actionHandlers.TryGetValue(action, out Func<Dictionary<string, object>, object> handler))
                return handler(parameters);

            throw new McpBridgeException("invalid_request", $"Unknown character runtime action '{action}'.");
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

        private static object GetCharacterNeeds(Dictionary<string, object> parameters)
        {
            string characterGuid = McpParameters.OptionalString(parameters, "characterGuid");

            if (string.IsNullOrEmpty(characterGuid))
            {
                Dictionary<string, object> activeChar = ParalivesActiveContextService.GetActiveCharacterInfo();
                if (activeChar.TryGetValue("available", out object avail) && (bool)avail)
                    return GetNeedsFromUI();

                return new Dictionary<string, object>
                {
                    ["available"] = false,
                    ["reason"] = "No character specified and no active character found"
                };
            }

            return GetCharacterNeedsByGuid(characterGuid);
        }

        private static Dictionary<string, object> GetNeedsFromUI()
        {
            GameObject uiThoughts = ParalivesUiQuery.FindUiRoot("UIThoughts");
            if (uiThoughts == null || !uiThoughts.activeInHierarchy)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "UIThoughts not visible" };

            List<object> needs = new();
            FindNeedsInChildren(uiThoughts.transform, needs);

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["source"] = "UIThoughts panel",
                ["needs"] = needs
            };
        }

        private static void FindNeedsInChildren(Transform parent, List<object> needs)
        {
            ParalivesUiQuery.VisitDescendants(parent, child =>
            {
                if (child.name.Contains("NeedItem") || child.name.Contains("UINeed") || child.name.Contains("EmotionsItem"))
                {
                    Dictionary<string, object> needInfo = new()
                    {
                        ["name"] = child.name,
                        ["path"] = UnityObjectSummary.GetPath(child.gameObject),
                        ["active"] = child.gameObject.activeInHierarchy
                    };

                    TryExtractNeedDetails(child.gameObject, needInfo);
                    needs.Add(needInfo);
                }
            }, () => needs.Count >= 20, 10);
        }

        private static void TryExtractNeedDetails(GameObject go, Dictionary<string, object> needInfo)
        {
            // 检查是否被忽略（通过 LabelIgnored 对象的可见性）
            bool isIgnored = false;
            foreach (Transform child in go.transform)
            {
                if (child.name == "LabelIgnored")
                {
                    isIgnored = child.gameObject.activeInHierarchy;
                    break;
                }
            }
            needInfo["isIgnored"] = isIgnored;

            foreach (Component component in go.GetComponentsInChildren<Component>(true))
            {
                if (!component)
                    continue;

                Type type = component.GetActualType();

                if (type.Name == "TooltipOpenerSimple" || type.Name == "TooltipOpenerNumericBreakdown")
                    TryApplyTooltipText(component, type, needInfo);

                if (type.Name == "UINeedsItem")
                    TryExtractNeedData(component, type, needInfo);

                if (type.Name == "TranslatedText")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "Key", out object keyValue))
                    {
                        string key = keyValue?.ToString();
                        if (!string.IsNullOrEmpty(key))
                        {
                            needInfo["translationKey"] = key;
                            // 只在没有从 Tooltip 获取到 needType 时才推断
                            if (!needInfo.ContainsKey("needType") || needInfo["needType"].ToString() == "UINeeds_NeedIgnored")
                                needInfo["needType"] = InferNeedTypeFromKey(key);
                        }
                    }
                }

                if (type.Name == "TextMeshProUGUI")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "text", out object textValue))
                    {
                        string text = textValue?.ToString();
                        if (!string.IsNullOrEmpty(text) && text.Length < 20)
                            needInfo["displayText"] = text;
                    }
                }

                if (type.Name == "Image")
                {
                    // 只读取 FillIcon 中的 Image 的 fillAmount
                    string componentPath = UnityObjectSummary.GetPath(component.gameObject);
                    if (componentPath.Contains("FillIcon"))
                    {
                        if (UnityReflectionUtility.TryReadMember(component, type, "fillAmount", out object fillValue))
                        {
                            float fill = Convert.ToSingle(fillValue);
                            if (fill >= 0 && fill <= 1)
                            {
                                needInfo["fillAmount"] = Math.Round(fill, 2);
                                needInfo["fillPercent"] = Math.Round(fill * 100, 0);
                            }
                        }
                    }
                }
            }

            // 计算需求状态
            CalculateNeedStatus(needInfo);
        }

        private static void CalculateNeedStatus(Dictionary<string, object> needInfo)
        {
            bool isIgnored = needInfo.ContainsKey("isIgnored") && (bool)needInfo["isIgnored"];
            float fillPercent = needInfo.ContainsKey("fillPercent") ? Convert.ToSingle(needInfo["fillPercent"]) : -1;
            float maxValue = needInfo.ContainsKey("maxValue") ? Convert.ToSingle(needInfo["maxValue"]) : 0;

            // 需求状态判断
            string status;
            if (isIgnored)
            {
                status = "ignored";
            }
            else if (fillPercent < 0)
            {
                status = "unknown";
            }
            else if (fillPercent <= 20)
            {
                status = "critical";  // 危险
            }
            else if (fillPercent <= 50)
            {
                status = "low";       // 低
            }
            else if (fillPercent <= 80)
            {
                status = "medium";    // 中等
            }
            else
            {
                status = "good";      // 良好
            }

            needInfo["status"] = status;

            // 添加人类可读的状态描述
            needInfo["statusText"] = status switch
            {
                "ignored" => "被忽略",
                "critical" => "危险",
                "low" => "偏低",
                "medium" => "中等",
                "good" => "良好",
                _ => "未知"
            };
        }

        private static void TryApplyTooltipText(Component component, Type type, Dictionary<string, object> needInfo)
        {
            if (!UnityReflectionUtility.TryReadMember(component, type, "TextToShow", out object textValue))
                return;

            string tooltipText = textValue?.ToString();
            if (string.IsNullOrEmpty(tooltipText))
                return;

            string title = ExtractTooltipTitle(tooltipText);
            if (string.IsNullOrEmpty(title))
                return;

            needInfo["needName"] = title;
            needInfo["displayText"] = title;
            needInfo["tooltipSource"] = type.Name;
            needInfo["tooltipText"] = tooltipText.Length > 300 ? tooltipText.Substring(0, 300) : tooltipText;
            
            // 从标题推断需求类型
            string inferredType = InferNeedTypeFromKey(title);
            if (!string.IsNullOrEmpty(inferredType) && inferredType != title)
                needInfo["needType"] = inferredType;
        }

        private static void TryExtractNeedData(Component component, Type type, Dictionary<string, object> needInfo)
        {
            // 读取最大值
            if (UnityReflectionUtility.TryReadMember(component, type, "_max", out object maxValue) && maxValue != null)
                needInfo["maxValue"] = Convert.ToSingle(maxValue);

            // 读取当前值（如果存在）
            if (UnityReflectionUtility.TryReadMember(component, type, "_value", out object currentValue) && currentValue != null)
                needInfo["currentValue"] = Convert.ToSingle(currentValue);

            // 读取是否被忽略
            if (UnityReflectionUtility.TryReadMember(component, type, "_isIgnored", out object isIgnoredObj))
                needInfo["isIgnoredFromComponent"] = Convert.ToBoolean(isIgnoredObj);

            // 读取需求 GUID
            if (UnityReflectionUtility.TryReadMember(component, type, "_needGUID", out object needGuidObj))
                needInfo["needGUID"] = needGuidObj?.ToString();

            // 读取需求名称键
            if (UnityReflectionUtility.TryReadMember(component, type, "_needNameKey", out object needNameKeyObj))
                needInfo["needNameKey"] = needNameKeyObj?.ToString();
        }

        private static string ExtractTooltipTitle(string tooltipText)
        {
            string plainText = StripRichTextTags(tooltipText).Trim();
            if (string.IsNullOrEmpty(plainText))
                return null;

            int end = plainText.IndexOfAny(new[] { '\r', '\n', '.', '。', ':', '：' });
            string title = end > 0 ? plainText.Substring(0, end) : plainText;
            title = title.Trim();
            return title.Length > 40 ? title.Substring(0, 40).Trim() : title;
        }

        private static string StripRichTextTags(string text)
        {
            System.Text.StringBuilder result = new();
            bool inTag = false;
            foreach (char ch in text)
            {
                if (ch == '<')
                {
                    inTag = true;
                    continue;
                }

                if (ch == '>')
                {
                    inTag = false;
                    continue;
                }

                if (!inTag)
                    result.Append(ch);
            }
            return result.ToString();
        }

        private static string InferNeedTypeFromKey(string key)
        {
            if (key.Contains("Hunger") || key.Contains("Food") || key.Contains("饥饿"))
                return "hunger";
            if (key.Contains("Hygiene") || key.Contains("Clean") || key.Contains("卫生") || key.Contains("清洁"))
                return "hygiene";
            if (key.Contains("Energy") || key.Contains("Sleep") || key.Contains("Tired") || key.Contains("精力") || key.Contains("体力") || key.Contains("活力"))
                return "energy";
            if (key.Contains("Fun") || key.Contains("Entertainment") || key.Contains("娱乐") || key.Contains("乐趣"))
                return "fun";
            if (key.Contains("Social") || key.Contains("社交"))
                return "social";
            if (key.Contains("Bladder") || key.Contains("Toilet") || key.Contains("如厕") || key.Contains("厕所") || key.Contains("膀胱"))
                return "bladder";
            if (key.Contains("Happy") || key.Contains("Emotion"))
                return "emotion";
            if (key.Contains("Comfort"))
                return "comfort";
            return key;
        }

        private static Dictionary<string, object> GetCharacterNeedsByGuid(string characterGuid)
        {
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

        private static object GetCharacterActions(Dictionary<string, object> parameters)
        {
            return GetActionsFromUI();
        }

        private static Dictionary<string, object> GetActionsFromUI()
        {
            GameObject uiInteractionQueue = ParalivesUiQuery.FindUiRoot("UIInteractionQueue");
            if (uiInteractionQueue == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "UIInteractionQueue not found" };

            List<object> actions = new();
            FindActionsInChildren(uiInteractionQueue.transform, actions);

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["source"] = "UIInteractionQueue",
                ["actions"] = actions
            };
        }

        private static void FindActionsInChildren(Transform parent, List<object> actions)
        {
            ParalivesUiQuery.VisitDescendants(parent, child =>
            {
                if (child.name.Contains("QueueItem") || child.name.Contains("Interaction"))
                {
                    Dictionary<string, object> actionInfo = new()
                    {
                        ["name"] = child.name,
                        ["path"] = UnityObjectSummary.GetPath(child.gameObject),
                        ["active"] = child.gameObject.activeInHierarchy
                    };

                    TryExtractActionDetails(child.gameObject, actionInfo);
                    actions.Add(actionInfo);
                }
            }, () => actions.Count >= 20, 10);
        }

        private static void TryExtractActionDetails(GameObject go, Dictionary<string, object> actionInfo)
        {
            foreach (Component component in go.GetComponentsInChildren<Component>(true))
            {
                if (!component)
                    continue;

                Type type = component.GetActualType();

                if (type.Name == "TextMeshProUGUI")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "text", out object textValue))
                    {
                        string text = textValue?.ToString();
                        if (!string.IsNullOrEmpty(text) && text.Length < 50)
                        {
                            string path = UnityObjectSummary.GetPath(component.gameObject);
                            if (path.Contains("LabelInteractionName") || path.Contains("LabelName"))
                                actionInfo["interactionName"] = text;
                        }
                    }
                }

                if (type.Name == "TranslatedText")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "Key", out object keyValue))
                    {
                        string key = keyValue?.ToString();
                        if (!string.IsNullOrEmpty(key) && key.Contains("Interaction"))
                            actionInfo["translationKey"] = key;
                    }
                }

                if (component.gameObject.name == "ImageIsInteractionRunning")
                    actionInfo["isRunning"] = component.gameObject.activeInHierarchy;
            }
        }
    }
}
#endif
