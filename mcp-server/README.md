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

The server exposes four tools:

- `UnityExplorer:find_game_objects`
- `UnityExplorer:get_object_detail`
- `UnityExplorer:set_component_property`
- `UnityExplorer:call_component_method`

It also exposes Paralives-specific tools when the bridge is running inside Paralives:

- `Paralives:get_type_index`
- `Paralives:list_content_mods`
- `Paralives:inspect_content_mod`
- `Paralives:create_content_mod`
- `Paralives:import_asset_to_mod`
- `Paralives:list_characters`
- `Paralives:list_households`
- `Paralives:list_lots`
- `Paralives:set_need_value`
- `Paralives:list_cheat_commands`
- `Paralives:run_whitelisted_cheat`

Writes default to dry-run. To execute a write, pass `dryRun: false` and `confirm: "CONFIRM_PARALIVES_WRITE"`.

Resources:

- `unity://scene/hierarchy`
- `unity://object/{instance_id}/components`
- `paralives://types/managers`
- `paralives://types/settings`
- `paralives://types/cheats`
