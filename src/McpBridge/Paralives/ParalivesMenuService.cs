#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesMenuService
    {
        private static readonly Dictionary<string, string> mainMenuActionButtons = new(StringComparer.OrdinalIgnoreCase)
        {
            ["continue_game"] = "ButtonContinueGame",
            ["new_game"] = "ButtonNewGame",
            ["load_game_menu"] = "ButtonLoadGame",
            ["mod_editor"] = "ButtonModEditor",
            ["options"] = "ButtonOptions"
        };

        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_list_main_menu_actions"] = _ => ListMainMenuActions(),
            ["paralives_invoke_main_menu_action"] = InvokeMainMenuAction,
            ["paralives_start_new_game"] = StartNewGame
        };

        internal static Dictionary<string, object> ListMainMenuActionSnapshots()
        {
            return (Dictionary<string, object>)ListMainMenuActions();
        }

        internal static Dictionary<string, object> InvokeMainMenuActionForUi(string action, bool confirmed)
        {
            Dictionary<string, object> parameters = new()
            {
                ["action"] = action,
                ["dryRun"] = !confirmed
            };
            if (confirmed)
                parameters["confirm"] = ParalivesShared.ConfirmPhrase;
            return (Dictionary<string, object>)InvokeMainMenuAction(parameters);
        }

        private static object ListMainMenuActions()
        {
            ParalivesShared.EnsureAvailable();
            List<object> actions = new();
            foreach (KeyValuePair<string, string> pair in mainMenuActionButtons)
                actions.Add(SummarizeMainMenuAction(pair.Key, pair.Value));

            return new Dictionary<string, object>
            {
                ["mainMenu"] = ParalivesShared.FindMainMenuRoot() is GameObject menu ? UnityObjectSummary.FromGameObject(menu) : null,
                ["actions"] = actions
            };
        }

        private static object InvokeMainMenuAction(Dictionary<string, object> parameters)
        {
            ParalivesShared.EnsureAvailable();
            string action = McpParameters.RequiredString(parameters, "action");
            bool dryRun = McpParameters.OptionalBool(parameters, "dryRun", true);
            bool confirmed = ParalivesShared.IsConfirmed(parameters);

            if (!mainMenuActionButtons.TryGetValue(action, out string buttonName))
                throw new McpBridgeException("validation_failed", $"Main menu action '{action}' is not whitelisted.");

            bool isStandardButton = FindButtonOrParaButton(buttonName, out Button button, out GameObject buttonObject);
            bool isParaButton = !isStandardButton && buttonObject != null;

            Dictionary<string, object> result = new()
            {
                ["operation"] = "invoke_main_menu_action",
                ["action"] = action,
                ["buttonName"] = buttonName,
                ["dryRun"] = dryRun,
                ["confirmed"] = confirmed,
                ["buttonType"] = isStandardButton ? "Button" : isParaButton ? "ParaButton" : "NotFound",
                ["button"] = buttonObject ? UnityObjectSummary.FromGameObject(buttonObject) : null,
                ["available"] = buttonObject && buttonObject.activeInHierarchy
            };

            if (!buttonObject)
                throw new McpBridgeException("not_available", $"Main menu button '{buttonName}' was not found.");
            if (!buttonObject.activeInHierarchy)
                throw new McpBridgeException("not_available", $"Main menu button '{buttonName}' is inactive.");

            if (isStandardButton)
            {
                result["interactable"] = button.interactable;
                if (!button.interactable)
                    throw new McpBridgeException("validation_failed", $"Main menu button '{buttonName}' is not interactable.");
            }
            else if (isParaButton)
            {
                Component paraButton = buttonObject.GetComponent("ParaButton");
                if (paraButton != null)
                {
                    Type paraButtonType = paraButton.GetActualType();
                    bool interactable = UnityReflectionUtility.TryReadMember(paraButton, paraButtonType, "Interactable", out object interactableValue)
                        ? (bool)interactableValue
                        : true;
                    result["interactable"] = interactable;
                    if (!interactable)
                        throw new McpBridgeException("validation_failed", $"Main menu button '{buttonName}' (ParaButton) is not interactable.");
                }
            }

            if (dryRun || !confirmed)
            {
                result["requiredConfirm"] = ParalivesShared.ConfirmPhrase;
                return result;
            }

            if (isStandardButton)
            {
                button.onClick.Invoke();
                result["invoked"] = true;
            }
            else if (isParaButton)
            {
                InvokeParaButton(buttonObject, result);
            }

            return result;
        }

        private static object StartNewGame(Dictionary<string, object> parameters)
        {
            Dictionary<string, object> actionParameters = new(parameters)
            {
                ["action"] = "new_game"
            };
            return InvokeMainMenuAction(actionParameters);
        }

        private static void InvokeParaButton(GameObject buttonObject, Dictionary<string, object> result)
        {
            Component messageEntity = buttonObject.GetComponent("ButtonCreateMessageEntity");
            if (messageEntity == null)
            {
                result["error"] = "ButtonCreateMessageEntity component not found.";
                return;
            }

            Type entityType = messageEntity.GetActualType();
            string messageComponentName = UnityReflectionUtility.TryReadMember(messageEntity, entityType, "MessageComponentName", out object msgValue)
                ? msgValue.ToString()
                : "";
            result["messageComponent"] = messageComponentName;

            Type eventSystemType = ReflectionUtility.GetTypeByName("EventSystem");
            if (eventSystemType == null)
            {
                result["error"] = "EventSystem type not found.";
                return;
            }

            Type messageType = ReflectionUtility.GetTypeByName(messageComponentName);
            if (messageType == null)
            {
                result["error"] = $"Message type '{messageComponentName}' not found.";
                return;
            }

            object message = Activator.CreateInstance(messageType);
            MethodInfo broadcast = eventSystemType.GetMethods(ReflectionUtility.FLAGS)
                .FirstOrDefault(m => m.Name == "Broadcast" && m.GetParameters().Length == 1);

            if (broadcast == null)
            {
                result["error"] = "Could not find EventSystem.Broadcast method.";
                return;
            }

            broadcast.Invoke(null, new[] { message });
            result["invoked"] = true;
        }

        private static bool FindButtonOrParaButton(string buttonName, out Button standardButton, out GameObject buttonObject)
        {
            standardButton = null;
            buttonObject = null;

            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (!go || go.name != buttonName)
                    continue;

                buttonObject = go;
                standardButton = go.GetComponent<Button>();
                if (standardButton)
                    return true;

                standardButton = go.GetComponentInChildren<Button>(true);
                if (standardButton)
                    return true;

                return false;
            }

            return false;
        }

        private static Dictionary<string, object> SummarizeMainMenuAction(string action, string buttonName)
        {
            bool isStandardButton = FindButtonOrParaButton(buttonName, out Button button, out GameObject buttonObject);
            bool isParaButton = !isStandardButton && buttonObject != null;

            bool available = buttonObject && buttonObject.activeInHierarchy;
            bool interactable = false;
            string buttonType = "NotFound";

            if (isStandardButton)
            {
                buttonType = "Button";
                interactable = button && button.interactable;
            }
            else if (isParaButton)
            {
                buttonType = "ParaButton";
                Component paraButton = buttonObject.GetComponent("ParaButton");
                if (paraButton != null)
                {
                    Type paraButtonType = paraButton.GetActualType();
                    interactable = UnityReflectionUtility.TryReadMember(paraButton, paraButtonType, "Interactable", out object interactableValue)
                        ? (bool)interactableValue
                        : true;
                }
            }

            return new Dictionary<string, object>
            {
                ["action"] = action,
                ["buttonName"] = buttonName,
                ["buttonType"] = buttonType,
                ["available"] = available,
                ["interactable"] = interactable,
                ["button"] = buttonObject ? UnityObjectSummary.FromGameObject(buttonObject) : null
            };
        }
    }
}
#endif
