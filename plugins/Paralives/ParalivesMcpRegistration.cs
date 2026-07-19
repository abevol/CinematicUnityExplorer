using System;
using System.Collections.Generic;
using CinematicUnityExplorer.Plugins.Paralives.Mcp;
using UnityExplorer.Plugins;

namespace CinematicUnityExplorer.Plugins.Paralives
{
    internal static class ParalivesMcpRegistration
    {
        private const string EmptySchema = "{\"type\":\"object\",\"properties\":{}}";

        public static void Register(IPluginMcpRegistry registry)
        {
            RegisterActions(registry, ParalivesStateService.Actions);
            RegisterActions(registry, ParalivesMenuService.Actions);
            RegisterActions(registry, ParalivesSaveService.Actions);
            RegisterActions(registry, ParalivesContentModService.Actions);
            RegisterActions(registry, ParalivesCollectionService.Actions);
            RegisterActions(registry, ParalivesNeedService.Actions);
            RegisterActions(registry, ParalivesCheatService.Actions);
            RegisterActions(registry, ParalivesRuntimeService.Actions);
            RegisterActions(registry, ParalivesActiveContextService.Actions);
            RegisterActions(registry, ParalivesCharacterRuntimeService.Actions);
            RegisterActions(registry, ParalivesLogService.Actions);
            RegisterActions(registry, ParalivesPerformanceCountersService.Actions);
            RegisterActions(registry, ParalivesGameDataService.Actions);

            Tool(registry, "Paralives:get_type_index", "paralives_get_type_index", "Read ParalivesBridge availability and Mono.Cecil type index summary.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_game_state", "paralives_get_game_state", "Read current Paralives scene/UI/loading state and manager availability.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:list_main_menu_actions", "paralives_list_main_menu_actions", "List whitelisted Paralives main menu actions and button availability.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:invoke_main_menu_action", "paralives_invoke_main_menu_action", "Invoke a whitelisted Paralives main menu UI button. Defaults to dry-run and requires confirmation.", ParalivesSchemas.InvokeMainMenuAction, "game-control/write", "write-confirmed");
            Tool(registry, "Paralives:list_saved_games", "paralives_list_saved_games", "List bounded saved-game candidates from manager and likely save directories.", ParalivesSchemas.ListSavedGames, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:load_saved_game", "paralives_load_saved_game", "Load a saved game when supported. Defaults to dry-run and requires confirmation.", ParalivesSchemas.LoadSavedGame, "game-control/write", "write-confirmed");
            Tool(registry, "Paralives:start_new_game", "paralives_start_new_game", "Start a new game via whitelisted UI action. Defaults to dry-run and requires confirmation.", ParalivesSchemas.DryRunConfirm, "game-control/write", "write-confirmed");
            Tool(registry, "Paralives:get_loading_state", "paralives_get_loading_state", "Read GameLoadingManager state and active scene.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:list_content_mods", "paralives_list_content_mods", "List Paralives .mod folders and metadata.", EmptySchema, "filesystem/mod", "read-only");
            Tool(registry, "Paralives:inspect_content_mod", "paralives_inspect_content_mod", "Inspect files and .meta data inside a content mod folder.", ParalivesSchemas.ModPath, "filesystem/mod", "read-only");
            Tool(registry, "Paralives:create_content_mod", "paralives_create_content_mod", "Create a new content mod folder and .mod.meta file. Defaults to dry-run.", ParalivesSchemas.CreateContentMod, "filesystem/mod", "filesystem-confirmed");
            Tool(registry, "Paralives:import_asset_to_mod", "paralives_import_asset_to_mod", "Copy an asset into a content mod and create schema-aware metadata. Defaults to dry-run.", ParalivesSchemas.ImportAsset, "filesystem/mod", "filesystem-confirmed");
            Tool(registry, "Paralives:list_characters", "paralives_list_characters", "List loaded Paralives characters through the whitelisted manager collection.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:list_households", "paralives_list_households", "List loaded Paralives households through the whitelisted manager collection.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:list_lots", "paralives_list_lots", "List loaded Paralives lots through the whitelisted manager collection.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:set_need_value", "paralives_set_need_value", "Set a character need value. Defaults to dry-run and requires confirmation.", ParalivesSchemas.SetNeedValue, "game-control/write", "write-confirmed");
            Tool(registry, "Paralives:list_cheat_commands", "paralives_list_cheat_commands", "List whitelisted diagnostic cheat commands.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:run_whitelisted_cheat", "paralives_run_whitelisted_cheat", "Run a whitelisted diagnostic cheat command. Defaults to dry-run and requires confirmation.", ParalivesSchemas.RunCheat, "game-control/write", "write-confirmed");
            Tool(registry, "Paralives:get_runtime_summary", "paralives_get_runtime_summary", "Read current Paralives runtime summary: time, funds, mode, selection, and family.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_game_time", "paralives_get_game_time", "Read game pause/speed/formatted time state.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_economy", "paralives_get_economy", "Read household funds and economic state.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_selection", "paralives_get_selection", "Read currently selected object/character.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_active_context", "paralives_get_active_context", "Read active household, character, and lot.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_character_needs", "paralives_get_character_needs", "Read character needs/status.", ParalivesSchemas.CharacterGuid, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_character_actions", "paralives_get_character_actions", "Read current and queued actions for a character.", ParalivesSchemas.CharacterGuid, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_performance_stats", "paralives_get_performance_stats", "Read lightweight performance counters: FPS, managed heap, GC, cached scene stats. Does not force scene-wide scans.", EmptySchema, "performance", "read-only");
            Tool(registry, "Paralives:get_performance_history", "paralives_get_performance_history", "Read recent FPS counter history.", ParalivesSchemas.PerformanceHistory, "performance", "read-only");
            Tool(registry, "Paralives:get_memory_stats", "paralives_get_memory_stats", "Read managed heap and GC counters.", EmptySchema, "performance", "read-only");
            Tool(registry, "Paralives:get_scene_stats", "paralives_get_scene_stats", "Read cached scene object/component counts; forceRefresh triggers an explicit scene-wide scan.", ParalivesSchemas.SceneStats, "performance", "read-only");
            Tool(registry, "Paralives:get_frame_timing", "paralives_get_frame_timing", "Read Unity FrameTimingManager samples when available; safely returns supported:false on unsupported runtimes.", EmptySchema, "performance", "read-only");
            Tool(registry, "Paralives:list_profiler_counters", "paralives_list_profiler_counters", "List Unity ProfilerRecorder counters by reflection with optional query/category filtering.", ParalivesSchemas.ListProfilerCounters, "performance", "read-only");
            Tool(registry, "Paralives:get_profiler_counter_samples", "paralives_get_profiler_counter_samples", "Read latest values from cached Unity ProfilerRecorders; first call can return warmingUp.", ParalivesSchemas.ProfilerCounterSamples, "performance", "read-only");
            Tool(registry, "Paralives:get_skill_data", "paralives_get_skill_data", "Read skill data from UISkillsInProgressAndUpcomingEvents. Returns skill names, levels, and progress.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_emotion_data", "paralives_get_emotion_data", "Read emotion data from UIThoughts/Emotions panel. Returns emotion names, types, and values.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_memory_data", "paralives_get_memory_data", "Read memory data from MemoryManager. Returns character memories and experiences.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_goals_data", "paralives_get_goals_data", "Read goals/wants data from GoalsManager. Returns active goals and their progress.", EmptySchema, "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:get_game_logs", "get_game_logs", "Read game console logs from Unity log callback.", "{\"type\":\"object\",\"properties\":{\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":500,\"default\":50},\"type\":{\"type\":\"string\",\"enum\":[\"all\",\"log\",\"warning\",\"exception\"],\"default\":\"all\"},\"includeCollapsed\":{\"type\":\"boolean\",\"default\":true}}}", "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:subscribe_logs", "subscribe_logs", "Subscribe to Unity log callback for real-time logs.", "{\"type\":\"object\",\"properties\":{\"bufferSize\":{\"type\":\"integer\",\"minimum\":10,\"maximum\":1000,\"default\":100},\"types\":{\"type\":\"array\",\"items\":{\"type\":\"string\",\"enum\":[\"log\",\"warning\",\"exception\"]},\"default\":[\"log\",\"warning\",\"exception\"]}}}", "diagnostics/read-only", "read-only");
            Tool(registry, "Paralives:poll_logs", "poll_logs", "Poll a subscribed log stream.", "{\"type\":\"object\",\"properties\":{\"subscriptionId\":{\"type\":\"string\"},\"since\":{\"type\":\"integer\"},\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":200,\"default\":50}},\"required\":[\"subscriptionId\"]}", "diagnostics/read-only", "read-only");

            registry.RegisterResource(new PluginMcpResourceDescriptor("paralives://types/managers", "Paralives Manager Types", "Mono.Cecil index of Paralives manager-like types.", "application/json", "paralives_read_resource", new Dictionary<string, object> { ["uri"] = "paralives://types/managers" }));
            registry.RegisterResource(new PluginMcpResourceDescriptor("paralives://types/settings", "Paralives Setting Types", "Mono.Cecil index of Paralives setting data types.", "application/json", "paralives_read_resource", new Dictionary<string, object> { ["uri"] = "paralives://types/settings" }));
            registry.RegisterResource(new PluginMcpResourceDescriptor("paralives://types/cheats", "Paralives Cheat Types", "Mono.Cecil index of Paralives cheat-related types.", "application/json", "paralives_read_resource", new Dictionary<string, object> { ["uri"] = "paralives://types/cheats" }));
        }

        private static void RegisterActions(IPluginMcpRegistry registry, Dictionary<string, Func<Dictionary<string, object>, object>> actions)
        {
            foreach (KeyValuePair<string, Func<Dictionary<string, object>, object>> action in actions)
                registry.RegisterAction(action.Key, action.Value);
        }

        private static void Tool(IPluginMcpRegistry registry, string name, string action, string description, string schema, string group, string risk)
        {
            registry.RegisterTool(new PluginMcpToolDescriptor(name, action, description, schema, group, risk));
        }
    }
}
