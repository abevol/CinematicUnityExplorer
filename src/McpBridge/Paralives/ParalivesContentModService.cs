#if MONO
namespace UnityExplorer.McpBridge.Paralives
{
    internal static class ParalivesContentModService
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["paralives_list_content_mods"] = parameters => ParalivesBridgeService.Handle("paralives_list_content_mods", parameters),
            ["paralives_inspect_content_mod"] = parameters => ParalivesBridgeService.Handle("paralives_inspect_content_mod", parameters),
            ["paralives_create_content_mod"] = parameters => ParalivesBridgeService.Handle("paralives_create_content_mod", parameters),
            ["paralives_import_asset_to_mod"] = parameters => ParalivesBridgeService.Handle("paralives_import_asset_to_mod", parameters)
        };
    }
}
#endif
