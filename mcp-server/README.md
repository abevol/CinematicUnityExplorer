# UnityExplorer MCP Server

This MCP server exposes a local UnityExplorer runtime bridge over stdio.

## Usage

1. Enable UnityExplorer in the target game. The C# bridge listens on `ws://127.0.0.1:8765` by default.
2. Install dependencies:

```sh
bun install
```

3. Build:

```sh
bun run build
```

4. Configure your MCP client to launch:

```sh
bun run start
```

Optional environment variables:

- `UNITY_EXPLORER_MCP_HOST`, default `127.0.0.1`
- `UNITY_EXPLORER_MCP_PORT`, default `8765`
- `UNITY_EXPLORER_MCP_TIMEOUT_MS`, default `5000`

The server exposes UnityExplorer tools:

- `UnityExplorer:find_game_objects`
- `UnityExplorer:get_object_detail`
- `UnityExplorer:set_component_property`
- `UnityExplorer:call_component_method`
- `UnityExplorer:get_runtime_status`
- `UnityExplorer:get_recent_logs`
- `UnityExplorer:list_config`
- `UnityExplorer:get_mcp_status`
- `UnityExplorer:get_plugin_status`

Game-specific tools and resources are discovered dynamically from loaded CinematicUnityExplorer plugins.
Plugin write tools define their own confirmation policy in the exposed schema and description.

Resources:

- `unity://scene/hierarchy`
- `unity://object/{instance_id}/components`
- `unity://runtime/status`
- `unity://config/options`
- `unity://mcp/status`
- `unity://plugins/status`
