#if MONO
namespace CinematicUnityExplorer.Plugins.Paralives.Mcp
{
    internal static class ParalivesCollectionService
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_list_characters"] = _ => ListManagerCollection("CharacterManager", "Characters"),
            ["paralives_list_households"] = _ => ListManagerCollection("HouseholdManager", "AllHouseholds"),
            ["paralives_list_lots"] = _ => ListManagerCollection("LotManager", "Lots")
        };

        private static object ListManagerCollection(string managerTypeName, string memberName)
        {
            ParalivesShared.EnsureAvailable();
            Type managerType = ReflectionUtility.GetTypeByName(managerTypeName);
            object manager = UnityReflectionUtility.GetSingletonInstance(managerType);
            if (manager == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = $"{managerTypeName}.Instance is not available." };

            object collection = UnityReflectionUtility.ReadMember(manager, managerType, memberName);
            List<object> items = new();
            if (collection is System.Collections.IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (item == null)
                        continue;
                    items.Add(UnityObjectSummary.DomainObject(item));
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
    }
}
#endif
