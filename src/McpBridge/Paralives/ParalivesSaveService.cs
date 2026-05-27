#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesSaveService
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_list_saved_games"] = parameters => ParalivesBridgeService.Handle("paralives_list_saved_games", parameters),
            ["paralives_load_saved_game"] = parameters => ParalivesBridgeService.Handle("paralives_load_saved_game", parameters)
        };
    }
}
#endif
