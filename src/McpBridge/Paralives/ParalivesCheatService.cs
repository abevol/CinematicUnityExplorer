#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesCheatService
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_list_cheat_commands"] = parameters => ParalivesBridgeService.Handle("paralives_list_cheat_commands", parameters),
            ["paralives_run_whitelisted_cheat"] = parameters => ParalivesBridgeService.Handle("paralives_run_whitelisted_cheat", parameters)
        };
    }
}
#endif
