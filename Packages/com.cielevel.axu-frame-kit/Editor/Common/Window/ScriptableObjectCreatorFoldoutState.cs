using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.Window
{
    /// <summary>
    /// ScriptableObjectCreatorWindow 左栏分组 Foldout 折叠状态的持久化工具：
    /// 经 EditorPrefs + JsonUtility 跨开窗保存，键由调用方传入。
    /// 语义为"收起集合"——仅记录被收起的分组路径（路径段间以 "/" 连接，如 "未分组/AxuFrameKit.Runtime"），
    /// 未记录的路径一律视为展开；因此从未操作过的分组天然保持默认展开。
    /// </summary>
    public static class ScriptableObjectCreatorFoldoutState
    {
        // ───────────────────── 数据模型 ─────────────────────

        /// <summary>EditorPrefs 承载的 JSON 数据结构：收起分组路径集合。</summary>
        [Serializable]
        private sealed class Data
        {
            public List<string> Collapsed = new List<string>();
        }

        // ───────────────────── 读写 ─────────────────────

        /// <summary>
        /// 读取折叠状态（收起集合）。无记录（键不存在或内容为空串）返回空集合；
        /// JSON 损坏时回退空集合并告警一次。返回集合归调用方所有，可自由增删。
        /// </summary>
        public static HashSet<string> Load(string prefsKey)
        {
            var result = new HashSet<string>();

            var json = EditorPrefs.GetString(prefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return result;

            try
            {
                var data = JsonUtility.FromJson<Data>(json);
                if (data?.Collapsed == null) return result;

                foreach (var path in data.Collapsed)
                {
                    if (!string.IsNullOrEmpty(path))
                        result.Add(path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ScriptableObjectCreatorFoldoutState] 折叠状态数据损坏，已回退为空集合：{ex.Message}");
            }

            return result;
        }

        /// <summary>写入折叠状态（收起集合）。null 入参按空集合处理（即清空记录、恢复全展开）。</summary>
        public static void Save(string prefsKey, HashSet<string> collapsed)
        {
            var data = new Data();
            if (collapsed != null)
                data.Collapsed.AddRange(collapsed);

            EditorPrefs.SetString(prefsKey, JsonUtility.ToJson(data));
        }
    }
}
