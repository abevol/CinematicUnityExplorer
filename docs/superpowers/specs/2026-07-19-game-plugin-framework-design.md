# 游戏专属插件框架与 Paralives 插件设计

## 状态

已确认设计，待实现计划。

## 日期

2026-07-19

## 背景

CinematicUnityExplorer 当前包含一组 Paralives 专属功能，包括 `src/McpBridge/Paralives/*` 下的 MCP bridge 服务、`src/UI/Panels/ParalivesPanel.cs` 面板、`UIManager` 中固定的 Paralives 面板注册、`McpActionRegistry` 中固定的 Paralives action 注册、`ExplorerBehaviour` 中固定的 Paralives 性能计数器生命周期调用，以及 `ConfigManager` 中固定的 Paralives 配置项。

这些功能只对游戏 Paralives 有意义。固定编译和固定加载会让其他游戏携带无用功能，也会让未来为其他游戏扩展专属工具时继续污染主程序集。因此需要把 Paralives 功能提取为可选插件，并建立通用游戏插件框架。

同时需要制作一份 AI Agent Skill，让 Agent 能通过阅读目标 Unity 游戏的反编译代码，在统一插件 API 指导下制作该游戏专属的工具面板和 MCP 功能插件。

首版验证限定使用 `BIE6_Unity_Mono` 变体。

## 目标

- 主程序集不再固定引用或注册 Paralives 专属功能。
- 主程序集提供稳定的游戏插件扩展 API。
- Paralives 迁移为首个外部游戏插件 DLL。
- 未安装或未启用 Paralives 插件时，不出现 Paralives 面板、配置分类、MCP 工具或资源。
- MCP Server 使用动态工具清单，插件未加载时不暴露 `Paralives:*`。
- AI Agent Skill 能指导 Agent 基于反编译代码设计和实现游戏专属插件。
- 首版构建验证使用 `dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono`。

## 非目标

- 首版不覆盖所有 Mono 或 IL2CPP 变体。
- 不兼容旧 Paralives 配置项。
- 不读取、不迁移、不删除旧 Paralives 配置残留。
- 不通过 `InternalsVisibleTo` 或反射把现有内部实现暴露给插件作为长期契约。
- 不要求 AI Skill 自动理解所有 Unity 游戏，只提供稳定工作流、模板、安全策略和验证路径。

## 已确认决策

- 插件方向：通用游戏插件框架加 Paralives 首个插件。
- 首发变体：仅 `BIE6_Unity_Mono`。
- MCP 语义：动态工具清单，插件未加载则不暴露游戏专属工具。
- AI Agent Skill：仓库内保留源文件，同时提供可安装包或安装说明。
- 实现方案：公开扩展契约加外部插件 DLL。
- Paralives 配置：设计全新的插件配置架构，不兼容旧配置项。

## 方案取舍

### 采用：公开扩展契约加外部插件 DLL

主程序集新增 `UnityExplorer.Plugins` 公开契约。Paralives 迁移到独立插件项目，引用公开 API，编译为 `CinematicUnityExplorer.ParalivesPlugin.dll`。主程序负责插件发现、加载、生命周期调度、UI 适配、MCP action 注册和动态工具清单输出。

优点：边界清晰，类型安全，适合未来其他游戏插件复用，也适合 AI Skill 生成插件代码。

代价：需要新增少量公共 API，并调整现有面板和 MCP 注册机制。

### 放弃：友元程序集加最小拆分

通过 `InternalsVisibleTo` 让 Paralives 插件直接访问内部类型。

放弃原因：会把内部实现变成事实 API，不适合作为通用插件框架，也不利于 AI Skill 生成稳定插件。

### 放弃：反射式插件适配

插件通过反射调用 `UIManager`、`McpActionRegistry` 和 `ConfigManager` 的内部成员。

放弃原因：脆弱、难调试、无类型安全，且会鼓励未来插件依赖内部结构。

## 架构

系统拆分为三层。

### 主程序集核心

`CinematicUnityExplorer.BIE6.Unity.Mono.dll` 保留通用能力：UI 宿主、基础 MCP bridge、基础 UnityExplorer MCP 工具、配置系统、生命周期调度、插件发现和插件加载。

主程序集不再引用 `UnityExplorer.McpBridge.Paralives`，也不再固定注册 `ParalivesPanel`、`Paralives:*` MCP 工具或 Paralives 配置项。

### 公共插件 API

新增公开命名空间 `UnityExplorer.Plugins`，只暴露插件必须依赖的契约：插件身份、可用性检测、生命周期、面板注册、MCP 注册、配置注册和基础运行时能力。

主程序内部集合和实现细节继续隐藏，包括 `UIManager.UIPanels`、`McpActionRegistry` 内部字典和 `ConfigManager` 内部配置集合。

### Paralives 插件

Paralives 相关代码迁移到独立项目和独立 DLL。该插件只在检测到 Paralives 运行环境时注册功能。

插件提供：Paralives 控制面板、Paralives MCP 工具、Paralives MCP 资源、性能计数器每帧更新和退出时资源释放。

## 公共插件 API

### 插件入口

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

约定：

- `Id` 使用稳定 ID，例如 `cinematic-unity-explorer.paralives`。
- `IsAvailable` 只做轻量检测，不写入游戏状态。
- `Initialize` 只注册面板、配置、MCP action、MCP 工具和资源。
- `Update` 与 `Shutdown` 可以为空。
- 主程序捕获每个插件生命周期异常，单个插件失败不阻止核心功能。

### 插件上下文

```csharp
public interface IUnityExplorerPluginContext
{
    IPluginPanelRegistry Panels { get; }
    IPluginMcpRegistry Mcp { get; }
    IPluginConfigRegistry Config { get; }
    IPluginRuntime Runtime { get; }
}
```

上下文职责：

- `Panels` 注册插件面板。
- `Mcp` 注册 bridge action、MCP 工具元数据和资源元数据。
- `Config` 创建插件配置项。
- `Runtime` 提供日志、路径、程序集查询、类型查询和基础状态等安全能力。

### 面板 API

现有 `UEPanel.PanelType` 强依赖 `UIManager.Panels` 枚举。插件面板不能继续依赖固定枚举，因此新增插件面板描述符和适配层。

```csharp
public sealed class PluginPanelDescriptor
{
    public string Id { get; }
    public string Title { get; }
    public Func<IPluginPanelHost, IPluginPanel> Create { get; }
    public int MinWidth { get; }
    public int MinHeight { get; }
    public bool ShowByDefault { get; }
}
```

插件面板实现窄接口：

```csharp
public interface IPluginPanel
{
    void Construct(IPluginPanelHost host);
    void SetActive(bool active);
}
```

`IPluginPanelHost` 包装必要 UI 能力，例如内容根节点、状态栏、滚动区域和常用控件创建方法。插件不直接操作 `UIManager.UIPanels`。

插件面板保存数据使用字符串 ID，例如 `plugin:cinematic-unity-explorer.paralives:panel:main`。

### MCP API

插件注册运行时 action 和 MCP 元数据。

```csharp
public sealed class PluginMcpToolDescriptor
{
    public string Name { get; }
    public string Action { get; }
    public string Description { get; }
    public string InputSchemaJson { get; }
    public string Group { get; }
    public string Risk { get; }
}
```

约定：

- `Name` 是 MCP 对外工具名，例如 `Paralives:get_game_state`。
- `Action` 是 bridge 内部 action，例如 `paralives_get_game_state`。
- `InputSchemaJson` 是 JSON Schema 字符串，避免 net35 与 TypeScript 共享复杂类型。
- `Group` 和 `Risk` 用于让 Agent 明确工具风险。
- 写入游戏状态或文件系统的工具必须默认 dry-run，并要求确认参数。

主程序新增基础 action `get_mcp_tool_definitions`，返回已注册插件工具和资源。MCP Server 使用该 action 动态生成清单。

### 配置 API

插件通过公共注册 API 创建配置项。

```csharp
ConfigElement<T> Create<T>(
    string name,
    string description,
    T defaultValue,
    string category,
    bool requiresRestart = false,
    bool advanced = false);
```

Paralives 插件使用全新配置命名空间，不兼容旧主程序配置项。推荐配置键使用插件 ID 前缀，例如：

- `cinematic-unity-explorer.paralives.safeActionMode`
- `cinematic-unity-explorer.paralives.savedGameListLimit`
- `cinematic-unity-explorer.paralives.loadingWaitTimeoutMs`
- `cinematic-unity-explorer.paralives.preferUiFlowForSaveLoad`

推荐分类：

- `Plugin:Paralives`
- `Plugin:Paralives.MCP`
- `Plugin:Paralives.UI`
- `Plugin:Paralives.Safety`

旧配置行为：旧 `Paralives Safe Action Mode` 等配置项不读取、不迁移、不删除、不警告。新插件首次加载时使用新默认值创建新配置项。

## Paralives 迁移

### 迁出文件

迁移到 Paralives 插件项目：

- `src/McpBridge/Paralives/*`
- `src/UI/Panels/ParalivesPanel.cs`

推荐新命名空间：

- `CinematicUnityExplorer.Plugins.Paralives`
- `CinematicUnityExplorer.Plugins.Paralives.Mcp`
- `CinematicUnityExplorer.Plugins.Paralives.UI`

### 主程序删除固定接入

主程序删除：

- `UIManager.Panels.Paralives`
- `UIManager.InitUI()` 中的固定 `ParalivesPanel` 注册
- `ExplorerBehaviour.Update()` 中对 `ParalivesPerformanceCountersService.Update()` 的直接调用
- `ExplorerBehaviour.OnApplicationQuit()` 中对 `ParalivesPerformanceCountersService.Shutdown()` 的直接调用
- `McpActionRegistry` 中所有 `Paralives.*.Actions` 固定注册
- `ConfigManager` 中所有 `Paralives_*` 字段、枚举和初始化

这些行为由 `ParalivesPlugin.Initialize(context)`、`Update()` 和 `Shutdown()` 接管。

### 插件入口

```csharp
public sealed class ParalivesPlugin : IUnityExplorerPlugin
{
    public string Id => "cinematic-unity-explorer.paralives";
    public string Name => "Paralives";
    public string Version => "1.0.0";

    public bool IsAvailable(IUnityExplorerPluginContext context)
        => ParalivesEnvironment.IsAvailable;

    public void Initialize(IUnityExplorerPluginContext context)
    {
        ParalivesPluginConfig.Register(context.Config, Id);
        ParalivesMcpRegistration.Register(context.Mcp);
        context.Panels.RegisterPanel(ParalivesPanelDescriptor.Create());
    }

    public void Update()
        => ParalivesPerformanceCountersService.Update();

    public void Shutdown()
        => ParalivesPerformanceCountersService.Shutdown();
}
```

### 面板迁移

`ParalivesPanel` 改为插件面板实现，不再返回 `UIManager.Panels.Paralives`。当前 State、Main Menu、Saves、Settings 四个 Tab 的行为可以保留，但通过 `IPluginPanelHost` 创建 UI，通过新插件配置项读取选项。

### MCP 服务迁移

各 `Paralives*Service.Actions` 可以保留 action 到 handler 的结构，但统一由 `ParalivesMcpRegistration` 注册到 `context.Mcp`。工具描述、输入 schema、风险等级和资源元数据也由该注册类集中维护。

## 插件发现与加载

主程序在核心初始化完成、UI 创建前加载插件。

流程：

1. 确定插件目录。
2. 扫描符合约定的 DLL，例如 `CinematicUnityExplorer.*Plugin.dll`。
3. 查找 public 非抽象 `IUnityExplorerPlugin` 实现。
4. 创建插件实例。
5. 调用 `IsAvailable(context)`。
6. 对可用插件调用 `Initialize(context)` 并加入生命周期列表。
7. 每帧调用可用插件的 `Update()`。
8. 退出时反向调用可用插件的 `Shutdown()`。

首版默认插件位置：

```text
BepInEx/plugins/CinematicUnityExplorer/
  CinematicUnityExplorer.BIE6.Unity.Mono.dll
  CinematicUnityExplorer.ParalivesPlugin.dll
  plugins/
    SomeOtherGamePlugin.dll
```

插件加载、检测、初始化、更新、关闭都要错误隔离。单个插件失败只记录日志，不阻止主 UI 和基础 MCP bridge 启动。

## 插件状态诊断

主程序新增基础 action：

```text
get_plugin_status
```

返回示例：

```json
{
  "plugins": [
    {
      "id": "cinematic-unity-explorer.paralives",
      "name": "Paralives",
      "version": "1.0.0",
      "assembly": "CinematicUnityExplorer.ParalivesPlugin.dll",
      "state": "loaded",
      "available": true,
      "error": null
    }
  ]
}
```

## MCP 动态清单

`mcp-server` 保留 UnityExplorer 基础工具作为静态清单。插件工具和资源通过 bridge 动态读取。

新增基础 action：

```text
get_mcp_tool_definitions
```

返回示例：

```json
{
  "tools": [
    {
      "name": "Paralives:get_game_state",
      "action": "paralives_get_game_state",
      "description": "Read current Paralives scene/UI/loading state.",
      "inputSchema": { "type": "object", "properties": {} },
      "group": "diagnostics/read-only",
      "risk": "read-only",
      "pluginId": "cinematic-unity-explorer.paralives"
    }
  ],
  "resources": [
    {
      "uri": "paralives://types/managers",
      "name": "Paralives Manager Types",
      "action": "paralives_read_resource",
      "params": { "uri": "paralives://types/managers" },
      "mimeType": "application/json",
      "pluginId": "cinematic-unity-explorer.paralives"
    }
  ]
}
```

MCP Server 行为：

- `ListToolsRequestSchema` 合并基础工具和动态插件工具。
- `CallToolRequestSchema` 使用工具名到 action 的动态映射。
- `ListResourcesRequestSchema` 合并基础资源和动态插件资源。
- `ReadResourceRequestSchema` 使用资源 URI 到 action 的动态映射。
- bridge 不可连接时仍返回基础工具，插件工具为空。
- 插件未加载时不暴露任何 `Paralives:*` 工具或 `paralives://` 资源。

## AI Agent Skill

仓库内新增 Skill 源文件，推荐目录：

```text
agent-skills/game-plugin-authoring/
  SKILL.md
  references/
    plugin-api.md
    mcp-tool-design.md
    decompiled-code-research.md
    safety-policy.md
  templates/
    Plugin.cs
    PluginConfig.cs
    PluginMcpRegistration.cs
    PluginPanel.cs
  evals/
    evals.json
```

Skill 目标：指导 AI Agent 阅读目标 Unity 游戏的反编译代码，设计并实现游戏专属 CinematicUnityExplorer 插件。

Skill 触发条件应覆盖：

- 为某 Unity 游戏制作专属工具面板。
- 基于反编译代码制作 MCP 工具。
- 把某游戏功能做成 CinematicUnityExplorer 插件。
- 用户提到 `Assembly-CSharp.dll`、dnSpy、ILSpy、反编译、Mono.Cecil、UnityExplorer MCP。

Skill 工作流：

1. 识别目标游戏和运行变体。
2. 阅读反编译代码，优先寻找 manager、service、UI、domain 类型。
3. 区分只读诊断、游戏状态写入、文件系统写入。
4. 为每个 MCP 工具定义 action、MCP 名称、schema、风险等级和确认策略。
5. 设计工具面板信息架构，不把所有反编译类型直接塞进 UI。
6. 使用 `UnityExplorer.Plugins` 公共 API 生成插件代码。
7. 运行构建验证。
8. 输出插件安装路径和 MCP 使用说明。

当前项目首版验证命令：

```powershell
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono
```

Skill 测试案例初稿：

- 为 Paralives 做一个插件，读取反编译代码后暴露当前家庭、角色需求和存档工具。
- 为某 Unity 生活模拟游戏制作 CUE 插件，只允许只读 MCP 工具，不允许写游戏状态。
- 用户提供 `Assembly-CSharp.dll`，要求找 manager 类型并生成专属 MCP 面板插件。

后续按 skill 创建流程创建 `evals.json`，运行带 Skill 和不带 Skill 的测试输出，并用 `eval-viewer/generate_review.py` 供人工审阅。

## 构建和验证

必须验证：

```powershell
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono
npm --prefix mcp-server run typecheck
```

行为验收：

- 未安装 Paralives 插件时，主程序不固定注册 Paralives 面板。
- 未安装 Paralives 插件时，MCP 清单不含 `Paralives:*` 或 `paralives://`。
- 安装插件但不在 Paralives 时，插件状态显示 unavailable，不暴露工具。
- 安装插件且在 Paralives 时，Paralives 面板、配置、MCP 工具和资源出现。
- 插件异常不会阻止基础 UnityExplorer UI 和基础 MCP 工具工作。
- Paralives 旧配置项不被读取、迁移、删除或警告。

## 风险与缓解

- 风险：插件面板适配层与现有 `UEPanel` 保存状态模型冲突。
  缓解：插件面板保存数据使用字符串 ID，不进入 `UIManager.Panels` 枚举。

- 风险：动态 MCP 清单在 bridge 未连接时影响基础工具可见性。
  缓解：MCP Server 保留基础工具静态清单，动态清单失败时只省略插件工具。

- 风险：插件异常影响核心启动。
  缓解：插件每个生命周期阶段独立 try/catch，并在 `get_plugin_status` 中暴露错误。

- 风险：AI Skill 生成过度危险的写工具。
  缓解：Skill 明确风险分级，写游戏状态和文件系统工具默认 dry-run，并要求确认参数。

- 风险：首版只支持 `BIE6_Unity_Mono`，其他变体用户误用。
  缓解：项目、Skill 和构建说明中明确首版限制。

## 实现顺序建议

1. 新增主程序插件 API、插件管理器、插件状态诊断。
2. 改造 UI 面板注册，使核心面板继续使用枚举，插件面板使用字符串 ID。
3. 改造 MCP 注册表和 MCP Server 动态清单。
4. 新建 Paralives 插件项目并迁移 Paralives 服务、面板、配置注册。
5. 从主程序删除固定 Paralives 接入。
6. 新增仓库内 AI Agent Skill、模板、参考文档和 evals 初稿。
7. 运行 `BIE6_Unity_Mono` 构建验证和 MCP Server typecheck。

## 待实施计划确认点

- 插件 API 文件和命名空间的具体文件布局。
- 插件项目在解决方案中的位置和输出路径。
- 动态 MCP 工具 schema 在 C# 中存储为字符串还是轻量对象后序列化。
- `IPluginPanelHost` 第一版暴露的 UI helper 范围。
