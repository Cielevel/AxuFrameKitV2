using System.Collections.Generic;
using UnityEngine;

namespace AxuIndieModules.StatPipeline
{
    /// <summary>
    /// 通用StatModifier
    /// </summary>
    [CreateAssetMenu(fileName = "StatModifier", menuName = "AxuIndieModules/StatPipeline/StatModifier")]
    public class StatModifier : StatModifierBase
    {
        [SerializeReference]
        public StatModifierCalculator Calculator;

        public List<string> Tags => Stat != null ? Stat.Tags : null;

        public override double Apply(double value) => Calculator.Calculate(value, Stat.Value);

        public override StatModifierBase Clone()
        {
            var modifier = ScriptableObject.CreateInstance<StatModifier>();
            modifier.Index = Index;
            modifier.Count = Count;
            modifier.Weight = Weight;
            modifier.StageTag = StageTag;
            modifier.Duration = Duration;
            modifier.Active = Active;
            modifier.CountType = CountType;
            modifier.LifeType = LifeType;
            modifier.CombineParamType = CombineParamType;
            modifier.CombineCountType = CombineCountType;
            modifier.ConditionType = ConditionType;
            modifier.Stat = Stat;
            modifier.Calculator = Calculator;
            return modifier;
        }

        // ----- Modifier的独立方法 -----

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
                newStat.name = $"{name}-Stat-{typeSuffix}";
                newStat.ID = name;
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