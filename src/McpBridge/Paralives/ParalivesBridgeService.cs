#if MONO
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesBridgeService
    {
        private const string ConfirmPhrase = "CONFIRM_PARALIVES_WRITE";
        private const int MaxListedFiles = 200;
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
