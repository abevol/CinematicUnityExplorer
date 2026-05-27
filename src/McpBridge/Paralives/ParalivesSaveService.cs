#if MONO
using System.Globalization;

namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesSaveService
    {
        private const int MaxListedSavedGames = 100;

        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_list_saved_games"] = ListSavedGames,
            ["paralives_load_saved_game"] = LoadSavedGame
        };

        internal static Dictionary<string, object> ListSavedGamesForUi(int limit)
        {
            return (Dictionary<string, object>)ListSavedGames(new Dictionary<string, object> { ["limit"] = limit });
        }

        internal static Dictionary<string, object> LoadSavedGameForUi(string argumentName, string argumentValue, bool confirmed)
        {
            Dictionary<string, object> parameters = new()
            {
                [argumentName] = argumentValue,
                ["dryRun"] = !confirmed
            };
            if (confirmed)
                parameters["confirm"] = ParalivesShared.ConfirmPhrase;
            return (Dictionary<string, object>)LoadSavedGame(parameters);
        }

        private static object ListSavedGames(Dictionary<string, object> parameters)
        {
            ParalivesShared.EnsureAvailable();
            int defaultLimit = UnityExplorer.Config.ConfigManager.Paralives_SavedGameListLimit != null
                ? UnityExplorer.Config.ConfigManager.Paralives_SavedGameListLimit.Value
                : 50;
            int limit = McpParameters.Clamp(McpParameters.OptionalInt(parameters, "limit", defaultLimit), 1, MaxListedSavedGames);
            List<object> managerItems = TryListSavedGamesFromManager(limit);
            List<object> files = ListSavedGameFiles(limit);

            return new Dictionary<string, object>
            {
                ["manager"] = ParalivesShared.SummarizeManager("SavedGameManager", new[] { "CurrentSavedGame", "CurrentSave", "LoadedGame", "IsGameLoaded", "HasLoadedGame" }),
                ["managerItems"] = managerItems,
                ["files"] = files,
                ["limit"] = limit,
                ["truncated"] = managerItems.Count >= limit || files.Count >= limit,
                ["persistentDataPath"] = Application.persistentDataPath
            };
        }

        private static object LoadSavedGame(Dictionary<string, object> parameters)
        {
            ParalivesShared.EnsureAvailable();
            string savePath = McpParameters.OptionalString(parameters, "savePath");
            string saveName = McpParameters.OptionalString(parameters, "saveName");
            string saveId = McpParameters.OptionalString(parameters, "saveId");
            string saveArgument = savePath ?? saveName ?? saveId;
            bool dryRun = McpParameters.OptionalBool(parameters, "dryRun", true);
            bool confirmed = ParalivesShared.IsConfirmed(parameters);

            if (string.IsNullOrEmpty(saveArgument))
                throw new McpBridgeException("invalid_request", "One of 'savePath', 'saveName', or 'saveId' is required.");

            Type managerType = ReflectionUtility.GetTypeByName("SavedGameManager");
            object manager = UnityReflectionUtility.GetSingletonInstance(managerType);
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
                result["requiredConfirm"] = ParalivesShared.ConfirmPhrase;
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

        private static List<object> TryListSavedGamesFromManager(int limit)
        {
            List<object> items = new();
            Type managerType = ReflectionUtility.GetTypeByName("SavedGameManager");
            object manager = UnityReflectionUtility.GetSingletonInstance(managerType);
            if (managerType == null || manager == null)
                return items;

            foreach (string memberName in new[] { "SavedGames", "AllSavedGames", "SaveGames", "Saves", "SavedGameList", "SaveList", "SavedGameMetas", "savedGames" })
            {
                if (!UnityReflectionUtility.TryReadMember(manager, managerType, memberName, out object collection) || collection == null)
                    continue;

                if (collection is System.Collections.IEnumerable enumerable && !(collection is string))
                {
                    foreach (object item in enumerable)
                    {
                        if (item == null)
                            continue;

                        items.Add(UnityObjectSummary.DomainObject(item));
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
            AddExistingDirectory(candidateDirectories, Path.Combine(ParalivesEnvironment.RootPath, "Saves"));
            AddExistingDirectory(candidateDirectories, Path.Combine(ParalivesEnvironment.RootPath, "SaveGames"));

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
    }
}
#endif
