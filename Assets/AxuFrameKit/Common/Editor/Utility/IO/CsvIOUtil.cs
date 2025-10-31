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
                    unit.AssetLoadPath = unit.AssetLoadPath[..unit.AssetLoadPath.LastIndexOf('.')];
                }
                else
                    unit.AssetLoadPath = Path.GetFileNameWithoutExtension(unit.AssetLoadPath);

                builder.AppendLine($"\"{unit.Name}\",\"{unit.AssetPath}\",\"{unit.AssetLoadPath}\",\"{unit.Guid}\"");
            }
            
            // 保存 CSV 文件
            try
            {
                File.WriteAllText(writePath, builder.ToString(), Encoding.UTF8);
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