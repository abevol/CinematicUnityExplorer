#if MONO
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesBridgeService
    {
        internal const string ConfirmPhrase = "CONFIRM_PARALIVES_WRITE";
        private const int MaxListedFiles = 200;
        private const int MaxListedSavedGames = 100;
        private static readonly Dictionary<string, string> mainMenuActionButtons = new(StringComparer.OrdinalIgnoreCase)
        {
            ["continue_game"] = "ButtonContinueGame",
            ["new_game"] = "ButtonNewGame",
            ["load_game_menu"] = "ButtonLoadGame",
            ["mod_editor"] = "ButtonModEditor",
            ["options"] = "ButtonOptions"
        };
        private static readonly HashSet<string> allowedCheats = new(StringComparer.OrdinalIgnoreCase)
        {
            "UNITYOBJECTCOUNT",
            "ASSETCOUNT",
            "ASSETCOUNTBYSIZE",
            "SHOWANIMATIONS",
            "SHOWANIMATIONSCONTAINERS"
        };

        private static bool initialized;
        private static string managedPath;
        private static string rootPath;
        private static string mainModPath;
        private static string paralivesAssemblyPath;
        private static ParalivesTypeIndex typeIndex;

        public static bool IsAvailable
        {
            get
            {
                EnsureInitialized();
                return File.Exists(paralivesAssemblyPath);
            }
        }

        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            EnsureAvailable();
            return action switch
            {
                "paralives_get_type_index" => GetTypeIndex(),
                "paralives_get_game_state" => GetGameState(),
                "paralives_list_main_menu_actions" => ListMainMenuActions(),
                "paralives_invoke_main_menu_action" => InvokeMainMenuAction(parameters),
                "paralives_list_saved_games" => ListSavedGames(parameters),
                "paralives_load_saved_game" => LoadSavedGame(parameters),
                "paralives_start_new_game" => StartNewGame(parameters),
                "paralives_get_loading_state" => GetLoadingState(),
                "paralives_list_content_mods" => ListContentMods(),
                "paralives_inspect_content_mod" => InspectContentMod(parameters),
                "paralives_create_content_mod" => CreateContentMod(parameters),
                "paralives_import_asset_to_mod" => ImportAssetToMod(parameters),
                "paralives_list_characters" => ListManagerCollection("CharacterManager", "Characters"),
                "paralives_list_households" => ListManagerCollection("HouseholdManager", "AllHouseholds"),
                "paralives_list_lots" => ListManagerCollection("LotManager", "Lots"),
                "paralives_set_need_value" => SetNeedValue(parameters),
                "paralives_list_cheat_commands" => ListCheatCommands(),
                "paralives_run_whitelisted_cheat" => RunWhitelistedCheat(parameters),
                _ => throw new McpBridgeException("invalid_request", $"Unknown Paralives bridge action '{action}'.")
            };
        }

        public static object ReadResource(string uri, Dictionary<string, object> parameters)
        {
            EnsureAvailable();
            return uri switch
            {
                "paralives://types/managers" => new Dictionary<string, object> { ["types"] = typeIndex.Managers },
                "paralives://types/settings" => new Dictionary<string, object> { ["types"] = typeIndex.Settings },
                "paralives://types/cheats" => new Dictionary<string, object> { ["types"] = typeIndex.Cheats },
                _ => throw new McpBridgeException("invalid_request", $"Unknown Paralives resource '{uri}'.")
            };
        }

        internal static Dictionary<string, object> GetGameStateSnapshot()
        {
            EnsureAvailable();
            return (Dictionary<string, object>)GetGameState();
        }

        internal static Dictionary<string, object> GetLoadingStateSnapshot()
        {
            EnsureAvailable();
            return (Dictionary<string, object>)GetLoadingState();
        }

        internal static Dictionary<string, object> ListMainMenuActionSnapshots()
        {
            EnsureAvailable();
            return (Dictionary<string, object>)ListMainMenuActions();
        }

        internal static Dictionary<string, object> InvokeMainMenuActionForUi(string action, bool confirmed)
        {
            EnsureAvailable();
            Dictionary<string, object> parameters = new()
            {
                ["action"] = action,
                ["dryRun"] = !confirmed
            };
            if (confirmed)
                parameters["confirm"] = ConfirmPhrase;
            return (Dictionary<string, object>)InvokeMainMenuAction(parameters);
        }

        internal static Dictionary<string, object> ListSavedGamesForUi(int limit)
        {
            EnsureAvailable();
            return (Dictionary<string, object>)ListSavedGames(new Dictionary<string, object> { ["limit"] = limit });
        }

        internal static Dictionary<string, object> LoadSavedGameForUi(string argumentName, string argumentValue, bool confirmed)
        {
            EnsureAvailable();
            Dictionary<string, object> parameters = new()
            {
                [argumentName] = argumentValue,
                ["dryRun"] = !confirmed
            };
            if (confirmed)
                parameters["confirm"] = ConfirmPhrase;
            return (Dictionary<string, object>)LoadSavedGame(parameters);
        }

        private static object GetTypeIndex()
        {
            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["rootPath"] = rootPath,
                ["mainModPath"] = mainModPath,
                ["index"] = typeIndex.ToSummary()
            };
        }

        private static object GetGameState()
        {
            GameObject mainMenu = FindMainMenuRoot();
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

            bool isMainMenu = mainMenu && mainMenu.activeInHierarchy;
            bool isLoading = loadingState.TryGetValue("isLoadingInferred", out object loadingValue) && loadingValue is bool loadingBool && loadingBool;
            string inferredMode = isLoading ? "loading" : isMainMenu ? "main_menu" : "in_game_or_editor";

            return new Dictionary<string, object>
            {
                ["mode"] = inferredMode,
                ["isMainMenu"] = isMainMenu,
                ["mainMenu"] = mainMenu ? SummarizeGameObject(mainMenu) : null,
                ["scenes"] = scenes,
                ["activeUiRoots"] = GetActiveUiRoots(30),
                ["loading"] = loadingState,
                ["savedGameManager"] = SummarizeManager("SavedGameManager", new[] { "CurrentSavedGame", "CurrentSave", "LoadedGame", "IsGameLoaded", "HasLoadedGame" }),
                ["gameLoadingManager"] = SummarizeManager("GameLoadingManager", new[] { "State", "CurrentState", "IsLoading", "Progress" })
            };
        }

        private static object ListMainMenuActions()
        {
            List<object> actions = new();
            foreach (KeyValuePair<string, string> pair in mainMenuActionButtons)
                actions.Add(SummarizeMainMenuAction(pair.Key, pair.Value));

            return new Dictionary<string, object>
            {
                ["mainMenu"] = FindMainMenuRoot() is GameObject menu ? SummarizeGameObject(menu) : null,
                ["actions"] = actions
            };
        }

        private static object InvokeMainMenuAction(Dictionary<string, object> parameters)
        {
            string action = GetRequiredString(parameters, "action");
            bool dryRun = GetOptionalBool(parameters, "dryRun", true);
            bool confirmed = IsConfirmed(parameters);

            if (!mainMenuActionButtons.TryGetValue(action, out string buttonName))
                throw new McpBridgeException("validation_failed", $"Main menu action '{action}' is not whitelisted.");

            // 查找按钮（支持标准 Button 和 ParaButton）
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
                ["button"] = buttonObject ? SummarizeGameObject(buttonObject) : null,
                ["available"] = buttonObject && buttonObject.activeInHierarchy
            };

            if (!buttonObject)
                throw new McpBridgeException("not_available", $"Main menu button '{buttonName}' was not found.");
            if (!buttonObject.activeInHierarchy)
                throw new McpBridgeException("not_available", $"Main menu button '{buttonName}' is inactive.");

            // 检查是否可交互
            if (isStandardButton)
            {
                result["interactable"] = button.interactable;
                if (!button.interactable)
                    throw new McpBridgeException("validation_failed", $"Main menu button '{buttonName}' is not interactable.");
            }
            else if (isParaButton)
            {
                // ParaButton 使用 ParaButton 组件
                Component paraButton = buttonObject.GetComponent("ParaButton");
                if (paraButton != null)
                {
                    Type paraButtonType = paraButton.GetActualType();
                    bool interactable = TryReadMember(paraButton, paraButtonType, "Interactable", out object interactableValue) 
                        ? (bool)interactableValue 
                        : true;
                    result["interactable"] = interactable;
                    if (!interactable)
                        throw new McpBridgeException("validation_failed", $"Main menu button '{buttonName}' (ParaButton) is not interactable.");
                }
            }

            if (dryRun || !confirmed)
            {
                result["requiredConfirm"] = ConfirmPhrase;
                return result;
            }

            // 执行按钮点击
            if (isStandardButton)
            {
                button.onClick.Invoke();
                result["invoked"] = true;
            }
            else if (isParaButton)
            {
                // ParaButton 使用 ButtonCreateMessageEntity 组件
                Component messageEntity = buttonObject.GetComponent("ButtonCreateMessageEntity");
                if (messageEntity != null)
                {
                    Type entityType = messageEntity.GetActualType();
                    string messageComponentName = TryReadMember(messageEntity, entityType, "MessageComponentName", out object msgValue) 
                        ? msgValue.ToString() 
                        : "";
                    result["messageComponent"] = messageComponentName;
                    
                    // 通过 EventSystem 广播消息
                    Type eventSystemType = ReflectionUtility.GetTypeByName("EventSystem");
                    if (eventSystemType != null)
                    {
                        // 查找消息类型
                        Type messageType = ReflectionUtility.GetTypeByName(messageComponentName);
                        if (messageType != null)
                        {
                            object message = Activator.CreateInstance(messageType);
                            
                            // 查找 Broadcast 方法
                            MethodInfo broadcast = eventSystemType.GetMethods(ReflectionUtility.FLAGS)
                                .FirstOrDefault(m => m.Name == "Broadcast" && m.GetParameters().Length == 1);
                            
                            if (broadcast != null)
                            {
                                broadcast.Invoke(null, new[] { message });
                                result["invoked"] = true;
                            }
                            else
                            {
                                result["error"] = "Could not find EventSystem.Broadcast method.";
                            }
                        }
                        else
                        {
                            result["error"] = $"Message type '{messageComponentName}' not found.";
                        }
                    }
                    else
                    {
                        result["error"] = "EventSystem type not found.";
                    }
                }
                else
                {
                    result["error"] = "ButtonCreateMessageEntity component not found.";
                }
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

        private static object GetLoadingState()
        {
            return BuildLoadingState();
        }

        private static object ListSavedGames(Dictionary<string, object> parameters)
        {
            int defaultLimit = UnityExplorer.Config.ConfigManager.Paralives_SavedGameListLimit != null
                ? UnityExplorer.Config.ConfigManager.Paralives_SavedGameListLimit.Value
                : 50;
            int limit = Clamp(GetOptionalInt(parameters, "limit", defaultLimit), 1, MaxListedSavedGames);
            List<object> managerItems = TryListSavedGamesFromManager(limit);
            List<object> files = ListSavedGameFiles(limit);

            return new Dictionary<string, object>
            {
                ["manager"] = SummarizeManager("SavedGameManager", new[] { "CurrentSavedGame", "CurrentSave", "LoadedGame", "IsGameLoaded", "HasLoadedGame" }),
                ["managerItems"] = managerItems,
                ["files"] = files,
                ["limit"] = limit,
                ["truncated"] = managerItems.Count >= limit || files.Count >= limit,
                ["persistentDataPath"] = Application.persistentDataPath
            };
        }

        private static object LoadSavedGame(Dictionary<string, object> parameters)
        {
            string savePath = GetOptionalString(parameters, "savePath");
            string saveName = GetOptionalString(parameters, "saveName");
            string saveId = GetOptionalString(parameters, "saveId");
            string saveArgument = savePath ?? saveName ?? saveId;
            bool dryRun = GetOptionalBool(parameters, "dryRun", true);
            bool confirmed = IsConfirmed(parameters);

            if (string.IsNullOrEmpty(saveArgument))
                throw new McpBridgeException("invalid_request", "One of 'savePath', 'saveName', or 'saveId' is required.");

            Type managerType = ReflectionUtility.GetTypeByName("SavedGameManager");
            object manager = GetSingletonInstance(managerType);
            List<object> candidateMethods = ListLoadSavedGameMethods(managerType, saveArgument);

            Dictionary<string, object> result = new()
            {
                ["operation"] = "load_saved_game",
                ["saveArgument"] = saveArgument,
                ["dryRun"] = dryRun,
                ["confirmed"] = confirmed,
                ["managerAvailable"] = manager != null,
                ["candidateMethods"] = candidateMethods
            };

            if (dryRun || !confirmed)
            {
                result["requiredConfirm"] = ConfirmPhrase;
                return result;
            }

            if (managerType == null)
                throw new McpBridgeException("not_available", "SavedGameManager type is not available.");

            MethodInfo method = ResolveSavedGameLoadMethod(managerType, saveArgument, out object[] arguments);
            if (method == null)
                throw new McpBridgeException("method_not_found", "No supported SavedGameManager load method was found. Use Paralives:invoke_main_menu_action with load_game_menu as a UI fallback.");
            if (!method.IsStatic && manager == null)
                throw new McpBridgeException("not_available", "SavedGameManager singleton is not available.");

            method.Invoke(method.IsStatic ? null : manager, arguments);
            result["invoked"] = true;
            result["method"] = method.Name;
            return result;
        }

        private static object ListContentMods()
        {
            List<object> mods = new();
            foreach (string metaPath in Directory.GetFiles(rootPath, "*.mod.meta", SearchOption.AllDirectories))
            {
                string modPath = Path.GetDirectoryName(metaPath);
                Dictionary<string, string> meta = ReadMetaFile(metaPath);
                mods.Add(new Dictionary<string, object>
                {
                    ["path"] = modPath,
                    ["folderName"] = Path.GetFileName(modPath),
                    ["metaPath"] = metaPath,
                    ["guid"] = GetMetaValue(meta, "GUID"),
                    ["modName"] = GetMetaValue(meta, "ModName"),
                    ["enabled"] = GetMetaValue(meta, "Enabled"),
                    ["isMainMod"] = string.Equals(modPath, mainModPath, StringComparison.OrdinalIgnoreCase)
                });
            }
            return new Dictionary<string, object> { ["mods"] = mods };
        }

        private static object InspectContentMod(Dictionary<string, object> parameters)
        {
            string modPath = ResolveModPath(GetRequiredString(parameters, "modPath"));
            int limit = Clamp(GetOptionalInt(parameters, "limit", 100), 1, MaxListedFiles);
            List<object> files = new();

            foreach (string file in Directory.GetFiles(modPath, "*", SearchOption.AllDirectories).Take(limit))
            {
                FileInfo info = new(file);
                string relative = MakeRelativePath(modPath, file);
                Dictionary<string, object> item = new()
                {
                    ["relativePath"] = relative,
                    ["extension"] = info.Extension,
                    ["length"] = info.Length,
                    ["lastWriteTimeUtc"] = info.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture)
                };

                if (info.Extension.Equals(".meta", StringComparison.OrdinalIgnoreCase))
                    item["meta"] = ReadMetaFile(file);

                files.Add(item);
            }

            return new Dictionary<string, object>
            {
                ["modPath"] = modPath,
                ["files"] = files,
                ["truncated"] = files.Count >= limit
            };
        }

        private static object CreateContentMod(Dictionary<string, object> parameters)
        {
            string modName = SanitizeModName(GetRequiredString(parameters, "modName"));
            bool dryRun = GetOptionalBool(parameters, "dryRun", true);
            bool confirmed = IsConfirmed(parameters);
            string targetPath = Path.Combine(rootPath, modName + ".mod");

            Dictionary<string, object> result = new()
            {
                ["operation"] = "create_content_mod",
                ["dryRun"] = dryRun,
                ["confirmed"] = confirmed,
                ["targetPath"] = targetPath
            };

            if (Directory.Exists(targetPath))
                throw new McpBridgeException("validation_failed", $"Content mod already exists: {targetPath}");

            if (dryRun || !confirmed)
            {
                result["wouldCreate"] = new List<object>
                {
                    targetPath,
                    Path.Combine(targetPath, modName + ".mod.meta")
                };
                result["requiredConfirm"] = ConfirmPhrase;
                return result;
            }

            Directory.CreateDirectory(targetPath);
            string metaPath = Path.Combine(targetPath, modName + ".mod.meta");
            File.WriteAllText(metaPath, BuildModMeta(modName), Encoding.UTF8);

            result["created"] = true;
            result["metaPath"] = metaPath;
            return result;
        }

        private static object ImportAssetToMod(Dictionary<string, object> parameters)
        {
            string sourcePath = Path.GetFullPath(GetRequiredString(parameters, "sourcePath"));
            string modPath = ResolveModPath(GetRequiredString(parameters, "modPath"));
            string subFolder = NormalizeRelativePath(GetOptionalString(parameters, "subFolder") ?? "");
            bool dryRun = GetOptionalBool(parameters, "dryRun", true);
            bool confirmed = IsConfirmed(parameters);

            if (!File.Exists(sourcePath))
                throw new McpBridgeException("validation_failed", $"Source file does not exist: {sourcePath}");

            string destinationFolder = Path.Combine(modPath, subFolder);
            string destinationPath = Path.Combine(destinationFolder, Path.GetFileName(sourcePath));
            string metaPath = destinationPath + ".meta";

            Dictionary<string, object> result = new()
            {
                ["operation"] = "import_asset_to_mod",
                ["dryRun"] = dryRun,
                ["confirmed"] = confirmed,
                ["sourcePath"] = sourcePath,
                ["destinationPath"] = destinationPath,
                ["metaPath"] = metaPath,
                ["sha1"] = ComputeSha1(sourcePath)
            };

            if (dryRun || !confirmed)
            {
                result["requiredConfirm"] = ConfirmPhrase;
                return result;
            }

            Directory.CreateDirectory(destinationFolder);
            File.Copy(sourcePath, destinationPath, false);
            File.WriteAllText(metaPath, BuildAssetMeta(sourcePath), Encoding.UTF8);
            result["imported"] = true;
            return result;
        }

        private static object ListManagerCollection(string managerTypeName, string memberName)
        {
            Type managerType = ReflectionUtility.GetTypeByName(managerTypeName);
            object manager = GetSingletonInstance(managerType);
            if (manager == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = $"{managerTypeName}.Instance is not available." };

            object collection = ReadMember(manager, managerType, memberName);
            List<object> items = new();
            if (collection is System.Collections.IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (item == null)
                        continue;
                    items.Add(SummarizeDomainObject(item));
                    if (items.Count >= 200)
                        break;
                }
            }

            return new Dictionary<string, object>
            {
                ["manager"] = managerTypeName,
                ["member"] = memberName,
                ["items"] = items,
                ["truncated"] = items.Count >= 200
            };
        }

        private static object ListCheatCommands()
        {
            List<object> methods = typeIndex.Cheats
                .Where(type => string.Equals(type["name"]?.ToString(), "ProcessCheatCommandEvent", StringComparison.Ordinal))
                .SelectMany(type => type["methods"] as List<object> ?? new List<object>())
                .Where(method => method is Dictionary<string, object> dict && allowedCheats.Contains(dict["name"]?.ToString() ?? ""))
                .ToList();

            return new Dictionary<string, object>
            {
                ["allowedCheats"] = methods,
                ["policy"] = "Only read-only diagnostic cheats are exposed. Add explicit whitelist entries in ParalivesBridgeService for more commands."
            };
        }

        private static object SetNeedValue(Dictionary<string, object> parameters)
        {
            ulong characterGuid = GetRequiredUInt64(parameters, "characterGuid");
            ulong needGuid = GetRequiredUInt64(parameters, "needGuid");
            float value = Convert.ToSingle(GetRequiredString(parameters, "value"), CultureInfo.InvariantCulture);
            bool force = GetOptionalBool(parameters, "force", true);
            bool dryRun = GetOptionalBool(parameters, "dryRun", true);
            bool confirmed = IsConfirmed(parameters);

            Type characterManagerType = ReflectionUtility.GetTypeByName("CharacterManager");
            object characterManager = GetSingletonInstance(characterManagerType);
            if (characterManager == null)
                throw new McpBridgeException("not_available", "CharacterManager.Instance is not available.");

            MethodInfo getCharacter = characterManagerType.GetMethod("GetCharacterByGUID", ReflectionUtility.FLAGS);
            if (getCharacter == null)
                throw new McpBridgeException("method_not_found", "CharacterManager.GetCharacterByGUID was not found.");

            object character = getCharacter.Invoke(characterManager, new object[] { characterGuid });
            if (character == null)
                throw new McpBridgeException("validation_failed", $"Character {characterGuid} was not found.");

            Type needManagerType = ReflectionUtility.GetTypeByName("NeedManager");
            object needManager = GetSingletonInstance(needManagerType);
            if (needManager == null)
                throw new McpBridgeException("not_available", "NeedManager singleton is not available.");

            MethodInfo getNeedValue = needManagerType.GetMethod("GetNeedValue", ReflectionUtility.FLAGS);
            MethodInfo setNeedToValue = needManagerType.GetMethod("SetNeedToValue", ReflectionUtility.FLAGS);
            if (setNeedToValue == null)
                throw new McpBridgeException("method_not_found", "NeedManager.SetNeedToValue was not found.");

            object oldValue = getNeedValue != null ? getNeedValue.Invoke(needManager, new object[] { needGuid, character }) : null;
            Dictionary<string, object> result = new()
            {
                ["operation"] = "set_need_value",
                ["characterGuid"] = characterGuid.ToString(CultureInfo.InvariantCulture),
                ["needGuid"] = needGuid.ToString(CultureInfo.InvariantCulture),
                ["oldValue"] = oldValue?.ToString(),
                ["newValue"] = value.ToString(CultureInfo.InvariantCulture),
                ["force"] = force,
                ["dryRun"] = dryRun,
                ["confirmed"] = confirmed
            };

            if (dryRun || !confirmed)
            {
                result["requiredConfirm"] = ConfirmPhrase;
                return result;
            }

            setNeedToValue.Invoke(needManager, new object[] { needGuid, character, value, force });
            result["applied"] = true;
            return result;
        }

        private static object RunWhitelistedCheat(Dictionary<string, object> parameters)
        {
            string command = GetRequiredString(parameters, "command").Trim();
            bool dryRun = GetOptionalBool(parameters, "dryRun", true);
            bool confirmed = IsConfirmed(parameters);
            string commandName = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

            if (!allowedCheats.Contains(commandName))
                throw new McpBridgeException("validation_failed", $"Cheat '{commandName}' is not whitelisted.");

            Dictionary<string, object> result = new()
            {
                ["operation"] = "run_whitelisted_cheat",
                ["command"] = command,
                ["dryRun"] = dryRun,
                ["confirmed"] = confirmed
            };

            if (dryRun || !confirmed)
            {
                result["requiredConfirm"] = ConfirmPhrase;
                return result;
            }

            Type messageType = ReflectionUtility.GetTypeByName("MessageProcessCheatCommand");
            Type eventSystemType = ReflectionUtility.GetTypeByName("EventSystem");
            if (messageType == null || eventSystemType == null)
                throw new McpBridgeException("execution_failed", "Could not find Paralives cheat event types.");

            object message = Activator.CreateInstance(messageType);
            WriteMember(message, messageType, "CommandID", UnityEngine.Random.Range(1, int.MaxValue));
            WriteMember(message, messageType, "Command", command);

            MethodInfo broadcast = eventSystemType.GetMethods(ReflectionUtility.FLAGS)
                .FirstOrDefault(method => method.Name == "Broadcast" && method.GetParameters().Length == 1);
            if (broadcast == null)
                throw new McpBridgeException("execution_failed", "Could not find EventSystem.Broadcast(message).");

            broadcast.Invoke(null, new[] { message });
            result["sent"] = true;
            return result;
        }

        private static void EnsureAvailable()
        {
            EnsureInitialized();
            if (!IsAvailable)
                throw new McpBridgeException("not_available", "Paralives.dll was not found; ParalivesBridge is disabled.");
        }

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            managedPath = Path.Combine(Application.dataPath, "Managed");
            rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            mainModPath = Path.Combine(rootPath, "Main.mod");
            paralivesAssemblyPath = Path.Combine(managedPath, "Paralives.dll");

            try
            {
                typeIndex = ParalivesTypeIndex.Build(paralivesAssemblyPath);
                if (File.Exists(paralivesAssemblyPath))
                    ExplorerCore.Log($"ParalivesBridge indexed {typeIndex.Managers.Count} managers, {typeIndex.Settings.Count} settings, {typeIndex.Cheats.Count} cheat types.");
            }
            catch (Exception ex)
            {
                typeIndex = new ParalivesTypeIndex();
                ExplorerCore.LogWarning($"ParalivesBridge failed to index Paralives.dll: {ex}");
            }
        }

        private static GameObject FindMainMenuRoot()
        {
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (!go)
                    continue;

                if (go.name == "UIMainMenu" || go.name == "UIMainMenu(Clone)")
                    return go;
            }

            return null;
        }

        private static Button FindButtonByName(string buttonName)
        {
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (!go || go.name != buttonName)
                    continue;

                // 先查找标准 Button
                Button button = go.GetComponent<Button>();
                if (button)
                    return button;

                button = go.GetComponentInChildren<Button>(true);
                if (button)
                    return button;
            }

            return null;
        }

        /// <summary>
        /// 查找按钮并返回是否为 ParaButton 类型
        /// </summary>
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

                // 查找标准 Button
                standardButton = go.GetComponent<Button>();
                if (standardButton)
                    return true;

                standardButton = go.GetComponentInChildren<Button>(true);
                if (standardButton)
                    return true;

                // 没有标准 Button，返回 false（是 ParaButton）
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
                    interactable = TryReadMember(paraButton, paraButtonType, "Interactable", out object interactableValue) 
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
                ["button"] = buttonObject ? SummarizeGameObject(buttonObject) : null
            };
        }

        private static Dictionary<string, object> BuildLoadingState()
        {
            Dictionary<string, object> result = SummarizeManager("GameLoadingManager", new[]
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

        private static Dictionary<string, object> SummarizeManager(string managerTypeName, string[] memberNames)
        {
            Type type = ReflectionUtility.GetTypeByName(managerTypeName);
            object manager = GetSingletonInstance(type);
            Dictionary<string, object> members = new();

            if (type != null && manager != null)
            {
                foreach (string memberName in memberNames)
                {
                    if (TryReadMember(manager, type, memberName, out object value))
                        members[memberName] = FormatRuntimeValue(value);
                }
            }

            return new Dictionary<string, object>
            {
                ["type"] = type?.FullName,
                ["available"] = manager != null,
                ["display"] = manager?.ToString(),
                ["selectedMembers"] = members
            };
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

                string path = GetPath(go);
                bool looksLikeUi = go.name.StartsWith("UI", StringComparison.OrdinalIgnoreCase)
                    || go.name.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("/UI", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!looksLikeUi || !seen.Add(go.GetInstanceID()))
                    continue;

                results.Add(SummarizeGameObject(go));
                if (results.Count >= limit)
                    break;
            }

            return results;
        }

        private static List<object> TryListSavedGamesFromManager(int limit)
        {
            List<object> items = new();
            Type managerType = ReflectionUtility.GetTypeByName("SavedGameManager");
            object manager = GetSingletonInstance(managerType);
            if (managerType == null || manager == null)
                return items;

            foreach (string memberName in new[] { "SavedGames", "AllSavedGames", "SaveGames", "Saves", "SavedGameList", "SaveList", "SavedGameMetas", "savedGames" })
            {
                if (!TryReadMember(manager, managerType, memberName, out object collection) || collection == null)
                    continue;

                if (collection is System.Collections.IEnumerable enumerable && !(collection is string))
                {
                    foreach (object item in enumerable)
                    {
                        if (item == null)
                            continue;

                        items.Add(SummarizeDomainObject(item));
                        if (items.Count >= limit)
                            return items;
                    }
                }
            }

            return items;
        }

        private static List<object> ListSavedGameFiles(int limit)
        {
            List<object> files = new();
            HashSet<string> candidateDirectories = new(StringComparer.OrdinalIgnoreCase);
            AddExistingDirectory(candidateDirectories, Application.persistentDataPath);
            AddExistingDirectory(candidateDirectories, Path.Combine(Application.persistentDataPath, "Saves"));
            AddExistingDirectory(candidateDirectories, Path.Combine(Application.persistentDataPath, "SaveGames"));
            AddExistingDirectory(candidateDirectories, Path.Combine(Application.persistentDataPath, "SavedGames"));
            AddExistingDirectory(candidateDirectories, Path.Combine(Application.persistentDataPath, "Saved Games"));
            AddExistingDirectory(candidateDirectories, Path.Combine(rootPath, "Saves"));
            AddExistingDirectory(candidateDirectories, Path.Combine(rootPath, "SaveGames"));

            foreach (string directory in candidateDirectories.ToList())
            {
                try
                {
                    foreach (string child in Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly))
                    {
                        string name = Path.GetFileName(child);
                        if (name.IndexOf("save", StringComparison.OrdinalIgnoreCase) >= 0)
                            AddExistingDirectory(candidateDirectories, child);
                    }
                }
                catch
                {
                }
            }

            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (string directory in candidateDirectories)
            {
                try
                {
                    foreach (string file in Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (!IsLikelySavedGameFile(file) || !seen.Add(file))
                            continue;

                        files.Add(SummarizeFile(file));
                        if (files.Count >= limit)
                            return files;
                    }
                }
                catch
                {
                }
            }

            return files;
        }

        private static bool IsLikelySavedGameFile(string file)
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            string name = Path.GetFileName(file);
            return extension == ".save"
                || extension == ".sav"
                || extension == ".savedgame"
                || extension == ".json" && name.IndexOf("save", StringComparison.OrdinalIgnoreCase) >= 0
                || extension == ".bytes" && name.IndexOf("save", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Dictionary<string, object> SummarizeFile(string file)
        {
            FileInfo info = new(file);
            return new Dictionary<string, object>
            {
                ["path"] = file,
                ["name"] = info.Name,
                ["extension"] = info.Extension,
                ["length"] = info.Length,
                ["lastWriteTimeUtc"] = info.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture)
            };
        }

        private static List<object> ListLoadSavedGameMethods(Type managerType, string saveArgument)
        {
            List<object> methods = new();
            if (managerType == null)
                return methods;

            foreach (MethodInfo method in managerType.GetMethods(ReflectionUtility.FLAGS))
            {
                if (!LooksLikeSavedGameLoadMethod(method) || !CanBuildSingleArgument(method, saveArgument))
                    continue;

                methods.Add(new Dictionary<string, object>
                {
                    ["name"] = method.Name,
                    ["isStatic"] = method.IsStatic,
                    ["parameters"] = method.GetParameters()
                        .Select(parameter => new Dictionary<string, object>
                        {
                            ["name"] = parameter.Name,
                            ["type"] = parameter.ParameterType.FullName
                        })
                        .Cast<object>()
                        .ToList()
                });

                if (methods.Count >= 20)
                    break;
            }

            return methods;
        }

        private static MethodInfo ResolveSavedGameLoadMethod(Type managerType, string saveArgument, out object[] arguments)
        {
            arguments = null;
            foreach (MethodInfo method in managerType.GetMethods(ReflectionUtility.FLAGS))
            {
                if (!LooksLikeSavedGameLoadMethod(method) || !TryBuildSingleArgument(method, saveArgument, out object[] parsedArguments))
                    continue;

                arguments = parsedArguments;
                return method;
            }

            return null;
        }

        private static bool LooksLikeSavedGameLoadMethod(MethodInfo method)
        {
            if (method.IsGenericMethod || method.ContainsGenericParameters)
                return false;

            string name = method.Name;
            return name == "LoadGame"
                || name == "LoadSavedGame"
                || name == "LoadSave"
                || name == "Load"
                || name.IndexOf("Load", StringComparison.OrdinalIgnoreCase) >= 0 && name.IndexOf("Save", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool CanBuildSingleArgument(MethodInfo method, string argument)
        {
            return TryBuildSingleArgument(method, argument, out _);
        }

        private static bool TryBuildSingleArgument(MethodInfo method, string argument, out object[] arguments)
        {
            arguments = null;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 1)
                return false;

            Type parameterType = parameters[0].ParameterType;
            object parsed = null;
            if (parameterType == typeof(string))
                parsed = argument;
            else if (parameterType == typeof(ulong) && ulong.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ulongValue))
                parsed = ulongValue;
            else if (parameterType == typeof(long) && long.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
                parsed = longValue;
            else if (parameterType == typeof(uint) && uint.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uintValue))
                parsed = uintValue;
            else if (parameterType == typeof(int) && int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                parsed = intValue;
            else if (parameterType == typeof(FileInfo))
                parsed = new FileInfo(argument);
            else
                return false;

            arguments = new[] { parsed };
            return true;
        }

        private static void AddExistingDirectory(HashSet<string> directories, string directory)
        {
            try
            {
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    directories.Add(Path.GetFullPath(directory));
            }
            catch
            {
            }
        }

        private static bool TryReadMember(object owner, Type type, string memberName, out object value)
        {
            value = null;
            if (type == null)
                return false;

            try
            {
                PropertyInfo property = type.GetProperty(memberName, ReflectionUtility.FLAGS);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    value = property.GetValue(owner, null);
                    return true;
                }

                FieldInfo field = type.GetField(memberName, ReflectionUtility.FLAGS);
                if (field != null)
                {
                    value = field.GetValue(owner);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static Dictionary<string, object> SummarizeGameObject(GameObject go)
        {
            UnityEngine.SceneManagement.Scene scene = go.scene;
            return new Dictionary<string, object>
            {
                ["instanceId"] = go.GetInstanceID(),
                ["name"] = go.name,
                ["path"] = GetPath(go),
                ["tag"] = GetTag(go),
                ["activeSelf"] = go.activeSelf,
                ["activeInHierarchy"] = go.activeInHierarchy,
                ["sceneName"] = scene.IsValid() ? scene.name : "",
                ["childCount"] = go.transform.childCount
            };
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

        private static string GetTag(GameObject go)
        {
            try
            {
                return go.tag;
            }
            catch
            {
                return "";
            }
        }

        private static object FormatRuntimeValue(object value)
        {
            if (value == null)
                return null;

            Type type = value.GetType();
            if (type.IsPrimitive || value is string || value is decimal || type.IsEnum)
                return value;

            if (value is UnityEngine.Object unityObject)
            {
                if (!unityObject)
                    return null;

                GameObject gameObject = unityObject as GameObject;
                if (!gameObject)
                {
                    Component component = unityObject.TryCast<Component>();
                    gameObject = component ? component.gameObject : null;
                }

                return gameObject ? SummarizeGameObject(gameObject) : unityObject.ToString();
            }

            return value.ToString();
        }

        private static Dictionary<string, object> SummarizeDomainObject(object obj)
        {
            Type type = obj.GetActualType();
            Dictionary<string, object> summary = new()
            {
                ["type"] = type.FullName,
                ["display"] = obj.ToString()
            };

            foreach (string memberName in new[] { "GUID", "guid", "Name", "name", "FirstName", "LastName", "ModName", "Enabled" })
            {
                try
                {
                    object value = ReadMember(obj, type, memberName);
                    if (value != null)
                        summary[memberName] = value.ToString();
                }
                catch
                {
                }
            }

            return summary;
        }

        private static object GetSingletonInstance(Type type)
        {
            if (type == null)
                return null;

            foreach (string memberName in new[] { "Instance", "_instance", "instance", "<Instance>k__BackingField" })
            {
                try
                {
                    object value = ReadMember(null, type, memberName);
                    if (value != null)
                        return value;
                }
                catch
                {
                }
            }

            try
            {
                object lazy = ReadMember(null, type, "lazy");
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

        private static object ReadMember(object owner, Type type, string memberName)
        {
            PropertyInfo property = type.GetProperty(memberName, ReflectionUtility.FLAGS);
            if (property != null)
                return property.GetValue(owner, null);

            FieldInfo field = type.GetField(memberName, ReflectionUtility.FLAGS);
            if (field != null)
                return field.GetValue(owner);

            throw new McpBridgeException("member_not_found", $"{type.FullName}.{memberName} was not found.");
        }

        private static void WriteMember(object owner, Type type, string memberName, object value)
        {
            PropertyInfo property = type.GetProperty(memberName, ReflectionUtility.FLAGS);
            if (property != null)
            {
                property.SetValue(owner, value, null);
                return;
            }

            FieldInfo field = type.GetField(memberName, ReflectionUtility.FLAGS);
            if (field != null)
            {
                field.SetValue(owner, value);
                return;
            }

            throw new McpBridgeException("member_not_found", $"{type.FullName}.{memberName} was not found.");
        }

        private static Dictionary<string, string> ReadMetaFile(string path)
        {
            Dictionary<string, string> meta = new(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadAllLines(path))
            {
                int separator = line.IndexOf(':');
                if (separator <= 0)
                    continue;
                meta[line.Substring(0, separator)] = line.Substring(separator + 1);
            }
            return meta;
        }

        private static string BuildModMeta(string modName)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            return string.Join(Environment.NewLine, new[]
            {
                $"GUID:{GenerateGuid64()}",
                "Type:401",
                $"ModName:{modName}",
                "Enabled:True",
                "IsSystemMod:False",
                $"CreationTime:{nowTicks}",
                $"LastEditTime:{nowTicks}",
                ""
            });
        }

        private static string BuildAssetMeta(string sourcePath)
        {
            return string.Join(Environment.NewLine, new[]
            {
                $"GUID:{GenerateGuid64()}",
                $"Type:{GuessAssetType(sourcePath)}",
                $"ImportFileCheckSum:{ComputeSha1(sourcePath)}",
                ""
            });
        }

        private static string GuessAssetType(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                return "2";
            if (ext == ".txt" || ext == ".md" || ext == ".json")
                return "202";
            return "0";
        }

        private static string ResolveModPath(string modPathOrName)
        {
            string candidate = modPathOrName;
            if (!Path.IsPathRooted(candidate))
                candidate = Path.Combine(rootPath, candidate.EndsWith(".mod", StringComparison.OrdinalIgnoreCase) ? candidate : candidate + ".mod");

            candidate = Path.GetFullPath(candidate);
            if (!candidate.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                throw new McpBridgeException("validation_failed", "Mod path must be inside the Paralives game directory.");
            if (!Directory.Exists(candidate))
                throw new McpBridgeException("validation_failed", $"Mod path does not exist: {candidate}");

            return candidate;
        }

        private static string SanitizeModName(string modName)
        {
            string sanitized = new string(modName.Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == ' ').ToArray()).Trim();
            if (string.IsNullOrEmpty(sanitized))
                throw new McpBridgeException("validation_failed", "modName must contain at least one valid character.");
            return sanitized;
        }

        private static string NormalizeRelativePath(string path)
        {
            string normalized = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar).Trim(Path.DirectorySeparatorChar);
            if (normalized.Contains(".."))
                throw new McpBridgeException("validation_failed", "subFolder must not contain '..'.");
            return normalized;
        }

        private static string MakeRelativePath(string root, string path)
        {
            Uri rootUri = new(AppendDirectorySeparator(root));
            Uri pathUri = new(path);
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString()) ? path : path + Path.DirectorySeparatorChar;
        }

        private static string ComputeSha1(string path)
        {
            using SHA1 sha1 = SHA1.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "");
        }

        private static ulong GenerateGuid64()
        {
            byte[] bytes = Guid.NewGuid().ToByteArray();
            return BitConverter.ToUInt64(bytes, 0);
        }

        private static bool IsConfirmed(Dictionary<string, object> parameters)
        {
            return string.Equals(GetOptionalString(parameters, "confirm"), ConfirmPhrase, StringComparison.Ordinal);
        }

        private static string GetMetaValue(Dictionary<string, string> meta, string key)
        {
            return meta.TryGetValue(key, out string value) ? value : null;
        }

        private static string GetRequiredString(Dictionary<string, object> parameters, string name)
        {
            if (!parameters.TryGetValue(name, out object value) || value == null)
                throw new McpBridgeException("invalid_request", $"'{name}' is required.");
            return value.ToString();
        }

        private static ulong GetRequiredUInt64(Dictionary<string, object> parameters, string name)
        {
            return ulong.Parse(GetRequiredString(parameters, name), CultureInfo.InvariantCulture);
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

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
#endif
