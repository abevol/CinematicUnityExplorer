using System.Globalization;
using UnityEngine.SceneManagement;
using UnityExplorer.Config;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.McpBridge
{
    internal static class McpBridgeService
    {
        private const int DefaultLimit = 50;
        private const int MaxLimit = 200;
        private const float MaxTransformAbs = 10000f;
        private const float MethodCooldownSeconds = 1f;

        private static readonly Dictionary<string, float> methodLastCalled = new();
        private static readonly HashSet<string> deniedMethods = new()
        {
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "OnDestroy",
            "OnApplicationQuit",
            "Awake",
            "Start"
        };

        public static object Handle(string action, Dictionary<string, object> parameters)
        {
#if MONO
            if (action.StartsWith("paralives_", StringComparison.Ordinal))
            {
                if (action == "paralives_read_resource")
                    return Paralives.ParalivesBridgeService.ReadResource(GetRequiredString(parameters, "uri"), parameters);
                
                // 运行时状态工具
                if (action == "paralives_get_runtime_summary" 
                    || action == "paralives_get_game_time"
                    || action == "paralives_get_economy" 
                    || action == "paralives_get_selection")
                    return Paralives.ParalivesRuntimeService.Handle(action, parameters);
                
                return Paralives.ParalivesBridgeService.Handle(action, parameters);
            }

            // 日志工具
            if (action == "get_game_logs" || action == "subscribe_logs" || action == "poll_logs")
                return Paralives.ParalivesRuntimeService.Handle(action, parameters);
#endif
            return action switch
            {
                "find_game_objects" => FindGameObjects(parameters),
                "get_object_detail" => GetObjectDetail(parameters),
                "set_component_property" => SetComponentProperty(parameters),
                "call_component_method" => CallComponentMethod(parameters),
                "get_scene_hierarchy" => GetSceneHierarchy(parameters),
                "get_object_components" => GetObjectComponents(parameters),
                "get_runtime_status" => GetRuntimeStatus(parameters),
                "get_recent_logs" => GetRecentLogs(parameters),
                "list_config" => ListConfig(parameters),
                "get_mcp_status" => GetMcpStatus(parameters),
                _ => throw new McpBridgeException("invalid_request", $"Unknown MCP bridge action '{action}'.")
            };
        }

        private static object FindGameObjects(Dictionary<string, object> parameters)
        {
            string query = GetOptionalString(parameters, "query");
            string tag = GetOptionalString(parameters, "tag");
            bool includeInactive = GetOptionalBool(parameters, "includeInactive", true);
            int limit = Clamp(GetOptionalInt(parameters, "limit", DefaultLimit), 1, MaxLimit);

            List<object> results = new();
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (!IsInspectable(go))
                    continue;

                if (!includeInactive && !go.activeInHierarchy)
                    continue;

                if (!string.IsNullOrEmpty(query) && !go.name.ContainsIgnoreCase(query) && !GetPath(go).ContainsIgnoreCase(query))
                    continue;

                if (!string.IsNullOrEmpty(tag) && !TagEquals(go, tag))
                    continue;

                results.Add(SummarizeGameObject(go));
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

        private static object GetObjectDetail(Dictionary<string, object> parameters)
        {
            GameObject go = RequireGameObject(GetRequiredInt(parameters, "instanceId"));
            return new Dictionary<string, object>
            {
                ["object"] = SummarizeGameObject(go),
                ["parent"] = go.transform.parent ? SummarizeGameObject(go.transform.parent.gameObject) : null,
                ["children"] = GetChildren(go.transform, 100),
                ["components"] = GetComponentSummaries(go, true)
            };
        }

        private static object SetComponentProperty(Dictionary<string, object> parameters)
        {
            GameObject go = RequireGameObject(GetRequiredInt(parameters, "instanceId"));
            Component component = RequireComponent(go, GetRequiredString(parameters, "componentName"));
            string propertyPath = GetRequiredString(parameters, "propertyPath");
            string valueText = GetRequiredString(parameters, "value");

            if (string.IsNullOrEmpty(propertyPath.Trim()))
                throw new McpBridgeException("invalid_request", "propertyPath is required.");

            Type componentType = component.GetActualType();
            object owner = component.TryCast(componentType);
            object oldValue = ReadPathValue(owner, componentType, propertyPath);
            Type targetType = oldValue != null ? oldValue.GetType() : GetPathValueType(componentType, propertyPath);

            if (!ParseUtility.TryParse(valueText, targetType, out object parsed, out Exception parseException))
            {
                string message = parseException != null ? parseException.Message : $"Cannot parse value as {targetType.FullName}.";
                throw new McpBridgeException("parse_failed", message);
            }

            if (IsTransformVectorMutation(component, propertyPath))
                ValidateTransformValue(parsed, targetType);

            WritePathValue(owner, componentType, propertyPath, parsed);
            object newValue = ReadPathValue(owner, componentType, propertyPath);

            return new Dictionary<string, object>
            {
                ["instanceId"] = go.GetInstanceID(),
                ["componentName"] = componentType.FullName,
                ["propertyPath"] = propertyPath,
                ["oldValue"] = FormatValue(oldValue, targetType),
                ["newValue"] = FormatValue(newValue, targetType)
            };
        }

        private static object CallComponentMethod(Dictionary<string, object> parameters)
        {
            GameObject go = RequireGameObject(GetRequiredInt(parameters, "instanceId"));
            Component component = RequireComponent(go, GetRequiredString(parameters, "componentName"));
            string methodName = GetRequiredString(parameters, "methodName");
            List<object> argumentValues = GetOptionalArray(parameters, "arguments");
            string[] argumentTexts = argumentValues.Select(it => it?.ToString() ?? "").ToArray();

            if (deniedMethods.Contains(methodName))
                throw new McpBridgeException("validation_failed", $"Method '{methodName}' is not allowed through MCP.");

            Type componentType = component.GetActualType();
            MethodInfo method = ResolveMethod(componentType, methodName, argumentTexts);

            string cooldownKey = $"{go.GetInstanceID()}:{componentType.FullName}:{method.Name}";
            if (methodLastCalled.TryGetValue(cooldownKey, out float lastCalled) && Time.realtimeSinceStartup - lastCalled < MethodCooldownSeconds)
                throw new McpBridgeException("rate_limited", $"Method '{method.Name}' is cooling down.");

            object[] parsedArguments = ParseMethodArguments(method, argumentTexts);
            object owner = component.TryCast(method.DeclaringType);
            object result = method.Invoke(owner, parsedArguments);
            methodLastCalled[cooldownKey] = Time.realtimeSinceStartup;

            return new Dictionary<string, object>
            {
                ["instanceId"] = go.GetInstanceID(),
                ["componentName"] = componentType.FullName,
                ["methodName"] = method.Name,
                ["returnType"] = method.ReturnType.FullName,
                ["result"] = method.ReturnType == typeof(void) ? null : FormatValue(result, method.ReturnType)
            };
        }

        private static object GetSceneHierarchy(Dictionary<string, object> parameters)
        {
            int childLimit = Clamp(GetOptionalInt(parameters, "childLimit", 50), 1, MaxLimit);
            int grandChildLimit = Clamp(GetOptionalInt(parameters, "grandChildLimit", 25), 1, MaxLimit);

            UnityExplorer.ObjectExplorer.SceneHandler.Update();
            List<object> roots = new();

            foreach (GameObject root in UnityExplorer.ObjectExplorer.SceneHandler.CurrentRootObjects)
            {
                if (!IsInspectable(root))
                    continue;

                Dictionary<string, object> rootSummary = SummarizeGameObject(root);
                List<object> children = new();
                int childCount = 0;
                foreach (Transform child in root.transform)
                {
                    if (childCount++ >= childLimit)
                        break;

                    Dictionary<string, object> childSummary = SummarizeGameObject(child.gameObject);
                    childSummary["children"] = GetChildren(child, grandChildLimit);
                    children.Add(childSummary);
                }
                rootSummary["children"] = children;
                roots.Add(rootSummary);
            }

            return new Dictionary<string, object> { ["roots"] = roots };
        }

        private static object GetObjectComponents(Dictionary<string, object> parameters)
        {
            GameObject go = RequireGameObject(GetRequiredInt(parameters, "instanceId"));
            return new Dictionary<string, object>
            {
                ["object"] = SummarizeGameObject(go),
                ["components"] = GetComponentSummaries(go, true)
            };
        }

        private static object GetRuntimeStatus(Dictionary<string, object> parameters)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            List<object> panels = new();

            foreach (KeyValuePair<UIManager.Panels, UEPanel> entry in UIManager.UIPanels)
            {
                UEPanel panel = entry.Value;
                panels.Add(new Dictionary<string, object>
                {
                    ["id"] = entry.Key.ToString(),
                    ["name"] = panel.Name,
                    ["active"] = panel.Enabled,
                    ["showByDefault"] = panel.ShowByDefault,
                    ["minWidth"] = panel.MinWidth,
                    ["minHeight"] = panel.MinHeight
                });
            }

            return new Dictionary<string, object>
            {
                ["name"] = ExplorerCore.NAME,
                ["version"] = ExplorerCore.VERSION,
                ["author"] = ExplorerCore.AUTHOR,
                ["guid"] = ExplorerCore.GUID,
                ["universeContext"] = Universe.Context.ToString(),
                ["unityVersion"] = Application.unityVersion,
                ["platform"] = Application.platform.ToString(),
                ["isEditor"] = Application.isEditor,
                ["isPlaying"] = Application.isPlaying,
                ["timeScale"] = Time.timeScale,
                ["realtimeSinceStartup"] = Time.realtimeSinceStartup,
                ["menuVisible"] = UIManager.ShowMenu,
                ["uiInitialized"] = !UIManager.Initializing,
                ["activeScene"] = new Dictionary<string, object>
                {
                    ["name"] = activeScene.IsValid() ? activeScene.name : "",
                    ["path"] = activeScene.IsValid() ? activeScene.path : "",
                    ["buildIndex"] = activeScene.IsValid() ? activeScene.buildIndex : -1,
                    ["isLoaded"] = activeScene.IsValid() && activeScene.isLoaded
                },
                ["panels"] = panels,
                ["mcp"] = McpBridgeController.GetStatusSnapshot()
            };
        }

        private static object GetRecentLogs(Dictionary<string, object> parameters)
        {
            int limit = Clamp(GetOptionalInt(parameters, "limit", DefaultLimit), 1, MaxLimit);
            return LogPanel.GetLogSnapshot(limit);
        }

        private static object ListConfig(Dictionary<string, object> parameters)
        {
            string category = GetOptionalString(parameters, "category");
            bool includeAdvanced = GetOptionalBool(parameters, "includeAdvanced", true);
            int limit = Clamp(GetOptionalInt(parameters, "limit", MaxLimit), 1, MaxLimit);

            List<object> entries = new();
            foreach (IConfigElement element in ConfigManager.ConfigElements.Values
                .OrderBy(it => it.Category)
                .ThenBy(it => it.Name))
            {
                if (!string.IsNullOrEmpty(category) && !string.Equals(element.Category, category, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!includeAdvanced && element.Advanced)
                    continue;

                entries.Add(new Dictionary<string, object>
                {
                    ["name"] = element.Name,
                    ["description"] = element.Description,
                    ["category"] = element.Category,
                    ["type"] = element.ElementType.FullName,
                    ["value"] = FormatConfigValue(element.BoxedValue),
                    ["defaultValue"] = FormatConfigValue(element.DefaultValue),
                    ["requiresRestart"] = element.RequiresRestart,
                    ["advanced"] = element.Advanced
                });

                if (entries.Count >= limit)
                    break;
            }

            return new Dictionary<string, object>
            {
                ["entries"] = entries,
                ["limit"] = limit,
                ["truncated"] = entries.Count >= limit,
                ["category"] = category,
                ["includeAdvanced"] = includeAdvanced
            };
        }

        private static object GetMcpStatus(Dictionary<string, object> parameters)
        {
            return McpBridgeController.GetStatusSnapshot();
        }

        private static MethodInfo ResolveMethod(Type componentType, string methodName, string[] argumentTexts)
        {
            List<MethodInfo> candidates = new();
            foreach (MethodInfo method in componentType.GetMethods(ReflectionUtility.FLAGS))
            {
                if (method.Name != methodName || method.IsGenericMethod || method.ContainsGenericParameters)
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != argumentTexts.Length)
                    continue;

                if (parameters.All(param => ParseUtility.CanParse(param.ParameterType)))
                    candidates.Add(method);
            }

            if (candidates.Count == 0)
                throw new McpBridgeException("method_not_found", $"No safe method named '{methodName}' with {argumentTexts.Length} parseable argument(s) was found.");
            if (candidates.Count > 1)
                throw new McpBridgeException("ambiguous_method", $"Method '{methodName}' has multiple safe overloads with {argumentTexts.Length} argument(s).");

            return candidates[0];
        }

        private static object[] ParseMethodArguments(MethodInfo method, string[] argumentTexts)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] parsed = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                if (!ParseUtility.TryParse(argumentTexts[i], parameterType, out parsed[i], out Exception parseException))
                {
                    string message = parseException != null ? parseException.Message : $"Cannot parse argument {i} as {parameterType.FullName}.";
                    throw new McpBridgeException("parse_failed", message);
                }
            }
            return parsed;
        }

        private static object ReadPathValue(object root, Type rootType, string path)
        {
            object current = root;
            Type currentType = rootType;
            foreach (string segment in SplitPath(path))
            {
                MemberInfo member = RequireReadableMember(currentType, segment);
                current = GetMemberValue(member, current);
                currentType = GetMemberType(member);
                if (current == null)
                    break;
            }
            return current;
        }

        private static Type GetPathValueType(Type rootType, string path)
        {
            Type currentType = rootType;
            foreach (string segment in SplitPath(path))
            {
                MemberInfo member = RequireReadableMember(currentType, segment);
                currentType = GetMemberType(member);
            }
            return currentType;
        }

        private static void WritePathValue(object root, Type rootType, string path, object value)
        {
            string[] segments = SplitPath(path);
            if (segments.Length == 0)
                throw new McpBridgeException("invalid_request", "propertyPath is required.");

            WritePathValue(root, rootType, segments, 0, value);
        }

        private static object WritePathValue(object owner, Type ownerType, string[] segments, int index, object value)
        {
            MemberInfo member = RequireReadableMember(ownerType, segments[index]);
            Type memberType = GetMemberType(member);

            if (index == segments.Length - 1)
            {
                SetMemberValue(member, owner, value);
            }
            else
            {
                object child = GetMemberValue(member, owner);
                if (child == null)
                    throw new McpBridgeException("validation_failed", $"Cannot traverse null member '{segments[index]}'.");

                object updatedChild = WritePathValue(child, memberType, segments, index + 1, value);
                SetMemberValue(member, owner, updatedChild);
            }

            return owner;
        }

        private static MemberInfo RequireReadableMember(Type type, string name)
        {
            PropertyInfo property = type.GetProperty(name, ReflectionUtility.FLAGS);
            if (property != null)
            {
                if (!property.CanRead || property.GetIndexParameters().Length != 0)
                    throw new McpBridgeException("member_not_found", $"Property '{name}' is not a readable non-indexed property on {type.FullName}.");
                return property;
            }

            FieldInfo field = type.GetField(name, ReflectionUtility.FLAGS);
            if (field != null)
                return field;

            throw new McpBridgeException("member_not_found", $"Member '{name}' was not found on {type.FullName}.");
        }

        private static object GetMemberValue(MemberInfo member, object owner)
        {
            if (member is PropertyInfo property)
                return property.GetValue(owner, null);
            return ((FieldInfo)member).GetValue(owner);
        }

        private static void SetMemberValue(MemberInfo member, object owner, object value)
        {
            if (member is PropertyInfo property)
            {
                if (!property.CanWrite)
                    throw new McpBridgeException("validation_failed", $"Property '{property.Name}' is read-only.");
                property.SetValue(owner, value, null);
                return;
            }

            FieldInfo field = (FieldInfo)member;
            if (field.IsLiteral || field.IsInitOnly)
                throw new McpBridgeException("validation_failed", $"Field '{field.Name}' is read-only.");
            field.SetValue(owner, value);
        }

        private static Type GetMemberType(MemberInfo member)
        {
            if (member is PropertyInfo property)
                return property.PropertyType;
            return ((FieldInfo)member).FieldType;
        }

        private static string[] SplitPath(string path)
        {
            return path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool IsTransformVectorMutation(Component component, string path)
        {
            if (!(component is Transform))
                return false;

            return path == "position"
                || path.StartsWith("position.")
                || path == "localPosition"
                || path.StartsWith("localPosition.")
                || path == "localScale"
                || path.StartsWith("localScale.");
        }

        private static void ValidateTransformValue(object value, Type targetType)
        {
            if (targetType == typeof(float))
            {
                ValidateFinite((float)value);
            }
            else if (targetType == typeof(Vector2))
            {
                Vector2 v = (Vector2)value;
                ValidateFinite(v.x);
                ValidateFinite(v.y);
            }
            else if (targetType == typeof(Vector3))
            {
                Vector3 v = (Vector3)value;
                ValidateFinite(v.x);
                ValidateFinite(v.y);
                ValidateFinite(v.z);
            }
            else if (targetType == typeof(Vector4))
            {
                Vector4 v = (Vector4)value;
                ValidateFinite(v.x);
                ValidateFinite(v.y);
                ValidateFinite(v.z);
                ValidateFinite(v.w);
            }
        }

        private static void ValidateFinite(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || Math.Abs(value) > MaxTransformAbs)
                throw new McpBridgeException("validation_failed", $"Transform value '{value.ToString(CultureInfo.InvariantCulture)}' is outside the allowed range.");
        }

        private static GameObject RequireGameObject(int instanceId)
        {
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (go && go.GetInstanceID() == instanceId)
                    return go;
            }

            throw new McpBridgeException("object_not_found", $"GameObject with instanceId {instanceId} was not found.");
        }

        private static Component RequireComponent(GameObject go, string componentName)
        {
            foreach (Component component in go.GetComponents<Component>())
            {
                if (!component)
                    continue;

                Type type = component.GetActualType();
                if (type.Name == componentName || type.FullName == componentName)
                    return component;
            }

            throw new McpBridgeException("component_not_found", $"Component '{componentName}' was not found on '{go.name}'.");
        }

        private static List<object> GetChildren(Transform transform, int limit)
        {
            List<object> children = new();
            int count = 0;
            foreach (Transform child in transform)
            {
                if (count++ >= limit)
                    break;
                children.Add(SummarizeGameObject(child.gameObject));
            }
            return children;
        }

        private static List<object> GetComponentSummaries(GameObject go, bool includeMembers)
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

        private static Dictionary<string, object> SummarizeGameObject(GameObject go)
        {
            Scene scene = go.scene;
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

        private static bool IsInspectable(GameObject go)
        {
            return go && go.transform.root.name != "UniverseLibCanvas" && go.name != "ExplorerBehaviour";
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

        private static bool TagEquals(GameObject go, string tag)
        {
            try
            {
                return go.CompareTag(tag);
            }
            catch
            {
                return false;
            }
        }

        private static string FormatValue(object value, Type type)
        {
            return value == null ? null : ParseUtility.ToStringForInput(value, type) ?? value.ToString();
        }

        private static string FormatConfigValue(object value)
        {
            return value == null ? null : value.ToString();
        }

        private static string GetRequiredString(Dictionary<string, object> parameters, string name)
        {
            if (!parameters.TryGetValue(name, out object value) || value == null)
                throw new McpBridgeException("invalid_request", $"'{name}' is required.");
            return value.ToString();
        }

        private static string GetOptionalString(Dictionary<string, object> parameters, string name)
        {
            return parameters.TryGetValue(name, out object value) && value != null ? value.ToString() : null;
        }

        private static int GetRequiredInt(Dictionary<string, object> parameters, string name)
        {
            if (!parameters.TryGetValue(name, out object value) || value == null)
                throw new McpBridgeException("invalid_request", $"'{name}' is required.");
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
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

        private static List<object> GetOptionalArray(Dictionary<string, object> parameters, string name)
        {
            return parameters.TryGetValue(name, out object value) && value is List<object> list
                ? list
                : new List<object>();
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
