# Plugin API Reference

## IUnityExplorerPlugin

`UnityExplorer.Plugins.IUnityExplorerPlugin`

The main plugin interface. Every game plugin must implement this.

```csharp
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
```

- `Id` – Unique identifier (e.g. `"cinematic-unity-explorer.paralives"`).
- `Name` – Display name shown in the CUE UI.
- `Version` – Plugin version string.
- `IsAvailable` – Return false when the target game assembly is not loaded. Use `context.Runtime.FindType(...)` to check.
- `Initialize` – Register config, MCP tools, and panels.
- `Update` – Called every frame; keep lightweight.
- `Shutdown` – Cleanup; remove hooks, stop timers.

## IUnityExplorerPluginContext

`UnityExplorer.Plugins.IUnityExplorerPluginContext`

Passed to `Initialize` and `IsAvailable`.

```csharp
public interface IUnityExplorerPluginContext
{
    IPluginPanelRegistry Panels { get; }
    IPluginMcpRegistry Mcp { get; }
    IPluginConfigRegistry Config { get; }
    IPluginRuntime Runtime { get; }
}
```

- `Panels` – Register UI panels.
- `Mcp` – Register MCP actions, tools, and resources.
- `Config` – Create config elements.
- `Runtime` – Runtime helpers (`FindType`, etc.).

## IPluginPanel

`UnityExplorer.Plugins.IPluginPanel`

```csharp
public interface IPluginPanel
{
    void Construct(IPluginPanelHost host);
    void SetActive(bool active);
}
```

## PluginPanelDescriptor

`UnityExplorer.Plugins.PluginPanelDescriptor`

```csharp
public sealed class PluginPanelDescriptor
{
    public PluginPanelDescriptor(string id, string title,
        Func<IPluginPanelHost, IPluginPanel> create,
        int minWidth, int minHeight, bool showByDefault = false);
    public string Id { get; }
    public string Title { get; }
    public Func<IPluginPanelHost, IPluginPanel> Create { get; }
    public int MinWidth { get; }
    public int MinHeight { get; }
    public bool ShowByDefault { get; }
}
```

## IPluginPanelHost

`UnityExplorer.Plugins.IPluginPanelHost`

```csharp
public interface IPluginPanelHost
{
    GameObject ContentRoot { get; }
    Text CreateStatus(string name, string text);
    ButtonRef CreateButton(GameObject parent, string name, string text);
    Text CreateLabel(GameObject parent, string name, string text, TextAnchor anchor);
    GameObject CreateHorizontalGroup(GameObject parent, string name, int spacing, TextAnchor alignment);
    GameObject CreateVerticalGroup(GameObject parent, string name, int spacing, TextAnchor alignment);
    void CreateScrollView(GameObject parent, string name, out GameObject content,
        out AutoSliderScrollbar scrollbar, Color background);
    void SetLayoutElement(GameObject target, int? minWidth = null, int? minHeight = null,
        int? flexibleWidth = null, int? flexibleHeight = null);
    void ClearChildren(GameObject target);
}
```

## IPluginPanelRegistry

`UnityExplorer.Plugins.IPluginPanelRegistry`

```csharp
public interface IPluginPanelRegistry
{
    void RegisterPanel(PluginPanelDescriptor descriptor);
}
```

## IPluginMcpRegistry

`UnityExplorer.Plugins.IPluginMcpRegistry`

```csharp
public interface IPluginMcpRegistry
{
    void RegisterAction(string action, Func<Dictionary<string, object>, object> handler);
    void RegisterTool(PluginMcpToolDescriptor descriptor);
    void RegisterResource(PluginMcpResourceDescriptor descriptor);
}
```

## PluginMcpToolDescriptor

`UnityExplorer.Plugins.PluginMcpToolDescriptor`

```csharp
public sealed class PluginMcpToolDescriptor
{
    public PluginMcpToolDescriptor(string name, string action, string description,
        string inputSchemaJson, string group, string risk);
    public string Name { get; }
    public string Action { get; }
    public string Description { get; }
    public string InputSchemaJson { get; }
    public string Group { get; }
    public string Risk { get; }
}
```

## PluginMcpResourceDescriptor

`UnityExplorer.Plugins.PluginMcpResourceDescriptor`

```csharp
public sealed class PluginMcpResourceDescriptor
{
    public PluginMcpResourceDescriptor(string uri, string name, string description,
        string mimeType, string action, Dictionary<string, object> parameters);
    public string Uri { get; }
    public string Name { get; }
    public string Description { get; }
    public string MimeType { get; }
    public string Action { get; }
    public Dictionary<string, object> Parameters { get; }
}
```

## IPluginConfigRegistry

`UnityExplorer.Plugins.IPluginConfigRegistry`

```csharp
public interface IPluginConfigRegistry
{
    ConfigElement<T> Create<T>(string name, string description, T defaultValue,
        string category, bool requiresRestart = false, bool advanced = false);
}
```

## IPluginRuntime

`UnityExplorer.Plugins.IPluginRuntime`

Available via `context.Runtime`.
- `FindType(string fullName)` – Returns the `Type` if found, null otherwise. Use in `IsAvailable` to detect the target game.

## Validation

```bash
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono
```
