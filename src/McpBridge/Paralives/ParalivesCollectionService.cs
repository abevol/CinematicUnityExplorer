#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesCollectionService
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_list_characters"] = parameters => ParalivesBridgeService.Handle("paralives_list_characters", parameters),
            ["paralives_list_households"] = parameters => ParalivesBridgeService.Handle("paralives_list_households", parameters),
            ["paralives_list_lots"] = parameters => ParalivesBridgeService.Handle("paralives_list_lots", parameters)
        };
    }
}
#endif
