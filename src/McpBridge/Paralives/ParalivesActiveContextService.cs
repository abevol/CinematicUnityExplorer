#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesActiveContextService
    {
        private static readonly Dictionary<string, Func<Dictionary<string, object>, object>> actionHandlers = new()
        {
            ["paralives_get_active_context"] = _ => GetActiveContext()
        };
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = BuildActions();

        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            if (actionHandlers.TryGetValue(action, out Func<Dictionary<string, object>, object> handler))
                return handler(parameters);

            throw new McpBridgeException("invalid_request", $"Unknown active context action '{action}'.");
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

        private static object GetActiveContext()
        {
            Dictionary<string, object> householdInfo = GetActiveHouseholdInfo();
            Dictionary<string, object> characterInfo = GetActiveCharacterInfo();
            Dictionary<string, object> lotInfo = GetCurrentLotInfo();

            return new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["activeHousehold"] = householdInfo,
                ["activeCharacter"] = characterInfo,
                ["currentLot"] = lotInfo
            };
        }

        private static Dictionary<string, object> GetActiveHouseholdInfo()
        {
            Type householdManagerType = ReflectionUtility.GetTypeByName("HouseholdManager");
            if (householdManagerType == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "HouseholdManager type not found" };

            object manager = UnityReflectionUtility.GetSingletonInstance(householdManagerType);
            if (manager == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "HouseholdManager instance not found" };

            ulong guid = UnityReflectionUtility.TryReadMember(manager, householdManagerType, "CurrentHouseholdGUID", out object guidValue)
                ? Convert.ToUInt64(guidValue) : 0;
            bool hasHousehold = UnityReflectionUtility.TryReadMember(manager, householdManagerType, "HasCurrentHousehold", out object hasValue)
                && (bool)hasValue;

            if (!hasHousehold || guid == 0)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "No active household" };

            string name = "Unknown";
            int memberCount = 0;
            List<object> members = new();

            UnityReflectionUtility.TryReadMember(manager, householdManagerType, "CurrentHousehold", out object householdObj);
            if (householdObj != null)
            {
                Type householdType = householdObj.GetActualType();
                name = UnityReflectionUtility.TryReadMember(householdObj, householdType, "Name", out object nameValue) ? nameValue?.ToString() : "Unknown";

                if (UnityReflectionUtility.TryReadMember(householdObj, householdType, "Members", out object membersObj) && membersObj is System.Collections.IEnumerable enumerable)
                {
                    foreach (object member in enumerable)
                    {
                        if (member == null)
                            continue;

                        memberCount++;
                        Type memberType = member.GetActualType();
                        members.Add(new Dictionary<string, object>
                        {
                            ["type"] = memberType.FullName,
                            ["display"] = member.ToString(),
                            ["guid"] = UnityReflectionUtility.TryReadMember(member, memberType, "GUID", out object memberGuid) ? memberGuid?.ToString() : null
                        });
                        if (members.Count >= 10)
                            break;
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["available"] = true,
                ["guid"] = guid.ToString(),
                ["name"] = name,
                ["memberCount"] = memberCount,
                ["members"] = members
            };
        }

        internal static Dictionary<string, object> GetActiveCharacterInfo()
        {
            GameObject uiCharacters = ParalivesUiQuery.FindUiRoot("UICharacters");
            if (uiCharacters == null)
                return new Dictionary<string, object> { ["available"] = false, ["reason"] = "UICharacters not found" };

            GameObject selectedCharacter = ParalivesUiQuery.FindChildByName(uiCharacters, "SelectedCharacter");
            if (selectedCharacter != null && selectedCharacter.activeInHierarchy)
            {
                Transform parent = selectedCharacter.transform.parent;
                if (parent != null)
                {
                    GameObject thumbnail = ParalivesUiQuery.FindChildByName(parent.gameObject, "CharacterThumbnail");
                    if (thumbnail != null)
                    {
                        return new Dictionary<string, object>
                        {
                            ["available"] = true,
                            ["source"] = "UICharacters selection",
                            ["parentPath"] = UnityObjectSummary.GetPath(parent.gameObject)
                        };
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["available"] = false,
                ["reason"] = "No character selected in UICharacters"
            };
        }

        private static Dictionary<string, object> GetCurrentLotInfo()
        {
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (!go || !go.name.StartsWith("NavMeshSurface lot "))
                    continue;

                string lotName = go.name.Replace("NavMeshSurface lot ", "");
                if (ulong.TryParse(lotName.Split('/')[0], out ulong lotGuid))
                {
                    return new Dictionary<string, object>
                    {
                        ["available"] = true,
                        ["guid"] = lotGuid.ToString(),
                        ["name"] = go.name,
                        ["path"] = UnityObjectSummary.GetPath(go),
                        ["isActive"] = go.activeInHierarchy
                    };
                }
            }

            return new Dictionary<string, object>
            {
                ["available"] = false,
                ["reason"] = "Could not determine current lot"
            };
        }
    }
}
#endif
