using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Common.Editor.CustomAttribute
{
    // This editor draws buttons for any method marked with [ButtonAttribute]
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects] // 接管了所有 MonoBehaviour，必须支持多对象编辑
    public class ButtonAttributeEditor : UnityEditor.Editor
    {
        // 反射结果按 Type 全局缓存（域重载后自动清空，无需手动失效）
        private static readonly ConcurrentDictionary<Type, List<(MethodInfo method, string label)>> ButtonMethodsCache =
            new ConcurrentDictionary<Type, List<(MethodInfo method, string label)>>();

        private List<(MethodInfo method, string label)> _buttonMethods;

        private void OnEnable()
        {
            // 首次遇到该类型时计算一次并写入缓存，按钮文案一并缓存以彻底消除逐帧反射
            _buttonMethods = ButtonMethodsCache.GetOrAdd(target.GetType(), CollectButtonMethods);
        }

        /// <summary>
        /// 收集带 [InsButton] 且无参数的实例方法及其按钮文案。
        /// </summary>
        private static List<(MethodInfo, string)> CollectButtonMethods(Type type)
        {
            var result = new List<(MethodInfo, string)>();

            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<InsButtonAttribute>();
                if (attr == null) continue;
                // Only support parameterless methods for simplicity
                if (method.GetParameters().Length > 0) continue;

                string buttonLabel = string.IsNullOrEmpty(attr.Name) ? method.Name : attr.Name;
                result.Add((method, buttonLabel));
            }

            return result;
        }

        public override void OnInspectorGUI()
        {
            // Draw the default inspector first
            DrawDefaultInspector();

            // 没有 InsButton 方法的类型直接默认绘制并返回
            if (_buttonMethods == null || _buttonMethods.Count == 0) return;

            foreach (var (method, buttonLabel) in _buttonMethods)
            {
                if (!GUILayout.Button(buttonLabel)) continue;

                // 多对象编辑：对每个 target 逐个调用
                foreach (var t in targets)
                {
                    // 多选混合派生类型时，跳过与缓存方法声明类型不兼容的对象
                    if (!method.DeclaringType.IsInstanceOfType(t)) continue;

                    // Record undo for safety
                    Undo.RecordObject(t, $"Invoke {buttonLabel}");
                    method.Invoke(t, null);
                    // Mark target dirty so changes are saved
                    EditorUtility.SetDirty(t);
                }
            }
        }
    }
}
