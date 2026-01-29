using System.Collections.Generic;
using UnityEngine;

namespace AxuIndieModules.StatPipeline
{
    /// <summary>
    /// 定义 StatPipeline 的计算阶段顺序
    /// </summary>
    [CreateAssetMenu(fileName = "StatStages", menuName = "AxuIndieModules/StatPipeline/Stages")]
    public class StatStages : ScriptableObject
    {
        [SerializeField]
        private List<string> _stageTags = new List<string> { "Base", "Add", "Final" };

        public IReadOnlyList<string> StageTags => _stageTags;

        public int GetStageIndex(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return -1;
            for (int i = 0; i < _stageTags.Count; i++)
            {
                if (_stageTags[i] == tag) return i;
            }
            return -1;
        }

        public StatStages Clone()
        {
            var stages = ScriptableObject.CreateInstance<StatStages>();
            stages._stageTags = new List<string>(_stageTags);
            return stages;
        }
    }
}
