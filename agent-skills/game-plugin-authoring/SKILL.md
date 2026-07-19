---
name: game-plugin-authoring
description: Use when the user wants to build a CinematicUnityExplorer game-specific plugin, create Unity game-specific tool panels, expose MCP tools from a Unity game's decompiled code, analyze Assembly-CSharp.dll/dnSpy/ILSpy output, or turn game-only functionality into an optional plugin. Use this skill even if the user only says they have a Unity game's decompiled code and want AI/MCP tooling for it.
---

# Game Plugin Authoring

Build optional CinematicUnityExplorer game plugins from decompiled Unity game code.

## Workflow

1. Confirm the target game, loader variant, and whether the plugin may write game state or must be read-only.
2. Inspect the available decompiled code, prioritizing manager, service, UI, save, character, world, economy, inventory, and settings types.
3. Build a small domain map: stable runtime entry points, read-only data sources, risky mutators, and filesystem paths.
4. Design the plugin boundary with `UnityExplorer.Plugins.IUnityExplorerPlugin`.
5. Design UI panels around user tasks, not around raw decompiled type lists.
6. Design MCP tools with explicit name, action, schema, group, risk, dry-run behavior, and confirmation policy.
7. Generate code from the templates in `templates/` and keep game-specific code outside the main CinematicUnityExplorer assembly.
8. Validate with `dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono` unless the user explicitly selects a later supported variant.

## Safety Rules

- Read-only tools may execute directly when bounded.
- Game-state writes must default to dry-run and require a confirmation argument.
- Filesystem writes must default to dry-run and require a confirmation argument.
- Do not expose arbitrary method invocation as a game plugin tool.
- Do not register tools for unavailable games.

## References

- Read `references/plugin-api.md` before writing plugin code.
- Read `references/mcp-tool-design.md` before adding MCP tools.
- Read `references/decompiled-code-research.md` before analyzing game assemblies.
- Read `references/safety-policy.md` before exposing write operations.
