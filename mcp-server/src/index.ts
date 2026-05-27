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

const bridge = new UnityBridgeClient({
  host: process.env.UNITY_EXPLORER_MCP_HOST ?? "127.0.0.1",
  port: Number.parseInt(process.env.UNITY_EXPLORER_MCP_PORT ?? "8765", 10),
  timeoutMs: Number.parseInt(process.env.UNITY_EXPLORER_MCP_TIMEOUT_MS ?? "5000", 10),
});

const server = new Server(
  {
    name: "unity-explorer-mcp",
    version: "1.0.0",
  },
  {
    capabilities: {
      tools: {},
      resources: {},
    },
  },
);

const findGameObjectsSchema = {
  type: "object",
  properties: {
    query: { type: "string" },
    tag: { type: "string" },
    includeInactive: { type: "boolean", default: true },
    limit: { type: "integer", minimum: 1, maximum: 200, default: 50 },
  },
} as const;

const instanceIdSchema = {
  type: "object",
  properties: {
    instanceId: { type: "integer" },
  },
  required: ["instanceId"],
} as const;

const recentLogsSchema = {
  type: "object",
  properties: {
    limit: { type: "integer", minimum: 1, maximum: 200, default: 50 },
  },
} as const;

const listConfigSchema = {
  type: "object",
  properties: {
    category: {
      type: "string",
      enum: ["General", "UI", "MCP", "Paralives", "Console", "Inspector", "Export", "Advanced"],
    },
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
    arguments: {
      type: "array",
      items: { type: "string" },
      default: [],
    },
  },
  required: ["instanceId", "componentName", "methodName"],
} as const;

const paralivesModPathSchema = {
  type: "object",
  properties: {
    modPath: { type: "string" },
    limit: { type: "integer", minimum: 1, maximum: 200, default: 100 },
  },
  required: ["modPath"],
} as const;

const paralivesCreateContentModSchema = {
  type: "object",
  properties: {
    modName: { type: "string" },
    dryRun: { type: "boolean", default: true },
    confirm: { type: "string" },
  },
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
  properties: {
    command: { type: "string" },
    dryRun: { type: "boolean", default: true },
    confirm: { type: "string" },
  },
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
    action: {
      type: "string",
      enum: ["continue_game", "new_game", "load_game_menu", "mod_editor", "options"],
    },
    dryRun: { type: "boolean", default: true },
    confirm: { type: "string" },
  },
  required: ["action"],
} as const;

const paralivesListSavedGamesSchema = {
  type: "object",
  properties: {
    limit: { type: "integer", minimum: 1, maximum: 100, default: 50 },
  },
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

const paralivesStartNewGameSchema = {
  type: "object",
  properties: {
    dryRun: { type: "boolean", default: true },
    confirm: { type: "string" },
  },
} as const;

server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: [
    {
      name: "UnityExplorer:find_game_objects",
      description: "Find Unity GameObjects by name/path substring or tag. Returns bounded summaries with instance IDs.",
      inputSchema: findGameObjectsSchema,
    },
    {
      name: "UnityExplorer:get_object_detail",
      description: "Read a GameObject summary, direct children, components, and parseable component members.",
      inputSchema: instanceIdSchema,
    },
    {
      name: "UnityExplorer:set_component_property",
      description: "Set a parseable field or property on a component, including simple nested paths like position.x.",
      inputSchema: setComponentPropertySchema,
    },
    {
      name: "UnityExplorer:call_component_method",
      description: "Call a safe, non-generic component method with parseable string arguments and built-in rate limiting.",
      inputSchema: callComponentMethodSchema,
    },
    {
      name: "UnityExplorer:get_runtime_status",
      description: "Read UnityExplorer runtime diagnostics including Unity version, active scene, panels, and bridge status.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "UnityExplorer:get_recent_logs",
      description: "Read recent UnityExplorer log entries and the current log file path.",
      inputSchema: recentLogsSchema,
    },
    {
      name: "UnityExplorer:list_config",
      description: "Read UnityExplorer config entries with category, type, value, default, restart, and advanced metadata.",
      inputSchema: listConfigSchema,
    },
    {
      name: "UnityExplorer:get_mcp_status",
      description: "Read MCP bridge diagnostics including listening state, timeout, pending requests, and recent request log.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "Paralives:get_type_index",
      description: "Read the ParalivesBridge availability and Mono.Cecil type index summary.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "Paralives:get_game_state",
      description: "Read current Paralives scene/UI/loading state and manager availability.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "Paralives:list_main_menu_actions",
      description: "List whitelisted Paralives main menu actions and whether their UI buttons are available.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "Paralives:invoke_main_menu_action",
      description: "Invoke a whitelisted Paralives main menu UI button. Defaults to dry-run and requires confirmation.",
      inputSchema: paralivesInvokeMainMenuActionSchema,
    },
    {
      name: "Paralives:list_saved_games",
      description: "List bounded saved-game candidates from SavedGameManager and likely save directories.",
      inputSchema: paralivesListSavedGamesSchema,
    },
    {
      name: "Paralives:load_saved_game",
      description: "Load a saved game through a supported SavedGameManager method when available. Defaults to dry-run and requires confirmation.",
      inputSchema: paralivesLoadSavedGameSchema,
    },
    {
      name: "Paralives:start_new_game",
      description: "Start a new game via the whitelisted main menu New Game button. Defaults to dry-run and requires confirmation.",
      inputSchema: paralivesStartNewGameSchema,
    },
    {
      name: "Paralives:get_loading_state",
      description: "Read GameLoadingManager state and current active scene.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "Paralives:list_content_mods",
      description: "List Paralives .mod folders and their .mod.meta metadata.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "Paralives:inspect_content_mod",
      description: "Inspect files and .meta data inside a Paralives content mod folder.",
      inputSchema: paralivesModPathSchema,
    },
    {
      name: "Paralives:create_content_mod",
      description: "Create a new Paralives content mod folder and .mod.meta file. Defaults to dry-run.",
      inputSchema: paralivesCreateContentModSchema,
    },
    {
      name: "Paralives:import_asset_to_mod",
      description: "Copy an asset into a Paralives content mod and create a schema-aware .meta file. Defaults to dry-run.",
      inputSchema: paralivesImportAssetSchema,
    },
    {
      name: "Paralives:list_characters",
      description: "List loaded Paralives characters through the whitelisted CharacterManager collection.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "Paralives:list_households",
      description: "List loaded Paralives households through the whitelisted HouseholdManager collection.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "Paralives:list_lots",
      description: "List loaded Paralives lots through the whitelisted LotManager collection.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "Paralives:set_need_value",
      description: "Set a character need value through NeedManager.SetNeedToValue. Defaults to dry-run and requires confirmation.",
      inputSchema: paralivesSetNeedValueSchema,
    },
    {
      name: "Paralives:list_cheat_commands",
      description: "List read-only diagnostic cheat commands whitelisted for MCP use.",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "Paralives:run_whitelisted_cheat",
      description: "Run a whitelisted read-only diagnostic Paralives cheat command. Defaults to dry-run and requires confirmation.",
      inputSchema: paralivesRunCheatSchema,
    },
  ],
}));

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const action = toolNameToAction(request.params.name);
  if (!action) {
    return toolError("invalid_request", `Unknown tool '${request.params.name}'.`);
  }

  try {
    const response = await bridge.request(action, (request.params.arguments ?? {}) as Record<string, unknown>);
    if (!response.ok) {
      return toolError(response.error.code, response.error.message);
    }

    return {
      content: [
        {
          type: "text",
          text: JSON.stringify(response.result, null, 2),
        },
      ],
    };
  } catch (error) {
    return toolError("not_connected", error instanceof Error ? error.message : String(error));
  }
});

server.setRequestHandler(ListResourcesRequestSchema, async () => ({
  resources: [
    {
      uri: "unity://scene/hierarchy",
      name: "Unity Scene Hierarchy",
      description: "Current Unity scene hierarchy, bounded to roots and shallow children.",
      mimeType: "application/json",
    },
    {
      uri: "unity://runtime/status",
      name: "UnityExplorer Runtime Status",
      description: "Runtime diagnostics including Unity version, active scene, panels, and bridge status.",
      mimeType: "application/json",
    },
    {
      uri: "unity://config/options",
      name: "UnityExplorer Config Options",
      description: "UnityExplorer config entries with categories and current values.",
      mimeType: "application/json",
    },
    {
      uri: "unity://mcp/status",
      name: "UnityExplorer MCP Status",
      description: "MCP bridge listening state and recent request diagnostics.",
      mimeType: "application/json",
    },
    {
      uri: "paralives://types/managers",
      name: "Paralives Manager Types",
      description: "Mono.Cecil index of Paralives manager-like types.",
      mimeType: "application/json",
    },
    {
      uri: "paralives://types/settings",
      name: "Paralives Setting Types",
      description: "Mono.Cecil index of Paralives setting data types.",
      mimeType: "application/json",
    },
    {
      uri: "paralives://types/cheats",
      name: "Paralives Cheat Types",
      description: "Mono.Cecil index of Paralives cheat-related types.",
      mimeType: "application/json",
    },
  ],
}));

server.setRequestHandler(ListResourceTemplatesRequestSchema, async () => ({
  resourceTemplates: [
    {
      uriTemplate: "unity://object/{instance_id}/components",
      name: "Unity GameObject Components",
      description: "Component and parseable member summary for a Unity GameObject instance ID.",
      mimeType: "application/json",
    },
  ],
}));

server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
  const uri = request.params.uri;

  if (uri === "unity://scene/hierarchy") {
    return readBridgeResource(uri, "get_scene_hierarchy", {});
  }

  if (uri === "unity://runtime/status") {
    return readBridgeResource(uri, "get_runtime_status", {});
  }

  if (uri === "unity://config/options") {
    return readBridgeResource(uri, "list_config", {});
  }

  if (uri === "unity://mcp/status") {
    return readBridgeResource(uri, "get_mcp_status", {});
  }

  if (uri === "paralives://types/managers" || uri === "paralives://types/settings" || uri === "paralives://types/cheats") {
    return readBridgeResource(uri, "paralives_read_resource", { uri });
  }

  const match = /^unity:\/\/object\/(-?\d+)\/components$/.exec(uri);
  if (match) {
    return readBridgeResource(uri, "get_object_components", { instanceId: Number.parseInt(match[1], 10) });
  }

  throw new Error(`Unknown Unity resource '${uri}'.`);
});

const transport = new StdioServerTransport();
await server.connect(transport);

function toolNameToAction(name: string): string | null {
  switch (name) {
    case "UnityExplorer:find_game_objects":
      return "find_game_objects";
    case "UnityExplorer:get_object_detail":
      return "get_object_detail";
    case "UnityExplorer:set_component_property":
      return "set_component_property";
    case "UnityExplorer:call_component_method":
      return "call_component_method";
    case "UnityExplorer:get_runtime_status":
      return "get_runtime_status";
    case "UnityExplorer:get_recent_logs":
      return "get_recent_logs";
    case "UnityExplorer:list_config":
      return "list_config";
    case "UnityExplorer:get_mcp_status":
      return "get_mcp_status";
    case "Paralives:get_type_index":
      return "paralives_get_type_index";
    case "Paralives:get_game_state":
      return "paralives_get_game_state";
    case "Paralives:list_main_menu_actions":
      return "paralives_list_main_menu_actions";
    case "Paralives:invoke_main_menu_action":
      return "paralives_invoke_main_menu_action";
    case "Paralives:list_saved_games":
      return "paralives_list_saved_games";
    case "Paralives:load_saved_game":
      return "paralives_load_saved_game";
    case "Paralives:start_new_game":
      return "paralives_start_new_game";
    case "Paralives:get_loading_state":
      return "paralives_get_loading_state";
    case "Paralives:list_content_mods":
      return "paralives_list_content_mods";
    case "Paralives:inspect_content_mod":
      return "paralives_inspect_content_mod";
    case "Paralives:create_content_mod":
      return "paralives_create_content_mod";
    case "Paralives:import_asset_to_mod":
      return "paralives_import_asset_to_mod";
    case "Paralives:list_characters":
      return "paralives_list_characters";
    case "Paralives:list_households":
      return "paralives_list_households";
    case "Paralives:list_lots":
      return "paralives_list_lots";
    case "Paralives:set_need_value":
      return "paralives_set_need_value";
    case "Paralives:list_cheat_commands":
      return "paralives_list_cheat_commands";
    case "Paralives:run_whitelisted_cheat":
      return "paralives_run_whitelisted_cheat";
    default:
      return null;
  }
}

function toolError(code: string, message: string) {
  return {
    isError: true,
    content: [
      {
        type: "text" as const,
        text: JSON.stringify({ error: { code, message } }, null, 2),
      },
    ],
  };
}

async function readBridgeResource(uri: string, action: string, params: Record<string, unknown>) {
  const response = await bridge.request(action, params);
  if (!response.ok) {
    throw new Error(`${response.error.code}: ${response.error.message}`);
  }

  return {
    contents: [
      {
        uri,
        mimeType: "application/json",
        text: JSON.stringify(response.result, null, 2),
      },
    ],
  };
}
