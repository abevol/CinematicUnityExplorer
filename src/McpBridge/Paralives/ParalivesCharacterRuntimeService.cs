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
            foreach (Component component in go.GetComponentsInChildren<Component>(true))
            {
                if (!component)
                    continue;

                Type type = component.GetActualType();

                if (type.Name == "TranslatedText")
                {
                    if (UnityReflectionUtility.TryReadMember(component, type, "Key", out object keyValue))
                    {
                        string key = keyValue?.ToString();
                        if (!string.IsNullOrEmpty(key))
                        {
                            needInfo["translationKey"] = key;
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
