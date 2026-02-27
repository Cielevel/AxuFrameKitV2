using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuExperimental.BehaviorTree.Editor
{
    public class BehaviorTreeEditor : EditorWindow
    {
        BehaviorTreeGraphView treeView;
        InspectorView inspectorView;

        [MenuItem("AxuFrameKit/BehaviorTree/Editor")]
        public static void OpenWindow()
        {
            BehaviorTreeEditor wnd = GetWindow<BehaviorTreeEditor>();
            wnd.titleContent = new GUIContent("BehaviorTreeEditor");
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (Selection.activeObject is Runtime.BehaviorTree)
            {
                OpenWindow();
                return true;
            }
            return false;
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Load the stylesheet
            var styleSheet = Resources.Load<StyleSheet>("BehaviorTreeEditor");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            // Simple TwoPaneSplitView layout via code
            var splitView = new TwoPaneSplitView(0, 280, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1;

            // Left Pane: Inspector
            var leftPane = new VisualElement();
            leftPane.name = "inspector-container";
            leftPane.style.minWidth = 200;
            leftPane.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f)); // --bt-bg-panel

            // Inspector header label
            var inspectorHeader = new Label("NODE INSPECTOR");
            inspectorHeader.style.height = 28;
            inspectorHeader.style.paddingLeft = 12;
            inspectorHeader.style.paddingTop = 6;
            inspectorHeader.style.backgroundColor = new StyleColor(new Color(0.10f, 0.10f, 0.10f));
            inspectorHeader.style.color = new StyleColor(new Color(0.63f, 0.63f, 0.63f));
            inspectorHeader.style.fontSize = 11;
            inspectorHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            inspectorHeader.style.borderBottomWidth = 1;
            inspectorHeader.style.borderBottomColor = new StyleColor(new Color(0.04f, 0.04f, 0.04f));
            leftPane.Add(inspectorHeader);

            inspectorView = new InspectorView();
            inspectorView.style.flexGrow = 1;
            leftPane.Add(inspectorView);

            // Right Pane: Graph
            var rightPane = new VisualElement();
            rightPane.name = "graph-container";
            rightPane.style.flexGrow = 1;

            treeView = new BehaviorTreeGraphView();
            treeView.OnNodeSelected = OnNodeSelectionChanged;
            treeView.style.flexGrow = 1;
            rightPane.Add(treeView);

            splitView.Add(leftPane);
            splitView.Add(rightPane);

            root.Add(splitView);

            OnSelectionChange();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    OnSelectionChange();
                    break;
            }
        }

        private void OnSelectionChange()
        {
            Runtime.BehaviorTree tree = Selection.activeObject as Runtime.BehaviorTree;
            // Support selecting a GameObject that has a BehaviorTreeRunner (if we made one), 
            // but for now, just support direct SO selection.
            if (tree && treeView != null)
            {
                treeView.PopulateView(tree);
            }
        }

        void OnNodeSelectionChanged(NodeView nodeView)
        {
            inspectorView.UpdateSelection(nodeView);
        }
    }

    // Embed InspectorView class here for simplicity or separate file
    [UxmlElement]
    public partial class InspectorView : VisualElement
    {
        private NodeView currentNodeView;
        private UnityEditor.UIElements.InspectorElement currentInspector;

        public InspectorView()
        {
            // Schedule periodic check for property changes
            schedule.Execute(CheckForChanges).Every(100);
        }

        private string lastRemark;

        private void CheckForChanges()
        {
            if (currentNodeView != null && currentNodeView.node != null)
            {
                // Check if remark has changed
                if (lastRemark != currentNodeView.node.remark)
                {
                    lastRemark = currentNodeView.node.remark;
                    currentNodeView.UpdateRemarkDisplay();
                }
            }
        }

        public void UpdateSelection(NodeView nodeView)
        {
            Clear();
            currentNodeView = nodeView;
            currentInspector = null;
            lastRemark = null;

            if (nodeView != null && nodeView.node != null)
            {
                lastRemark = nodeView.node.remark;
                currentInspector = new UnityEditor.UIElements.InspectorElement(nodeView.node);
                Add(currentInspector);
            }
        }
    }
}
