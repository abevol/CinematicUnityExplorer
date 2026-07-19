using System.Collections.Generic;

using UnityExplorer.Config;



namespace UnityExplorer.Localization

{

    public static class Localizer

    {

        private static readonly Dictionary<string, string> zhCN = new()

        {

            // Panel Names

            { "PANEL_OBJECT_EXPLORER", "物体浏览器" },

            { "PANEL_INSPECTOR", "检查器" },

            { "PANEL_CS_CONSOLE", "C# 控制台" },

            { "PANEL_HOOK_MANAGER", "Hook 管理器" },

            { "PANEL_FREECAM", "自由相机" },

            { "PANEL_CLIPBOARD", "剪贴板" },

            { "PANEL_LOG", "日志" },

            { "PANEL_OPTIONS", "设置" },

            { "PANEL_PARALIVES", "Paralives 控制台" },

            { "PANEL_MCP", "MCP 桥接" },
            { "PANEL_UI_INSPECTOR_RESULTS", "UI 检查器结果" },
            { "PANEL_ANIMATOR", "动画器" },
            { "PANEL_CAM_PATHS", "相机路径" },
            { "PANEL_LIGHTS_MANAGER", "灯光管理器" },
            { "PANEL_POST_PROCESSING", "后期处理" },
            { "PANEL_MISC", "杂项" },




            // Tabs
            { "TAB_SCENE_EXPLORER", "场景浏览器" },

            { "TAB_OBJECT_SEARCH", "物体搜索" },



            // Common Buttons / Labels

            { "BTN_SEARCH", "搜索" },

            { "BTN_CLEAR", "清除" },

            { "BTN_RESET", "重置" },

            { "BTN_RUN", "运行" },

            { "BTN_SAVE", "保存" },

            { "BTN_CLOSE", "关闭" },

            { "LBL_ACTIVE", "活跃" },

            { "LBL_INACTIVE", "非活跃" },

            { "LBL_ALL", "全部" },

            { "LBL_ENABLED", "已启用" },

            { "LBL_DISABLED", "已禁用" },

            { "LBL_NOT_SET", "未设置" },

            { "LBL_WORLD", "游戏世界" },

            { "LBL_UI", "用户界面" },



            // Scene Explorer Specific

            { "LBL_SCENE_LABEL", "场景:" },

            { "LBL_SEARCH_PLACEHOLDER", "搜索并按回车..." },

            { "BTN_UPDATE", "更新" },

            { "LBL_AUTO_UPDATE_SEC", "自动更新 (1 秒)" },

            { "LBL_NAME", "名称" },

            { "LBL_SIBLING_INDEX", "兄弟节点索引" },

            { "LBL_SCENE_LOADER", "场景加载器" },

            { "LBL_FILTER_SCENES_PLACEHOLDER", "过滤场景名称..." },

            { "BTN_LOAD_SINGLE", "加载 (单场景)" },

            { "BTN_LOAD_ADDITIVE", "加载 (叠加)" },

            { "LBL_UNTITLED", "<无标题>" },

            { "LBL_SELECT_SCENE", "[选择场景]" },



            // Object Search Specific

            { "LBL_SEARCHING_FOR", "搜索目标:" },

            { "LBL_CLASS_FILTER", "类型过滤:" },

            { "LBL_CHILD_FILTER", "子级过滤:" },

            { "LBL_SCENE_FILTER", "场景过滤:" },

            { "LBL_NAME_CONTAINS", "名称包含:" },

            { "LBL_RESULTS_COUNT", "共 {0} 个结果" },
            { "STATUS_SEARCHING", "正在搜索..." },
            { "STATUS_SEARCH_EMPTY", "没有找到匹配当前过滤器的 {0} 结果。" },
            { "STATUS_SEARCH_RESULTS", "{0}: {1} 个结果" },
            { "STATUS_SEARCH_FAILED", "搜索失败: {0}" },
            { "STATUS_SEARCH_MODE", "模式: {0}" },
            { "CONTEXT_UNITYOBJECT", "Unity 对象" },

            { "CONTEXT_GAMEOBJECT", "GameObject" },

            { "CONTEXT_COMPONENT", "Component" },
            { "CONTEXT_SINGLETON", "单例 (Singleton)" },

            { "CONTEXT_CLASS", "类型 (Class)" },

            { "CONTEXT_STATICCLASS", "静态类 (Static Class)" },
            { "FILTER_ANY", "任意" },
            { "FILTER_ROOTOBJECT", "仅根级" },
            { "FILTER_HASPARENT", "仅子级" },
            { "FILTER_ACTIVELYLOADED", "已加载场景" },
            { "FILTER_HIDEANDDONTSAVE", "HideAndDontSave" },
            { "FILTER_ROOTONLY", "仅根级" },

            { "FILTER_CHILDONLY", "仅子级" },

            { "FILTER_ACTIVE", "仅活跃" },

            { "FILTER_DONTDESTROYONLOAD", "DontDestroyOnLoad" },

            { "FILTER_ASSET", "资源文件/内存预制件" },



            // Time Scale Widget Specific

            { "LBL_TIME_SCALE", "时间倍率:" },

            { "BTN_LOCK", "锁定" },

            { "BTN_UNLOCK", "解锁" },



            // FreeCam Specific

            { "BTN_FREECAM", "自由相机" },

            { "BTN_END_FREECAM", "关闭自由相机" },

            { "BTN_BEGIN_FREECAM", "开启自由相机" },

            { "LBL_USE_GAME_CAMERA", "使用游戏相机?" },

            { "LBL_FREECAM_POS", "相机位置:" },

            { "TXT_FREECAM_POS_PLACEHOLDER", "例如: 0 0 0" },

            { "LBL_MOVE_SPEED", "移动速度:" },

            { "TXT_MOVE_SPEED_PLACEHOLDER", "默认: 1" },

            { "BTN_INSPECT_FREECAM", "检查自由相机对象" },

            { "TXT_FREECAM_INSTRUCTIONS", @"操作说明:

- WASD / 方向键: 移动

- 空格 / PgUp: 向上移动

- 左Ctrl / PgDown: 向下移动

- 鼠标右键: 观察视角

- Shift键: 极限加速" },



            // Hooks Specific

            { "LBL_CURRENT_HOOKS", "当前 Hook 列表" },

            { "BTN_ON", "开启" },

            { "BTN_OFF", "关闭" },

            { "BTN_EDIT", "编辑" },

            { "BTN_ADD_HOOK", "挂钩" },

            { "LBL_ADDING_HOOKS_TO", "正在添加 Hook 至: {0}" },

            { "LBL_EDITING_HOOK", "正在编辑: {0}" },

            { "TXT_HOOK_CLASS_PLACEHOLDER", "输入要添加 Hook 的类名..." },

            { "BTN_VIEW_METHODS", "查看方法" },

            { "LBL_CHOOSE_CLASS_BEGIN", "选择一个类以开始..." },

            { "TXT_FILTER_METHODS_PLACEHOLDER", "过滤方法名称..." },

            { "TXT_HOOK_EDITOR_INSTRUCTIONS", @"* 接受的 patch 方法名为 <b>Prefix</b>（前置）、<b>Postfix</b>（后置）、<b>Finalizer</b>（终结器）和 <b>Transpiler</b>（编译器，可定义多个）。

* 你的 patch 方法必须是静态（static）方法。

* Hook 是临时的！若要永久保留修改，请将源码复制到你的 IDE 外部保存！" },

            { "BTN_SAVE_AND_RETURN", "保存并返回" },

            { "BTN_CANCEL_AND_RETURN", "取消并返回" },



            // CS Console Specific

            { "BTN_COMPILE", "编译" },

            { "LBL_HELP", "帮助" },

            { "LBL_COMPILE_CTRL_R", "按 Ctrl+R 编译" },

            { "LBL_SUGGESTIONS", "代码建议" },

            { "LBL_AUTO_INDENT", "自动缩进" },
            { "STATUS_CONSOLE_COMPILE_REQUESTED", "已请求编译。" },
            { "STATUS_CONSOLE_RESET_REQUESTED", "已请求重置控制台。" },
            { "STATUS_CONSOLE_READY", "模式: REPL / 脚本 / 类输入。可使用按钮或启用 Ctrl+R 后编译。" },
            { "CS_STARTUP_TEXT", @"<color=#5d8556>// 欢迎使用 UnityExplorer C# 控制台！

//

// 建议在使用此工具时开启日志面板（或游戏控制台日志窗口）。

// 使用右侧的帮助下拉菜单，可以查看控制台的详细使用示例。

//

// 如需在启动时自动执行脚本，请将脚本放置在 'sinai-dev-UnityExplorer\Scripts\startup.cs'</color>" },

            { "HELP_OPT_HELP", "帮助 (Help)" },

            { "HELP_OPT_USINGS", "命名空间引用 (Usings)" },

            { "HELP_OPT_REPL", "即时编译 (REPL)" },

            { "HELP_OPT_CLASSES", "自定义类 (Classes)" },

            { "HELP_OPT_COROUTINES", "协程 (Coroutines)" },

            { "HELP_VAL_USINGS", @"// 你可以导入任何命名空间，但需要对其进行编译使其生效。

// 导入的引用在重置控制台前将一直有效。

using UnityEngine.UI;



// 查看当前的导入，可以使用 ""GetUsing();"" 助手方法。

// 注意：导入语句和 REPL 代码无法在同一次编译中执行。" },

            { "HELP_VAL_REPL", @"/* REPL (即时编译运行循环) 能够直接执行 C# 表达式和代码段。

 * REPL 代码中不能包含 using 导入语句或定义新 class。

 * 最后一行的返回值会自动打印 to 日志面板中。

 * 定义的临时变量会持续存在，直到你重置控制台。

 */



// 比如：以下代码将首先输出 ""Hello, World!""，然后打印 6 作为返回值。

Log(""Hello, world!"");

var x = 5;

++x;



/* REPL 模式下内置了以下辅助工具：

 * CurrentTarget;     - System.Object，当前检查器面板正在查看的对象

 * AllTargets;        - System.Object[]，所有检查器面板中正在查看的对象列表

 * Log(obj);          - 将一条信息打印到控制台日志中

 * Inspect(obj);      - 使用检查器打开并分析该对象

 * Inspect(someType); - 使用静态反射检查一个类类型

 * Start(enumerator); - 开始运行协程，并返回 Coroutine 实例

 * Stop(coroutine);   - 停止以 Start() 方式启动的协程

 * Copy(obj);         - 复制对象至 UnityExplorer 剪贴板

 * Paste();           - 粘贴剪贴板中的当前内容

 * GetUsing();        - 打印当前已生效的 using 导入指令

 * GetVars();         - 打印当前定义的 REPL 变量及其当前值

 * GetClasses();      - 打印已定义的自定义类列表及结构

 * help;              - 打开控制台默认的命令行帮助，提供更多小工具

 */" },

            { "HELP_VAL_CLASSES", @"// 自定义编译的类会持续驻留在内存中，直到游戏关闭。

// 再次编译同名的类可以完成热覆写（注意：老版本在内存中仍会被系统保留）。

//

// 编译成功的类支持在此控制台内部和游戏外部被自由调用。

// 注意：在 IL2CPP 环境下，你必须配置 Namespace 命名空间以便 ClassInjector 注入，否则会引发崩溃。



public class HelloWorld

{

    public static void Main()

    {

        UnityExplorer.ExplorerCore.Log(""Hello, world!"");

    }

}



// 在 REPL 中，你可以直接调用上面的测试类方法：""HelloWorld.Main();""

// 注意：编译器不允许你在定义新类的同时，执行普通的 REPL 独立逻辑代码。

//

// 在 REPL 中，使用 ""GetClasses();"" 可以查看上次重置后所定义的所有类。" },

            { "HELP_VAL_COROUTINES", @"// 要启动协程，可以在 REPL 模式中直接执行 ""Start(SomeCoroutine());""。

//

// 在定义协程时，首先要先编译它的容器类，例如：

public class MyCoro

{

    public static IEnumerator Main()

    {

        yield return null;

        UnityExplorer.ExplorerCore.Log(""Hello, world after one frame!"");

    }

}

// 随后在 REPL 模式中，使用 ""Start(MyCoro.Main());"" 运行它。" },



            // Clipboard Specific

            { "LBL_CURRENT_PASTE", "当前剪贴内容:" },

            { "BTN_CLEAR_CLIPBOARD", "清空剪贴板" },

            { "BTN_INSPECT", "检查" },

            { "BTN_CLOSE_ALL", "全部关闭" },

            { "MOUSE_INSPECT", "鼠标检查" },

            { "MSG_COPIED", "已复制！" },

            { "MSG_PASTED", "已粘贴！" },

            { "MSG_CANNOT_ASSIGN", "无法将类型“{0}”赋值给“{1}”！" },

            { "MSG_CANNOT_INSPECT_NULL", "无法检查 null 或已销毁的对象！" },
            { "STATUS_INSPECTOR_READY", "检查器就绪。" },
            { "STATUS_INSPECTOR_TABS", "{0} 个检查器标签页。" },


            // GameObject Inspector Buttons & Labels

            { "LBL_CHILDREN", "子物体" },

            { "LBL_ENTER_NAME", "输入物体名称..." },

            { "BTN_ADD_CHILD", "创建子物体" },

            { "LBL_COMPONENTS", "组件列表" },

            { "LBL_ENTER_COMP_TYPE", "输入组件类型..." },

            { "BTN_ADD_COMP", "挂载组件" },

            { "BTN_VIEW_PARENT", "◄ 查看父物体" },

            { "LBL_NO_PARENT", "无父级" },

            { "BTN_COPY_TO_CLIPBOARD", "复制到剪贴板" },

            { "LBL_ACTIVE_SELF", "自身激活 (ActiveSelf)" },

            { "LBL_IS_STATIC", "静态 (IsStatic)" },

            { "LBL_INSTANCE_ID", "实例 ID:" },

            { "LBL_TAG", "标签 (Tag):" },

            { "BTN_INSTANTIATE", "克隆 (Instantiate)" },

            { "BTN_DESTROY", "销毁 (Destroy)" },

            { "BTN_SHOW_IN_EXPLORER", "在浏览器中定位" },

            { "LBL_SCENE", "所属场景:" },

            { "LBL_LAYER", "渲染层级 (Layer):" },

            { "LBL_FLAGS", "隐藏标志 (Flags):" },

            { "LBL_NONE_ASSET_RESOURCE", "无 (资源文件/内存预制件)" },



            // Transform Controls Specific

            { "LBL_POSITION", "世界坐标 (Position):" },

            { "LBL_LOCAL_POSITION", "本地坐标 (Local Position):" },

            { "LBL_ROTATION", "旋转角度 (Rotation):" },

            { "LBL_SCALE", "缩放大小 (Scale):" },



            // CacheObject Cell Operations

            { "BTN_APPLY", "应用" },

            { "BTN_COPY", "复制" },

            { "BTN_PASTE", "粘贴" },

            { "LBL_VALUE_HERE", "值显示在此处" },



            // Reflection Inspector Specific

            { "BTN_CONSTRUCT_GENERIC", "构建泛型" },

            { "BTN_VIEW_IN_DNSPY", "在 dnSpy 中查看" },

            { "LBL_FILTER_NAMES", "过滤名称:" },

            { "BTN_UPDATE_DISPLAYED", "更新当前显示值" },

            { "LBL_AUTO_UPDATE", "自动更新" },

            { "LBL_SCOPE", "范围:" },

            { "MEMBER_TYPE_PROPERTY", "属性" },

            { "MEMBER_TYPE_FIELD", "字段" },

            { "MEMBER_TYPE_METHOD", "方法" },

            { "MEMBER_TYPE_CONSTRUCTOR", "构造函数" },

            { "LBL_ASSEMBLY", "<color=grey>程序集:</color> {0}" },

            { "MSG_SET_DNSPY_PATH", "请在 UnityExplorer 设置中配置有效的 dnSpy 路径。" },

            { "SCOPE_ALL", "全部" },

            { "SCOPE_INSTANCE", "实例" },

            { "SCOPE_STATIC", "静态" },



            // Evaluate Widget Specific

            { "LBL_GENERIC_ARGUMENTS", "泛型参数" },

            { "LBL_ARGUMENTS", "方法参数" },

            { "BTN_EVALUATE", "求值/调用" },



            // AutoCompleteModal Specific

            { "LBL_AUTOCOMPLETE_HELP", "↑/↓ 键选择，Enter 键使用，Esc 键关闭" },
            { "BTN_OPEN_LOG_FILE", "打开日志文件" },
            { "LBL_LOG_UNITY_DEBUG", "记录 Unity 调试日志" },
            { "STATUS_LOG_READY", "{0} 条日志。" },
            { "STATUS_LOG_CLEARED", "日志已清除。" },
            { "STATUS_LOG_FILE", "日志文件: {0}" },
            { "BTN_SAVE_OPTIONS", "保存设置" },
            { "BTN_RESET_DEFAULTS", "重置为默认值" },
            { "TXT_SEARCH_OPTIONS", "搜索设置..." },
            { "CATEGORY_ALL", "全部" },
            { "CATEGORY_GENERAL", "通用" },
            { "CATEGORY_UI", "界面" },
            { "CATEGORY_MCP", "MCP" },
            { "CATEGORY_PARALIVES", "Paralives" },
            { "CATEGORY_CONSOLE", "控制台" },
            { "CATEGORY_INSPECTOR", "检查器" },
            { "CATEGORY_EXPORT", "导出" },
            { "CATEGORY_ADVANCED", "高级" },
            { "BADGE_RESTART", "需重启" },
            { "BADGE_ADVANCED", "高级" },
            { "STATUS_OPTIONS_SAVED", "设置已保存。" },
            { "STATUS_OPTIONS_RESET", "当前显示的设置已重置为默认值。" },
            { "STATUS_OPTIONS_COUNT", "{0} 个设置项" },
            { "STATUS_OPTIONS_FILTERED", "显示 {0} 个设置项。" },
            { "STATUS_MCP_LISTENING", "正在监听 ws://127.0.0.1:{0}" },
            { "STATUS_MCP_RESTART_REQUIRED", "已启用但未监听，可能需要重启。" },
            { "STATUS_MCP_DISABLED", "MCP 桥接已禁用。" },
            { "LBL_MCP_BRIDGE", "桥接状态" },
            { "LBL_MCP_LISTENING", "监听中" },
            { "LBL_MCP_PORT", "端口" },
            { "LBL_MCP_TIMEOUT", "超时" },
            { "LBL_MCP_LAST_ACTION", "上次动作" },
            { "LBL_MCP_LAST_ERROR", "上次错误" },
            { "LBL_MCP_LAST_DURATION", "上次耗时" },
            { "LBL_MCP_TOOL_GROUPS", "工具分组" },
            { "LBL_MCP_RECENT_REQUESTS", "最近请求" },
            { "TXT_MCP_PARALIVES_TOOLS", "仅在 Paralives 中可用；写入工具保留 dry-run/确认策略。" },
            { "TXT_MCP_NO_REQUESTS", "尚无 MCP 请求记录。" },


            // Cinematic / Animation Panels
            { "BTN_MASTER_MESH_TOGGLE", "主网格开关" },
            { "BTN_MASTER_ANIMATOR_TOGGLE", "主动画器开关" },
            { "BTN_RESET_ANIMATORS", "重置动画器" },
            { "BTN_REFRESH_ANIMATORS", "刷新动画器" },
            { "BTN_CLEAR_PATH", "清除路径" },
            { "LBL_CLOSED_LOOP", "循环闭合路径" },
            { "LBL_VISUALIZE_PATH", "可视化路径" },
            { "LBL_UNPAUSE_ON_PLAY", "播放时取消暂停" },
            { "LBL_PAUSE_ON_FINISH", "完成后暂停" },
            { "LBL_WAIT_BEFORE_PLAY", "启动前等待 3 秒" },
            { "TXT_FILE_NAME", "文件名" },
            { "BTN_SAVE_PATH", "保存路径" },
            { "BTN_LOAD_PATH", "加载路径" },
            { "LBL_LOAD_PATH_ON_CAM", "从当前相机状态加载路径" },
            { "BTN_CREATE_SPOTLIGHT", "创建聚光灯" },
            { "BTN_CREATE_POINTLIGHT", "创建点光源" },
            { "LBL_VANILLA_GAME_LIGHTS", "游戏原生灯光" },
            { "BTN_REFRESH_EFFECTS", "刷新效果" },
            { "LBL_OPEN_IN_INSPECTOR", "在检查器中打开对象" },
            { "LBL_TOGGLE_HUD", "开关 HUD" },
            { "LBL_HIGH_LODS", "高 LOD" },
            { "BTN_TAKE_SCREENSHOT", "截图" },
            { "LBL_ALL_MESHES_SHADOW", "让所有网格投射/接收阴影" },
            { "LBL_ALL_LIGHTS_SHADOW", "让所有游戏光源投射阴影" },
            { "LBL_HIGH_RES_SHADOWS", "高分辨率阴影" },

            // FreeCam Panel
            { "LBL_CAMERA_MODE", "相机模式:" },
            { "LBL_TARGET_CAM", "目标相机:" },
            { "LBL_DISABLE_COMPONENTS", "禁用组件/GameObject:" },
            { "LBL_CONTROLLER", "控制器:" },
            { "BTN_FOLLOW_GAMEOBJECT", "跟随 GameObject" },
            { "BTN_LOOK_AT_GAMEOBJECT", "看向 GameObject" },
            { "BTN_RELEASE_FOLLOW_LOOK", "解除跟随/看向" },
            { "BTN_TELEPORT_SELECTED", "传送到所选对象" },
            { "LBL_BLOCK_FREECAM_MOVEMENT", "锁定自由相机移动" },
            { "LBL_BLOCK_GAMES_INPUT", "锁定游戏输入" },
            { "LBL_FOLLOW_ROTATION", "跟随对象旋转" },
            { "LBL_NOT_FOLLOWING", "未跟随/看向任何对象。" },

            // Inspector Widgets
            { "BTN_MOVE_TO_CAMERA", "移动到相机" },
            { "BTN_FOLLOW_WITH_FREECAM", "自由相机跟随对象" },
            { "BTN_LOOK_AT_WITH_FREECAM", "自由相机看向对象" },
            { "BTN_INSPECT_GAMEOBJECT", "检查 GameObject" },
            { "LBL_UNIFORM_SCALE", "等比缩放:" },

            // Config settings name and description

            { "Language", "语言设置 (Language)" },

            { "The language used by UnityExplorer. Requires restart to fully take effect.", "UnityExplorer 界面所使用的语言。修改后需要重启游戏才能完全生效。" },

            { "UnityExplorer Toggle", "UnityExplorer 开关" },

            { "The key to enable or disable UnityExplorer's menu and features.", "开启或关闭 UnityExplorer 的菜单和功能的快捷键。" },

            { "Hide On Startup", "启动时隐藏" },

            { "Should UnityExplorer be hidden on startup?", "UnityExplorer 是否在游戏启动时自动隐藏？" },

            { "MCP Bridge Enabled", "启用 MCP 桥接" },

            { "Expose a local WebSocket bridge for Model Context Protocol tooling.", "为 Model Context Protocol 工具开放本地 WebSocket 桥接。" },

            { "MCP Bridge Port", "MCP 桥接端口" },

            { "The localhost WebSocket port used by the UnityExplorer MCP bridge.", "UnityExplorer MCP 桥接使用的本机 WebSocket 端口。" },

            { "MCP Bridge Request Timeout Ms", "MCP 请求超时毫秒数" },

            { "How long the bridge waits for Unity main-thread MCP commands before timing out.", "桥接等待 Unity 主线程 MCP 命令完成的超时时间。" },

            { "Paralives Safe Action Mode", "Paralives 安全操作模式" },

            { "Controls whether game-side Paralives UI actions require a second click confirmation. MCP writes still require dryRun false and the confirmation phrase.", "控制游戏内 Paralives UI 操作是否需要二次点击确认。MCP 写入仍需要 dryRun=false 和确认短语。" },

            { "Paralives Saved Game List Limit", "Paralives 存档列表上限" },

            { "Maximum saved games to display in the Paralives panel.", "Paralives 面板中最多显示的存档数量。" },

            { "Paralives Loading Wait Timeout Ms", "Paralives 加载等待超时毫秒数" },

            { "Maximum time to wait for Paralives loading actions before treating them as timed out.", "Paralives 加载操作被视为超时前的最长等待时间。" },

            { "Paralives Prefer UI Flow For Save Load", "Paralives 存读档优先使用 UI 流程" },

            { "Prefer visible Paralives UI flows for save loading when available; fallback methods are shown before execution.", "可用时优先使用可见的 Paralives UI 流程加载存档；执行前会显示备用方法。" },

            { "Startup Delay Time", "启动延迟秒数" },
            { "The delay on startup before the UI is created.", "游戏加载后，UI 创建前的延迟秒数。" },

            { "Target Display", "目标显示器" },

            { "The monitor index for UnityExplorer to use, if you have multiple. 0 is the default display, 1 is secondary, etc. Restart recommended when changing this setting. Make sure your extra monitors are the same resolution as your primary monitor.", "多显示器时 UnityExplorer 渲染的屏幕索引。0 为主显示器，1 为第二显示器等。更改此设置建议重启。并确保多显示器的分辨率一致。" },

            { "Force Unlock Mouse", "强制解锁鼠标" },

            { "Force the Cursor to be unlocked (visible) when the UnityExplorer menu is open.", "当 UnityExplorer 菜单打开时，强制显示并解锁鼠标光标。" },

            { "Force Unlock Toggle Key", "强行解锁快捷键" },

            { "The keybind to toggle the 'Force Unlock Mouse' setting. Only usable when UnityExplorer is open.", "切换“强制解锁鼠标”设置的快捷键。仅在 UnityExplorer 打开时可用。" },

            { "Disable EventSystem override", "禁用事件系统覆盖" },

            { "If enabled, UnityExplorer will not override the EventSystem from the game.\n<b>May require restart to take effect.</b>", "如果启用，UnityExplorer 将不会覆盖游戏自身的 EventSystem。<b>可能需要重启才能生效。</b>" },

            { "Disable Setup Force Reload ManagedAssemblies", "禁用启动时强制重载 ManagedAssemblies" },

            { "If enabled, UnityExplorer will not reload ManagedAssemblies on setup. Currently only Mono is supported.\n<b>May require restart to take effect.</b>", "如果启用，UnityExplorer 在初始化时不会重载 ManagedAssemblies。目前仅支持 Mono。<b>可能需要重启才能生效。</b>" },

            { "Bypass UniverseLib ICall", "绕过 UniverseLib ICall" },

            { "If enabled, UnityExplorer will bypass UniverseLib's ICall reflection system. This may help with compatibility in some games.\n<b>May require restart to take effect.</b>", "如果启用，UnityExplorer 将绕过 UniverseLib 的 ICall 反射系统。这可能改善部分游戏的兼容性。<b>可能需要重启才能生效。</b>" },

            { "Default Output Path", "默认输出路径" },
            { "The default output path when exporting things from UnityExplorer.", "从 UnityExplorer 导出文件时的默认路径。" },

            { "dnSpy Path", "dnSpy.exe 路径" },

            { "The full path to dnSpy.exe (64-bit).", "dnSpy.exe (64位) 软件的完整物理路径。" },

            { "Main Navbar Anchor", "主导航栏锚点" },

            { "The vertical anchor of the main UnityExplorer Navbar, in case you want to move it.", "UnityExplorer 主导航栏的垂直对齐方向（可以贴靠在屏幕顶部或底部）。" },

            { "Log Unity Debug", "记录 Unity 调试日志" },

            { "Should UnityEngine.Debug.Log messages be printed to UnityExplorer's log?", "是否将 UnityEngine.Debug.Log 的常规输出也打印到 UnityExplorer 日志面板中？" },

            { "Log To Disk", "保存日志到磁盘" },

            { "Should UnityExplorer save log files to the disk?", "UnityExplorer 是否将日志文件保存到磁盘？" },

            { "World Mouse-Inspect Keybind", "世界模式鼠标检查快捷键" },
            { "Optional keybind to being a World-mode Mouse Inspect.", "用于开始游戏世界模式下鼠标悬停检查的快捷键。" },

            { "UI Mouse-Inspect Keybind", "UI 模式鼠标检查快捷键" },

            { "Optional keybind to begin a UI-mode Mouse Inspect.", "用于开始 UI 模式下鼠标悬停检查的快捷键。" },

            { "TimeScale Toggle Keybind", "时间倍率锁定切换快捷键" },

            { "Optional keybind to lock or unlock Time.timeScale.", "用于锁定或解锁 Time.timeScale 的可选快捷键。" },

            { "TimeScale Zero Keybind", "时间倍率归零快捷键" },

            { "Optional keybind to lock Time.timeScale to 0.0.", "用于将 Time.timeScale 锁定为 0.0 的可选快捷键。" },

            { "TimeScale Normal Keybind", "时间倍率恢复正常快捷键" },

            { "Optional keybind to lock Time.timeScale to 1.0.", "用于将 Time.timeScale 锁定为 1.0 的可选快捷键。" },

            { "TimeScale Half Keybind", "时间倍率减半快捷键" },

            { "Optional keybind to halve and lock the current Time.timeScale target.", "用于将当前 Time.timeScale 目标减半并锁定的可选快捷键。" },

            { "TimeScale Double Keybind", "时间倍率加倍快捷键" },

            { "Optional keybind to double and lock the current Time.timeScale target.", "用于将当前 Time.timeScale 目标加倍并锁定的可选快捷键。" },

            { "CSharp Console Assembly Blacklist", "C# 控制台引用黑名单" },
            { "Use this to blacklist Assembly names from being referenced by the C# Console. Requires a Reset of the C# Console.\nSeparate each Assembly with a semicolon ';'.For example, to blacklist Assembly-CSharp, you would add 'Assembly-CSharp;'", "阻止 C# 控制台引用的程序集名称黑名单。更改后需要重置 C# 控制台。使用分号“;”分隔各个程序集名。" },

            { "Member Signature Blacklist", "类成员签名黑名单" },

            { "Use this to blacklist certain member signatures if they are known to cause a crash or other issues.\r\nSeperate signatures with a semicolon ';'.\r\nFor example, to blacklist Camera.main, you would add 'UnityEngine.Camera.main;'", "过滤掉某些可能会引起崩溃的成员签名。使用分号“;”分隔。例如：'UnityEngine.Camera.main;'" },

            { "Hide NativeMethodInfoPtr_s and NativeFieldInfoPtr_s", "隐藏 Native 成员指针" },

            { "Use this to blacklist NativeMethodPtr_s and NativeFieldInfoPtrs_s from the class inspector, mainly to reduce clutter.\r\nFor example, this will hide 'Class.NativeFieldInfoPtr_value' for the field 'Class.value'.", "在类检查器中隐藏 NativeMethodInfoPtr_s 与 NativeFieldInfoPtr_s 指针以减少混乱。" },

            { "CinematicUnityExplorer Toggle", "CinematicUnityExplorer 开关" },
            { "The key to enable or disable CinematicUnityExplorer's menu and features.", "开启或关闭 CinematicUnityExplorer 菜单和功能的快捷键。" },
            { "Should CinematicUnityExplorer be hidden on startup?", "CinematicUnityExplorer 是否在游戏启动时自动隐藏？" },
            { "MCP Bridge Max Requests Per Frame", "MCP 桥接每帧最大请求数" },
            { "Maximum MCP bridge requests to execute on the Unity main thread in one frame.", "MCP 桥接每帧在 Unity 主线程上执行的最大请求数。" },
            { "MCP Bridge Max Frame Budget Ms", "MCP 桥接每帧预算毫秒数" },
            { "Maximum MCP bridge main-thread time budget per frame. A single request is allowed to finish even if it exceeds this budget.", "MCP 桥接每帧主线程时间预算（毫秒）。单个请求允许超出此预算直至完成。" },
            { "The monitor index for CinematicUnityExplorer to use, if you have multiple. 0 is the default display, 1 is secondary, etc. Restart recommended when changing this setting. Make sure your extra monitors are the same resolution as your primary monitor.", "多显示器时 CinematicUnityExplorer 渲染的屏幕索引。0 为主显示器，1 为第二显示器等。更改此设置建议重启。并确保多显示器的分辨率一致。" },
            { "Force the Cursor to be unlocked (visible) when the CinematicUnityExplorer menu is open.", "当 CinematicUnityExplorer 菜单打开时，强制显示并解锁鼠标光标。" },
            { "The keybind to toggle the 'Force Unlock Mouse' setting. Only usable when CinematicUnityExplorer is open.", "切换“强制解锁鼠标”设置的快捷键。仅在 CinematicUnityExplorer 打开时可用。" },
            { "If enabled, CinematicUnityExplorer will not override the EventSystem from the game.\n<b>May require restart to take effect.</b>", "如果启用，CinematicUnityExplorer 将不会覆盖游戏自身的 EventSystem。<b>可能需要重启才能生效。</b>" },
            { "If enabled, CinematicUnityExplorer will bypass UniverseLib's ICall reflection system. This may help with compatibility in some games.\n<b>May require restart to take effect.</b>", "如果启用，CinematicUnityExplorer 将绕过 UniverseLib 的 ICall 反射系统。这可能改善部分游戏的兼容性。<b>可能需要重启才能生效。</b>" },
            { "The default output path when exporting things from CinematicUnityExplorer.", "从 CinematicUnityExplorer 导出文件时的默认路径。" },
            { "The vertical anchor of the main CinematicUnityExplorer Navbar, in case you want to move it.", "CinematicUnityExplorer 主导航栏的垂直对齐方向（可以贴靠在屏幕顶部或底部）。" },
            { "Should UnityEngine.Debug.Log messages be printed to CinematicUnityExplorer's log?", "是否将 UnityEngine.Debug.Log 的常规输出也打印到 CinematicUnityExplorer 日志面板中？" },
            { "Should CinematicUnityExplorer save log files to the disk?", "CinematicUnityExplorer 是否将日志文件保存到磁盘？" },
            { "Make the mod UI automatically scale with resolution", "让模组 UI 随分辨率自动缩放" },
            { "Especially useful when running games in high resolutions and you are having a hard time reading the mods menu (requires restart).", "在运行高分辨率游戏时尤其有用，如果模组菜单文字难以阅读（需要重启）。" },
            { "Reset Camera transform on freecam disable", "关闭自由相机时重置相机变换" },
            { "Reset the camera position and rotation between freecam sessions, so the freecam always starts from the gameplay position and rotation.", "在自由相机会话之间重置位置和旋转，使自由相机始终从游戏视角位置开始。" },
            { "Visualizers arrows size", "可视化箭头大小" },
            { "Cam Paths nodes and Lights Manager lights visualizers' arrow size (must be positive) (needs visualizer toggled to reflect changes).", "相机路径节点和灯光管理器可视化器的箭头大小（必须为正数，需要切换可视化器以反映更改）。" },
            { "Freecam Camera Target Selection", "自由相机目标选择" },
            { "Enables certain advanced settings on the Freecam panel, in case the user can't get the freecam to work properly (requires game reset).", "启用自由相机面板上的某些高级设置，适用于自由相机无法正常工作的情况（需要重启游戏）。" },
            { "Pause", "暂停" },
            { "Toggle the pause of the game.", "切换游戏暂停。" },
            { "Frameskip", "帧跳过" },
            { "Skip a frame when the game is paused.", "游戏暂停时跳过一个帧。" },
            { "Take a screenshot", "截图" },
            { "Takes a screenshot with the size multiplier specified in the Misc panel.\nSaves the screenshot to 'CinematicUnityExplorer\\Screenshots' in png format.", "使用杂项面板中指定的尺寸倍率截图。以 png 格式保存到 'CinematicUnityExplorer\\Screenshots'。" },
            { "HUD Toggle", "HUD 开关" },
            { "Toggle the games HUD. If there are elements that are still visible try loading the 'Load HUD elements' button on the Misc panel, and toggle the HUD again.", "切换游戏 HUD。如果仍有元素可见，请尝试在杂项面板上点击'加载 HUD 元素'按钮，然后重新切换 HUD。" },
            { "Freecam", "自由相机" },
            { "Toggles freecamera mode.", "切换自由相机模式。" },
            { "Toggle block Freecam movement", "切换锁定自由相机移动" },
            { "Blocks the freecam from moving when pressing the freecam-related hotkeys.", "按自由相机相关快捷键时阻止自由相机移动。" },
            { "Toggle block games input", "切换锁定游戏输入" },
            { "Blocks the games input when the the freecam is on.\nIf you don't see a 'Block games input on Freecam' checkbox on the FreecCam panel then it's not supported for this game.", "自由相机开启时阻止游戏输入。\n如果在自由相机面板上看不到'锁定游戏输入'复选框，则表示此游戏不支持该功能。" },
            { "Speed up movement", "加速移动" },
            { "Maintain this key pressed while moving the camera around to increase the freecam movement speed.", "按住此键移动相机时可以提高自由相机移动速度。" },
            { "Speed down movement", "减速移动" },
            { "Maintain this key pressed while moving the camera around to decrease the freecam movement speed.", "按住此键移动相机时可以降低自由相机移动速度。" },
            { "Forwards 1", "前进 1" },
            { "Move the freecam forwards.", "向前移动自由相机。" },
            { "Forwards 2", "前进 2" },
            { "Move the freecam forwards, alt key.", "向前移动自由相机（备用键）。" },
            { "Backwards 1", "后退 1" },
            { "Move the freecam backward.", "向后移动自由相机。" },
            { "Backwards 2", "后退 2" },
            { "Move the freecam backward, alt key.", "向后移动自由相机（备用键）。" },
            { "Left 1", "左移 1" },
            { "Move the freecam to the left.", "向左移动自由相机。" },
            { "Left 2", "左移 2" },
            { "Move the freecam to the left, alt key.", "向左移动自由相机（备用键）。" },
            { "Right 1", "右移 1" },
            { "Move the freecam to the right.", "向右移动自由相机。" },
            { "Right 2", "右移 2" },
            { "Move the freecam to the right, alt key.", "向右移动自由相机（备用键）。" },
            { "Up", "上移" },
            { "Move the freecam upwards.", "向上移动自由相机。" },
            { "Down", "下移" },
            { "Move the freecam down.", "向下移动自由相机。" },
            { "Tilt left", "向左倾斜" },
            { "Tilt the camera to the left.", "将相机向左倾斜。" },
            { "Tilt right", "向右倾斜" },
            { "Tilt reset", "重置倾斜" },
            { "Resets the tilt the camera.", "重置相机倾斜。" },
            { "Increase FOV", "增大视野" },
            { "Increase the field of view of the current freecam.", "增大当前自由相机的视野。" },
            { "Decrease FOV", "减小视野" },
            { "Decrease the field of view of the current freecam.", "减小当前自由相机的视野。" },
            { "Reset FOV", "重置视野" },
            { "Resets the field of view of the current freecam to the original one.", "将当前自由相机的视野重置为原始值。" },
            { "Toggle NPC animations", "切换 NPC 动画" },
            { "Toggle NPC animations as selected in the Animator panel.", "切换在动画器面板中选择的 NPC 动画。" },
            { "Default Freecam mode", "默认自由相机模式" },
            { "Default type of freecam selected on startup (gets automatically updated with the last type of camera used).", "启动时默认选择的自由相机类型（会自动更新为上次使用的相机类型）。" },
            { "Custom components to disable", "自定义禁用的组件" },
            { "List of custom components to disable when enabling the freecam (gets automatically updated when editing it from the freecam panel).", "启用自由相机时禁用的自定义组件列表（从自由相机面板编辑时会自动更新）。" },
            { "Preferred Target Camera", "首选目标相机" },
            { "The camera that will be targeted by the freecam methods.\nOnly used when Freecam Camera Target Selection is enabled.", "自由相机方法将使用的目标相机。\n仅在启用自由相机目标选择时使用。" },

        };



        public static string Get(string key, string defaultEnglish)

        {

            if (ConfigManager.LanguageSetting != null && 

                ConfigManager.LanguageSetting.Value == ConfigManager.Language.Chinese)

            {

                if (zhCN.TryGetValue(key, out string value))

                {

                    return value;

                }

            }

            return defaultEnglish;

        }

    }

}

