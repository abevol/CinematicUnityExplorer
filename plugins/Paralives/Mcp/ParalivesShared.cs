#if MONO
namespace CinematicUnityExplorer.Plugins.Paralives.Mcp
{
    internal static class ParalivesShared
    {
        internal const string ConfirmPhrase = "CONFIRM_PARALIVES_WRITE";

        internal static bool IsConfirmed(Dictionary<string, object> parameters)
        {
            return string.Equals(McpParameters.OptionalString(parameters, "confirm"), ConfirmPhrase, StringComparison.Ordinal);
        }

        internal static void EnsureAvailable()
        {
            ParalivesEnvironment.EnsureAvailable();
        }

        internal static GameObject FindMainMenuRoot()
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

        internal static Dictionary<string, object> SummarizeManager(string managerTypeName, string[] memberNames)
        {
            Type type = ReflectionUtility.GetTypeByName(managerTypeName);
            object manager = UnityReflectionUtility.GetSingletonInstance(type);
            Dictionary<string, object> members = new();

            if (type != null && manager != null)
            {
                foreach (string memberName in memberNames)
                {
                    if (UnityReflectionUtility.TryReadMember(manager, type, memberName, out object value))
                        members[memberName] = UnityObjectSummary.RuntimeValue(value);
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

        internal static string GetMetaValue(Dictionary<string, string> meta, string key)
        {
            return meta.TryGetValue(key, out string value) ? value : null;
        }
    }
}
#endif
