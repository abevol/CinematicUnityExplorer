# Decompiled Code Research

## Search Order

1. **Identify game assemblies** – Look for `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, game-specific DLLs in `Managed/` or `MelonLoader/Managed/`.
2. **Prioritize manager/service/UI/domain classes** – Focus on:
   - Manager singletons (`GameManager`, `CharacterManager`, `SaveManager`, etc.)
   - Service wrappers that expose read-only accessors
   - UI controllers and panel classes
   - Domain/data types (character, household, inventory, economy, settings)
3. **Find stable singleton or collection accessors** – Look for:
   - `Instance` / `instance` static properties
   - Public static `get_Current` / `get_Active` accessors
   - Public `List<T>` or array fields/properties on managers
4. **Map read-only properties before mutators** – Read-only paths are safe to expose directly. Mutators need the full safety review.
5. **Record every mutator as risky until proven safe** – Any method or property setter that:
   - Modifies game state, character data, or world data
   - Calls `Load`, `Save`, `Delete`, `Reset`, `Clear`
   - Invokes reflection-based assignment (`SetValue`, `InvokeMember`)
   - Writes to `PlayerPrefs` or filesystem
   
   is **presumed unsafe** and must be reviewed against `safety-policy.md`.

## Notes

- Document each found type with: class name, namespace, access level, key members (fields, properties, methods), and whether it's a singleton.
- Record the Mono.Cecil type name for use in `IsAvailable` checks.
- Always check for `[Obsolete]` or `[HideInInspector]` attributes that suggest internal-only APIs.
