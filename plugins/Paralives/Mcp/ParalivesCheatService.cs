#if MONO
namespace CinematicUnityExplorer.Plugins.Paralives.Mcp
{
    internal static class ParalivesCheatService
    {
        private static readonly HashSet<string> allowedCheats = new(StringComparer.OrdinalIgnoreCase)
        {
            "UNITYOBJECTCOUNT",
            "ASSETCOUNT",
            "ASSETCOUNTBYSIZE",
            "SHOWANIMATIONS",
            "SHOWANIMATIONSCONTAINERS"
        };

        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_list_cheat_commands"] = _ => ListCheatCommands(),
            ["paralives_run_whitelisted_cheat"] = RunWhitelistedCheat
        };

        private static object ListCheatCommands()
        {
            ParalivesShared.EnsureAvailable();
            List<object> methods = ParalivesEnvironment.TypeIndex.Cheats
                .Where(type => string.Equals(type["name"]?.ToString(), "ProcessCheatCommandEvent", StringComparison.Ordinal))
                .SelectMany(type => type["methods"] as List<object> ?? new List<object>())
                .Where(method => method is Dictionary<string, object> dict && allowedCheats.Contains(dict["name"]?.ToString() ?? ""))
                .ToList();

            return new Dictionary<string, object>
            {
                ["allowedCheats"] = methods,
                ["policy"] = "Only read-only diagnostic cheats are exposed. Add explicit whitelist entries in ParalivesCheatService for more commands."
            };
        }

        private static object RunWhitelistedCheat(Dictionary<string, object> parameters)
        {
            ParalivesShared.EnsureAvailable();
            string command = McpParameters.RequiredString(parameters, "command").Trim();
            bool dryRun = McpParameters.OptionalBool(parameters, "dryRun", true);
            bool confirmed = ParalivesShared.IsConfirmed(parameters);
            string commandName = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

            if (!allowedCheats.Contains(commandName))
                throw new McpBridgeException("validation_failed", $"Cheat '{commandName}' is not whitelisted.");

            Dictionary<string, object> result = new()
            {
                ["operation"] = "run_whitelisted_cheat",
                ["command"] = command,
                ["dryRun"] = dryRun,
                ["confirmed"] = confirmed
            };

            if (dryRun || !confirmed)
            {
                result["requiredConfirm"] = ParalivesShared.ConfirmPhrase;
                return result;
            }

            Type messageType = ReflectionUtility.GetTypeByName("MessageProcessCheatCommand");
            Type eventSystemType = ReflectionUtility.GetTypeByName("EventSystem");
            if (messageType == null || eventSystemType == null)
                throw new McpBridgeException("execution_failed", "Could not find Paralives cheat event types.");

            object message = Activator.CreateInstance(messageType);
            UnityReflectionUtility.WriteMember(message, messageType, "CommandID", UnityEngine.Random.Range(1, int.MaxValue));
            UnityReflectionUtility.WriteMember(message, messageType, "Command", command);

            MethodInfo broadcast = eventSystemType.GetMethods(ReflectionUtility.FLAGS)
                .FirstOrDefault(method => method.Name == "Broadcast" && method.GetParameters().Length == 1);
            if (broadcast == null)
                throw new McpBridgeException("execution_failed", "Could not find EventSystem.Broadcast(message).");

            broadcast.Invoke(null, new[] { message });
            result["sent"] = true;
            return result;
        }
    }
}
#endif
