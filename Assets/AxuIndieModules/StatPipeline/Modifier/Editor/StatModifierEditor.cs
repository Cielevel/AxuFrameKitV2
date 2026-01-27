using UnityEditor;
using UnityEngine;
using System.Linq;

namespace AxuIndieModules.StatPipeline.Modifier.Editor
{
    [CustomEditor(typeof(StatModifierBase), true)]
    [CanEditMultipleObjects]
    public class StatModifierEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var modifier = target as StatModifier;
            if (modifier == null) return;

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
