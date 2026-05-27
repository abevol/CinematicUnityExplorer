#!/usr/bin/env node
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListResourceTemplatesRequestSchema,
  ListResourcesRequestSchema,
  ListToolsRequestSchema,
  ReadResourceRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { UnityBridgeClient } from "./unity-bridge-client.js";

type JsonSchema = {
  type: "object";
  properties: Record<string, unknown>;
  required?: readonly string[];
};

type ToolDefinition = {
  name: string;
  action: string;
  description: string;
  inputSchema: JsonSchema;
  group: "diagnostics/read-only" | "performance" | "game-control/write" | "filesystem/mod";
  risk: "read-only" | "write-confirmed" | "filesystem-confirmed";
};

const bridge = new UnityBridgeClient({
  host: process.env.UNITY_EXPLORER_MCP_HOST ?? "127.0.0.1",
  port: Number.parseInt(process.env.UNITY_EXPLORER_MCP_PORT ?? "8765", 10),
  timeoutMs: Number.parseInt(process.env.UNITY_EXPLORER_MCP_TIMEOUT_MS ?? "5000", 10),
});

const server = new Server(
  { name: "unity-explorer-mcp", version: "1.0.0" },
  { capabilities: { tools: {}, resources: {} } },
);

const emptySchema = { type: "object", properties: {} } as const;
const findGameObjectsSchema = {
  type: "object",
  properties: {
    query: { type: "string" },
    tag: { type: "string" },
    includeInactive: { type: "boolean", default: true },
    limit: { type: "integer", minimum: 1, maximum: 200, default: 50 },
  },
} as const;
const instanceIdSchema = { type: "object", properties: { instanceId: { type: "integer" } }, required: ["instanceId"] } as const;
const recentLogsSchema = { type: "object", properties: { limit: { type: "integer", minimum: 1, maximum: 200, default: 50 } } } as const;
const listConfigSchema = {
  type: "object",
  properties: {
    category: { type: "string", enum: ["General", "UI", "MCP", "Paralives", "Console", "Inspector", "Export", "Advanced"] },
    includeAdvanced: { type: "boolean", default: true },
    limit: { type: "integer", minimum: 1, maximum: 200, default: 200 },
  },
} as const;
const setComponentPropertySchema = {
  type: "object",
  properties: {
    instanceId: { type: "integer" },
    componentName: { type: "string" },
    propertyPath: { type: "string" },
    value: { type: "string" },
  },
  required: ["instanceId", "componentName", "propertyPath", "value"],
} as const;
const callComponentMethodSchema = {
  type: "object",
  properties: {
    instanceId: { type: "integer" },
    componentName: { type: "string" },
    methodName: { type: "string" },
    arguments: { type: "array", items: { type: "string" }, default: [] },
  },
  required: ["instanceId", "componentName", "methodName"],
} as const;
const paralivesModPathSchema = {
  type: "object",
  properties: { modPath: { type: "string" }, limit: { type: "integer", minimum: 1, maximum: 200, default: 100 } },
  required: ["modPath"],
} as const;
const paralivesCreateContentModSchema = {
  type: "object",
  properties: { modName: { type: "string" }, dryRun: { type: "boolean", default: true }, confirm: { type: "string" } },
  required: ["modName"],
} as const;
const paralivesImportAssetSchema = {
  type: "object",
  properties: {
    sourcePath: { type: "string" },
    modPath: { type: "string" },
    subFolder: { type: "string" },
    dryRun: { type: "boolean", default: true },
    confirm: { type: "string" },
  },
  required: ["sourcePath", "modPath"],
} as const;
const paralivesRunCheatSchema = {
  type: "object",
  properties: { command: { type: "string" }, dryRun: { type: "boolean", default: true }, confirm: { type: "string" } },
  required: ["command"],
} as const;
const paralivesSetNeedValueSchema = {
  type: "object",
  properties: {
    characterGuid: { type: "string" },
    needGuid: { type: "string" },
    value: { type: "string" },
    force: { type: "boolean", default: true },
    dryRun: { type: "boolean", default: true },
    confirm: { type: "string" },
  },
  required: ["characterGuid", "needGuid", "value"],
} as const;
const paralivesInvokeMainMenuActionSchema = {
  type: "object",
  properties: {
    action: { type: "string", enum: ["continue_game", "new_game", "load_game_menu", "mod_editor", "options"] },
    dryRun: { type: "boolean", default: true },
    confirm: { type: "string" },
  },
  required: ["action"],
} as const;
const paralivesListSavedGamesSchema = {
  type: "object",
  properties: { limit: { type: "integer", minimum: 1, maximum: 100, default: 50 } },
} as const;
const paralivesLoadSavedGameSchema = {
  type: "object",
  properties: {
    saveId: { type: "string" },
    saveName: { type: "string" },
    savePath: { type: "string" },
    dryRun: { type: "boolean", default: true },
    confirm: { type: "string" },
  },
} as const;
const getGameLogsSchema = {
  type: "object",
  properties: {
    limit: { type: "integer", minimum: 1, maximum: 500, default: 50 },
    type: { type: "string", enum: ["all", "log", "warning", "exception"], default: "all" },
    includeCollapsed: { type: "boolean", default: true },
  },
} as const;
const subscribeLogsSchema = {
  type: "object",
  properties: {
    bufferSize: { type: "integer", minimum: 10, maximum: 1000, default: 100 },
    types: { type: "array", items: { type: "string", enum: ["log", "warning", "exception"] }, default: ["log", "warning", "exception"] },
  },
} as const;
const pollLogsSchema = {
  type: "object",
  properties: {
    subscriptionId: { type: "string" },
    since: { type: "integer" },
    limit: { type: "integer", minimum: 1, maximum: 200, default: 50 },
  },
  required: ["subscriptionId"],
} as const;
const characterGuidSchema = {
  type: "object",
  properties: { characterGuid: { type: "string", description: "Character GUID. If omitted, uses currently selected character." } },
} as const;
const getPerformanceHistorySchema = {
  type: "object",
  properties: { limit: { type: "integer", minimum: 1, maximum: 100, default: 50 } },
} as const;
const getSceneStatsSchema = {
  type: "object",
  properties: { forceRefresh: { type: "boolean", default: false, description: "Force a scene-wide object scan; otherwise cached stats are returned." } },
} as const;
const listProfilerCountersSchema = {
  type: "object",
  properties: {
    query: { type: "string" },
    category: { type: "string" },
    limit: { type: "integer", minimum: 1, maximum: 500, default: 100 },
  },
} as const;
const getProfilerCounterSamplesSchema = {
  type: "object",
  properties: {
    counters: {
      type: "array",
      items: {
        oneOf: [
          { type: "string", description: "Counter name, or Category/Counter Name." },
          {
            type: "object",
            properties: { name: { type: "string" }, category: { type: "string" } },
            required: ["name"],
          },
        ],
      },
      minItems: 1,
    },
  },
  required: ["counters"],
} as const;

const toolDefinitions: ToolDefinition[] = [
  tool("UnityExplorer:find_game_objects", "find_game_objects", "Find Unity GameObjects by name/path substring or tag. Risk: read-only diagnostics.", findGameObjectsSchema),
  tool("UnityExplorer:get_object_detail", "get_object_detail", "Read a GameObject summary, direct children, components, and parseable component members. Risk: read-only diagnostics.", instanceIdSchema),
  tool("UnityExplorer:set_component_property", "set_component_property", "Set a parseable component field/property. Risk: writes game state; use only with intent.", setComponentPropertySchema, "game-control/write", "write-confirmed"),
  tool("UnityExplorer:call_component_method", "call_component_method", "Call a bounded component method with parseable string arguments and rate limiting. Risk: writes or triggers gameplay depending on method.", callComponentMethodSchema, "game-control/write", "write-confirmed"),
  tool("UnityExplorer:get_runtime_status", "get_runtime_status", "Read UnityExplorer runtime diagnostics including bridge status and MCP request budget.", emptySchema),
  tool("UnityExplorer:get_recent_logs", "get_recent_logs", "Read recent UnityExplorer log entries and the current log file path. Risk: read-only diagnostics.", recentLogsSchema),
  tool("UnityExplorer:list_config", "list_config", "Read UnityExplorer config entries. Risk: read-only diagnostics.", listConfigSchema),
  tool("UnityExplorer:get_mcp_status", "get_mcp_status", "Read MCP bridge diagnostics including pending requests, per-frame budget, and recent request durations. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:get_type_index", "paralives_get_type_index", "Read ParalivesBridge availability and Mono.Cecil type index summary. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:get_game_state", "paralives_get_game_state", "Read current Paralives scene/UI/loading state and manager availability. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:list_main_menu_actions", "paralives_list_main_menu_actions", "List whitelisted Paralives main menu actions and button availability. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:invoke_main_menu_action", "paralives_invoke_main_menu_action", "Invoke a whitelisted Paralives main menu UI button. Defaults to dry-run and requires confirmation. Risk: game-control/write.", paralivesInvokeMainMenuActionSchema, "game-control/write", "write-confirmed"),
  tool("Paralives:list_saved_games", "paralives_list_saved_games", "List bounded saved-game candidates from manager and likely save directories. Risk: read-only diagnostics.", paralivesListSavedGamesSchema),
  tool("Paralives:load_saved_game", "paralives_load_saved_game", "Load a saved game when supported. Defaults to dry-run and requires confirmation. Risk: game-control/write.", paralivesLoadSavedGameSchema, "game-control/write", "write-confirmed"),
  tool("Paralives:start_new_game", "paralives_start_new_game", "Start a new game via whitelisted UI action. Defaults to dry-run and requires confirmation. Risk: game-control/write.", { ...emptySchema, properties: { dryRun: { type: "boolean", default: true }, confirm: { type: "string" } } }, "game-control/write", "write-confirmed"),
  tool("Paralives:get_loading_state", "paralives_get_loading_state", "Read GameLoadingManager state and active scene. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:list_content_mods", "paralives_list_content_mods", "List Paralives .mod folders and metadata. Risk: filesystem read.", emptySchema, "filesystem/mod"),
  tool("Paralives:inspect_content_mod", "paralives_inspect_content_mod", "Inspect files and .meta data inside a content mod folder. Risk: filesystem read.", paralivesModPathSchema, "filesystem/mod"),
  tool("Paralives:create_content_mod", "paralives_create_content_mod", "Create a new content mod folder and .mod.meta file. Defaults to dry-run. Risk: filesystem write.", paralivesCreateContentModSchema, "filesystem/mod", "filesystem-confirmed"),
  tool("Paralives:import_asset_to_mod", "paralives_import_asset_to_mod", "Copy an asset into a content mod and create schema-aware metadata. Defaults to dry-run. Risk: filesystem write.", paralivesImportAssetSchema, "filesystem/mod", "filesystem-confirmed"),
  tool("Paralives:list_characters", "paralives_list_characters", "List loaded Paralives characters through the whitelisted manager collection. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:list_households", "paralives_list_households", "List loaded Paralives households through the whitelisted manager collection. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:list_lots", "paralives_list_lots", "List loaded Paralives lots through the whitelisted manager collection. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:set_need_value", "paralives_set_need_value", "Set a character need value. Defaults to dry-run and requires confirmation. Risk: game-control/write.", paralivesSetNeedValueSchema, "game-control/write", "write-confirmed"),
  tool("Paralives:list_cheat_commands", "paralives_list_cheat_commands", "List whitelisted diagnostic cheat commands. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:run_whitelisted_cheat", "paralives_run_whitelisted_cheat", "Run a whitelisted diagnostic cheat command. Defaults to dry-run and requires confirmation. Risk: game-control/write.", paralivesRunCheatSchema, "game-control/write", "write-confirmed"),
  tool("Paralives:get_runtime_summary", "paralives_get_runtime_summary", "Read current Paralives runtime summary: time, funds, mode, selection, and family. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:get_game_time", "paralives_get_game_time", "Read game pause/speed/formatted time state. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:get_economy", "paralives_get_economy", "Read household funds and economic state. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:get_selection", "paralives_get_selection", "Read currently selected object/character. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:get_active_context", "paralives_get_active_context", "Read active household, character, and lot. Risk: read-only diagnostics.", emptySchema),
  tool("Paralives:get_character_needs", "paralives_get_character_needs", "Read character needs/status. Risk: read-only diagnostics.", characterGuidSchema),
  tool("Paralives:get_character_actions", "paralives_get_character_actions", "Read current and queued actions for a character. Risk: read-only diagnostics.", characterGuidSchema),
  tool("Paralives:get_performance_stats", "paralives_get_performance_stats", "Read lightweight performance counters: FPS, managed heap, GC, cached scene stats. Does not force scene-wide scans. Risk: read-only performance.", emptySchema, "performance"),
  tool("Paralives:get_performance_history", "paralives_get_performance_history", "Read recent FPS counter history. Risk: read-only performance.", getPerformanceHistorySchema, "performance"),
  tool("Paralives:get_memory_stats", "paralives_get_memory_stats", "Read managed heap and GC counters. Risk: read-only performance.", emptySchema, "performance"),
  tool("Paralives:get_scene_stats", "paralives_get_scene_stats", "Read cached scene object/component counts; forceRefresh triggers a bounded-on-demand scene scan. Risk: read-only performance.", getSceneStatsSchema, "performance"),
  tool("Paralives:get_frame_timing", "paralives_get_frame_timing", "Read Unity FrameTimingManager samples when available; safely returns supported:false on unsupported runtimes. Risk: read-only performance.", emptySchema, "performance"),
  tool("Paralives:list_profiler_counters", "paralives_list_profiler_counters", "List Unity ProfilerRecorder counters by reflection with optional query/category filtering. Risk: read-only performance.", listProfilerCountersSchema, "performance"),
  tool("Paralives:get_profiler_counter_samples", "paralives_get_profiler_counter_samples", "Read latest values from cached Unity ProfilerRecorders; first call can return warmingUp. Risk: read-only performance.", getProfilerCounterSamplesSchema, "performance"),
  tool("UnityExplorer:get_game_logs", "get_game_logs", "Read game console logs from Unity log callback. Risk: read-only diagnostics.", getGameLogsSchema),
  tool("UnityExplorer:subscribe_logs", "subscribe_logs", "Subscribe to Unity log callback for real-time logs. Risk: read-only diagnostics.", subscribeLogsSchema),
  tool("UnityExplorer:poll_logs", "poll_logs", "Poll a subscribed log stream. Risk: read-only diagnostics.", pollLogsSchema),
];

const toolActionByName = new Map(toolDefinitions.map((definition) => [definition.name, definition.action]));

server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: toolDefinitions.map(({ name, description, inputSchema, group, risk }) => ({
    name,
    description: `${description} Group: ${group}; risk: ${risk}.`,
    inputSchema,
  })),
}));

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const action = toolActionByName.get(request.params.name);
  if (!action) {
    return toolError("invalid_request", `Unknown tool '${request.params.name}'.`);
  }

  try {
    const response = await bridge.request(action, (request.params.arguments ?? {}) as Record<string, unknown>);
    if (!response.ok) {
      return toolError(response.error.code, response.error.message);
    }

    return { content: [{ type: "text" as const, text: JSON.stringify(response.result, null, 2) }] };
  } catch (error) {
    return toolError(classifyBridgeError(error), error instanceof Error ? error.message : String(error));
  }
});

server.setRequestHandler(ListResourcesRequestSchema, async () => ({
  resources: [
    { uri: "unity://scene/hierarchy", name: "Unity Scene Hierarchy", description: "Current Unity scene hierarchy, bounded to roots and shallow children.", mimeType: "application/json" },
    { uri: "unity://runtime/status", name: "UnityExplorer Runtime Status", description: "Runtime diagnostics including Unity version, active scene, panels, and bridge status.", mimeType: "application/json" },
    { uri: "unity://config/options", name: "UnityExplorer Config Options", description: "UnityExplorer config entries with categories and current values.", mimeType: "application/json" },
    { uri: "unity://mcp/status", name: "UnityExplorer MCP Status", description: "MCP bridge listening state, per-frame budget, and recent request diagnostics.", mimeType: "application/json" },
    { uri: "paralives://types/managers", name: "Paralives Manager Types", description: "Mono.Cecil index of Paralives manager-like types.", mimeType: "application/json" },
    { uri: "paralives://types/settings", name: "Paralives Setting Types", description: "Mono.Cecil index of Paralives setting data types.", mimeType: "application/json" },
    { uri: "paralives://types/cheats", name: "Paralives Cheat Types", description: "Mono.Cecil index of Paralives cheat-related types.", mimeType: "application/json" },
  ],
}));

server.setRequestHandler(ListResourceTemplatesRequestSchema, async () => ({
  resourceTemplates: [
    { uriTemplate: "unity://object/{instance_id}/components", name: "Unity GameObject Components", description: "Component and parseable member summary for a Unity GameObject instance ID.", mimeType: "application/json" },
  ],
}));

server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
  const uri = request.params.uri;

  if (uri === "unity://scene/hierarchy") return readBridgeResource(uri, "get_scene_hierarchy", {});
  if (uri === "unity://runtime/status") return readBridgeResource(uri, "get_runtime_status", {});
  if (uri === "unity://config/options") return readBridgeResource(uri, "list_config", {});
  if (uri === "unity://mcp/status") return readBridgeResource(uri, "get_mcp_status", {});
  if (uri === "paralives://types/managers" || uri === "paralives://types/settings" || uri === "paralives://types/cheats") {
    return readBridgeResource(uri, "paralives_read_resource", { uri });
  }

  const match = /^unity:\/\/object\/(-?\d+)\/components$/.exec(uri);
  if (match) return readBridgeResource(uri, "get_object_components", { instanceId: Number.parseInt(match[1], 10) });

  throw new Error(`Unknown Unity resource '${uri}'.`);
});

const transport = new StdioServerTransport();
await server.connect(transport);

function tool(
  name: string,
  action: string,
  description: string,
  inputSchema: JsonSchema,
  group: ToolDefinition["group"] = "diagnostics/read-only",
  risk: ToolDefinition["risk"] = "read-only",
): ToolDefinition {
  return { name, action, description, inputSchema, group, risk };
}

function classifyBridgeError(error: unknown): string {
  const message = error instanceof Error ? error.message : String(error);
  const lower = message.toLowerCase();
  if (lower.includes("timed out") || lower.includes("timeout")) return "timeout";
  if (lower.includes("econnrefused") || lower.includes("not connected") || lower.includes("disconnected") || lower.includes("closed")) return "not_connected";
  return "execution_failed";
}

function toolError(code: string, message: string) {
  const details = errorDetails(code);
  return {
    isError: true,
    content: [{ type: "text" as const, text: JSON.stringify({ error: { code, message, retryable: details.retryable, hint: details.hint } }, null, 2) }],
  };
}

function errorDetails(code: string): { retryable: boolean; hint: string } {
  switch (code) {
    case "timeout":
      return { retryable: true, hint: "Retry once after checking Unity is responsive; if it repeats, increase UNITY_EXPLORER_MCP_TIMEOUT_MS or reduce the request size." };
    case "not_connected":
      return { retryable: true, hint: "Start the target game with UnityExplorer loaded, enable the MCP bridge, and verify UNITY_EXPLORER_MCP_HOST/PORT." };
    case "rate_limited":
      return { retryable: true, hint: "Wait briefly before retrying the same Unity method call." };
    case "invalid_request":
    case "validation_failed":
    case "parse_failed":
    case "member_not_found":
    case "method_not_found":
    case "component_not_found":
    case "object_not_found":
      return { retryable: false, hint: "Inspect the tool schema and current Unity object state, then retry with corrected arguments." };
    default:
      return { retryable: false, hint: "Read the message, inspect runtime status/logs, and retry only after changing the failing precondition." };
  }
}

async function readBridgeResource(uri: string, action: string, params: Record<string, unknown>) {
  const response = await bridge.request(action, params);
  if (!response.ok) {
    throw new Error(`${response.error.code}: ${response.error.message}`);
  }

  return { contents: [{ uri, mimeType: "application/json", text: JSON.stringify(response.result, null, 2) }] };
}
