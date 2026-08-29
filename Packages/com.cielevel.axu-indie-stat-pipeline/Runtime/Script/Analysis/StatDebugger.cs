using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AxuIndieModules.StatPipeline
{
    /// <summary>
    /// StatPipeline 调试辅助类
    /// </summary>
    public static class StatDebugger
    {
        public class LogRecord
        {
            public string StatID;
            public string StatName;
            public double BaseValue;
            public double FinalValue;
            public double RealValue;
            public string FullLog;
            public string Timestamp;
            public List<StageLog> Stages = new List<StageLog>();
            // 记录该次计算中涉及的所有依赖 Stat 的快照
            public Dictionary<string, LogRecord> DependencySnapshots = new Dictionary<string, LogRecord>();
        }

        public class StageLog
        {
            public string Name;
            public List<ModifierLog> Modifiers = new List<ModifierLog>();
        }

        public class ModifierLog
        {
            public string Name;
            public string ID;
            public string Type;
            public string Calculator;
            public string StatInfo;
            public int Weight;
            public double PrevValue;
            public double NextValue;
            public string Color;
            // 标记此 Modifier 是否有关联的 Stat 需要下钻
            public string SourceStatID;
        }

        private static List<LogRecord> _logs = new List<LogRecord>();
        public static IReadOnlyList<LogRecord> Logs => _logs;
        public static System.Action OnLogAdded;

        public static void ClearLogs()
        {
            _logs.Clear();
            OnLogAdded?.Invoke();
        }

        public static void LogCalculatePipeline(StatBase stat)
        {
            if (stat == null) return;
            var record = CaptureRecursive(stat, new HashSet<string>());
            if (record != null)
            {
                _logs.Add(record);
                if (_logs.Count > 100) _logs.RemoveAt(0);
                OnLogAdded?.Invoke();
            }
        }

        public static void LogCalculatePipeline<T>(Stat<T> stat) where T : struct
        {
            LogCalculatePipeline((StatBase)stat);
        }

        private static LogRecord CaptureRecursive(StatBase stat, HashSet<string> visited)
        {
            if (stat == null || visited.Contains(stat.ID)) return null;
            visited.Add(stat.ID);

            LogRecord record = new LogRecord
            {
                StatID = stat.ID,
                StatName = stat.name,
                Timestamp = System.DateTime.Now.ToString("HH:mm:ss"),
            };

            // 通过反射获取 BaseValue（因为 StatBase 没有定义 BaseValue）
            var baseValueProp = stat.GetType().GetProperty("BaseValue");
            if (baseValueProp != null)
                record.BaseValue = System.Convert.ToDouble(baseValueProp.GetValue(stat));

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<color=#5fb3b3>[Stat Debugger]</color> Pipeline Trace for <b>{stat.ID}</b>");

            double currentValue = record.BaseValue;
            sb.AppendLine($"  <color=#fac863>Initial Base Value:</color> <b>{currentValue}</b>");

            if (stat.Stages == null)
            {
                StageLog defaultStage = new StageLog { Name = "Default" };
                ProcessModifierList(stat.Modifiers, ref currentValue, sb, defaultStage, visited, record);
                record.Stages.Add(defaultStage);
            }
            else
            {
                var tags = stat.Stages.StageTags;
                var stageGroups = new List<StatModifierBase>[tags.Count];
                for (int i = 0; i < tags.Count; i++) stageGroups[i] = new List<StatModifierBase>();

                foreach (var mod in stat.Modifiers)
                {
                    if (mod == null) continue;
                    int index = stat.Stages.GetStageIndex(mod.StageTag);
                    if (index >= 0) stageGroups[index].Add(mod);
                }

                for (int i = 0; i < tags.Count; i++)
                {
                    var group = stageGroups[i];
                    group.Sort((a, b) => a.Weight.CompareTo(b.Weight));
                    StageLog stageLog = new StageLog { Name = tags[i] };
                    ProcessModifierList(group, ref currentValue, sb, stageLog, visited, record, 4);
                    record.Stages.Add(stageLog);
                }
            }

            record.FinalValue = currentValue;
            record.RealValue = stat.Value;
            record.FullLog = sb.ToString();

            return record;
        }

        private static void ProcessModifierList(List<StatModifierBase> mods, ref double currentValue, StringBuilder sb, StageLog stageLog, HashSet<string> visited, LogRecord parentRecord, int indent = 2)
        {
            string space = new string(' ', indent);
            foreach (var mod in mods)
            {
                double prevValue = currentValue;
                currentValue = mod.Apply(currentValue);

                string modType = mod.GetType().Name;
                string calcInfo = GetCalculatorInfo(mod);
                string statInfo = mod.Stat != null ? $"{mod.Stat.ID}({mod.Stat.Value})" : "null";
                string color = GetModColor(mod);

                sb.AppendLine($"{space}- [<color=#{color}>{modType}</color>] <b>{mod.name}</b>");

                var modLog = new ModifierLog
                {
                    Name = mod.name,
                    ID = mod.ID,
                    Type = modType,
                    Calculator = calcInfo,
                    StatInfo = statInfo,
                    Weight = mod.Weight,
                    PrevValue = prevValue,
                    NextValue = currentValue,
                    Color = color
                };

                // 如果 Modifier 有关联的 Stat，且该 Stat 有 Modifier，递归记录它的计算过程
                if (mod.Stat != null && mod.Stat.Modifiers != null && mod.Stat.Modifiers.Count > 0)
                {
                    modLog.SourceStatID = mod.Stat.ID;
                    if (!parentRecord.DependencySnapshots.ContainsKey(mod.Stat.ID))
                    {
                        var childRecord = CaptureRecursive(mod.Stat, visited);
                        if (childRecord != null)
                            parentRecord.DependencySnapshots[mod.Stat.ID] = childRecord;
                    }
                }

                stageLog.Modifiers.Add(modLog);
            }
        }

        private static string GetCalculatorInfo(StatModifierBase mod)
        {
            if (mod is StatModifier m)
            {
                return m.Calculator != null ? m.Calculator.GetType().Name : "None";
            }
            if (mod is StatModifierLink l)
            {
                string calcName = l.LinkCalculator != null ? l.LinkCalculator.GetType().Name : "None";
                return $"{calcName} (Coeff: {l.Coefficient})";
            }
            return "Unknown";
        }

        private static string GetModColor(StatModifierBase mod)
        {
            if (mod is StatModifier) return "c594c5"; // Purple
            if (mod is StatModifierLink) return "6699cc"; // Blue
            return "ffffff";
        }
    }
}
