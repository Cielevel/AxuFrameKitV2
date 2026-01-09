# Csv表格工具文档 (Table Csv Utility)

## 1. 简介
本模块位于 `Assets/AxuFrameKit/Common/Utility/Table/CSV`，是一套适用于 Unity 的精准 Csv 解析工具。它提供了从基础文本解析到 ScriptableObject 数据容器的完整支持，能够处理包含复杂格式（如引号、逗号嵌套）的 CSV 数据。

- **`CsvParser`**: 核心解析器，提供了最底层、最灵活的解析功能，支持标准 CSV 格式及多种自定义配置。
- **`CsvTool`**: 一个轻量级的包装类，提供了向后兼容的静态方法，以及简化实例调用的接口。
- **`CsvContainerSo`**: 基于 ScriptableObject 的数据容器，能够将 CSV 文件直接作为资源管理，自动解析为对象列表，适合游戏中配置表的管理。

---

## 2. 工具一：CsvTool (通用调用)

### A. 功能说明
1.  **静态/实例双模式**：
    *   提供 `Parse<T>` 实例方法，支持通过 `CsvParseOptions` 自定义解析规则。
    *   提供 `CsvToList<T>` 静态方法，兼容旧版逻辑（自动跳过首行中文名、使用 `/` 分割数组）。
2.  **简化的 API**：封装了底层的 `CsvParser`，对外提供更直接的接口。

### B. 如何使用

**1. 静态快速调用 (兼容旧版格式)：**
适用于标准的遗留格式表格：
*   第1行：中文备注（会被忽略）
*   第2行：字段名（英文 Key）
*   第3行：类型/Tag（会被忽略）
*   第4行+：数据内容
*   数组分隔符：`/`

```csharp
using AxuFrameKit.Common.Utility.Table.CSV;

// 直接传入 CSV 文本内容
string csvText = myTextAsset.text;
List<MyData> dataList = CsvTool.CsvToList<MyData>(csvText);
```

**2. 实例自定义调用：**
如果需要解析标准 CSV 或自定义格式：
```csharp
var options = new CsvParseOptions
{
    HasHeaderRow = true,     // 第一行是标题
    HasTagsRow = false,      // 没有标签行
    ArraySeparator = '|',    // 数组用 | 分割
    Delimiter = ','          // 使用逗号作为列分隔符
};

var tool = new CsvTool(options);
var resultList = tool.Parse<MyData>(csvContent);
```

---

## 3. 工具二：CsvContainerSo (配置表容器)

### A. 功能说明
1.  **资源化管理**：继承自 `ScriptableObject`，可以直接创建为 Asset 文件。
2.  **自动关联**：在 Inspector 面板拖入 CSV 文件 (`TextAsset`) 即可工作。
3.  **按需解析**：支持预加载或懒加载，解析后的数据会自动缓存。
4.  **字典索引**：除了 List 外，还支持通过 String/Int/Long 类型的 Key 快速查找数据。

### B. 如何使用

**1. 定义数据类：**
使用 `[CsvColumn]` 特性指定 CSV 列名与属性的映射关系。
```csharp
public class ItemData
{
    [CsvColumn("ID")]
    public int Id { get; set; }

    [CsvColumn("Name")]
    public string Name { get; set; }

    [CsvColumn("Price")]
    public float Price { get; set; }
}
```

**2. 定义容器类：**
创建一个继承自 `CsvContainerSo<T>` 的类。
```csharp
[CreateAssetMenu(menuName = "Game/Table/ItemDataContainer")]
public class ItemDataContainer : CsvContainerSo<ItemData>
{
    public override Task Init() => Task.CompletedTask;
}
```

**3. 在 Unity 中使用：**
1.  右键创建 `ItemDataContainer` 的 Asset 文件。
2.  将对应的 CSV 文件拖入 Asset 的 inspector 中。
3.  在代码中引用该 Asset 并获取数据：

```csharp
public ItemDataContainer itemTable; // 在 Inspector 中赋值

void Start()
{
    // 获取列表
    foreach (var item in itemTable)
    {
        Debug.Log($"{item.Name}: {item.Price}");
    }
    
    // 或者使用字典查找（需要先初始化字典，具体见 CsvContainerSo 源码）
    // itemTable.Get(1001); 
}
```

---

## 4. 核心解析器：CsvParser (底层核心)

### A. 功能说明
这是整个模块的核心，负责实际的字符串解析工作。它支持：
*   RFC 4180 标准 CSV 格式（支持引号包裹、换行符等）。
*   自定义分隔符、编码、日期格式。
*   强大的反射映射功能 (`CsvColumn` 特性)。
*   数据校验与错误报告（Result 包含 Errors 和 Warnings）。

### B. 使用场景
一般情况下不需要直接使用 `CsvParser`，建议优先使用 `CsvTool` 或 `CsvContainerSo`。只有在需要极度定制化解析流程，或者需要获取原始的解析错误报告时，才直接操作 `CsvParser`。

---

## 5. 总结

*   **简单数据转换** -> 使用 **`CsvTool.CsvToList<T>`**。
*   **游戏配置表管理** -> 使用 **`CsvContainerSo<T>`**。
*   **需要高度自定义** -> 使用 **`CsvTool` (实例)** 或 **`CsvParser`**。
