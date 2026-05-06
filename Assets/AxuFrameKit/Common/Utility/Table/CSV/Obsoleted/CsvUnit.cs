using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Table.CSV
{
    /// <summary>
    /// 继承该类，以实现快速从字符串中读取数据生成List
    /// </summary>
    [System.Obsolete("Use CsvContainerSo instead")]
    public abstract class CsvUnit<T> where T : CsvUnit<T>, new()
    {
        protected abstract CsvParseOptions ParseOption { get; }

        protected CsvParseResult<T> _result;

        private CsvParser _parser;

        public List<T> Create(string textContent)
        {
            _parser ??= new CsvParser(ParseOption);

            _result ??= _parser.Parse<T>(textContent);

            return _result.Data;
        }

        public List<T> Create(TextAsset textAsset)
        {
            _parser ??= new CsvParser(ParseOption);

            _result ??= _parser.Parse<T>(textAsset.text);

            return _result.Data;
        }

        public static List<T> CreateList(string textContent)
        {
            CsvParser parser = new();

            return parser.Parse<T>(textContent).Data;
        }

        public static List<T> CreateList(TextAsset textAsset)
        {
            CsvParser parser = new();

            return parser.Parse<T>(textAsset.text).Data;
        }
    }
}