using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Common.CustomAttribute.Editor
{
    [CustomEditor(typeof(CustomMonoBehaviour), true)]
    public class CustomMonoBehaviourAttributeEditor : UnityEditor.Editor
    {
        // Cached attribute data
        private InspectorTitleAttribute _titleAttr;
        private InspectorIntroAttribute _introAttr;

        // Cached button methods and labels, sorted by InsButtonAttribute.Order
        private List<(MethodInfo method, string label, bool recordUndo)> _buttonEntries;

        // GUIStyles are lazily initialized on first draw to ensure EditorStyles is ready
        private GUIStyle _titleStyle;
        private GUIStyle _wordWrappedLabel;

        private void OnEnable()
        {
            var type = target.GetType();

            _titleAttr = (InspectorTitleAttribute)Attribute.GetCustomAttribute(type, typeof(InspectorTitleAttribute), inherit: true);
            _introAttr = (InspectorIntroAttribute)Attribute.GetCustomAttribute(type, typeof(InspectorIntroAttribute), inherit: true);

            CacheButtonMethods(type);
        }

        private void CacheButtonMethods(Type type)
        {
            _buttonEntries = new List<(MethodInfo, string, bool)>();

            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<InsButtonAttribute>();
                if (attr == null) continue;
                if (method.GetParameters().Length > 0) continue;

                string label = string.IsNullOrEmpty(attr.Name) ? method.Name : attr.Name;
                _buttonEntries.Add((method, label, attr.RecordUndo));
            }

            // Sort by Order field to ensure consistent and controllable button ordering
            _buttonEntries.Sort((a, b) =>
            {
                var attrA = a.method.GetCustomAttribute<InsButtonAttribute>();
                var attrB = b.method.GetCustomAttribute<InsButtonAttribute>();
                return attrA.Order.CompareTo(attrB.Order);
            });
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();

            if (_titleAttr != null)
            {
                // Rebuild title style only if font size changed (edge case: rarely needed in practice)
                if (_titleStyle.fontSize != _titleAttr.FontSize)
                    _titleStyle.fontSize = _titleAttr.FontSize;

                GUILayout.Label(_titleAttr.Title, _titleStyle);
                DrawSeparator(2f);
            }

            if (_introAttr != null)
            {
                EditorGUILayout.LabelField(_introAttr.Description, _wordWrappedLabel);
                DrawSeparator(1f);
            }

            DrawDefaultInspector();

            foreach (var (method, label, recordUndo) in _buttonEntries)
            {
                if (!GUILayout.Button(label)) continue;

                if (recordUndo)
                    Undo.RecordObject(target, $"Invoke {label}");

                method.Invoke(target, null);

                if (recordUndo)
                    EditorUtility.SetDirty(target);
            }
        }

        /// <summary>
        /// Lazily initializes GUIStyles.
        /// Must be called inside OnInspectorGUI, not OnEnable,
        /// because EditorStyles is not guaranteed to be ready before the first GUI pass.
        /// </summary>
        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = _titleAttr?.FontSize ?? 14,
                alignment = TextAnchor.MiddleCenter
            };

            _wordWrappedLabel = new GUIStyle(EditorStyles.wordWrappedLabel);
        }

        private static void DrawSeparator(float height)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawRect(rect, Color.gray);
        }
    }
}