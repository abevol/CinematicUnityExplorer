using System.Globalization;

namespace UnityExplorer.McpBridge
{
    internal static class UnityComponentService
    {
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

        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["set_component_property"] = SetComponentProperty,
            ["call_component_method"] = CallComponentMethod
        };

        private static object SetComponentProperty(Dictionary<string, object> parameters)
        {
            GameObject go = UnityObjectService.RequireGameObject(McpParameters.RequiredInt(parameters, "instanceId"));
            Component component = UnityObjectService.RequireComponent(go, McpParameters.RequiredString(parameters, "componentName"));
            string propertyPath = McpParameters.RequiredString(parameters, "propertyPath");
            string valueText = McpParameters.RequiredString(parameters, "value");

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
            GameObject go = UnityObjectService.RequireGameObject(McpParameters.RequiredInt(parameters, "instanceId"));
            Component component = UnityObjectService.RequireComponent(go, McpParameters.RequiredString(parameters, "componentName"));
            string methodName = McpParameters.RequiredString(parameters, "methodName");
            List<object> argumentValues = McpParameters.OptionalArray(parameters, "arguments");
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

        private static string FormatValue(object value, Type type)
        {
            return value == null ? null : ParseUtility.ToStringForInput(value, type) ?? value.ToString();
        }
    }
}
