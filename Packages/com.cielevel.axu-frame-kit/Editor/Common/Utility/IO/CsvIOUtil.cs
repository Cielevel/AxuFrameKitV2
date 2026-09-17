using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.Utility.IO
{
    public class CsvRowUnit
    {
        public string Name { get; }
        public string AssetPath { get; }
        public string AssetLoadPath { get; set; }
        public string Guid { get; }

        public CsvRowUnit(string name, string assetPath, string guid)
        {
            Name          = name;
            AssetPath     = assetPath;
            AssetLoadPath = assetPath;
            Guid          = guid;
        }
    }
    
    public static class CsvIOUtil
    {
        // 无 BOM 的 UTF-8（与 KitBox CsvEditViewLite 的保存口径一致）
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public static bool WriteCsv(string writePath, string origin, IEnumerable<CsvRowUnit> units)
        {
            var builder = new StringBuilder();

            builder.AppendLine(origin);

            foreach (var unit in units)
            {
                var idx = unit.AssetLoadPath.LastIndexOf("Resources/", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    unit.AssetLoadPath = unit.AssetLoadPath[(idx + "Resources/".Length)..];
                    // 路径中无 '.' 时 LastIndexOf 返回 -1，范围操作会抛异常，此时保留原路径兜底
                    int dotIdx = unit.AssetLoadPath.LastIndexOf('.');
                    if (dotIdx > 0)
                        unit.AssetLoadPath = unit.AssetLoadPath[..dotIdx];
                }
                else
                    unit.AssetLoadPath = Path.GetFileNameWithoutExtension(unit.AssetLoadPath);

                builder.AppendLine($"\"{unit.Name}\",\"{unit.AssetPath}\",\"{unit.AssetLoadPath}\",\"{unit.Guid}\"");
            }

            // 保存 CSV 文件
            try
            {
                File.WriteAllText(writePath, builder.ToString(), Utf8NoBom);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("写入CSV文件失败：");
                Debug.LogException(ex);
                return false;
            }
        }
    }
}