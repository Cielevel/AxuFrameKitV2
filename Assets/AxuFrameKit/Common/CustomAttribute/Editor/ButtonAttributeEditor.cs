using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Common.CustomAttribute.Editor
{
    // This editor draws buttons for any method marked with [ButtonAttribute]
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class ButtonAttributeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector first
            base.OnInspectorGUI();

            // Find all instance methods with ButtonAttribute on the target object
            var targetType = target.GetType();
            var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<InsButtonAttribute>();
                if (attr == null) continue;
                // Only support parameterless methods for simplicity
                if (method.GetParameters().Length > 0) continue;

                string buttonLabel = string.IsNullOrEmpty(attr.Name) ? method.Name : attr.Name;
                if (GUILayout.Button(buttonLabel))
                {
                    // Record undo for safety
                    Undo.RecordObject((UnityEngine.Object)target, $"Invoke {buttonLabel}");
                    method.Invoke(target, null);
                    // Mark target dirty so changes are saved
                    EditorUtility.SetDirty((UnityEngine.Object)target);
                }
            }
        }
    }
}
