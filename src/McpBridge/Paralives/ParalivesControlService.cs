#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesControlService
    {
        public static bool IsAvailable => ParalivesBridgeService.IsAvailable;

        public static string RequiredConfirmPhrase => ParalivesBridgeService.ConfirmPhrase;

        public static Dictionary<string, object> GetGameState()
            => ParalivesBridgeService.GetGameStateSnapshot();

        public static Dictionary<string, object> GetLoadingState()
            => ParalivesBridgeService.GetLoadingStateSnapshot();

        public static Dictionary<string, object> ListMainMenuActions()
            => ParalivesBridgeService.ListMainMenuActionSnapshots();

        public static Dictionary<string, object> InvokeMainMenuAction(string action, bool confirmed)
            => ParalivesBridgeService.InvokeMainMenuActionForUi(action, confirmed);

        public static Dictionary<string, object> ListSavedGames(int limit)
            => ParalivesBridgeService.ListSavedGamesForUi(limit);

        public static Dictionary<string, object> LoadSavedGame(string argumentName, string argumentValue, bool confirmed)
            => ParalivesBridgeService.LoadSavedGameForUi(argumentName, argumentValue, confirmed);
    }
}
#endif
