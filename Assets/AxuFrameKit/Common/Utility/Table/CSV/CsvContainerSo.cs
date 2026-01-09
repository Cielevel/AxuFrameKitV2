using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;
using AxuFrameKit.Common.Utility.Analysis;

namespace AxuFrameKit.Common.Utility.Table.CSV
{
    [Serializable]
    public abstract class CsvContainerBase : CustomScriptableObject
    {
        public abstract Task Init();

        public abstract void PreloadFromTextAsset(TextAsset textAsset = null, bool forceReParse = false);
        public abstract void PreloadFromStr(string content = null, bool forceReParse = false);
        public abstract bool PreloadFromJson(string jsonContent, bool forceReParse = false);
    }

    /// <summary>
    /// 即开即用的Csv表解析容器，导入CsvAsset后能直接使用解析后的元素集合
    /// </summary>
    public abstract class CsvContainerSo<T> : CsvContainerBase, IEnumerable<T>, IDisposable where T : class, new()
    {
        [field: SerializeField]
        public TextAsset CsvAsset { get; private set; }

        [field: SerializeField]
        public CsvParseOptions ParseOptions { get; private set; }

        private CsvParseResult<T> _parseResult;

        private List<T> TableUnitList
        {
            get
            {
                if (_tableUnitList is { Count: > 0 })
                    return _tableUnitList;

                var csvParser = new CsvParser(ParseOptions);
                _parseResult = csvParser.Parse<T>(CsvAsset.text);
                _tableUnitList = _parseResult.Data;

                return _tableUnitList;
            }
        }

        private List<T> _tableUnitList;

        public int Count => TableUnitList?.Count ?? -1;

        #region Dictionary

        private Dictionary<string, T> _tableUnitStrDictionary;
        private Dictionary<int, T> _tableUnitIntDictionary;
        private Dictionary<long, T> _tableUnitLongDictionary;

        public bool ContainsKey(string key) => _tableUnitStrDictionary.ContainsKey(key);
        public bool ContainsKey(int key) => _tableUnitIntDictionary.ContainsKey(key);
        public bool ContainsKey(long key) => _tableUnitLongDictionary.ContainsKey(key);

        public void SetSelfDictionary(Func<T, string> converter)
        {
            try
            {
                _tableUnitStrDictionary = TableUnitList.ToDictionary(converter);
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"转换失败：存在重复的字符串键值。{ex.Message}");
            }
        }

        public void SetSelfDictionary(Func<T, int> converter)
        {
            try
            {
                _tableUnitIntDictionary = TableUnitList.ToDictionary(converter);
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"转换失败：存在重复的整数键值。{ex.Message}");
            }
        }

        public void SetSelfDictionary(Func<T, long> converter)
        {
            try
            {
                _tableUnitLongDictionary = TableUnitList.ToDictionary(converter);
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"转换失败：存在重复的字符串键值。{ex.Message}");
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
            if (textAsset)
                CsvAsset = textAsset;

            if (forceReParse == false && _tableUnitList != null)
                return;

            _parseResult = new CsvParser(ParseOptions).Parse<T>(CsvAsset.text);
            _tableUnitList = _parseResult.Data;
        }

        /// <summary>
        /// 预加载基础元素集合
        /// </summary>
        public override void PreloadFromStr(string content = null, bool forceReParse = false)
        {
            if (forceReParse == false && _tableUnitList != null)
                return;

            _parseResult = new CsvParser(ParseOptions).Parse<T>(content);
            _tableUnitList = _parseResult.Data;
        }

        /// <summary>
        /// 从json注入数据（注意，将覆盖当前数据）
        /// </summary>
        public override bool PreloadFromJson(string json, bool forceReParse = false)
        {
            if (string.IsNullOrEmpty(json))
            {
                DebugX.LogError("PreloadFromJson: 输入的json字符串为空!");
                return false;
            }

            var setting = new JsonSerializerSettings
            {
                ContractResolver = new CsvColumnContractResolver(),
                Converters = new List<JsonConverter> { new NumberToBooleanConverter() }
            };

            if (forceReParse == false && _tableUnitList != null)
                return true;

            try
            {
                _tableUnitList = JsonConvert.DeserializeObject<List<T>>(json, setting);

                if (_tableUnitList == null || _tableUnitList.Count == 0)
                {
                    DebugX.LogError("解析Json后无任何内容！");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                DebugX.LogError($"PreloadFromJson反序列化失败: {ex.Message}\n{ex.StackTrace}");
                _tableUnitList = new List<T>();

                return false;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= TableUnitList.Count)
                    return null;
                return TableUnitList[index];
            }
        }

        public void Foreach(Action<T> action)
        {
            if (TableUnitList == null)
            {
                DebugX.LogError("TableUnitList is null, please preload data first.");
                return;
            }

            foreach (var item in TableUnitList)
                action?.Invoke(item);
        }

        public T Find(Predicate<T> predicate)
        {
            if (TableUnitList == null)
            {
                DebugX.LogError("TableUnitList is null, please preload data first.");
                return null;
            }

            return TableUnitList.Find(predicate);
        }

        public T Get(int key)
        {
            return _tableUnitIntDictionary?.GetValueOrDefault(key);
        }

        public T Get(string key)
        {
            return _tableUnitStrDictionary?.GetValueOrDefault(key);
        }

        public T Get(long key)
        {
            return _tableUnitLongDictionary?.GetValueOrDefault(key);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return TableUnitList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        ~CsvContainerSo()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            _tableUnitList?.Clear();
            _tableUnitIntDictionary?.Clear();
            _tableUnitStrDictionary?.Clear();
            _tableUnitLongDictionary?.Clear();

            _tableUnitList = null;
            _tableUnitIntDictionary = null;
            _tableUnitStrDictionary = null;
            _tableUnitLongDictionary = null;
        }

        [ContextMenu("TestFromJsonToTable")]
        protected virtual void TestFromJsonToTable(string jsonContent)
        {
            PreloadFromJson(jsonContent, true);
            Debug.Log("List.Count: " + _tableUnitList.Count);
        }
    }
}