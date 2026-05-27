namespace UnityExplorer.McpBridge
{
    internal static class UnityObjectService
    {
        private const int DefaultLimit = 50;
        private const int MaxLimit = 200;

        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["find_game_objects"] = FindGameObjects,
            ["get_object_detail"] = GetObjectDetail,
            ["get_scene_hierarchy"] = GetSceneHierarchy,
            ["get_object_components"] = GetObjectComponents
        };

        public static object FindGameObjects(Dictionary<string, object> parameters)
        {
            string query = McpParameters.OptionalString(parameters, "query");
            string tag = McpParameters.OptionalString(parameters, "tag");
            bool includeInactive = McpParameters.OptionalBool(parameters, "includeInactive", true);
            int limit = McpParameters.Clamp(McpParameters.OptionalInt(parameters, "limit", DefaultLimit), 1, MaxLimit);

            List<object> results = new();
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (!IsInspectable(go))
                    continue;

                if (!includeInactive && !go.activeInHierarchy)
                    continue;

                string path = UnityObjectSummary.GetPath(go);
                if (!string.IsNullOrEmpty(query) && !go.name.ContainsIgnoreCase(query) && !path.ContainsIgnoreCase(query))
                    continue;

                if (!string.IsNullOrEmpty(tag) && !UnityObjectSummary.TagEquals(go, tag))
                    continue;

                results.Add(UnityObjectSummary.FromGameObject(go));
                if (results.Count >= limit)
                    break;
            }

            return new Dictionary<string, object>
            {
                ["objects"] = results,
                ["limit"] = limit,
                ["truncated"] = results.Count >= limit
            };
        }

        public static object GetObjectDetail(Dictionary<string, object> parameters)
        {
            GameObject go = RequireGameObject(McpParameters.RequiredInt(parameters, "instanceId"));
            return new Dictionary<string, object>
            {
                ["object"] = UnityObjectSummary.FromGameObject(go),
                ["parent"] = go.transform.parent ? UnityObjectSummary.FromGameObject(go.transform.parent.gameObject) : null,
                ["children"] = GetChildren(go.transform, 100),
                ["components"] = GetComponentSummaries(go, true)
            };
        }

        public static object GetSceneHierarchy(Dictionary<string, object> parameters)
        {
            int childLimit = McpParameters.Clamp(McpParameters.OptionalInt(parameters, "childLimit", 50), 1, MaxLimit);
            int grandChildLimit = McpParameters.Clamp(McpParameters.OptionalInt(parameters, "grandChildLimit", 25), 1, MaxLimit);

            UnityExplorer.ObjectExplorer.SceneHandler.Update();
            List<object> roots = new();

            foreach (GameObject root in UnityExplorer.ObjectExplorer.SceneHandler.CurrentRootObjects)
            {
                if (!IsInspectable(root))
                    continue;

                Dictionary<string, object> rootSummary = UnityObjectSummary.FromGameObject(root);
                List<object> children = new();
                int childCount = 0;
                foreach (Transform child in root.transform)
                {
                    if (childCount++ >= childLimit)
                        break;

                    Dictionary<string, object> childSummary = UnityObjectSummary.FromGameObject(child.gameObject);
                    childSummary["children"] = GetChildren(child, grandChildLimit);
                    children.Add(childSummary);
                }
                rootSummary["children"] = children;
                roots.Add(rootSummary);
            }

            return new Dictionary<string, object> { ["roots"] = roots };
        }

        public static object GetObjectComponents(Dictionary<string, object> parameters)
        {
            GameObject go = RequireGameObject(McpParameters.RequiredInt(parameters, "instanceId"));
            return new Dictionary<string, object>
            {
                ["object"] = UnityObjectSummary.FromGameObject(go),
                ["components"] = GetComponentSummaries(go, true)
            };
        }

        public static GameObject RequireGameObject(int instanceId)
        {
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (go && go.GetInstanceID() == instanceId)
                    return go;
            }

            throw new McpBridgeException("object_not_found", $"GameObject with instanceId {instanceId} was not found.");
        }

        public static Component RequireComponent(GameObject go, string componentName)
        {
            Component component = UnityReflectionUtility.FindComponentByName(go, componentName);
            if (component)
                return component;

            throw new McpBridgeException("component_not_found", $"Component '{componentName}' was not found on '{go.name}'.");
        }

        private static bool IsInspectable(GameObject go)
        {
            return go && go.transform.root.name != "UniverseLibCanvas" && go.name != "ExplorerBehaviour";
        }

        private static List<object> GetChildren(Transform transform, int limit)
        {
            List<object> children = new();
            int count = 0;
            foreach (Transform child in transform)
            {
                if (count++ >= limit)
                    break;
                children.Add(UnityObjectSummary.FromGameObject(child.gameObject));
            }
            return children;
        }

        public static List<object> GetComponentSummaries(GameObject go, bool includeMembers)
        {
            List<object> components = new();
            foreach (Component component in go.GetComponents<Component>())
            {
                if (!component)
                {
                    components.Add(new Dictionary<string, object> { ["name"] = "<missing>", ["type"] = null });
                    continue;
                }

                Type type = component.GetActualType();
                Dictionary<string, object> summary = new()
                {
                    ["name"] = type.Name,
                    ["type"] = type.FullName
                };

                if (includeMembers)
                    summary["members"] = GetMemberSummaries(component, type, 80);

                components.Add(summary);
            }
            return components;
        }

        private static List<object> GetMemberSummaries(object owner, Type type, int limit)
        {
            List<object> members = new();
            foreach (PropertyInfo property in type.GetProperties(ReflectionUtility.FLAGS))
            {
                if (members.Count >= limit)
                    break;
                if (!property.CanRead || property.GetIndexParameters().Length != 0)
                    continue;
                AddMemberSummary(members, owner, property, property.PropertyType, property.CanWrite);
            }

            foreach (FieldInfo field in type.GetFields(ReflectionUtility.FLAGS))
            {
                if (members.Count >= limit)
                    break;
                AddMemberSummary(members, owner, field, field.FieldType, !(field.IsLiteral || field.IsInitOnly));
            }

            return members;
        }

        private static void AddMemberSummary(List<object> members, object owner, MemberInfo member, Type memberType, bool canWrite)
        {
            if (!ParseUtility.CanParse(memberType))
                return;

            object value = null;
            bool canRead = true;
            try
            {
                value = GetMemberValue(member, owner);
            }
            catch
            {
                canRead = false;
            }

            members.Add(new Dictionary<string, object>
            {
                ["name"] = member.Name,
                ["type"] = memberType.FullName,
                ["canWrite"] = canWrite,
                ["canRead"] = canRead,
                ["value"] = canRead ? FormatValue(value, memberType) : null
            });
        }

        private static object GetMemberValue(MemberInfo member, object owner)
        {
            if (member is PropertyInfo property)
                return property.GetValue(owner, null);
            return ((FieldInfo)member).GetValue(owner);
        }

        private static string FormatValue(object value, Type type)
        {
            return value == null ? null : ParseUtility.ToStringForInput(value, type) ?? value.ToString();
        }
    }
}
