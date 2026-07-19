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

type DynamicToolDefinition = ToolDefinition & { pluginId?: string };

type DynamicResourceDefinition = {
  uri: string;
  name: string;
  description: string;
  mimeType: string;
  action: string;
  params?: Record<string, unknown>;
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

const toolDefinitions: ToolDefinition[] = [
  tool("UnityExplorer:find_game_objects", "find_game_objects", "Find Unity GameObjects by name/path substring or tag. Risk: read-only diagnostics.", findGameObjectsSchema),
  tool("UnityExplorer:get_object_detail", "get_object_detail", "Read a GameObject summary, direct children, components, and parseable component members. Risk: read-only diagnostics.", instanceIdSchema),
  tool("UnityExplorer:set_component_property", "set_component_property", "Set a parseable component field/property. Risk: writes game state; use only with intent.", setComponentPropertySchema, "game-control/write", "write-confirmed"),
  tool("UnityExplorer:call_component_method", "call_component_method", "Call a bounded component method with parseable string arguments and rate limiting. Risk: writes or triggers gameplay depending on method.", callComponentMethodSchema, "game-control/write", "write-confirmed"),
  tool("UnityExplorer:get_runtime_status", "get_runtime_status", "Read UnityExplorer runtime diagnostics including bridge status and MCP request budget.", emptySchema),
  tool("UnityExplorer:get_recent_logs", "get_recent_logs", "Read recent UnityExplorer log entries and the current log file path. Risk: read-only diagnostics.", recentLogsSchema),
  tool("UnityExplorer:list_config", "list_config", "Read UnityExplorer config entries. Risk: read-only diagnostics.", listConfigSchema),
  tool("UnityExplorer:get_mcp_status", "get_mcp_status", "Read MCP bridge diagnostics including pending requests, per-frame budget, and recent request durations. Risk: read-only diagnostics.", emptySchema),
  tool("UnityExplorer:get_plugin_status", "get_plugin_status", "Read loaded CinematicUnityExplorer plugin status. Risk: read-only diagnostics.", emptySchema),
];

const staticResources = [
  { uri: "unity://scene/hierarchy", name: "Unity Scene Hierarchy", description: "Current Unity scene hierarchy, bounded to roots and shallow children.", mimeType: "application/json" },
  { uri: "unity://runtime/status", name: "UnityExplorer Runtime Status", description: "Runtime diagnostics including Unity version, active scene, panels, and bridge status.", mimeType: "application/json" },
  { uri: "unity://config/options", name: "UnityExplorer Config Options", description: "UnityExplorer config entries with categories and current values.", mimeType: "application/json" },
  { uri: "unity://mcp/status", name: "UnityExplorer MCP Status", description: "MCP bridge listening state, per-frame budget, and recent request diagnostics.", mimeType: "application/json" },
  { uri: "unity://plugins/status", name: "CinematicUnityExplorer Plugin Status", description: "Loaded plugin status reported by the Unity runtime bridge.", mimeType: "application/json" },
];

server.setRequestHandler(ListToolsRequestSchema, async () => {
  const dynamic = await getDynamicDefinitions();
  const allTools = [...toolDefinitions, ...dynamic.tools];
  return {
    tools: allTools.map(({ name, description, inputSchema, group, risk }) => ({
      name,
      description: `${description} Group: ${group}; risk: ${risk}.`,
      inputSchema,
    })),
  };
});

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const dynamic = await getDynamicDefinitions();
  const toolActionByName = new Map([...toolDefinitions, ...dynamic.tools].map((definition) => [definition.name, definition.action]));
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

server.setRequestHandler(ListResourcesRequestSchema, async () => {
  const dynamic = await getDynamicDefinitions();
  return { resources: [...staticResources, ...dynamic.resources] };
});

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
  if (uri === "unity://plugins/status") return readBridgeResource(uri, "get_plugin_status", {});

  const match = /^unity:\/\/object\/(-?\d+)\/components$/.exec(uri);
  if (match) return readBridgeResource(uri, "get_object_components", { instanceId: Number.parseInt(match[1], 10) });

  const dynamic = await getDynamicDefinitions();
  const resource = dynamic.resources.find((definition) => definition.uri === uri);
  if (resource) return readBridgeResource(uri, resource.action, resource.params ?? {});

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

async function getDynamicDefinitions(): Promise<{ tools: DynamicToolDefinition[]; resources: DynamicResourceDefinition[] }> {
  try {
    const response = await bridge.request("get_mcp_tool_definitions", {});
    if (!response.ok) return { tools: [], resources: [] };
    const result = response.result as { tools?: DynamicToolDefinition[]; resources?: DynamicResourceDefinition[] };
    return { tools: result.tools ?? [], resources: result.resources ?? [] };
  } catch {
    return { tools: [], resources: [] };
  }
}
