using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Common.Utility.Table.CSV
{
    [Serializable]
    public abstract class CsvContainerBase : CustomScriptableObject
    {
        /// <summary>
        /// 异步初始化入口，子类在此执行 Preload、SetSelfDictionary 等初始化逻辑。
        /// 由外部管理器（如 TableManager）在合适时机统一调用，不应在构造期或 OnEnable 中直接触发。
        /// </summary>
        public abstract Task Init();

        public abstract void PreloadFromTextAsset(TextAsset textAsset = null, bool forceReParse = false);
        public abstract void PreloadFromStr(string content = null, bool forceReParse = false);
        public abstract bool PreloadFromJson(string jsonContent, bool forceReParse = false);
    }

    /// <summary>
    /// 即开即用的Csv表解析容器，导入CsvAsset后能直接使用解析后的元素集合。
    /// 通过继承该类，并在子类中指定泛型参数为对应的表元素类型，即可快速实现从Csv文本到元素集合的转换。
    /// 本类支持直接当做集合来使用（实现了IEnumerable&lt;T&gt;接口），也支持通过预设的字典来快速访问元素（需要先调用SetSelfDictionary方法进行设置）。
    /// </summary>
    public abstract class CsvContainerSo<T> : CsvContainerBase, IEnumerable<T>, IDisposable where T : class, new()
    {
        [field: SerializeField]
        public TextAsset CsvAsset { get; private set; }

        [field: SerializeField]
        public CsvParseOptions ParseOptions { get; private set; }

        private List<T> _tableUnitList;

        // _parseResult 仅在 CSV 路径下赋值，Json 路径不产生解析结果对象，保持 null。
        private CsvParseResult<T> _parseResult;

        private bool _disposed;

        private List<T> TableUnitList
        {
            get
            {
                if (_tableUnitList is { Count: > 0 })
                    return _tableUnitList;

                if (CsvAsset == null)
                {
                    Debug.LogError($"[{GetType().Name}] CsvAsset 未赋值，无法执行懒加载解析。请先调用 PreloadFromTextAsset 或在 Inspector 中指定 CsvAsset。");
                    return null;
                }

                var csvParser = new CsvParser(ParseOptions);
                _parseResult  = csvParser.Parse<T>(CsvAsset.text);
                _tableUnitList = _parseResult.Data;

                return _tableUnitList;
            }
        }

        /// <summary>
        /// 当前元素数量。未加载时与 foreach 口径一致，返回 0（空集合），不再返回 -1。
        /// </summary>
        public int Count => TableUnitList?.Count ?? 0;

        #region Dictionary

        private Dictionary<string, T> _tableUnitStrDictionary;
        private Dictionary<int, T>    _tableUnitIntDictionary;
        private Dictionary<long, T>   _tableUnitLongDictionary;

        /// <summary>
        /// 字典未初始化时返回 false，而不是抛出 NullReferenceException。
        /// 请确保在调用前已执行对应的 SetSelfDictionary 重载。
        /// </summary>
        public bool ContainsKey(string key) => _tableUnitStrDictionary?.ContainsKey(key) ?? false;

        /// <inheritdoc cref="ContainsKey(string)"/>
        public bool ContainsKey(int key) => _tableUnitIntDictionary?.ContainsKey(key) ?? false;

        /// <inheritdoc cref="ContainsKey(string)"/>
        public bool ContainsKey(long key) => _tableUnitLongDictionary?.ContainsKey(key) ?? false;

        public void SetSelfDictionary(Func<T, string> converter)
        {
            var list = TableUnitList;
            if (list == null)
            {
                Debug.LogError($"[{GetType().Name}] SetSelfDictionary: 表数据未加载（TableUnitList 为 null），跳过字符串字典构建。");
                return;
            }

            try
            {
                _tableUnitStrDictionary = list.ToDictionary(converter);
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"[{GetType().Name}] 转换失败：存在重复的字符串键值。{ex.Message}");
            }
        }

        public void SetSelfDictionary(Func<T, int> converter)
        {
            var list = TableUnitList;
            if (list == null)
            {
                Debug.LogError($"[{GetType().Name}] SetSelfDictionary: 表数据未加载（TableUnitList 为 null），跳过整数字典构建。");
                return;
            }

            try
            {
                _tableUnitIntDictionary = list.ToDictionary(converter);
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"[{GetType().Name}] 转换失败：存在重复的整数键值。{ex.Message}");
            }
        }

        public void SetSelfDictionary(Func<T, long> converter)
        {
            var list = TableUnitList;
            if (list == null)
            {
                Debug.LogError($"[{GetType().Name}] SetSelfDictionary: 表数据未加载（TableUnitList 为 null），跳过长整型字典构建。");
                return;
            }

            try
            {
                _tableUnitLongDictionary = list.ToDictionary(converter);
            }
            catch (ArgumentException ex)
            {
                // 修正：原为"字符串键值"，实为长整型键值
                Debug.LogError($"[{GetType().Name}] 转换失败：存在重复的长整型键值。{ex.Message}");
            }
        }

        #endregion

        // 待扩展
        private void SetSelfDictionary<TKey>(Func<T, TKey> keySelector) { }

        /// <summary>
        /// 预加载基础元素集合
        /// </summary>
        public override void PreloadFromTextAsset(TextAsset textAsset = null, bool forceReParse = false)
        {
            if (textAsset != null)
                CsvAsset = textAsset;

            if (CsvAsset == null)
            {
                Debug.LogError($"[{GetType().Name}] PreloadFromTextAsset: CsvAsset 为 null，无法解析。");
                return;
            }

            if (forceReParse == false && _tableUnitList != null)
                return;

            _parseResult   = new CsvParser(ParseOptions).Parse<T>(CsvAsset.text);
            _tableUnitList = _parseResult.Data;
        }

        /// <summary>
        /// 预加载基础元素集合
        /// </summary>
        public override void PreloadFromStr(string content = null, bool forceReParse = false)
        {
            if (string.IsNullOrEmpty(content))
            {
                Debug.LogError($"[{GetType().Name}] PreloadFromStr: 输入内容为空，跳过解析。");
                return;
            }

            if (forceReParse == false && _tableUnitList != null)
                return;

            _parseResult   = new CsvParser(ParseOptions).Parse<T>(content);
            _tableUnitList = _parseResult.Data;
        }

        /// <summary>
        /// 从 JSON 注入数据（注意，将覆盖当前数据）。
        /// 此路径不产生 CsvParseResult，_parseResult 保持 null。
        /// </summary>
        public override bool PreloadFromJson(string json, bool forceReParse = false)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[{GetType().Name}] PreloadFromJson: 输入的 JSON 字符串为空！");
                return false;
            }

            if (forceReParse == false && _tableUnitList != null)
                return true;

            var setting = new JsonSerializerSettings
            {
                ContractResolver = new CsvColumnContractResolver(),
                Converters       = new List<JsonConverter> { new NumberToBooleanConverter() }
            };

            try
            {
                _tableUnitList = JsonConvert.DeserializeObject<List<T>>(json, setting);
                _parseResult   = null; // JSON 路径不产生解析结果，显式置空

                if (_tableUnitList == null || _tableUnitList.Count == 0)
                {
                    Debug.LogError($"[{GetType().Name}] PreloadFromJson: 解析 JSON 后无任何内容！");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] PreloadFromJson 反序列化失败: {ex.Message}\n{ex.StackTrace}");
                _tableUnitList = new List<T>();
                return false;
            }
        }

        public T this[int index]
        {
            get
            {
                var list = TableUnitList;
                if (list == null || index < 0 || index >= list.Count)
                    return null;
                return list[index];
            }
        }

        public void Foreach(Action<T> action)
        {
            var list = TableUnitList;
            if (list == null)
            {
                Debug.LogError($"[{GetType().Name}] TableUnitList 为 null，请先完成预加载。");
                return;
            }

            foreach (var item in list)
                action?.Invoke(item);
        }

        public T Find(Predicate<T> predicate)
        {
            var list = TableUnitList;
            if (list == null)
            {
                Debug.LogError($"[{GetType().Name}] TableUnitList 为 null，请先完成预加载。");
                return null;
            }

            return list.Find(predicate);
        }

        public T Get(int key)    => _tableUnitIntDictionary?.GetValueOrDefault(key);
        public T Get(string key) => _tableUnitStrDictionary?.GetValueOrDefault(key);
        public T Get(long key)   => _tableUnitLongDictionary?.GetValueOrDefault(key);

        public IEnumerator<T> GetEnumerator()
        {
            // TableUnitList 可能为 null（CsvAsset 未赋值时），返回空枚举器避免 NRE
            return (TableUnitList ?? Enumerable.Empty<T>()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region IDisposable（标准模式）

        /// <summary>
        /// 手动释放资源，清空已加载的表数据与字典。
        /// ScriptableObject 属托管资源，不挂 finalizer（析构会与 Unity 生命周期冲突）。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <param name="disposing">true = 由 Dispose() 主动调用；false = 由析构函数调用（仅释放非托管资源）。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _tableUnitList?.Clear();
                _tableUnitIntDictionary?.Clear();
                _tableUnitStrDictionary?.Clear();
                _tableUnitLongDictionary?.Clear();

                _tableUnitList           = null;
                _tableUnitIntDictionary  = null;
                _tableUnitStrDictionary  = null;
                _tableUnitLongDictionary = null;

                _parseResult = null;
            }

            _disposed = true;
        }

        #endregion

        [ContextMenu("TestFromJsonToTable")]
        protected virtual void TestFromJsonToTable(string jsonContent)
        {
            PreloadFromJson(jsonContent, true);
            Debug.Log($"[{GetType().Name}] List.Count: " + (_tableUnitList?.Count.ToString() ?? "null"));
        }
    }
}