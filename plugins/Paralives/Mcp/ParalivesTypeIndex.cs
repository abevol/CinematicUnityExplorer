#if MONO
using Mono.Cecil;

namespace CinematicUnityExplorer.Plugins.Paralives.Mcp
{
    internal sealed class ParalivesTypeIndex
    {
        private static readonly string[] managerNames =
        {
            "ModManager",
            "CharacterManager",
            "HouseholdManager",
            "LotManager",
            "SavedGameManager",
            "GameLoadingManager",
            "GameSavingManager",
            "InteractionManager",
            "NeedManager",
            "ItemManager",
            "InventoryManager",
            "CalendarEventManager",
            "AutonomyManager",
            "EmotionManager",
            "MemoryManager",
            "GoalsManager"
        };

        public string AssemblyPath { get; private set; }
        public List<Dictionary<string, object>> Managers { get; } = new();
        public List<Dictionary<string, object>> Settings { get; } = new();
        public List<Dictionary<string, object>> Cheats { get; } = new();

        public static ParalivesTypeIndex Build(string assemblyPath)
        {
            ParalivesTypeIndex index = new() { AssemblyPath = assemblyPath };
            if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
                return index;

            using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                if (type.FullName.StartsWith("<") || type.FullName.Contains("PrivateImplementationDetails"))
                    continue;

                if (IsManager(type))
                    index.Managers.Add(SummarizeType(type));

                if (IsSetting(type))
                    index.Settings.Add(SummarizeType(type));

                if (IsCheat(type))
                    index.Cheats.Add(SummarizeType(type));
            }

            return index;
        }

        public Dictionary<string, object> ToSummary()
        {
            return new Dictionary<string, object>
            {
                ["assemblyPath"] = AssemblyPath,
                ["managerCount"] = Managers.Count,
                ["settingCount"] = Settings.Count,
                ["cheatCount"] = Cheats.Count
            };
        }

        private static bool IsManager(TypeDefinition type)
        {
            return managerNames.Contains(type.Name)
                || type.Name.EndsWith("Manager")
                || InheritsFrom(type, "UnityEngine.MonoBehaviour") && type.Name.Contains("Manager");
        }

        private static bool IsSetting(TypeDefinition type)
        {
            return type.Namespace == "Setting"
                || type.FullName.StartsWith("Setting.")
                || type.Name.EndsWith("Setting")
                || type.Name.EndsWith("Settings");
        }

        private static bool IsCheat(TypeDefinition type)
        {
            return type.Name.Contains("Cheat")
                || type.FullName.Contains("Cheat")
                || type.Name == "ProcessCheatCommandEvent"
                || type.Name == "MessageProcessCheatCommand";
        }

        private static bool InheritsFrom(TypeDefinition type, string baseTypeName)
        {
            TypeReference current = type.BaseType;
            while (current != null)
            {
                if (current.FullName == baseTypeName)
                    return true;

                try
                {
                    current = current.Resolve()?.BaseType;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        private static Dictionary<string, object> SummarizeType(TypeDefinition type)
        {
            return new Dictionary<string, object>
            {
                ["name"] = type.Name,
                ["fullName"] = type.FullName,
                ["namespace"] = type.Namespace ?? "",
                ["baseType"] = type.BaseType?.FullName,
                ["methodCount"] = type.Methods.Count,
                ["fieldCount"] = type.Fields.Count,
                ["methods"] = type.Methods
                    .Where(method => !method.IsGetter && !method.IsSetter && !method.IsConstructor)
                    .Take(40)
                    .Select(SummarizeMethod)
                    .Cast<object>()
                    .ToList()
            };
        }

        private static Dictionary<string, object> SummarizeMethod(MethodDefinition method)
        {
            return new Dictionary<string, object>
            {
                ["name"] = method.Name,
                ["returnType"] = method.ReturnType.FullName,
                ["isPublic"] = method.IsPublic,
                ["parameters"] = method.Parameters
                    .Select(parameter => new Dictionary<string, object>
                    {
                        ["name"] = parameter.Name,
                        ["type"] = parameter.ParameterType.FullName
                    })
                    .Cast<object>()
                    .ToList()
            };
        }
    }
}
#endif
