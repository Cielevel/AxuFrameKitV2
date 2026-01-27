using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AxuIndieModules.StatPipeline.Stat;

namespace AxuIndieModules.StatPipeline.Modifier
{
    /// <summary>
    /// 通用StatModifier，不再是泛型类。
    /// </summary>
    [CreateAssetMenu(fileName = "StatModifier", menuName = "AxuIndieModules/StatPipeline/StatModifier")]
    public class StatModifier : StatModifierBase
    {
        public string ID => Stat != null ? Stat.ID : string.Empty;

        public List<string> Tags => Stat != null ? Stat.Tags : null;

#if UNITY_EDITOR
        public void CreateStatFloat() => CreateStatSubAsset<StatFloat>("Float");
        public void CreateStatInt() => CreateStatSubAsset<StatInt>("Int");
        public void CreateStatDouble() => CreateStatSubAsset<StatDouble>("Double");
        public void CreateStatLong() => CreateStatSubAsset<StatLong>("Long");

        private void CreateStatSubAsset<T>(string typeSuffix) where T : StatBase
        {
            if (this == null) return;

            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(path)) return;

            // 移除旧资产
            RemoveStatSubAsset();

            var newStat = ScriptableObject.CreateInstance<T>();
            if (newStat != null)
            {
                newStat.name = $"SelfStat-{typeSuffix}";
                UnityEditor.AssetDatabase.AddObjectToAsset(newStat, this);

                Stat = newStat;

                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
            }
        }

        public void RemoveStatSubAsset()
        {
            if (this == null) return;
            if (Stat == null) return;

            string path = UnityEditor.AssetDatabase.GetAssetPath(this);

            // 查找所有子资产，找到它并删除
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset == Stat && asset != this)
                {
                    UnityEditor.AssetDatabase.RemoveObjectFromAsset(asset);
                    DestroyImmediate(asset, true);
                    break;
                }
            }

            Stat = null;

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(path))
                UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
        }
#endif
    }
}