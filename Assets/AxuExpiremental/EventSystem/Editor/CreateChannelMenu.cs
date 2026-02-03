#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AxuExpiremental.EventSystem.Editor
{
    public static class CreateChannelMenu
    {
        [MenuItem("Assets/Create/Events/Create Generic Channel (placeholder)")]
        public static void CreateGenericChannel()
        {
            // This is a helper template. You can't create a strongly-typed generic asset via menu.
            var so = ScriptableObject.CreateInstance<ScriptableObject>();
            AssetDatabase.CreateAsset(so, "Assets/EventChannel.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = so;
        }
    }
}
#endif