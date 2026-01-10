using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit._Core.Extension;
using AxuFrameKit.Common.Utility.Analysis;

namespace AxuFrameKit.Common.Utility.Table.CSV
{
    public class CsvTool
    {
        private readonly CsvParser _parser;
        private readonly CsvParseOptions _options;

        /// <summary>
        /// 创建一个新的CsvTool实例
        /// </summary>
        /// <param name="options">解析配置，如果为null则使用默认配置</param>
        public CsvTool(CsvParseOptions options = null)
        {
            _options = options ?? new CsvParseOptions();
            _parser = new CsvParser(_options);
        }

        /// <summary>
        /// 使用当前实例配置解析CSV内容
        /// </summary>
        public List<T> Parse<T>(string csvContent) where T : new()
        {
            var result = _parser.Parse<T>(csvContent);

            if (result.Errors != null && result.Errors.Count > 0)
            {
                foreach (var error in result.Errors)
                {
                    Debugger.LogError($"CsvTool Parse Error: {error}");
                }
            }

            return result.Data;
        }

        #region Static Methods (Legacy Support)

        /// <summary>
        /// 从csv文本中读取出对应序列化类的集合；
        /// 使用默认的Legacy配置（跳过第一行中文名，'/'数组分隔符）
        /// </summary>
        public static List<T> CsvToList<T>(string table) where T : new()
        {
            if (table.IsNullOrEmpty())
            {
                Debugger.LogWarning("获取到的table文本是空!");
                return null;
            }

            var options = new CsvParseOptions
            {
                HasHeaderRow = true,
                HasTagsRow = false,
                ArraySeparator = '/',
                TrimFields = true
            };

            return new CsvTool(options).Parse<T>(table);
        }

        #endregion
    }
}