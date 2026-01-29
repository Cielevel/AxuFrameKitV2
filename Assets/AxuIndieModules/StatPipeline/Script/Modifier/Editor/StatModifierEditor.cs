using UnityEditor;
using UnityEngine;

namespace AxuIndieModules.StatPipeline
{
    [CustomEditor(typeof(StatModifierBase), true)]
    [CanEditMultipleObjects]
    public class StatModifierEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawStageSelection();

            base.OnInspectorGUI();

            var modifier = target as StatModifier;
            if (modifier != null)
            {
                DrawStatSubAssetManagement(modifier);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStageSelection()
        {
            var baseModifier = target as StatModifierBase;
            if (baseModifier == null || baseModifier.Owner == null) return;

            var stages = baseModifier.Owner.Stages;
            if (stages == null || stages.StageTags == null || stages.StageTags.Count == 0) return;

            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Quick Stage Selection (from Owner)", EditorStyles.boldLabel);
                GUILayout.Space(5);

                var stageTagProp = serializedObject.FindProperty("<StageTag>k__BackingField");
                if (stageTagProp == null)
                    stageTagProp = serializedObject.FindProperty("StageTag");

                if (stageTagProp != null)
                {
                    int columnCount = 3;
                    var tags = stages.StageTags;
                    for (int i = 0; i < tags.Count; i += columnCount)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            for (int j = 0; j < columnCount && (i + j) < tags.Count; j++)
                            {
                                string tag = tags[i + j];
                                bool isSelected = !stageTagProp.hasMultipleDifferentValues && stageTagProp.stringValue == tag;

                                if (isSelected) GUI.backgroundColor = new Color(0.1f, 0.8f, 0.4f);
                                if (GUILayout.Button(tag, GUILayout.Height(25)))
                                {
                                    stageTagProp.stringValue = tag;
                                    serializedObject.ApplyModifiedProperties(); // 立即应用修改，防止被 base.OnInspectorGUI 覆盖
                                    GUI.FocusControl(null); // 清除焦点，确保 UI 刷新
                                }
                                GUI.backgroundColor = Color.white;
                            }
                        }
                    }
                }
                GUILayout.Space(5);
            }
            GUILayout.Space(10);
        }

        private void DrawStatSubAssetManagement(StatModifier modifier)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Stat Sub-Asset Management", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Float", GUILayout.Height(25)))
                {
                    ExecuteOnSelected(m => m.CreateStatFloat());
                }
                if (GUILayout.Button("Add Int", GUILayout.Height(25)))
                {
                    ExecuteOnSelected(m => m.CreateStatInt());
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Double", GUILayout.Height(25)))
                {
                    ExecuteOnSelected(m => m.CreateStatDouble());
                }
                if (GUILayout.Button("Add Long", GUILayout.Height(25)))
                {
                    ExecuteOnSelected(m => m.CreateStatLong());
                }
            }

            GUILayout.Space(5);
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f); // 浅红色
            if (GUILayout.Button("Remove / Clear Stat Sub-Asset", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm Remove", "Are you sure you want to delete the sub-asset and clear the Stat reference?", "Yes", "No"))
                {
                    ExecuteOnSelected(m => m.RemoveStatSubAsset());
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void ExecuteOnSelected(System.Action<StatModifier> action)
        {
            foreach (var t in targets)
            {
                if (t is StatModifier m)
                {
                    action(m);
                }
            }
        }
    }
}
