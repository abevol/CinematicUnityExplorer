#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesBridgeService
    {
        internal const string ConfirmPhrase = ParalivesBridgeCore.ConfirmPhrase;

        public static bool IsAvailable => ParalivesBridgeCore.IsAvailable;

        public static Dictionary<string, Func<Dictionary<string, object>, object>> Actions => ParalivesBridgeCore.Actions;

        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            return ParalivesBridgeCore.Handle(action, parameters);
        }

        public static object ReadResource(string uri, Dictionary<string, object> parameters)
        {
            return ParalivesBridgeCore.ReadResource(uri, parameters);
        }

        internal static Dictionary<string, object> GetGameStateSnapshot()
        {
            return ParalivesBridgeCore.GetGameStateSnapshot();
        }

        internal static Dictionary<string, object> GetLoadingStateSnapshot()
        {
            return ParalivesBridgeCore.GetLoadingStateSnapshot();
        }

        internal static Dictionary<string, object> ListMainMenuActionSnapshots()
        {
            return ParalivesBridgeCore.ListMainMenuActionSnapshots();
        }

        internal static Dictionary<string, object> InvokeMainMenuActionForUi(string action, bool confirmed)
        {
            return ParalivesBridgeCore.InvokeMainMenuActionForUi(action, confirmed);
        }

        internal static Dictionary<string, object> ListSavedGamesForUi(int limit)
        {
            return ParalivesBridgeCore.ListSavedGamesForUi(limit);
        }

        internal static Dictionary<string, object> LoadSavedGameForUi(string argumentName, string argumentValue, bool confirmed)
        {
            return ParalivesBridgeCore.LoadSavedGameForUi(argumentName, argumentValue, confirmed);
        }
    }
}
#endif
