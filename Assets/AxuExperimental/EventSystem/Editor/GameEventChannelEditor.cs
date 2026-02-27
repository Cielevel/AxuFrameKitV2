#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AxuExperimental.EventSystem.Editor
{
    [CustomEditor(typeof(GameEventChannelBase), true)]
    public class GameEventChannelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // Provide a Raise button and show UnityEvent invocation count if possible
            var so = serializedObject;
            so.ApplyModifiedProperties();

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Event Tools", EditorStyles.boldLabel);

            var targetObj = target as GameEventChannelBase;
            if (targetObj == null) return;

            // Try to reflect to find Raise method
            var raiseMethod = targetObj.GetType().GetMethod("Raise", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (raiseMethod != null)
            {
                if (GUILayout.Button("Raise (default)"))
                {
                    // Try to construct a default instance for parameter (only works for structs with default ctor)
                    var parameters = raiseMethod.GetParameters();
                    if (parameters.Length == 1)
                    {
                        var    paramType    = parameters[0].ParameterType;
                        object defaultValue = null;
                        if (paramType.IsValueType)
                            defaultValue = Activator.CreateInstance(paramType);
                        // else null for reference types

                        raiseMethod.Invoke(targetObj, new[] { defaultValue });
                    }
                }
            }

            if (GUILayout.Button("Ping Asset"))
            {
                EditorGUIUtility.PingObject(targetObj);
            }
        }
    }
}
#endif