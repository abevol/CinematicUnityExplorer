using System.Globalization;

namespace UnityExplorer.McpBridge
{
    public static class McpParameters
    {
        public static string RequiredString(Dictionary<string, object> parameters, string name)
        {
            if (!parameters.TryGetValue(name, out object value) || value == null)
                throw new McpBridgeException("invalid_request", $"'{name}' is required.");
            return value.ToString();
        }

        public static int RequiredInt(Dictionary<string, object> parameters, string name)
        {
            if (!parameters.TryGetValue(name, out object value) || value == null)
                throw new McpBridgeException("invalid_request", $"'{name}' is required.");
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        public static ulong RequiredUInt64(Dictionary<string, object> parameters, string name)
        {
            return ulong.Parse(RequiredString(parameters, name), CultureInfo.InvariantCulture);
        }

        public static string OptionalString(Dictionary<string, object> parameters, string name)
        {
            return parameters.TryGetValue(name, out object value) && value != null ? value.ToString() : null;
        }

        public static int OptionalInt(Dictionary<string, object> parameters, string name, int fallback)
        {
            return parameters.TryGetValue(name, out object value) && value != null
                ? Convert.ToInt32(value, CultureInfo.InvariantCulture)
                : fallback;
        }

        public static bool OptionalBool(Dictionary<string, object> parameters, string name, bool fallback)
        {
            return parameters.TryGetValue(name, out object value) && value != null
                ? Convert.ToBoolean(value, CultureInfo.InvariantCulture)
                : fallback;
        }

        public static float OptionalFloat(Dictionary<string, object> parameters, string name, float fallback)
        {
            return parameters.TryGetValue(name, out object value) && value != null
                ? Convert.ToSingle(value, CultureInfo.InvariantCulture)
                : fallback;
        }

        public static List<object> OptionalArray(Dictionary<string, object> parameters, string name)
        {
            return parameters.TryGetValue(name, out object value) && value is List<object> list
                ? list
                : new List<object>();
        }

        public static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        public static bool IsConfirmed(Dictionary<string, object> parameters, string confirmPhrase)
        {
            return string.Equals(OptionalString(parameters, "confirm"), confirmPhrase, StringComparison.Ordinal);
        }
    }
}
