#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesStateService
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_get_type_index"] = parameters => ParalivesBridgeService.Handle("paralives_get_type_index", parameters),
            ["paralives_get_game_state"] = parameters => ParalivesBridgeService.Handle("paralives_get_game_state", parameters),
            ["paralives_get_loading_state"] = parameters => ParalivesBridgeService.Handle("paralives_get_loading_state", parameters),
            ["paralives_read_resource"] = parameters => ParalivesBridgeService.ReadResource(McpParameters.RequiredString(parameters, "uri"), parameters)
        };
    }
}
#endif
