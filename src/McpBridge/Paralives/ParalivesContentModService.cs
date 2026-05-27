#if MONO
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesContentModService
    {
        private const int MaxListedFiles = 200;

        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_list_content_mods"] = _ => ListContentMods(),
            ["paralives_inspect_content_mod"] = InspectContentMod,
            ["paralives_create_content_mod"] = CreateContentMod,
            ["paralives_import_asset_to_mod"] = ImportAssetToMod
        };

        private static object ListContentMods()
        {
            ParalivesShared.EnsureAvailable();
            List<object> mods = new();
            foreach (string metaPath in Directory.GetFiles(ParalivesEnvironment.RootPath, "*.mod.meta", SearchOption.AllDirectories))
            {
                string modPath = Path.GetDirectoryName(metaPath);
                Dictionary<string, string> meta = ReadMetaFile(metaPath);
                mods.Add(new Dictionary<string, object>
                {
                    ["path"] = modPath,
                    ["folderName"] = Path.GetFileName(modPath),
                    ["metaPath"] = metaPath,
                    ["guid"] = ParalivesShared.GetMetaValue(meta, "GUID"),
                    ["modName"] = ParalivesShared.GetMetaValue(meta, "ModName"),
                    ["enabled"] = ParalivesShared.GetMetaValue(meta, "Enabled"),
                    ["isMainMod"] = string.Equals(modPath, ParalivesEnvironment.MainModPath, StringComparison.OrdinalIgnoreCase)
                });
            }
            return new Dictionary<string, object> { ["mods"] = mods };
        }

        private static object InspectContentMod(Dictionary<string, object> parameters)
        {
            ParalivesShared.EnsureAvailable();
            string modPath = ResolveModPath(McpParameters.RequiredString(parameters, "modPath"));
            int limit = McpParameters.Clamp(McpParameters.OptionalInt(parameters, "limit", 100), 1, MaxListedFiles);
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
            ParalivesShared.EnsureAvailable();
            string modName = SanitizeModName(McpParameters.RequiredString(parameters, "modName"));
            bool dryRun = McpParameters.OptionalBool(parameters, "dryRun", true);
            bool confirmed = ParalivesShared.IsConfirmed(parameters);
            string targetPath = Path.Combine(ParalivesEnvironment.RootPath, modName + ".mod");

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
                result["requiredConfirm"] = ParalivesShared.ConfirmPhrase;
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
            ParalivesShared.EnsureAvailable();
            string sourcePath = Path.GetFullPath(McpParameters.RequiredString(parameters, "sourcePath"));
            string modPath = ResolveModPath(McpParameters.RequiredString(parameters, "modPath"));
            string subFolder = NormalizeRelativePath(McpParameters.OptionalString(parameters, "subFolder") ?? "");
            bool dryRun = McpParameters.OptionalBool(parameters, "dryRun", true);
            bool confirmed = ParalivesShared.IsConfirmed(parameters);

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
                result["requiredConfirm"] = ParalivesShared.ConfirmPhrase;
                return result;
            }

            Directory.CreateDirectory(destinationFolder);
            File.Copy(sourcePath, destinationPath, false);
            File.WriteAllText(metaPath, BuildAssetMeta(sourcePath), Encoding.UTF8);
            result["imported"] = true;
            return result;
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
                candidate = Path.Combine(ParalivesEnvironment.RootPath, candidate.EndsWith(".mod", StringComparison.OrdinalIgnoreCase) ? candidate : candidate + ".mod");

            candidate = Path.GetFullPath(candidate);
            if (!candidate.StartsWith(ParalivesEnvironment.RootPath, StringComparison.OrdinalIgnoreCase))
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
    }
}
#endif
