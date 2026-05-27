namespace UnityExplorer.McpBridge
{
    internal static class UnityReflectionUtility
    {
        public static bool TryReadMember(object owner, Type type, string memberName, out object value)
        {
            value = null;
            if (type == null)
                return false;

            try
            {
                PropertyInfo property = type.GetProperty(memberName, ReflectionUtility.FLAGS);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    value = property.GetValue(owner, null);
                    return true;
                }

                FieldInfo field = type.GetField(memberName, ReflectionUtility.FLAGS);
                if (field != null)
                {
                    value = field.GetValue(owner);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public static object ReadMember(object owner, Type type, string memberName)
        {
            if (TryReadMember(owner, type, memberName, out object value))
                return value;

            throw new McpBridgeException("member_not_found", $"{type.FullName}.{memberName} was not found.");
        }

        public static object GetSingletonInstance(Type type)
        {
            if (type == null)
                return null;

            foreach (string memberName in new[] { "Instance", "_instance", "instance", "<Instance>k__BackingField" })
            {
                if (TryReadMember(null, type, memberName, out object value) && value != null)
                    return value;
            }

            try
            {
                if (TryReadMember(null, type, "lazy", out object lazy) && lazy != null)
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

        public static GameObject FindGameObjectByName(string name)
        {
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                GameObject go = obj.TryCast<GameObject>();
                if (go && go.name == name)
                    return go;
            }
            return null;
        }

        public static Component FindComponentByName(GameObject go, string componentName)
        {
            if (!go)
                return null;

            foreach (Component component in go.GetComponents<Component>())
            {
                if (!component)
                    continue;

                Type type = component.GetActualType();
                if (type.Name == componentName || type.FullName == componentName)
                    return component;
            }
            return null;
        }

        public static string GetPath(GameObject go)
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
    }
}
