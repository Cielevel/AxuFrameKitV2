# InGame Console

一个高性能的游戏内Console系统，使用UI Toolkit构建，提供与Unity Editor Console一致的功能。

## 功能特性

- ✅ **日志捕获**: 自动捕获所有Unity日志类型 (Log, Warning, Error, Exception)
- ✅ **过滤功能**: 按类型过滤日志 (信息/警告/错误)
- ✅ **搜索功能**: 实时搜索日志内容
- ✅ **折叠模式**: 合并重复的日志条目并显示数量
- ✅ **堆栈追踪**: 查看完整的错误堆栈信息
- ✅ **虚拟化滚动**: 支持高性能的大量日志显示
- ✅ **可拖动调整**: 支持拖动调整Console面板高度
- ✅ **热键切换**: 使用热键快速切换显示/隐藏

## 快速开始

### 1. 添加Manager到场景

在场景中创建一个空GameObject，添加 `InGameConsoleManager` 组件：

```csharp
// 方式1: 在Inspector中添加
// 创建空GameObject -> Add Component -> InGameConsoleManager

// 方式2: 代码创建
var consoleGO = new GameObject("[InGameConsole]");
consoleGO.AddComponent<InGameConsoleManager>();
```

### 2. 配置选项

在Inspector中可以配置以下选项：

| 属性 | 描述 | 默认值 |
|------|------|--------|
| Show On Start | 游戏启动时显示Console | false |
| Toggle Key | 切换显示/隐藏的按键 | BackQuote (`) |
| Max Log Count | 最大日志条目数（用于性能优化） | 1000 |
| Capture Unity Logs | 是否捕获Unity日志 | true |
| Capture Stack Trace | 是否捕获异常堆栈 | true |
| Initial Height Ratio | Console面板的初始高度比例 | 0.4 |

### 3. 运行时控制

```csharp
using AxuFrameKit.Common.Utility.Analysis.InGameConsole;

// 显示Console
InGameConsoleManager.Instance.ShowConsole();

// 隐藏Console
InGameConsoleManager.Instance.HideConsole();

// 切换显示/隐藏
InGameConsoleManager.Instance.ToggleConsole();

// 清除日志
InGameConsoleManager.Instance.ClearLogs();

// 检查是否可见
bool isVisible = InGameConsoleManager.Instance.IsVisible;
```

## UI操作说明

### 工具栏

- **Clear**: 清除所有日志
- **Collapse**: 切换折叠模式（合并重复日志）
- **搜索框**: 输入关键词实时过滤日志
- **ℹ️ 按钮**: 切换显示/隐藏普通日志
- **⚠️ 按钮**: 切换显示/隐藏警告日志
- **✕ 按钮**: 切换显示/隐藏错误日志
- **关闭按钮**: 隐藏Console

### 日志列表

- 单击日志条目查看详情和堆栈追踪
- 不同颜色区分日志类型：
  - 白色: 普通日志
  - 黄色: 警告
  - 红色: 错误/异常

### 调整大小

- 拖动Console顶部的拖动条可以调整面板高度

## 性能优化

此Console采用以下优化策略：

1. **虚拟化列表**: 只渲染可见的日志条目
2. **容量限制**: 自动移除最旧的日志，防止内存无限增长
3. **高效数据结构**: 使用Dictionary优化折叠模式的查询
4. **内联样式**: 减少样式表加载开销

## 注意事项

1. **仅在开发/调试版本使用**: 建议在Release版本中禁用或移除Console
2. **日志上限**: 超过最大日志数量时，旧日志将被自动移除
3. **单例模式**: 场景中只能有一个 `InGameConsoleManager` 实例

## 文件结构

```
InGameConsole/
├── InGameConsoleManager.cs  # Manager单例组件
├── InGameConsole.cs         # 核心逻辑类
├── InGameConsoleUI.cs       # UI Toolkit界面
├── LogEntry.cs              # 日志数据结构
├── LogEntryPool.cs          # UI元素对象池
├── Resources/
│   └── InGameConsole.uss    # 样式表 (可选)
└── README.md                # 本文档
```
