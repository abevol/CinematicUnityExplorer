# Paralives MCP 运行时状态工具设计方案

## 概述

本文档描述如何为 Paralives MCP 服务器添加两类新工具：
1. **运行时状态工具** - 快速获取玩家此刻最关心的信息
2. **日志读取工具** - 读取 UIConsole 和 Unity 日志回调

## 架构设计

```
┌─────────────────────────────────────────────────────────────┐
│                    MCP Server (index.ts)                     │
├─────────────────────────────────────────────────────────────┤
│  Layer 1: 运行时状态工具 (Runtime State)                      │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Paralives:get_runtime_summary                       │   │
│  │   - 时间、资金、模式、选中对象                          │   │
│  │   - 一次调用获取所有关键状态                            │   │
│  └─────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│  Layer 2: 日志工具 (Logging)                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ UnityExplorer:get_game_logs                         │   │
│  │   - 读取 UIConsole 中的游戏日志                        │   │
│  │                                                     │   │
│  │ UnityExplorer:subscribe_logs                        │   │
│  │   - 订阅 Unity 日志回调                               │   │
│  │   - 返回实时日志流                                    │   │
│  └─────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│  Layer 3: 数据查询工具 (Data Query) - 现有工具                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ list_characters, list_households, list_lots, etc.   │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## 详细设计

### 1. 运行时状态工具

#### 1.1 `Paralives:get_runtime_summary`

**用途**: 一次调用获取所有关键运行时状态

**输入参数**: 无

**返回结构**:
```json
{
  "timestamp": "2026-05-27T12:00:00Z",
  "gameState": {
    "mode": "game|main_menu|loading|character_creation|build_mode",
    "isPaused": false,
    "timeSpeed": 1,
    "gameTime": {
      "minutes": 819,
      "formatted": "13:39",
      "day": 1,
      "hour": 13,
      "minute": 39
    }
  },
  "economy": {
    "householdFunds": 200292,
    "formatted": "$200,292"
  },
  "selection": {
    "hasSelection": false,
    "selectedObject": null,
    "selectedType": null
  },
  "currentFamily": {
    "name": "Smith Household",
    "memberCount": 3,
    "currentLot": "123 Main Street"
  },
  "uiState": {
    "visiblePanels": ["UIGameBar", "UITime"],
    "isMainMenuVisible": false
  }
}
```

**Unity Bridge Action**: `paralives_get_runtime_summary`

**实现逻辑**:
1. 读取 `UITime` 组件获取时间状态
2. 读取 `UIGameBar` 组件获取资金
3. 读取 `UISelected` 组件获取选中对象
4. 读取 `GameLoadingManager` 获取游戏模式
5. 读取 `SavedGameManager` 获取存档状态
6. 聚合所有数据返回

#### 1.2 `Paralives:get_game_time`

**用途**: 专门获取游戏时间状态

**返回结构**:
```json
{
  "isPaused": true,
  "timeSpeed": 0,
  "minutes": 819,
  "formatted": "13:39",
  "day": 1,
  "hour": 13,
  "minute": 39,
  "realtimeSinceStartup": 606.0641
}
```

#### 1.3 `Paralives:get_economy`

**用途**: 专门获取经济状态

**返回结构**:
```json
{
  "householdFunds": 200292,
  "formatted": "$200,292"
}
```

#### 1.4 `Paralives:get_selection`

**用途**: 获取当前选中的对象

**返回结构**:
```json
{
  "hasSelection": false,
  "selectedObject": null,
  "selectedType": null,
  "selectedSubEntity": -1
}
```

### 2. 日志工具

#### 2.1 `UnityExplorer:get_game_logs`

**用途**: 读取 UIConsole 中的游戏日志

**输入参数**:
```json
{
  "limit": 50,           // 返回的日志条数
  "type": "all|log|warning| exception",  // 日志类型过滤
  "includeCollapsed": true  // 是否包含折叠计数
}
```

**返回结构**:
```json
{
  "logs": [
    {
      "id": 1,
      "type": "log",
      "message": "Game initialized successfully",
      "timestamp": "2026-05-27T12:00:00Z",
      "collapseCount": 1,
      "stackTrace": null
    },
    {
      "id": 2,
      "type": "warning",
      "message": "Texture quality reduced for performance",
      "timestamp": "2026-05-27T12:00:01Z",
      "collapseCount": 3,
      "stackTrace": null
    }
  ],
  "totalCount": 150,
  "logCount": 120,
  "warningCount": 20,
  "exceptionCount": 10
}
```

**Unity Bridge Action**: `get_game_logs`

**实现逻辑**:
1. 查找 `UIConsole` 对象
2. 遍历 `UIConsoleItem` 子对象
3. 读取每个 `LabelLog` 的文本
4. 读取 `ImageIconLogType` 判断日志类型
5. 读取 `CollapseCount` 获取折叠计数
6. 按类型过滤并返回

#### 2.2 `UnityExplorer:subscribe_logs`

**用途**: 订阅 Unity 日志回调，获取实时日志

**输入参数**:
```json
{
  "bufferSize": 100,  // 缓冲区大小
  "types": ["log", "warning", "exception"]  // 订阅的日志类型
}
```

**返回结构**:
```json
{
  "subscriptionId": "sub_abc123",
  "status": "active",
  "bufferSize": 100,
  "subscribedTypes": ["log", "warning", "exception"]
}
```

**Unity Bridge Action**: `subscribe_logs`

**实现逻辑**:
1. 在 Unity 端注册 `Application.logMessageReceived` 回调
2. 创建环形缓冲区存储日志
3. 通过 WebSocket 推送新日志到 MCP 服务器
4. MCP 服务器缓存日志供客户端查询

#### 2.3 `UnityExplorer:poll_logs`

**用途**: 轮询已订阅的日志流

**输入参数**:
```json
{
  "subscriptionId": "sub_abc123",
  "since": 1234567890,  // 可选：从指定时间戳开始
  "limit": 50
}
```

**返回结构**:
```json
{
  "logs": [...],
  "hasMore": false,
  "nextPollToken": "token_xyz"
}
```

### 3. 实现文件结构

#### 3.1 新增文件

```
mcp-server/
├── src/
│   ├── index.ts                    # 主入口（修改）
│   ├── unity-bridge-client.ts      # WebSocket 客户端（不变）
│   ├── schemas/
│   │   ├── runtime.ts              # 运行时状态工具 schema
│   │   └── logging.ts              # 日志工具 schema
│   └── handlers/
│       ├── runtime.ts              # 运行时状态处理
│       └── logging.ts              # 日志处理
```

#### 3.2 `src/schemas/runtime.ts`

```typescript
// 运行时状态工具 schema 定义
export const getRuntimeSummarySchema = {
  type: "object",
  properties: {},
} as const;

export const getGameTimeSchema = {
  type: "object",
  properties: {},
} as const;

export const getEconomySchema = {
  type: "object",
  properties: {},
} as const;

export const getSelectionSchema = {
  type: "object",
  properties: {},
} as const;
```

#### 3.3 `src/schemas/logging.ts`

```typescript
// 日志工具 schema 定义
export const getGameLogsSchema = {
  type: "object",
  properties: {
    limit: { 
      type: "integer", 
      minimum: 1, 
      maximum: 500, 
      default: 50 
    },
    type: { 
      type: "string", 
      enum: ["all", "log", "warning", "exception"], 
      default: "all" 
    },
    includeCollapsed: { 
      type: "boolean", 
      default: true 
    },
  },
} as const;

export const subscribeLogsSchema = {
  type: "object",
  properties: {
    bufferSize: { 
      type: "integer", 
      minimum: 10, 
      maximum: 1000, 
      default: 100 
    },
    types: {
      type: "array",
      items: { 
        type: "string", 
        enum: ["log", "warning", "exception"] 
      },
      default: ["log", "warning", "exception"],
    },
  },
} as const;

export const pollLogsSchema = {
  type: "object",
  properties: {
    subscriptionId: { type: "string" },
    since: { type: "integer" },
    limit: { 
      type: "integer", 
      minimum: 1, 
      maximum: 200, 
      default: 50 
    },
  },
  required: ["subscriptionId"],
} as const;
```

### 4. Unity Bridge Actions

#### 4.1 新增 Actions

```typescript
// Unity Bridge 需要支持的新 actions
type UnityBridgeAction = 
  // 现有 actions
  | "find_game_objects"
  | "get_object_detail"
  | "set_component_property"
  | "call_component_method"
  | "get_runtime_status"
  | "get_recent_logs"
  | "list_config"
  | "get_mcp_status"
  | "paralives_get_type_index"
  | "paralives_get_game_state"
  | "paralives_list_main_menu_actions"
  | "paralives_invoke_main_menu_action"
  | "paralives_list_saved_games"
  | "paralives_load_saved_game"
  | "paralives_start_new_game"
  | "paralives_get_loading_state"
  | "paralives_list_content_mods"
  | "paralives_inspect_content_mod"
  | "paralives_create_content_mod"
  | "paralives_import_asset_to_mod"
  | "paralives_list_characters"
  | "paralives_list_households"
  | "paralives_list_lots"
  | "paralives_set_need_value"
  | "paralives_list_cheat_commands"
  | "paralives_run_whitelisted_cheat"
  // 新增 actions
  | "paralives_get_runtime_summary"
  | "paralives_get_game_time"
  | "paralives_get_economy"
  | "paralives_get_selection"
  | "get_game_logs"
  | "subscribe_logs"
  | "poll_logs";
```

#### 4.2 Unity 端实现示例

```csharp
// UnityBridge.cs 中需要添加的处理
public class UnityBridge : MonoBehaviour
{
    // 日志缓冲区
    private readonly List<LogEntry> _logBuffer = new List<LogEntry>();
    private readonly int _maxBufferSize = 1000;
    
    // 订阅管理
    private readonly Dictionary<string, LogSubscription> _subscriptions = new();
    
    void OnEnable()
    {
        // 订阅 Unity 日志回调
        Application.logMessageReceived += OnLogMessageReceived;
    }
    
    void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
    }
    
    private void OnLogMessageReceived(string message, string stackTrace, LogType type)
    {
        var entry = new LogEntry
        {
            Id = _logBuffer.Count + 1,
            Type = type.ToString().ToLower(),
            Message = message,
            StackTrace = stackTrace,
            Timestamp = DateTime.UtcNow,
            CollapseCount = 1
        };
        
        // 检查是否可以合并（相同消息）
        if (_logBuffer.Count > 0 && _logBuffer[^1].Message == message)
        {
            _logBuffer[^1].CollapseCount++;
        }
        else
        {
            _logBuffer.Add(entry);
            
            // 限制缓冲区大小
            if (_logBuffer.Count > _maxBufferSize)
            {
                _logBuffer.RemoveAt(0);
            }
        }
        
        // 推送给所有订阅者
        foreach (var subscription in _subscriptions.Values)
        {
            if (subscription.Types.Contains(type.ToString().ToLower()))
            {
                subscription.Buffer.Add(entry);
            }
        }
    }
    
    // 处理 get_game_logs 请求
    private JObject HandleGetGameLogs(JObject parameters)
    {
        int limit = parameters.Value<int?>("limit") ?? 50;
        string type = parameters.Value<string>("type") ?? "all";
        bool includeCollapsed = parameters.Value<bool?>("includeCollapsed") ?? true;
        
        var filteredLogs = type == "all" 
            ? _logBuffer 
            : _logBuffer.Where(l => l.Type == type);
        
        var result = filteredLogs
            .TakeLast(limit)
            .Select(l => new
            {
                id = l.Id,
                type = l.Type,
                message = l.Message,
                timestamp = l.Timestamp.ToString("O"),
                collapseCount = includeCollapsed ? l.CollapseCount : 1,
                stackTrace = l.StackTrace
            })
            .ToList();
        
        return JObject.FromObject(new
        {
            logs = result,
            totalCount = _logBuffer.Count,
            logCount = _logBuffer.Count(l => l.Type == "log"),
            warningCount = _logBuffer.Count(l => l.Type == "warning"),
            exceptionCount = _logBuffer.Count(l => l.Type == "exception")
        });
    }
    
    // 处理 subscribe_logs 请求
    private JObject HandleSubscribeLogs(JObject parameters)
    {
        int bufferSize = parameters.Value<int?>("bufferSize") ?? 100;
        var types = parameters["types"]?.ToObject<string[]>() 
            ?? new[] { "log", "warning", "exception" };
        
        string subscriptionId = $"sub_{Guid.NewGuid():N}";
        
        _subscriptions[subscriptionId] = new LogSubscription
        {
            Id = subscriptionId,
            Types = types.ToHashSet(),
            Buffer = new List<LogEntry>(),
            MaxSize = bufferSize,
            CreatedAt = DateTime.UtcNow
        };
        
        return JObject.FromObject(new
        {
            subscriptionId,
            status = "active",
            bufferSize,
            subscribedTypes = types
        });
    }
    
    // 处理 poll_logs 请求
    private JObject HandlePollLogs(JObject parameters)
    {
        string subscriptionId = parameters.Value<string>("subscriptionId");
        int limit = parameters.Value<int?>("limit") ?? 50;
        long? since = parameters.Value<long?>("since");
        
        if (!_subscriptions.TryGetValue(subscriptionId, out var subscription))
        {
            return JObject.FromObject(new
            {
                error = new { code = "not_found", message = "Subscription not found" }
            });
        }
        
        var logs = since.HasValue
            ? subscription.Buffer.Where(l => l.Timestamp.Ticks > since.Value).ToList()
            : subscription.Buffer.ToList();
        
        // 清空已读取的缓冲区
        subscription.Buffer.Clear();
        
        return JObject.FromObject(new
        {
            logs = logs.Take(limit).Select(l => new
            {
                id = l.Id,
                type = l.Type,
                message = l.Message,
                timestamp = l.Timestamp.ToString("O"),
                collapseCount = l.CollapseCount,
                stackTrace = l.StackTrace
            }),
            hasMore = logs.Count > limit,
            nextPollToken = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
        });
    }
}

// 日志条目结构
public class LogEntry
{
    public int Id { get; set; }
    public string Type { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public DateTime Timestamp { get; set; }
    public int CollapseCount { get; set; }
}

// 日志订阅结构
public class LogSubscription
{
    public string Id { get; set; }
    public HashSet<string> Types { get; set; }
    public List<LogEntry> Buffer { get; set; }
    public int MaxSize { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 5. MCP Server 修改

#### 5.1 `index.ts` 修改

```typescript
// 添加新工具到 ListToolsRequestSchema 处理器
server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: [
    // ... 现有工具 ...
    
    // ===== 运行时状态工具 =====
    {
      name: "Paralives:get_runtime_summary",
      description: "Get comprehensive runtime state: time, funds, mode, selection, and current family. USE WHEN: user asks 'what's happening now', 'current state', or needs quick overview.",
      inputSchema: getRuntimeSummarySchema,
    },
    {
      name: "Paralives:get_game_time",
      description: "Get game time state: pause status, speed, and formatted time. USE WHEN: user asks about time, 'is it paused?', or time speed.",
      inputSchema: getGameTimeSchema,
    },
    {
      name: "Paralives:get_economy",
      description: "Get household funds and economic state. USE WHEN: user asks about money, funds, or finances.",
      inputSchema: getEconomySchema,
    },
    {
      name: "Paralives:get_selection",
      description: "Get currently selected object/character. USE WHEN: user asks 'what did I select?', 'what's selected?'.",
      inputSchema: getSelectionSchema,
    },
    
    // ===== 日志工具 =====
    {
      name: "UnityExplorer:get_game_logs",
      description: "Read game console logs from UIConsole. USE WHEN: user asks about logs, errors, warnings, or 'what went wrong?'.",
      inputSchema: getGameLogsSchema,
    },
    {
      name: "UnityExplorer:subscribe_logs",
      description: "Subscribe to Unity log callback for real-time logs. USE WHEN: user wants to monitor logs continuously.",
      inputSchema: subscribeLogsSchema,
    },
    {
      name: "UnityExplorer:poll_logs",
      description: "Poll subscribed log stream for new entries. USE WHEN: user has active subscription and wants latest logs.",
      inputSchema: pollLogsSchema,
    },
  ],
}));

// 添加工具映射
function toolNameToAction(name: string): string | null {
  switch (name) {
    // ... 现有映射 ...
    
    // 运行时状态工具
    case "Paralives:get_runtime_summary":
      return "paralives_get_runtime_summary";
    case "Paralives:get_game_time":
      return "paralives_get_game_time";
    case "Paralives:get_economy":
      return "paralives_get_economy";
    case "Paralives:get_selection":
      return "paralives_get_selection";
    
    // 日志工具
    case "UnityExplorer:get_game_logs":
      return "get_game_logs";
    case "UnityExplorer:subscribe_logs":
      return "subscribe_logs";
    case "UnityExplorer:poll_logs":
      return "poll_logs";
    
    default:
      return null;
  }
}
```

### 6. 使用示例

#### 6.1 获取运行时状态

```typescript
// 用户问："现在游戏是什么状态？"
const result = await mcp.callTool("Paralives:get_runtime_summary");
// 返回：
{
  "gameState": {
    "mode": "game",
    "isPaused": true,
    "timeSpeed": 0,
    "gameTime": {
      "formatted": "13:39",
      "day": 1
    }
  },
  "economy": {
    "householdFunds": 200292,
    "formatted": "$200,292"
  },
  "selection": {
    "hasSelection": false
  }
}
```

#### 6.2 获取游戏日志

```typescript
// 用户问："最近有什么错误吗？"
const result = await mcp.callTool("UnityExplorer:get_game_logs", {
  limit: 20,
  type: "exception"
});
// 返回：
{
  "logs": [
    {
      "id": 42,
      "type": "exception",
      "message": "NullReferenceException: Object reference not set",
      "timestamp": "2026-05-27T12:05:30Z",
      "collapseCount": 1,
      "stackTrace": "at GameManager.Update() ..."
    }
  ],
  "exceptionCount": 1
}
```

#### 6.3 实时监控日志

```typescript
// 用户问："帮我监控游戏日志"
const sub = await mcp.callTool("UnityExplorer:subscribe_logs", {
  bufferSize: 50,
  types: ["warning", "exception"]
});

// 轮询新日志
const logs = await mcp.callTool("UnityExplorer:poll_logs", {
  subscriptionId: sub.subscriptionId,
  limit: 10
});
```

### 7. 优先级和路线图

#### Phase 1: 运行时状态工具（优先级：高）
- [ ] `Paralives:get_runtime_summary`
- [ ] `Paralives:get_game_time`
- [ ] `Paralives:get_economy`
- [ ] `Paralives:get_selection`

#### Phase 2: UIConsole 日志读取（优先级：中）
- [ ] `UnityExplorer:get_game_logs`

#### Phase 3: Unity 日志回调订阅（优先级：低）
- [ ] `UnityExplorer:subscribe_logs`
- [ ] `UnityExplorer:poll_logs`

### 8. 注意事项

1. **性能考虑**
   - 运行时状态工具应该快速返回，避免阻塞
   - 日志缓冲区大小需要限制，避免内存溢出
   - 轮询间隔需要合理设置

2. **错误处理**
   - 对象不存在时返回空值而非错误
   - 订阅过期时返回明确错误码
   - 提供重试建议

3. **向后兼容**
   - 新工具不影响现有工具
   - 使用新的 action 名称避免冲突

4. **安全性**
   - 日志中可能包含敏感信息
   - 考虑添加日志过滤选项
