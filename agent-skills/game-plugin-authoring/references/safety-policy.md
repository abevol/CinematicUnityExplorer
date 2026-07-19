# Safety Policy

## Hard Rules

1. **No arbitrary method invocation tools** – Do not create a tool that takes a method name or type name as a parameter and invokes it. Every tool must call a fixed, reviewed method.
2. **No unbounded reflection mutation tools** – Do not expose `SetValue`, `InvokeMember`, or similar reflection-based mutation as a plugin tool.
3. **Bounded reads only** – Read tools must have a documented upper bound (e.g. max 100 results, max 500 log lines). Unbounded enumeration is not allowed.
4. **Explicit dry-run for writes** – Every game-state or filesystem write tool must accept and default to `true` a `dryRun` parameter.
5. **Confirmation phrase for writes** – Every write tool must require a `confirm` parameter matching a documented phrase (e.g. `"I confirm this game state modification"`).
6. **Unavailable plugins register no tools** – A plugin whose `IsAvailable` returns false must not register MCP tools, actions, or panels that reference game types.

## Write Operation Contract

Every write handler must:
- Check `dryRun` first; if `true`, return a summary of what would happen without executing.
- Check `confirm` against the required phrase; if mismatch, return an error.
- Execute the write and return a result summary.
- Wrap execution in try/catch and return failure details on exception.

## Review Checklist

- [ ] Every tool has a fixed action target (no dynamic dispatch strings).
- [ ] No tool accepts arbitrary type/method names.
- [ ] No unbounded reflection mutation.
- [ ] Read tools have explicit result limits.
- [ ] Write tools have `dryRun` defaulting to `true`.
- [ ] Write tools require `confirm` matching a fixed phrase.
- [ ] `IsAvailable` returns false = zero tool registration.
