# Extensions

此文件夹包含基于纯 C# 的通用扩展方法，**不包含任何 Unity 引擎相关代码**。

## 目录

- [CollectionExtension](#collectionextension)
- [EnumExtension](#enumextension)
- [StringExtension](#stringextension)
- [ReflectionExtension](#reflectionextension)
- [TypeExtension](#typeextension)

---

## CollectionExtension

为 `IEnumerable<T>`, `ICollection<T>`, `IList<T>` 提供集合操作扩展。

### 常用方法
- **判空检查**
  - `IsNullOrEmpty()`: 检查集合是否为 null 或空。
  - `IsNotNullOrEmpty()`: 检查集合是否不为 null 且包含元素。
- **随机获取**
  - `GetRandom()`: 随机获取一个元素。
  - `GetRandom(T exclude)`: 随机获取一个元素，排除指定值。
  - `GetRandomUniqueItems(int n)`: 获取 n 个不重复的随机元素。
- **修改集合**
  - `AddRange(IEnumerable<T>)`: 为 `ICollection<T>` 添加多个元素。
  - `Shuffle()`: 使用 Fisher-Yates 算法对 `IList<T>` 进行就地洗牌。
  - `Chunk(int size)`: 将集合按指定大小分块。
- **操作**
  - `Do(Action<T>)`: 对集合中每个元素执行操作（链式调用辅助）。
  - `RemoveWhere(Predicate<T>)`: 移除满足条件的元素并返回移除数量。

---

## EnumExtension

为 `Enum` 类型提供扩展。

### 常用方法
- **元数据**
  - `GetDescription()`: 获取枚举字段上的 `[Description]` 特性文本。
  - `GetName()`: 获取枚举名称。
- **逻辑判断**
  - `HasFlagUnsafe(T flag)`: 泛型位标志检查（避免高版本 .NET 之前的装箱问题）。
  - `IsDefined()`: 判断值是否在枚举定义中。
- **工具**
  - `GetRandomValue<T>()`: 获取枚举类型的随机一个值。
  - `GetAllValues<T>()`: 获取枚举的所有值数组。

---

## StringExtension

为 `string` 类型提供扩展。

### 常用方法
- **格式化与转换**
  - `Format(params object[] args)`: `string.Format` 的语法糖。
  - `ToEnum<T>()`: 将字符串转换为枚举。
  - `ToSnakeCase()`: 驼峰转下划线命名。
  - `Truncate(int maxLength)`: 截断字符串并添加后缀。
- **检查**
  - `IsNullOrEmpty()` / `IsNotNullOrEmpty()`
  - `IsNullOrWhiteSpace()`
- **处理**
  - `Repeat(int count)`: 重复字符串。
  - `RemoveWhitespace()`: 移除所有空白字符。
  - `MakeSureFilePath()`: 统一路径分隔符。

---

## ReflectionExtension

反射相关辅助方法，简化反射调用。

### 常用方法
- **值获取**
  - `GetPropertyValue(string name)`: 获取属性值。
  - `GetFieldValue(string name)`: 获取字段值。
- **类型关系**
  - `InheritsFrom(Type baseType)`: 判断是否继承自某类型。
  - `InheritsFromGeneric(Type genericDef)`: 判断是否继承自某泛型定义。
  - `IsInstanceOf(Type type)`: 判断对象是否是某类型实例。
- **特性**
  - `HasAttribute<T>()`: 检查是否拥有某特性。
  - `GetAttribute<T>()`: 获取特性实例。

---

## TypeExtension

为 `Type` 提供扩展。

### 常用方法
- `GetDefaultValue()`: 获取类型的默认值（等同于 `default(T)`）。
- `ImplementsInterface(Type interfaceType)`: 检查是否实现了某接口（支持泛型接口定义）。
- `IsNumeric()`: 判断是否为数字类型。
