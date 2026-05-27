#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesNeedService
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_set_need_value"] = parameters => ParalivesBridgeService.Handle("paralives_set_need_value", parameters)
        };
    }
}
#endif
