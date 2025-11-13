using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuFrameKit.Modules.Gizmos.Editor.UI
{
    public class GizmosUtilEditor : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset mVisualTreeAsset = default;

        [MenuItem("Dev/Window/GizmosUtilEditor")]
        public static void ShowExample()
        {
            GizmosUtilEditor wnd = GetWindow<GizmosUtilEditor>();
            wnd.titleContent = new GUIContent("GizmosUtilEditor");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // VisualElements objects can contain other VisualElement following a tree hierarchy.
            // VisualElement label = new Label("Hello World! From C#");
            // root.Add(label);

            // Instantiate UXML
            VisualElement labelFromUXML = mVisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);
        }
    }
}
