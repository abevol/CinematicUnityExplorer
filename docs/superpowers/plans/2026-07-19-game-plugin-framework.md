# Game-Specific Plugin Framework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a reusable CinematicUnityExplorer game-plugin framework, migrate Paralives into an optional plugin, expose plugin MCP tools dynamically, and add an AI Agent Skill for authoring future game plugins.

**Architecture:** The main assembly owns stable public plugin contracts, plugin discovery, lifecycle dispatch, core UI, and core MCP bridge. Game plugins compile as separate DLLs against `UnityExplorer.Plugins`, register panels/config/MCP descriptors through a context, and are initialized only when their target game is available. The TypeScript MCP server keeps UnityExplorer base tools static and adds plugin tools/resources from a runtime bridge action.

**Tech Stack:** C# net35 for `BIE6_Unity_Mono`, Unity/UniverseLib UI, BepInEx 6 Unity Mono, Mono.Cecil for Paralives type indexing, TypeScript MCP server using `@modelcontextprotocol/sdk`, Markdown skill files.

## Global Constraints

- Validate with `dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono`.
- Validate MCP TypeScript with `npm --prefix mcp-server run typecheck`.
- 首版 plugin framework targets `BIE6_Unity_Mono` only.
- Do not read, migrate, delete, or warn about old Paralives config keys.
- Do not use `InternalsVisibleTo` as the plugin boundary.
- Do not rely on reflection to call private/internal host implementation details as a plugin API.
- Do not stage or commit existing DLL changes in `UnityEditorPackage/Runtime/` unless the user explicitly asks.
- Keep plugin-facing APIs under `UnityExplorer.Plugins`.
- Plugin write tools must default to dry-run and require explicit confirmation.

---

## File Structure

### Main Assembly

- Create: `src/Plugins/IUnityExplorerPlugin.cs` — public plugin entrypoint contract.
- Create: `src/Plugins/IUnityExplorerPluginContext.cs` — public context exposed to plugins.
- Create: `src/Plugins/IPluginRuntime.cs` — public runtime helper contract for logging, paths, assembly/type lookup.
- Create: `src/Plugins/IPluginConfigRegistry.cs` — public plugin config creation contract.
- Create: `src/Plugins/IPluginMcpRegistry.cs` — public MCP action/tool/resource registration contract.
- Create: `src/Plugins/IPluginPanelRegistry.cs` — public panel registration contract.
- Create: `src/Plugins/IPluginPanel.cs` — public narrow plugin panel contract.
- Create: `src/Plugins/IPluginPanelHost.cs` — public narrow UI host contract for plugin panels.
- Create: `src/Plugins/PluginPanelDescriptor.cs` — public immutable panel descriptor.
- Create: `src/Plugins/PluginMcpToolDescriptor.cs` — public immutable MCP tool descriptor.
- Create: `src/Plugins/PluginMcpResourceDescriptor.cs` — public immutable MCP resource descriptor.
- Create: `src/Plugins/PluginStatusEntry.cs` — public or internal status DTO for diagnostics.
- Create: `src/Plugins/PluginManager.cs` — internal plugin discovery, availability, initialization, update, shutdown, and status snapshots.
- Create: `src/Plugins/PluginContext.cs` — internal implementation of `IUnityExplorerPluginContext` and registry contracts.
- Create: `src/UI/Panels/PluginPanelAdapter.cs` — internal `UEPanel` adapter wrapping `IPluginPanel`.
- Modify: `src/ExplorerCore.cs` — load plugins after config load and before UI creation.
- Modify: `src/ExplorerBehaviour.cs` — dispatch plugin update and shutdown instead of direct Paralives lifecycle calls.
- Modify: `src/Config/ConfigManager.cs` — remove old Paralives config fields and expose controlled plugin config creation plus string-key panel save data.
- Modify: `src/Config/InternalConfigHandler.cs` — preserve existing enum panel data and support plugin panel save keys.
- Modify: `src/UI/UIManager.cs` — remove `Panels.Paralives`, register plugin panels after core panels, update resize loops to include plugin panels.
- Modify: `src/UI/Panels/UEPanel.cs` — support string save keys through an overridable property while keeping enum panels unchanged.
- Modify: `src/McpBridge/McpActionRegistry.cs` — remove static Paralives registrations and expose dynamic tool/resource descriptors.
- Modify: `src/McpBridge/UnityRuntimeService.cs` — include plugin panels in runtime status and add `get_plugin_status` plus `get_mcp_tool_definitions` actions.
- Modify: `src/McpBridge/McpBridgeService.cs` — remove hard-coded non-Mono Paralives branch and rely on actual action registration.

### TypeScript MCP Server

- Modify: `mcp-server/src/index.ts` — remove hard-coded `Paralives:*` tools/resources and merge dynamic runtime descriptors from `get_mcp_tool_definitions`.
- Modify: `mcp-server/README.md` — describe dynamic plugin tools.

### Paralives Plugin

- Create: `plugins/Paralives/CinematicUnityExplorer.ParalivesPlugin.csproj` — `BIE6_Unity_Mono` plugin project referencing main assembly and net35 dependencies.
- Create: `plugins/Paralives/ParalivesPlugin.cs` — plugin entrypoint.
- Create: `plugins/Paralives/ParalivesPluginConfig.cs` — new namespaced config definitions.
- Create: `plugins/Paralives/ParalivesMcpRegistration.cs` — tool/resource descriptor registration.
- Create: `plugins/Paralives/UI/ParalivesPanel.cs` — plugin panel implementation migrated from current `src/UI/Panels/ParalivesPanel.cs`.
- Move/Copy then delete from main: `src/McpBridge/Paralives/*` to `plugins/Paralives/Mcp/*`.
- Modify: `src/CinematicUnityExplorer.sln` — add Paralives plugin project and map `Release_BIE6_Unity_Mono` to its `BIE6_Unity_Mono` configuration.

### AI Agent Skill

- Create: `agent-skills/game-plugin-authoring/SKILL.md` — skill instructions.
- Create: `agent-skills/game-plugin-authoring/references/plugin-api.md` — plugin API reference.
- Create: `agent-skills/game-plugin-authoring/references/mcp-tool-design.md` — MCP naming, schema, risk, confirmation guidance.
- Create: `agent-skills/game-plugin-authoring/references/decompiled-code-research.md` — reverse-engineered Unity code research workflow.
- Create: `agent-skills/game-plugin-authoring/references/safety-policy.md` — write-tool safety rules.
- Create: `agent-skills/game-plugin-authoring/templates/Plugin.cs` — plugin entrypoint template.
- Create: `agent-skills/game-plugin-authoring/templates/PluginConfig.cs` — config template.
- Create: `agent-skills/game-plugin-authoring/templates/PluginMcpRegistration.cs` — MCP registration template.
- Create: `agent-skills/game-plugin-authoring/templates/PluginPanel.cs` — panel template.
- Create: `agent-skills/game-plugin-authoring/evals/evals.json` — initial qualitative eval prompts.

---

### Task 1: Public Plugin Contracts and Config Boundaries

**Files:**
- Create: `src/Plugins/IUnityExplorerPlugin.cs`
- Create: `src/Plugins/IUnityExplorerPluginContext.cs`
- Create: `src/Plugins/IPluginRuntime.cs`
- Create: `src/Plugins/IPluginConfigRegistry.cs`
- Create: `src/Plugins/IPluginMcpRegistry.cs`
- Create: `src/Plugins/IPluginPanelRegistry.cs`
- Create: `src/Plugins/IPluginPanel.cs`
- Create: `src/Plugins/IPluginPanelHost.cs`
- Create: `src/Plugins/PluginPanelDescriptor.cs`
- Create: `src/Plugins/PluginMcpToolDescriptor.cs`
- Create: `src/Plugins/PluginMcpResourceDescriptor.cs`
- Modify: `src/Config/ConfigManager.cs`
- Modify: `src/Config/InternalConfigHandler.cs`
- Modify: `src/UI/Panels/UEPanel.cs`
- Modify: `src/McpBridge/McpActionRegistry.cs`

**Interfaces:**
- Consumes: existing `ConfigElement<T>`, `ConfigManager`, `UEPanel` save behavior.
- Produces: `UnityExplorer.Plugins.IUnityExplorerPlugin`, `IUnityExplorerPluginContext`, `IPluginPanel`, `PluginPanelDescriptor`, `PluginMcpToolDescriptor`, `PluginMcpResourceDescriptor`, `IPluginConfigRegistry.Create<T>()`, plugin MCP registration methods, and string-key panel save support used by Tasks 2-6.

- [ ] **Step 1: Add plugin contract files**

Create `src/Plugins/IUnityExplorerPlugin.cs`:

```csharp
namespace UnityExplorer.Plugins
{
    public interface IUnityExplorerPlugin
    {
        string Id { get; }
        string Name { get; }
        string Version { get; }

        bool IsAvailable(IUnityExplorerPluginContext context);
        void Initialize(IUnityExplorerPluginContext context);
        void Update();
        void Shutdown();
    }
}
```

Create `src/Plugins/IUnityExplorerPluginContext.cs`:

```csharp
namespace UnityExplorer.Plugins
{
    public interface IUnityExplorerPluginContext
    {
        IPluginPanelRegistry Panels { get; }
        IPluginMcpRegistry Mcp { get; }
        IPluginConfigRegistry Config { get; }
        IPluginRuntime Runtime { get; }
    }
}
```

Create `src/Plugins/IPluginRuntime.cs`:

```csharp
namespace UnityExplorer.Plugins
{
    public interface IPluginRuntime
    {
        string ExplorerFolder { get; }
        string PluginFolder { get; }
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
        Assembly[] GetAssemblies();
        Type FindType(string fullName);
    }
}
```

Create `src/Plugins/IPluginConfigRegistry.cs`:

```csharp
using UnityExplorer.Config;

namespace UnityExplorer.Plugins
{
    public interface IPluginConfigRegistry
    {
        ConfigElement<T> Create<T>(string name, string description, T defaultValue, string category, bool requiresRestart = false, bool advanced = false);
    }
}
```

Create `src/Plugins/IPluginMcpRegistry.cs`:

```csharp
namespace UnityExplorer.Plugins
{
    public interface IPluginMcpRegistry
    {
        void RegisterAction(string action, Func<Dictionary<string, object>, object> handler);
        void RegisterTool(PluginMcpToolDescriptor descriptor);
        void RegisterResource(PluginMcpResourceDescriptor descriptor);
    }
}
```

Create `src/Plugins/IPluginPanelRegistry.cs`:

```csharp
namespace UnityExplorer.Plugins
{
    public interface IPluginPanelRegistry
    {
        void RegisterPanel(PluginPanelDescriptor descriptor);
    }
}
```

Create `src/Plugins/IPluginPanel.cs`:

```csharp
namespace UnityExplorer.Plugins
{
    public interface IPluginPanel
    {
        void Construct(IPluginPanelHost host);
        void SetActive(bool active);
    }
}
```

Create `src/Plugins/IPluginPanelHost.cs`:

```csharp
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;

namespace UnityExplorer.Plugins
{
    public interface IPluginPanelHost
    {
        GameObject ContentRoot { get; }
        Text CreateStatus(string name, string text);
        ButtonRef CreateButton(GameObject parent, string name, string text);
        Text CreateLabel(GameObject parent, string name, string text, TextAnchor anchor);
        GameObject CreateHorizontalGroup(GameObject parent, string name, int spacing, TextAnchor alignment);
        GameObject CreateVerticalGroup(GameObject parent, string name, int spacing, TextAnchor alignment);
        void CreateScrollView(GameObject parent, string name, out GameObject content, out AutoSliderScrollbar scrollbar, Color background);
        void SetLayoutElement(GameObject target, int? minWidth = null, int? minHeight = null, int? flexibleWidth = null, int? flexibleHeight = null);
        void ClearChildren(GameObject target);
    }
}
```

- [ ] **Step 2: Add immutable plugin descriptor classes**

Create `src/Plugins/PluginPanelDescriptor.cs`:

```csharp
namespace UnityExplorer.Plugins
{
    public sealed class PluginPanelDescriptor
    {
        public PluginPanelDescriptor(string id, string title, Func<IPluginPanelHost, IPluginPanel> create, int minWidth, int minHeight, bool showByDefault = false)
        {
            Id = id;
            Title = title;
            Create = create;
            MinWidth = minWidth;
            MinHeight = minHeight;
            ShowByDefault = showByDefault;
        }

        public string Id { get; }
        public string Title { get; }
        public Func<IPluginPanelHost, IPluginPanel> Create { get; }
        public int MinWidth { get; }
        public int MinHeight { get; }
        public bool ShowByDefault { get; }
    }
}
```

Create `src/Plugins/PluginMcpToolDescriptor.cs`:

```csharp
namespace UnityExplorer.Plugins
{
    public sealed class PluginMcpToolDescriptor
    {
        public PluginMcpToolDescriptor(string name, string action, string description, string inputSchemaJson, string group, string risk)
        {
            Name = name;
            Action = action;
            Description = description;
            InputSchemaJson = inputSchemaJson;
            Group = group;
            Risk = risk;
        }

        public string Name { get; }
        public string Action { get; }
        public string Description { get; }
        public string InputSchemaJson { get; }
        public string Group { get; }
        public string Risk { get; }
    }
}
```

Create `src/Plugins/PluginMcpResourceDescriptor.cs`:

```csharp
namespace UnityExplorer.Plugins
{
    public sealed class PluginMcpResourceDescriptor
    {
        public PluginMcpResourceDescriptor(string uri, string name, string description, string mimeType, string action, Dictionary<string, object> parameters)
        {
            Uri = uri;
            Name = name;
            Description = description;
            MimeType = mimeType;
            Action = action;
            Parameters = parameters ?? new Dictionary<string, object>();
        }

        public string Uri { get; }
        public string Name { get; }
        public string Description { get; }
        public string MimeType { get; }
        public string Action { get; }
        public Dictionary<string, object> Parameters { get; }
    }
}
```

- [ ] **Step 3: Add string-key panel save data support without breaking enum panels**

Modify `src/Config/ConfigManager.cs` by replacing the existing `PanelSaveData` field and `GetPanelSaveData(UIManager.Panels panel)` block with this shape:

```csharp
internal static readonly Dictionary<string, ConfigElement<string>> PanelSaveData = new();

internal static ConfigElement<string> GetPanelSaveData(UIManager.Panels panel)
{
    return GetPanelSaveData(panel.ToString());
}

internal static ConfigElement<string> GetPanelSaveData(string panelKey)
{
    if (!PanelSaveData.ContainsKey(panelKey))
        PanelSaveData.Add(panelKey, new ConfigElement<string>(panelKey, string.Empty, string.Empty, true));
    return PanelSaveData[panelKey];
}
```

Modify `src/UI/Panels/UEPanel.cs` so `PanelType` remains for core panels but save data can use a string key:

```csharp
public abstract UIManager.Panels PanelType { get; }
public virtual string PanelSaveKey => PanelType.ToString();
public virtual string NavButtonId => PanelType.ToString();
```

Replace calls to `ConfigManager.GetPanelSaveData(this.PanelType)` with `ConfigManager.GetPanelSaveData(PanelSaveKey)`.

In `UEPanel.ConstructUI()`, replace the nav button creation and click lines with virtual hooks:

```csharp
NavButton = UIFactory.CreateButton(UIManager.NavbarTabButtonHolder, $"Button_{NavButtonId}", Name);
...
NavButton.OnClick += OnNavButtonClicked;
```

Add the default hook to `UEPanel`:

```csharp
protected virtual void OnNavButtonClicked()
{
    UIManager.TogglePanel(PanelType);
}
```

Modify `src/Config/InternalConfigHandler.cs` in `TryLoadConfig()` so it accepts legacy enum keys and plugin string keys:

```csharp
foreach (string key in document.Keys)
{
    ConfigManager.GetPanelSaveData(key).Value = document.GetString(key);
}
```

- [ ] **Step 4: Expose controlled plugin config creation**

In `src/Config/ConfigManager.cs`, keep `RegisterConfigElement<T>` internal, and add a public helper used only through plugin context:

```csharp
public static ConfigElement<T> CreatePluginConfig<T>(string name, string description, T defaultValue, string category, bool requiresRestart = false, bool advanced = false)
{
    return new ConfigElement<T>(name, description, defaultValue, category: category, requiresRestart: requiresRestart, advanced: advanced);
}
```

Leave the existing Paralives config fields and enum in place for now. They are removed in Task 5 after the Paralives source has moved out of the main assembly.

- [ ] **Step 5: Add plugin MCP registration storage without removing existing actions**

Modify `src/McpBridge/McpActionRegistry.cs` to add plugin descriptor collections and registration methods while keeping current built-in and Paralives action registrations intact until Task 4 and Task 5:

```csharp
using UnityExplorer.Plugins;
```

Add these fields and properties near `actions`:

```csharp
private static readonly List<PluginMcpToolDescriptor> pluginTools = new();
private static readonly List<PluginMcpResourceDescriptor> pluginResources = new();

public static List<PluginMcpToolDescriptor> PluginTools => pluginTools;
public static List<PluginMcpResourceDescriptor> PluginResources => pluginResources;
```

Add these methods before `BuildActions()`:

```csharp
public static void RegisterPluginAction(string action, Func<Dictionary<string, object>, object> handler)
{
    if (actions.ContainsKey(action))
        throw new McpBridgeException("invalid_request", $"Duplicate MCP action registration for '{action}'.");
    actions[action] = handler;
}

public static void RegisterPluginTool(PluginMcpToolDescriptor descriptor)
{
    if (pluginTools.Any(tool => tool.Name == descriptor.Name))
        throw new McpBridgeException("invalid_request", $"Duplicate MCP tool registration for '{descriptor.Name}'.");
    pluginTools.Add(descriptor);
}

public static void RegisterPluginResource(PluginMcpResourceDescriptor descriptor)
{
    if (pluginResources.Any(resource => resource.Uri == descriptor.Uri))
        throw new McpBridgeException("invalid_request", $"Duplicate MCP resource registration for '{descriptor.Uri}'.");
    pluginResources.Add(descriptor);
}
```

- [ ] **Step 6: Run compile check and commit Task 1**

Run: `dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono`

Expected: PASS.

Commit only Task 1 files:

```bash
git add src/Plugins src/Config/ConfigManager.cs src/Config/InternalConfigHandler.cs src/UI/Panels/UEPanel.cs src/McpBridge/McpActionRegistry.cs
git commit -m "feat(plugins): add public plugin contracts"
```

Task 5 removes these old Paralives members after no main-assembly code references them:

```csharp
public static ConfigElement<ParalivesSafeActionMode> Paralives_SafeActionMode;
public static ConfigElement<int> Paralives_SavedGameListLimit;
public static ConfigElement<int> Paralives_LoadingWaitTimeoutMs;
public static ConfigElement<bool> Paralives_PreferUiFlowForSaveLoad;

public enum ParalivesSafeActionMode
{
    ConfirmRequired,
    OneClickInUI
}
```

Also remove the four `new("Paralives ...")` assignments from `CreateConfigElements()` in Task 5.

---

### Task 2: Plugin Manager, Context, and Lifecycle Dispatch

**Files:**
- Create: `src/Plugins/PluginStatusEntry.cs`
- Create: `src/Plugins/PluginManager.cs`
- Create: `src/Plugins/PluginContext.cs`
- Modify: `src/ExplorerCore.cs`
- Modify: `src/ExplorerBehaviour.cs`

**Interfaces:**
- Consumes: Task 1 `IUnityExplorerPlugin`, `IUnityExplorerPluginContext`, `IPluginConfigRegistry`, `IPluginMcpRegistry`, `IPluginPanelRegistry`, `IPluginRuntime`.
- Produces: `PluginManager.LoadPlugins()`, `PluginManager.UpdatePlugins()`, `PluginManager.ShutdownPlugins()`, `PluginManager.GetStatusSnapshot()`, `PluginManager.RegisteredPanels`, and registry state used by UI and MCP tasks.

- [ ] **Step 1: Add plugin status DTO**

Create `src/Plugins/PluginStatusEntry.cs`:

```csharp
namespace UnityExplorer.Plugins
{
    internal sealed class PluginStatusEntry
    {
        public string Id;
        public string Name;
        public string Version;
        public string AssemblyPath;
        public string State;
        public bool Available;
        public string Error;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["id"] = Id,
                ["name"] = Name,
                ["version"] = Version,
                ["assembly"] = AssemblyPath,
                ["state"] = State,
                ["available"] = Available,
                ["error"] = Error
            };
        }
    }
}
```

- [ ] **Step 2: Add plugin context implementation**

Create `src/Plugins/PluginContext.cs` with one internal class implementing all registry contracts:

```csharp
using UnityExplorer.Config;

namespace UnityExplorer.Plugins
{
    internal sealed class PluginContext : IUnityExplorerPluginContext, IPluginPanelRegistry, IPluginMcpRegistry, IPluginConfigRegistry, IPluginRuntime
    {
        private readonly string pluginFolder;

        public PluginContext(string pluginFolder)
        {
            this.pluginFolder = pluginFolder;
        }

        public IPluginPanelRegistry Panels => this;
        public IPluginMcpRegistry Mcp => this;
        public IPluginConfigRegistry Config => this;
        public IPluginRuntime Runtime => this;

        public string ExplorerFolder => ExplorerCore.ExplorerFolder;
        public string PluginFolder => pluginFolder;

        public void RegisterPanel(PluginPanelDescriptor descriptor)
            => PluginManager.RegisterPanel(descriptor);

        public void RegisterAction(string action, Func<Dictionary<string, object>, object> handler)
            => UnityExplorer.McpBridge.McpActionRegistry.RegisterPluginAction(action, handler);

        public void RegisterTool(PluginMcpToolDescriptor descriptor)
            => UnityExplorer.McpBridge.McpActionRegistry.RegisterPluginTool(descriptor);

        public void RegisterResource(PluginMcpResourceDescriptor descriptor)
            => UnityExplorer.McpBridge.McpActionRegistry.RegisterPluginResource(descriptor);

        public ConfigElement<T> Create<T>(string name, string description, T defaultValue, string category, bool requiresRestart = false, bool advanced = false)
            => ConfigManager.CreatePluginConfig(name, description, defaultValue, category, requiresRestart, advanced);

        public void Log(string message) => ExplorerCore.Log(message);
        public void LogWarning(string message) => ExplorerCore.LogWarning(message);
        public void LogError(string message) => ExplorerCore.LogError(message);
        public Assembly[] GetAssemblies() => AppDomain.CurrentDomain.GetAssemblies();
        public Type FindType(string fullName) => GetAssemblies().Select(assembly => assembly.GetType(fullName, false)).FirstOrDefault(type => type != null);
    }
}
```

- [ ] **Step 3: Add plugin manager discovery and lifecycle**

Create `src/Plugins/PluginManager.cs`:

```csharp
namespace UnityExplorer.Plugins
{
    internal static class PluginManager
    {
        private sealed class LoadedPlugin
        {
            public IUnityExplorerPlugin Plugin;
            public PluginStatusEntry Status;
        }

        private static readonly List<LoadedPlugin> loadedPlugins = new();
        private static readonly List<PluginStatusEntry> statuses = new();
        private static readonly List<PluginPanelDescriptor> panels = new();

        public static List<PluginPanelDescriptor> RegisteredPanels => panels;

        public static void LoadPlugins()
        {
            string root = ExplorerCore.ExplorerFolder;
            string pluginFolder = Path.Combine(root, "plugins");
            List<string> candidates = new();

            if (Directory.Exists(root))
                candidates.AddRange(Directory.GetFiles(root, "CinematicUnityExplorer.*Plugin.dll"));
            if (Directory.Exists(pluginFolder))
                candidates.AddRange(Directory.GetFiles(pluginFolder, "CinematicUnityExplorer.*Plugin.dll"));

            foreach (string path in candidates.Distinct().OrderBy(it => it))
                LoadPluginAssembly(path);
        }

        public static void RegisterPanel(PluginPanelDescriptor descriptor)
        {
            if (panels.Any(panel => panel.Id == descriptor.Id))
                throw new InvalidOperationException("Duplicate plugin panel id '" + descriptor.Id + "'.");
            panels.Add(descriptor);
        }

        public static List<object> GetStatusSnapshot()
        {
            return statuses.Select(status => status.ToDictionary()).Cast<object>().ToList();
        }

        public static void UpdatePlugins()
        {
            foreach (LoadedPlugin entry in loadedPlugins)
            {
                try
                {
                    entry.Plugin.Update();
                }
                catch (Exception ex)
                {
                    entry.Status.State = "error";
                    entry.Status.Error = ex.GetInnerMostException().Message;
                    ExplorerCore.LogWarning("Plugin update failed for " + entry.Status.Id + ": " + ex);
                }
            }
        }

        public static void ShutdownPlugins()
        {
            for (int i = loadedPlugins.Count - 1; i >= 0; i--)
            {
                LoadedPlugin entry = loadedPlugins[i];
                try
                {
                    entry.Plugin.Shutdown();
                    entry.Status.State = "shutdown";
                }
                catch (Exception ex)
                {
                    entry.Status.State = "error";
                    entry.Status.Error = ex.GetInnerMostException().Message;
                    ExplorerCore.LogWarning("Plugin shutdown failed for " + entry.Status.Id + ": " + ex);
                }
            }
        }

        private static void LoadPluginAssembly(string path)
        {
            PluginStatusEntry status = new() { AssemblyPath = Path.GetFileName(path), State = "discovered" };
            statuses.Add(status);

            try
            {
                Assembly assembly = Assembly.LoadFrom(path);
                foreach (Type type in assembly.GetTypes().Where(IsPluginType))
                    LoadPluginType(type, path, status);
            }
            catch (Exception ex)
            {
                status.State = "error";
                status.Error = ex.GetInnerMostException().Message;
                ExplorerCore.LogWarning("Plugin assembly load failed for " + path + ": " + ex);
            }
        }

        private static void LoadPluginType(Type type, string path, PluginStatusEntry status)
        {
            IUnityExplorerPlugin plugin = null;
            try
            {
                plugin = (IUnityExplorerPlugin)Activator.CreateInstance(type);
                status.Id = plugin.Id;
                status.Name = plugin.Name;
                status.Version = plugin.Version;

                PluginContext context = new(Path.GetDirectoryName(path));
                status.Available = plugin.IsAvailable(context);
                if (!status.Available)
                {
                    status.State = "unavailable";
                    return;
                }

                plugin.Initialize(context);
                status.State = "loaded";
                loadedPlugins.Add(new LoadedPlugin { Plugin = plugin, Status = status });
            }
            catch (Exception ex)
            {
                status.State = "error";
                status.Error = ex.GetInnerMostException().Message;
                ExplorerCore.LogWarning("Plugin initialization failed for " + (plugin?.Id ?? type.FullName) + ": " + ex);
            }
        }

        private static bool IsPluginType(Type type)
        {
            return typeof(IUnityExplorerPlugin).IsAssignableFrom(type)
                && type.IsClass
                && !type.IsAbstract
                && type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}
```

- [ ] **Step 4: Wire lifecycle into core startup and shutdown**

Modify `src/ExplorerCore.cs` imports to include:

```csharp
using UnityExplorer.Plugins;
```

In `ExplorerCore.Init`, after `ConfigManager.SetUniverseLibBypassICall(...)` and before `Universe.Init(...)`, add:

```csharp
PluginManager.LoadPlugins();
```

Modify `src/ExplorerBehaviour.cs` imports to remove `UnityExplorer.McpBridge.Paralives` and add:

```csharp
using UnityExplorer.Plugins;
```

Replace the direct Paralives update block with:

```csharp
PluginManager.UpdatePlugins();
```

Replace the direct Paralives shutdown block with:

```csharp
PluginManager.ShutdownPlugins();
```

- [ ] **Step 5: Run compile check and commit Task 2**

Run: `dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono`

Expected: PASS.

Commit only Task 2 files:

```bash
git add src/Plugins/PluginStatusEntry.cs src/Plugins/PluginManager.cs src/Plugins/PluginContext.cs src/ExplorerCore.cs src/ExplorerBehaviour.cs
git commit -m "feat(plugins): load and run game plugins"
```

---

### Task 3: Plugin Panel Adapter and UI Registration

**Files:**
- Create: `src/UI/Panels/PluginPanelAdapter.cs`
- Modify: `src/UI/UIManager.cs`
- Modify: `src/McpBridge/UnityRuntimeService.cs`

**Interfaces:**
- Consumes: Task 1 `IPluginPanelHost`, `IPluginPanel`, `PluginPanelDescriptor`; Task 2 `PluginManager.RegisteredPanels`.
- Produces: plugin panels displayed in the main navbar, plugin panel save data by string key, plugin panel runtime status entries.

- [ ] **Step 1: Add `PluginPanelAdapter`**

Create `src/UI/Panels/PluginPanelAdapter.cs`:

```csharp
using UnityExplorer.Plugins;
using UnityExplorer.UI.Widgets;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;

namespace UnityExplorer.UI.Panels
{
    internal sealed class PluginPanelAdapter : UEPanel, IPluginPanelHost
    {
        private readonly PluginPanelDescriptor descriptor;
        private IPluginPanel pluginPanel;

        public PluginPanelAdapter(UIBase owner, PluginPanelDescriptor descriptor) : base(owner)
        {
            this.descriptor = descriptor;
        }

        public override string Name => descriptor.Title;
        public override UIManager.Panels PanelType => UIManager.Panels.Plugin;
        public override string PanelSaveKey => "plugin:" + descriptor.Id;
        public override string NavButtonId => "Plugin_" + descriptor.Id.Replace('.', '_').Replace(':', '_');
        public override int MinWidth => descriptor.MinWidth;
        public override int MinHeight => descriptor.MinHeight;
        public override bool ShowByDefault => descriptor.ShowByDefault;

        protected override void ConstructPanelContent()
        {
            pluginPanel = descriptor.Create(this);
            pluginPanel.Construct(this);
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            pluginPanel?.SetActive(active);
        }

        protected override void OnNavButtonClicked()
        {
            SetActive(!Enabled);
        }

        public Text CreateStatus(string name, string text) => UEUI.CreateStatus(ContentRoot, name, text);
        public ButtonRef CreateButton(GameObject parent, string name, string text) => UIFactory.CreateButton(parent, name, text);
        public Text CreateLabel(GameObject parent, string name, string text, TextAnchor anchor) => UIFactory.CreateLabel(parent, name, text, anchor);

        public GameObject CreateHorizontalGroup(GameObject parent, string name, int spacing, TextAnchor alignment)
            => UIFactory.CreateHorizontalGroup(parent, name, false, false, true, true, spacing, default, default, alignment);

        public GameObject CreateVerticalGroup(GameObject parent, string name, int spacing, TextAnchor alignment)
            => UIFactory.CreateVerticalGroup(parent, name, true, false, true, true, spacing, default, default, alignment);

        public void CreateScrollView(GameObject parent, string name, out GameObject content, out AutoSliderScrollbar scrollbar, Color background)
            => UIFactory.CreateScrollView(parent, name, out content, out scrollbar, background);

        public void SetLayoutElement(GameObject target, int? minWidth = null, int? minHeight = null, int? flexibleWidth = null, int? flexibleHeight = null)
            => UIFactory.SetLayoutElement(target, minWidth: minWidth, minHeight: minHeight, flexibleWidth: flexibleWidth, flexibleHeight: flexibleHeight);

        public void ClearChildren(GameObject target) => UEUI.ClearChildren(target);
    }
}
```

- [ ] **Step 2: Add `Plugin` sentinel enum value and plugin panel list**

Modify `src/UI/UIManager.cs` enum by keeping `Paralives` for now and adding `Plugin` at the end. `Paralives` is removed in Task 5 after the old panel source leaves the main assembly:

```csharp
public enum Panels
{
    ObjectExplorer,
    Inspector,
    CSConsole,
    Options,
    ConsoleLog,
    AutoCompleter,
    UIInspectorResults,
    HookManager,
    Clipboard,
    Freecam,
    LightsManager,
    CamPaths,
    PostProcessingPanel,
    MCP,
    Paralives,
    AnimatorPanel,
    Misc,
    Plugin,
}
```

Add a plugin panel list near `UIPanels`:

```csharp
internal static readonly List<UEPanel> PluginPanels = new();
```

In `InitUI()`, remove the `#if MONO` block that constructs `ParalivesPanel`. After creating `McpPanel` and before `AnimatorPanel`, add:

```csharp
foreach (UnityExplorer.Plugins.PluginPanelDescriptor descriptor in UnityExplorer.Plugins.PluginManager.RegisteredPanels)
    PluginPanels.Add(new PluginPanelAdapter(UiBase, descriptor));
```

In `OnScreenDimensionsChanged()`, after the existing `foreach (KeyValuePair<Panels, UEPanel> panel in UIPanels)` loop, add:

```csharp
foreach (UEPanel panel in PluginPanels)
{
    panel.EnsureValidSize();
    panel.EnsureValidPosition();
    panel.Dragger.OnEndResize();
}
```

- [ ] **Step 3: Include plugin panels in runtime status**

Modify `src/McpBridge/UnityRuntimeService.cs` `GetRuntimeStatus()` after the core panel loop:

```csharp
foreach (UEPanel panel in UIManager.PluginPanels)
{
    panels.Add(new Dictionary<string, object>
    {
        ["id"] = panel.PanelSaveKey,
        ["name"] = panel.Name,
        ["active"] = panel.Enabled,
        ["showByDefault"] = panel.ShowByDefault,
        ["minWidth"] = panel.MinWidth,
        ["minHeight"] = panel.MinHeight,
        ["plugin"] = true
    });
}
```

- [ ] **Step 4: Run compile check and commit Task 3**

Run: `dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono`

Expected: PASS.

Commit only Task 3 files:

```bash
git add src/UI/Panels/PluginPanelAdapter.cs src/UI/UIManager.cs src/McpBridge/UnityRuntimeService.cs
git commit -m "feat(ui): support plugin panels"
```

---

### Task 4: Dynamic MCP Registry and TypeScript Tool Discovery

**Files:**
- Modify: `src/McpBridge/McpActionRegistry.cs`
- Modify: `src/McpBridge/UnityRuntimeService.cs`
- Modify: `src/McpBridge/McpBridgeService.cs`
- Modify: `src/UI/Panels/McpPanel.cs`
- Modify: `mcp-server/src/index.ts`
- Modify: `mcp-server/README.md`

**Interfaces:**
- Consumes: Task 1 `PluginMcpToolDescriptor`, `PluginMcpResourceDescriptor`; Task 2 `PluginManager.GetStatusSnapshot()`.
- Produces: dynamic bridge actions `get_plugin_status`, `get_mcp_tool_definitions`; dynamic MCP tool/resource merge in TypeScript.

- [ ] **Step 1: Remove hard-coded Paralives registrations from `McpActionRegistry`**

Modify `src/McpBridge/McpActionRegistry.cs` to remove all `Paralives.*.Actions` registrations from `BuildActions()`. Keep the plugin fields and registration methods added in Task 1. After this step, `BuildActions()` must look like this:

```csharp
using UnityExplorer.Plugins;

namespace UnityExplorer.McpBridge
{
    internal static class McpActionRegistry
    {
        private static readonly Dictionary<string, Func<Dictionary<string, object>, object>> actions = BuildActions();
        private static readonly List<PluginMcpToolDescriptor> pluginTools = new();
        private static readonly List<PluginMcpResourceDescriptor> pluginResources = new();

        public static Dictionary<string, Func<Dictionary<string, object>, object>> Actions => actions;
        public static List<PluginMcpToolDescriptor> PluginTools => pluginTools;
        public static List<PluginMcpResourceDescriptor> PluginResources => pluginResources;

        private static Dictionary<string, Func<Dictionary<string, object>, object>> BuildActions()
        {
            Dictionary<string, Func<Dictionary<string, object>, object>> registry = new();
            Register(registry, UnityObjectService.Actions);
            Register(registry, UnityComponentService.Actions);
            Register(registry, UnityRuntimeService.Actions);
            return registry;
        }

        private static void Register(Dictionary<string, Func<Dictionary<string, object>, object>> registry, Dictionary<string, Func<Dictionary<string, object>, object>> serviceActions)
        {
            foreach (KeyValuePair<string, Func<Dictionary<string, object>, object>> action in serviceActions)
            {
                if (registry.ContainsKey(action.Key))
                    throw new McpBridgeException("invalid_request", $"Duplicate MCP action registration for '{action.Key}'.");

                registry[action.Key] = action.Value;
            }
        }
    }
}
```

Do not remove `RegisterPluginAction`, `RegisterPluginTool`, or `RegisterPluginResource`; those methods were added in Task 1 and are still required.

- [ ] **Step 2: Add bridge actions for plugin status and dynamic tool definitions**

Modify `src/McpBridge/UnityRuntimeService.cs` `Actions` dictionary:

```csharp
["get_plugin_status"] = GetPluginStatus,
["get_mcp_tool_definitions"] = GetMcpToolDefinitions
```

Add methods:

```csharp
private static object GetPluginStatus(Dictionary<string, object> parameters)
{
    return new Dictionary<string, object>
    {
        ["plugins"] = UnityExplorer.Plugins.PluginManager.GetStatusSnapshot()
    };
}

private static object GetMcpToolDefinitions(Dictionary<string, object> parameters)
{
    List<object> tools = McpActionRegistry.PluginTools.Select(tool => new Dictionary<string, object>
    {
        ["name"] = tool.Name,
        ["action"] = tool.Action,
        ["description"] = tool.Description,
        ["inputSchema"] = McpJson.Parse(tool.InputSchemaJson),
        ["group"] = tool.Group,
        ["risk"] = tool.Risk
    }).Cast<object>().ToList();

    List<object> resources = McpActionRegistry.PluginResources.Select(resource => new Dictionary<string, object>
    {
        ["uri"] = resource.Uri,
        ["name"] = resource.Name,
        ["description"] = resource.Description,
        ["mimeType"] = resource.MimeType,
        ["action"] = resource.Action,
        ["params"] = resource.Parameters
    }).Cast<object>().ToList();

    return new Dictionary<string, object>
    {
        ["tools"] = tools,
        ["resources"] = resources
    };
}
```

- [ ] **Step 3: Remove hard-coded Paralives branch from bridge service and MCP panel text**

Modify `src/McpBridge/McpBridgeService.cs` to remove the `#if !MONO` Paralives special case and `IsParalivesLogAction()` method. `Handle()` should be:

```csharp
public static object Handle(string action, Dictionary<string, object> parameters)
{
    Dictionary<string, Func<Dictionary<string, object>, object>> actions = McpActionRegistry.Actions;
    if (actions.TryGetValue(action, out Func<Dictionary<string, object>, object> handler))
        return handler(parameters);

    throw new McpBridgeException("invalid_request", $"Unknown MCP bridge action '{action}'.");
}
```

Modify `src/UI/Panels/McpPanel.cs` tools section. Replace hard-coded Paralives line with plugin status summary:

```csharp
UEUI.AddInfoRow(tools, "Plugins", "Plugin MCP tools are exposed dynamically when a game plugin is available.");
```

- [ ] **Step 4: Update TypeScript MCP server to fetch dynamic tools/resources**

Modify `mcp-server/src/index.ts`:

1. Remove all hard-coded `Paralives:*` tool definitions and `paralives://` resource entries from static arrays.
2. Add dynamic descriptor types:

```typescript
type DynamicToolDefinition = ToolDefinition & { pluginId?: string };
type DynamicResourceDefinition = {
  uri: string;
  name: string;
  description: string;
  mimeType: string;
  action: string;
  params?: Record<string, unknown>;
};
```

3. Add loader:

```typescript
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
```

4. In `ListToolsRequestSchema`, merge definitions:

```typescript
const dynamic = await getDynamicDefinitions();
const allTools = [...toolDefinitions, ...dynamic.tools];
return {
  tools: allTools.map(({ name, description, inputSchema, group, risk }) => ({
    name,
    description: `${description} Group: ${group}; risk: ${risk}.`,
    inputSchema,
  })),
};
```

5. In `CallToolRequestSchema`, build map per call:

```typescript
const dynamic = await getDynamicDefinitions();
const toolActionByName = new Map([...toolDefinitions, ...dynamic.tools].map((definition) => [definition.name, definition.action]));
```

6. In `ListResourcesRequestSchema`, merge static and dynamic resources.
7. In `ReadResourceRequestSchema`, first handle static resources, then dynamic resources by exact URI.

- [ ] **Step 5: Update MCP README and run typecheck**

Modify `mcp-server/README.md` to say game-specific tools are discovered dynamically from loaded CinematicUnityExplorer plugins.

Run: `npm --prefix mcp-server run typecheck`

Expected: PASS.

Run: `dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono`

Expected: PASS.

Commit only Task 4 files:

```bash
git add src/McpBridge/McpActionRegistry.cs src/McpBridge/UnityRuntimeService.cs src/McpBridge/McpBridgeService.cs src/UI/Panels/McpPanel.cs mcp-server/src/index.ts mcp-server/README.md
git commit -m "feat(mcp): expose plugin tools dynamically"
```

---

### Task 5: Paralives Plugin Project and Main Assembly Cleanup

**Files:**
- Create: `plugins/Paralives/CinematicUnityExplorer.ParalivesPlugin.csproj`
- Create: `plugins/Paralives/ParalivesPlugin.cs`
- Create: `plugins/Paralives/ParalivesPluginConfig.cs`
- Create: `plugins/Paralives/ParalivesMcpRegistration.cs`
- Move: `src/McpBridge/Paralives/*.cs` to `plugins/Paralives/Mcp/*.cs`
- Move: `src/UI/Panels/ParalivesPanel.cs` to `plugins/Paralives/UI/ParalivesPanel.cs`
- Modify: `plugins/Paralives/UI/ParalivesPanel.cs`
- Modify: moved Paralives MCP files under `plugins/Paralives/Mcp/*.cs`
- Modify: `src/CinematicUnityExplorer.sln`
- Modify: `src/CinematicUnityExplorer.csproj`

**Interfaces:**
- Consumes: Task 1-4 plugin APIs, config API, MCP registry API, panel host API.
- Produces: optional `CinematicUnityExplorer.ParalivesPlugin.dll`; main assembly no longer contains fixed Paralives source or registrations.

- [ ] **Step 1: Create Paralives plugin project**

Create `plugins/Paralives/CinematicUnityExplorer.ParalivesPlugin.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net35</TargetFramework>
    <PlatformTarget>AnyCPU</PlatformTarget>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <LangVersion>latest</LangVersion>
    <Configurations>BIE6_Unity_Mono</Configurations>
    <OutputPath>..\..\Release\CinematicUnityExplorer.BepInEx6.Unity.Mono\</OutputPath>
    <AssemblyName>CinematicUnityExplorer.ParalivesPlugin</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies.net35" Version="1.0.3" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="CinematicUnityExplorer.BIE6.Unity.Mono">
      <HintPath>..\..\Release\CinematicUnityExplorer.BepInEx6.Unity.Mono\CinematicUnityExplorer.BIE6.Unity.Mono.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="UniverseLib.Mono">
      <HintPath>..\..\UniverseLib\Release\UniverseLib.Mono\UniverseLib.Mono.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="UnityEngine">
      <HintPath>..\..\lib\net35\UnityEngine.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="UnityEngine.UI">
      <HintPath>..\..\lib\net35\UnityEngine.UI.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="Mono.Cecil">
      <HintPath>..\..\UnityEditorPackage\Runtime\Mono.Cecil.dll</HintPath>
      <Private>True</Private>
    </Reference>
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Move Paralives source into plugin folder**

Use `git mv` for each file:

```bash
git mv src/McpBridge/Paralives plugins/Paralives/Mcp
git mv src/UI/Panels/ParalivesPanel.cs plugins/Paralives/UI/ParalivesPanel.cs
```

Update namespaces in moved files from `UnityExplorer.McpBridge.Paralives` to `CinematicUnityExplorer.Plugins.Paralives.Mcp` and from `UnityExplorer.UI.Panels` to `CinematicUnityExplorer.Plugins.Paralives.UI`.

Update using directives in moved files so they reference:

```csharp
using UnityExplorer.Plugins;
using UnityExplorer.Config;
using UnityExplorer.McpBridge;
```

- [ ] **Step 3: Add new Paralives plugin config**

Create `plugins/Paralives/ParalivesPluginConfig.cs`:

```csharp
using UnityExplorer.Config;
using UnityExplorer.Plugins;

namespace CinematicUnityExplorer.Plugins.Paralives
{
    internal static class ParalivesPluginConfig
    {
        public enum SafeActionMode
        {
            ConfirmRequired,
            OneClickInUI
        }

        public static ConfigElement<SafeActionMode> SafeActionModeSetting;
        public static ConfigElement<int> SavedGameListLimit;
        public static ConfigElement<int> LoadingWaitTimeoutMs;
        public static ConfigElement<bool> PreferUiFlowForSaveLoad;

        public static void Register(IPluginConfigRegistry config, string pluginId)
        {
            SafeActionModeSetting = config.Create(pluginId + ".safeActionMode", "Controls whether Paralives UI actions require a second click confirmation.", SafeActionMode.ConfirmRequired, "Plugin:Paralives.Safety");
            SavedGameListLimit = config.Create(pluginId + ".savedGameListLimit", "Maximum saved games to display in the Paralives panel.", 50, "Plugin:Paralives.UI");
            LoadingWaitTimeoutMs = config.Create(pluginId + ".loadingWaitTimeoutMs", "Maximum time to wait for Paralives loading actions before treating them as timed out.", 30000, "Plugin:Paralives.MCP", advanced: true);
            PreferUiFlowForSaveLoad = config.Create(pluginId + ".preferUiFlowForSaveLoad", "Prefer visible Paralives UI flows for save loading when available.", true, "Plugin:Paralives.Safety");
        }
    }
}
```

Replace moved code references:

- `ConfigManager.Paralives_SafeActionMode` -> `ParalivesPluginConfig.SafeActionModeSetting`
- `ConfigManager.Paralives_SavedGameListLimit` -> `ParalivesPluginConfig.SavedGameListLimit`
- `ConfigManager.Paralives_LoadingWaitTimeoutMs` -> `ParalivesPluginConfig.LoadingWaitTimeoutMs`
- `ConfigManager.Paralives_PreferUiFlowForSaveLoad` -> `ParalivesPluginConfig.PreferUiFlowForSaveLoad`
- `ConfigManager.ParalivesSafeActionMode.OneClickInUI` -> `ParalivesPluginConfig.SafeActionMode.OneClickInUI`

- [ ] **Step 4: Add plugin entrypoint and MCP registration**

Create `plugins/Paralives/ParalivesPlugin.cs`:

```csharp
using CinematicUnityExplorer.Plugins.Paralives.Mcp;
using CinematicUnityExplorer.Plugins.Paralives.UI;
using UnityExplorer.Plugins;

namespace CinematicUnityExplorer.Plugins.Paralives
{
    public sealed class ParalivesPlugin : IUnityExplorerPlugin
    {
        public string Id => "cinematic-unity-explorer.paralives";
        public string Name => "Paralives";
        public string Version => "1.0.0";

        public bool IsAvailable(IUnityExplorerPluginContext context)
            => ParalivesControlService.IsAvailable;

        public void Initialize(IUnityExplorerPluginContext context)
        {
            ParalivesPluginConfig.Register(context.Config, Id);
            ParalivesMcpRegistration.Register(context.Mcp);
            context.Panels.RegisterPanel(ParalivesPanel.CreateDescriptor());
        }

        public void Update()
            => ParalivesPerformanceCountersService.Update();

        public void Shutdown()
            => ParalivesPerformanceCountersService.Shutdown();
    }
}
```

Create `plugins/Paralives/ParalivesMcpRegistration.cs` with all existing service actions and descriptors. Start with this structure:

```csharp
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
```

Add `ParalivesSchemas` in the same file or a sibling `ParalivesSchemas.cs` file. Each constant is a JSON Schema string copied from the current TypeScript schema shape before the hard-coded tools are removed. The constants required by the tool list are: `InvokeMainMenuAction`, `ListSavedGames`, `LoadSavedGame`, `DryRunConfirm`, `ModPath`, `CreateContentMod`, `ImportAsset`, `SetNeedValue`, `RunCheat`, `CharacterGuid`, `PerformanceHistory`, `SceneStats`, `ListProfilerCounters`, and `ProfilerCounterSamples`.

- [ ] **Step 5: Convert Paralives panel to `IPluginPanel`**

Modify `plugins/Paralives/UI/ParalivesPanel.cs`:

1. Remove inheritance from `UEPanel`.
2. Implement `IPluginPanel`.
3. Add static descriptor:

```csharp
public static PluginPanelDescriptor CreateDescriptor()
{
    return new PluginPanelDescriptor("cinematic-unity-explorer.paralives.panel", "Paralives", host => new ParalivesPanel(), 680, 320);
}
```

4. Store `IPluginPanelHost host` and use `host.ContentRoot` instead of `ContentRoot`.
5. Replace direct `UIFactory` and `UEUI` calls with host methods where available. If a helper is missing, add the narrow helper to `IPluginPanelHost` and `PluginPanelAdapter` rather than exposing `UIManager` internals.
6. Replace config references with `ParalivesPluginConfig`.

- [ ] **Step 6: Add plugin project to solution and exclude old source from main**

Modify `src/CinematicUnityExplorer.sln` to add a project entry with a new GUID:

```text
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "CinematicUnityExplorer.ParalivesPlugin", "..\plugins\Paralives\CinematicUnityExplorer.ParalivesPlugin.csproj", "{F7C3C9F8-8D3F-4D3E-A2A7-5DBE8B5F30C4}"
EndProject
```

Add project configuration mappings for `Release_BIE6_Unity_Mono` only:

```text
{F7C3C9F8-8D3F-4D3E-A2A7-5DBE8B5F30C4}.Release_BIE6_Unity_Mono|Any CPU.ActiveCfg = BIE6_Unity_Mono|Any CPU
{F7C3C9F8-8D3F-4D3E-A2A7-5DBE8B5F30C4}.Release_BIE6_Unity_Mono|Any CPU.Build.0 = BIE6_Unity_Mono|Any CPU
```

Run `git status --short` and confirm no `src/McpBridge/Paralives/*` or `src/UI/Panels/ParalivesPanel.cs` remains as untracked duplicates.

- [ ] **Step 7: Build and commit Task 5**

Run: `dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono`

Expected: PASS.

If the plugin project fails because it references the main assembly before it is built, add a solution project dependency or change plugin project `HintPath` only after confirming the main assembly output path from the same build.

Commit only Task 5 files:

```bash
git add plugins/Paralives src/CinematicUnityExplorer.sln src/CinematicUnityExplorer.csproj src/McpBridge/Paralives src/UI/Panels/ParalivesPanel.cs
git commit -m "feat(paralives): move game tools into optional plugin"
```

---

### Task 6: Game Plugin Authoring Skill

**Files:**
- Create: `agent-skills/game-plugin-authoring/SKILL.md`
- Create: `agent-skills/game-plugin-authoring/references/plugin-api.md`
- Create: `agent-skills/game-plugin-authoring/references/mcp-tool-design.md`
- Create: `agent-skills/game-plugin-authoring/references/decompiled-code-research.md`
- Create: `agent-skills/game-plugin-authoring/references/safety-policy.md`
- Create: `agent-skills/game-plugin-authoring/templates/Plugin.cs`
- Create: `agent-skills/game-plugin-authoring/templates/PluginConfig.cs`
- Create: `agent-skills/game-plugin-authoring/templates/PluginMcpRegistration.cs`
- Create: `agent-skills/game-plugin-authoring/templates/PluginPanel.cs`
- Create: `agent-skills/game-plugin-authoring/evals/evals.json`

**Interfaces:**
- Consumes: final public plugin APIs from Tasks 1-5.
- Produces: project-local skill source and initial eval prompts for human review.

- [ ] **Step 1: Create skill frontmatter and workflow**

Create `agent-skills/game-plugin-authoring/SKILL.md`:

```markdown
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
```

- [ ] **Step 2: Add reference docs and templates**

Create `agent-skills/game-plugin-authoring/references/plugin-api.md` with sections for `IUnityExplorerPlugin`, `IUnityExplorerPluginContext`, `PluginPanelDescriptor`, `PluginMcpToolDescriptor`, `PluginMcpResourceDescriptor`, and the validation command `dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono`.

Create `agent-skills/game-plugin-authoring/references/mcp-tool-design.md` with these rules: MCP names use `GameName:verb_noun`; bridge actions use `game_name_verb_noun`; read-only tools use group `diagnostics/read-only` or `performance`; game-state write tools use group `game-control/write`, risk `write-confirmed`, default `dryRun: true`, and require `confirm`; filesystem write tools use group `filesystem/mod`, risk `filesystem-confirmed`, default `dryRun: true`, and require `confirm`.

Create `agent-skills/game-plugin-authoring/references/decompiled-code-research.md` with this search order: identify game assemblies, inspect manager/service/UI/domain classes, find stable singleton or collection accessors, map read-only properties before mutators, and record every mutator as risky until proven safe.

Create `agent-skills/game-plugin-authoring/references/safety-policy.md` with these rules: no arbitrary method invocation tools, no unbounded reflection mutation tools, bounded reads only, explicit dry-run for writes, confirmation phrase for writes, and unavailable plugins must register no tools.

Create `agent-skills/game-plugin-authoring/templates/Plugin.cs`:

```csharp
using UnityExplorer.Plugins;

namespace {{PluginNamespace}}
{
    public sealed class {{PluginClassName}} : IUnityExplorerPlugin
    {
        public string Id => "{{PluginId}}";
        public string Name => "{{PluginDisplayName}}";
        public string Version => "1.0.0";

        public bool IsAvailable(IUnityExplorerPluginContext context)
            => context.Runtime.FindType("{{AvailabilityTypeFullName}}") != null;

        public void Initialize(IUnityExplorerPluginContext context)
        {
            {{PluginClassName}}Config.Register(context.Config, Id);
            {{PluginClassName}}McpRegistration.Register(context.Mcp);
            context.Panels.RegisterPanel({{PluginClassName}}Panel.CreateDescriptor());
        }

        public void Update() { }
        public void Shutdown() { }
    }
}
```

Create `agent-skills/game-plugin-authoring/templates/PluginConfig.cs`:

```csharp
using UnityExplorer.Config;
using UnityExplorer.Plugins;

namespace {{PluginNamespace}}
{
    internal static class {{PluginClassName}}Config
    {
        public static ConfigElement<int> ResultLimit;

        public static void Register(IPluginConfigRegistry config, string pluginId)
        {
            ResultLimit = config.Create(pluginId + ".resultLimit", "Maximum results returned by {{PluginDisplayName}} tools.", 50, "Plugin:{{PluginDisplayName}}.MCP");
        }
    }
}
```

Create `agent-skills/game-plugin-authoring/templates/PluginMcpRegistration.cs`:

```csharp
using UnityExplorer.Plugins;

namespace {{PluginNamespace}}
{
    internal static class {{PluginClassName}}McpRegistration
    {
        private const string EmptySchema = "{\"type\":\"object\",\"properties\":{}}";

        public static void Register(IPluginMcpRegistry registry)
        {
            registry.RegisterAction("{{action_name}}", {{PluginClassName}}Service.Handle{{ActionPascalName}});
            registry.RegisterTool(new PluginMcpToolDescriptor("{{PluginDisplayName}}:{{tool_name}}", "{{action_name}}", "{{tool_description}}", EmptySchema, "diagnostics/read-only", "read-only"));
        }
    }
}
```

Create `agent-skills/game-plugin-authoring/templates/PluginPanel.cs`:

```csharp
using UnityExplorer.Plugins;

namespace {{PluginNamespace}}
{
    internal sealed class {{PluginClassName}}Panel : IPluginPanel
    {
        private IPluginPanelHost host;
        private GameObject content;

        public static PluginPanelDescriptor CreateDescriptor()
        {
            return new PluginPanelDescriptor("{{PluginId}}.panel", "{{PluginDisplayName}}", panelHost => new {{PluginClassName}}Panel(), 640, 320);
        }

        public void Construct(IPluginPanelHost host)
        {
            this.host = host;
            host.CreateScrollView(host.ContentRoot, "{{PluginClassName}}Scroll", out content, out _, new Color(0.12f, 0.12f, 0.12f, 1f));
            Refresh();
        }

        public void SetActive(bool active)
        {
            if (active)
                Refresh();
        }

        private void Refresh()
        {
            if (content == null)
                return;
            host.ClearChildren(content);
            host.CreateLabel(content, "Status", "{{PluginDisplayName}} plugin loaded.", TextAnchor.MiddleLeft);
        }
    }
}
```

- [ ] **Step 3: Add eval prompts**

Create `agent-skills/game-plugin-authoring/evals/evals.json`:

```json
{
  "skill_name": "game-plugin-authoring",
  "evals": [
    {
      "id": 1,
      "prompt": "我想为 Paralives 做一个 CinematicUnityExplorer 插件。请阅读游戏反编译代码后，暴露当前家庭、角色需求和存档相关 MCP 工具，并做一个工具面板。",
      "expected_output": "设计并生成一个可选游戏插件，不把 Paralives 功能固定进主程序集；MCP 写工具默认 dry-run 并要求确认。",
      "files": []
    },
    {
      "id": 2,
      "prompt": "为一个 Unity 生活模拟游戏制作 CUE 插件，只允许只读 MCP 工具，不允许写游戏状态。反编译代码里有 CharacterManager、NeedManager、HouseholdManager。",
      "expected_output": "只设计只读工具，基于 manager 类型建立工具和面板，不生成写入或 cheat 工具。",
      "files": []
    },
    {
      "id": 3,
      "prompt": "我有一个游戏的 Assembly-CSharp.dll，帮我找 manager 类型并生成专属 MCP 面板插件。构建验证用 BIE6_Unity_Mono。",
      "expected_output": "先研究反编译程序集结构，再按 UnityExplorer.Plugins API 生成插件，并使用 Release_BIE6_Unity_Mono 验证。",
      "files": []
    }
  ]
}
```

- [ ] **Step 4: Commit Task 6**

Run: `git status --short`

Expected: only `agent-skills/game-plugin-authoring/*` plus unrelated pre-existing DLL changes.

Commit only skill files:

```bash
git add agent-skills/game-plugin-authoring
git commit -m "docs(skills): add game plugin authoring skill"
```

---

### Task 7: End-to-End Verification and Cleanup

**Files:**
- Modify: `README.md`
- Modify: `mcp-server/README.md` if Task 4 did not fully document dynamic plugin tools.
- Modify: `docs/superpowers/specs/2026-07-19-game-plugin-framework-design.md` only if implementation intentionally deviates from the accepted design.

**Interfaces:**
- Consumes: deliverables from Tasks 1-6.
- Produces: verified build, typecheck, docs, and final review-ready branch state.

- [ ] **Step 1: Update README build/plugin documentation**

In `README.md`, under Building, add this short plugin note near the existing targeted validation command:

```text
Game-specific plugins are optional assemblies loaded from the CinematicUnityExplorer plugin folder. The first supported plugin build path is `BIE6_Unity_Mono`; validate it with:

~~~powershell
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono
~~~

When no game-specific plugin is installed or available, CinematicUnityExplorer runs with only its core panels and core MCP tools.
```

- [ ] **Step 2: Run final verification**

Run: `dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono`

Expected: PASS.

Run: `npm --prefix mcp-server run typecheck`

Expected: PASS.

Run: `git grep "Paralives" -- src/McpBridge src/UI src/Config src/ExplorerBehaviour.cs src/ExplorerCore.cs`

Expected: no main-assembly hard-coded Paralives registrations or config fields. Acceptable matches are documentation comments only if they do not create runtime behavior.

Run: `git grep "Paralives:" -- mcp-server/src/index.ts`

Expected: no matches.

- [ ] **Step 3: Inspect git state and commit verification docs**

Run: `git status --short && git diff --stat`

Expected: only README/docs changes intended for Task 7 plus pre-existing DLL changes if still present.

Commit only Task 7 files:

```bash
git add README.md mcp-server/README.md docs/superpowers/specs/2026-07-19-game-plugin-framework-design.md
git commit -m "docs(plugins): document optional game plugin loading"
```

- [ ] **Step 4: Prepare final handoff summary**

Collect these outputs for the final response:

```text
git log --oneline -7
git status --short
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono
npm --prefix mcp-server run typecheck
```

The final summary must state whether the two unrelated DLL changes in `UnityEditorPackage/Runtime/` remain uncommitted and untouched.

---

## Self-Review

Spec coverage:

- Public plugin API is covered by Task 1.
- Plugin discovery, availability, lifecycle, status, and error isolation are covered by Task 2.
- Plugin panel registration and save keys are covered by Task 3.
- Dynamic MCP tools/resources are covered by Task 4.
- Paralives migration and new plugin config architecture are covered by Task 5.
- AI Agent Skill source, references, templates, and evals are covered by Task 6.
- `BIE6_Unity_Mono` build and MCP typecheck verification are covered by Task 7.

Placeholder scan:

- The plan intentionally uses template placeholders only inside files under `agent-skills/game-plugin-authoring/templates/`; these are template variables for future plugin generation, not missing implementation details.
- No implementation step uses placeholder instructions; template variables only appear inside generated skill templates.

Type consistency:

- `IUnityExplorerPluginContext` exposes `Panels`, `Mcp`, `Config`, and `Runtime` and `PluginContext` implements all four registries.
- `PluginPanelDescriptor` uses `Func<IPluginPanelHost, IPluginPanel>`, consumed by `PluginPanelAdapter`.
- `PluginMcpToolDescriptor.InputSchemaJson` is parsed by `McpJson.Parse` in `get_mcp_tool_definitions`.
- `PluginManager.RegisteredPanels` feeds `UIManager.InitUI` before plugin panels are displayed.
