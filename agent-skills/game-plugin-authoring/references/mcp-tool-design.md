# MCP Tool Design

## Naming

- **Tool names** (shown to LLM): use Pascal-case `GameName:verb_noun`
  - Example: `Paralives:list_characters`, `Paralives:load_saved_game`
- **Bridge action names** (internal routing): use snake_case `game_name_verb_noun`
  - Example: `paralives_list_characters`, `paralives_load_saved_game`
  - Exception: cross-cutting tools like `get_game_logs`, `poll_logs` may omit the game prefix.

## Group & Risk Classification

| Type | group | risk | dryRun default | confirm required |
|---|---|---|---|---|
| Read-only diagnostics | `diagnostics/read-only` | `read-only` | N/A | No |
| Performance monitoring | `performance` | `read-only` | N/A | No |
| Game-state write | `game-control/write` | `write-confirmed` | `true` | Yes |
| Filesystem write | `filesystem/mod` | `filesystem-confirmed` | `true` | Yes |

## Confirmation Policy

All write tools must:
1. Accept a `dryRun` parameter that defaults to `true`.
2. Accept a `confirm` parameter that must match a required confirmation phrase.
3. Log writes with the action name and argument summary before executing.

## Example Registration Pattern

```csharp
private const string EmptySchema = "{\"type\":\"object\",\"properties\":{}}";

registry.RegisterAction("paralives_list_characters", ParalivesService.HandleListCharacters);
registry.RegisterTool(new PluginMcpToolDescriptor(
    "Paralives:list_characters",
    "paralives_list_characters",
    "List loaded Paralives characters through the whitelisted manager collection.",
    EmptySchema,
    "diagnostics/read-only",
    "read-only"));
```

## Schema Requirements

- All tool schemas must be valid JSON Schema (`inputSchemaJson` string).
- Use `EmptySchema` for tools with no parameters.
- Parameters must have explicit `type`, and where applicable `minimum`/`maximum`/`enum` constraints.
