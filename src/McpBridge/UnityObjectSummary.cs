using System.Globalization;
using UnityEngine.SceneManagement;

namespace UnityExplorer.McpBridge
{
    internal static class UnityObjectSummary
    {
        public static Dictionary<string, object> FromGameObject(GameObject go)
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

        public static string GetTag(GameObject go)
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

        public static bool TagEquals(GameObject go, string tag)
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

        public static object RuntimeValue(object value)
        {
            if (value == null)
                return null;

            Type type = value.GetType();
            if (type.IsPrimitive || value is string || value is decimal || type.IsEnum)
                return value;

            if (value is UnityEngine.Object unityObject)
            {
                if (!unityObject)
                    return null;

                GameObject gameObject = unityObject as GameObject;
                if (!gameObject)
                {
                    Component component = unityObject.TryCast<Component>();
                    gameObject = component ? component.gameObject : null;
                }

                return gameObject ? FromGameObject(gameObject) : unityObject.ToString();
            }

            return value.ToString();
        }

        public static Dictionary<string, object> DomainObject(object obj)
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
                    object value = UnityReflectionUtility.ReadMember(obj, type, memberName);
                    if (value != null)
                        summary[memberName] = value.ToString();
                }
                catch
                {
                }
            }

            return summary;
        }

        public static Dictionary<string, object> File(string file)
        {
            FileInfo info = new(file);
            return new Dictionary<string, object>
            {
                ["path"] = file,
                ["name"] = info.Name,
                ["extension"] = info.Extension,
                ["length"] = info.Length,
                ["lastWriteTimeUtc"] = info.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture)
            };
        }
    }
}
