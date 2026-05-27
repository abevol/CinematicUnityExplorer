#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesBridgeService
    {
        internal const string ConfirmPhrase = ParalivesShared.ConfirmPhrase;

        public static bool IsAvailable => ParalivesEnvironment.IsAvailable;

        public static Dictionary<string, Func<Dictionary<string, object>, object>> Actions => McpActionRegistry.Actions
            .Where(pair => pair.Key.StartsWith("paralives_", StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            if (!action.StartsWith("paralives_", StringComparison.Ordinal))
                throw new McpBridgeException("invalid_request", $"Unknown Paralives bridge action '{action}'.");

            Dictionary<string, Func<Dictionary<string, object>, object>> actions = McpActionRegistry.Actions;
            if (actions.TryGetValue(action, out Func<Dictionary<string, object>, object> handler))
                return handler(parameters);

            throw new McpBridgeException("invalid_request", $"Unknown Paralives bridge action '{action}'.");
        }

        public static object ReadResource(string uri, Dictionary<string, object> parameters)
        {
            return ParalivesStateService.ReadResource(uri, parameters);
        }

        internal static Dictionary<string, object> GetGameStateSnapshot()
        {
            return ParalivesStateService.GetGameStateSnapshot();
        }

        internal static Dictionary<string, object> GetLoadingStateSnapshot()
        {
            return ParalivesStateService.GetLoadingStateSnapshot();
        }

        internal static Dictionary<string, object> ListMainMenuActionSnapshots()
        {
            return ParalivesMenuService.ListMainMenuActionSnapshots();
        }

        internal static Dictionary<string, object> InvokeMainMenuActionForUi(string action, bool confirmed)
        {
            return ParalivesMenuService.InvokeMainMenuActionForUi(action, confirmed);
        }

        internal static Dictionary<string, object> ListSavedGamesForUi(int limit)
        {
            return ParalivesSaveService.ListSavedGamesForUi(limit);
        }

        internal static Dictionary<string, object> LoadSavedGameForUi(string argumentName, string argumentValue, bool confirmed)
        {
            return ParalivesSaveService.LoadSavedGameForUi(argumentName, argumentValue, confirmed);
        }
    }
}
#endif
