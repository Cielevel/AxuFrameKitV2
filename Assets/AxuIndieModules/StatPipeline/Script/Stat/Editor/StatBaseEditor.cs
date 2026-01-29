using UnityEditor;
using UnityEngine;

namespace AxuIndieModules.StatPipeline
{
    [CustomEditor(typeof(StatBase), true)]
    [CanEditMultipleObjects]
    public class StatBaseEditor : Editor
    {
        private SerializedProperty _stagesProp;

        private void OnEnable()
        {
            // 对于 [field: SerializeField] 自动属性，需要访问其 backing field
            _stagesProp = serializedObject.FindProperty("<Stages>k__BackingField");
            if (_stagesProp == null)
            {
                _stagesProp = serializedObject.FindProperty("Stages");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var stat = (StatBase)target;

            if (_stagesProp != null && _stagesProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Warning: 'Stages' is not assigned. This Stat will not be able to group modifiers by stage and may result in incorrect calculations if stages are required.", MessageType.Error);
                
                if (GUILayout.Button("Find Default StatStages"))
                {
                    FindAndAssignDefaultStages();
                }
                GUILayout.Space(5);
            }

            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }

        private void FindAndAssignDefaultStages()
        {
            string[] guids = AssetDatabase.FindAssets("t:StatStages");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                StatStages stages = AssetDatabase.LoadAssetAtPath<StatStages>(path);
                
                foreach (var t in targets)
                {
                    var s = (StatBase)t;
                    SerializedObject so = new SerializedObject(s);
                    var p = so.FindProperty("<Stages>k__BackingField");
                    if (p == null) p = so.FindProperty("Stages");
                    
                    if (p != null && p.objectReferenceValue == null)
                    {
                        p.objectReferenceValue = stages;
                        so.ApplyModifiedProperties();
                    }
                }
            }
            else
            {
                Debug.LogWarning("No StatStages asset found in project.");
            }
        }
    }
}
