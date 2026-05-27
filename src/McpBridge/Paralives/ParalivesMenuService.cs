#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesMenuService
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_list_main_menu_actions"] = parameters => ParalivesBridgeService.Handle("paralives_list_main_menu_actions", parameters),
            ["paralives_invoke_main_menu_action"] = parameters => ParalivesBridgeService.Handle("paralives_invoke_main_menu_action", parameters),
            ["paralives_start_new_game"] = parameters => ParalivesBridgeService.Handle("paralives_start_new_game", parameters)
        };
    }
}
#endif
