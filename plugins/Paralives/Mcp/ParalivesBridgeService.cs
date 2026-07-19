#if MONO
namespace CinematicUnityExplorer.Plugins.Paralives.Mcp
{
    internal static class ParalivesBridgeService
    {
        internal const string ConfirmPhrase = ParalivesShared.ConfirmPhrase;

        public static bool IsAvailable => ParalivesEnvironment.IsAvailable;

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
