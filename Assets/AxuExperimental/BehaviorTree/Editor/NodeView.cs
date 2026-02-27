using System;
using AxuExperimental.BehaviorTree.Runtime;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Node = AxuExperimental.BehaviorTree.Runtime.Node;
using Runtime_Node = AxuExperimental.BehaviorTree.Runtime.Node;

namespace AxuExperimental.BehaviorTree.Editor
{
    public class NodeView : UnityEditor.Experimental.GraphView.Node
    {
        public Action<NodeView> OnNodeSelected;
        public Runtime_Node node;
        public Port input;
        public Port output;

        private Label indexLabel;

        // Node type colors matching USS variables
        private static readonly Color ActionColor = new Color(0.79f, 0.29f, 0.29f);      // #c94a4a
        private static readonly Color CompositeColor = new Color(0.79f, 0.64f, 0.29f);   // #c9a34a
        private static readonly Color DecoratorColor = new Color(0.35f, 0.35f, 0.79f);   // #5a5ac9
        private static readonly Color RootColor = new Color(0.29f, 0.79f, 0.44f);        // #4ac970
        private static readonly Color DefaultPortColor = new Color(0.72f, 0.72f, 0.72f); // #b8b8b8

        public NodeView(Runtime_Node node) : base()
        {
            this.node = node;
            this.title = node.name;
            this.viewDataKey = node.guid;

            style.left = node.position.x;
            style.top = node.position.y;

            CreateInputPorts();
            CreateOutputPorts();
            SetupClasses();
            SetupRemark();
            SetupDescription();
            SetupIndexLabel();

            // Force input container to align ports to the left
            inputContainer.style.flexDirection = FlexDirection.Column;
            inputContainer.style.alignItems = Align.FlexStart;
            inputContainer.style.backgroundColor = StyleKeyword.None;

            // Force output container to align ports to the right
            outputContainer.style.flexDirection = FlexDirection.Column;
            outputContainer.style.alignItems = Align.FlexEnd;
            outputContainer.style.backgroundColor = StyleKeyword.None;

            // Unify the bottom section background color
            var topContainer = this.Q("top");
            if (topContainer != null)
            {
                topContainer.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.15f));
            }
        }

        private void SetupIndexLabel()
        {
            // Create index label to show sequence position
            indexLabel = new Label();
            indexLabel.name = "sequence-index";
            indexLabel.style.position = Position.Absolute;
            indexLabel.style.right = 8;
            indexLabel.style.top = 0;
            indexLabel.style.bottom = 0;
            indexLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            indexLabel.style.fontSize = 11;
            indexLabel.style.color = new StyleColor(new Color(1f, 1f, 1f, 0.8f));
            indexLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            indexLabel.style.display = DisplayStyle.None; // Hidden by default

            // Add to title container
            var titleContainer = this.Q("title");
            if (titleContainer != null)
            {
                titleContainer.Add(indexLabel);
            }
        }

        /// <summary>
        /// Set the sequence index display. Call with -1 or 0 to hide.
        /// Index is 1-based (first child = 1).
        /// </summary>
        public void SetSequenceIndex(int index)
        {
            if (indexLabel == null) return;

            if (index > 0)
            {
                indexLabel.text = index.ToString();
                indexLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                indexLabel.style.display = DisplayStyle.None;
            }
        }

        private Label remarkLabel;
        private VisualElement remarkContainer;

        private void SetupRemark()
        {
            // Create remark container - will be placed in the node's extension container
            remarkContainer = new VisualElement();
            remarkContainer.name = "remark-container";
            remarkContainer.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.25f));
            remarkContainer.style.paddingLeft = 10;
            remarkContainer.style.paddingRight = 10;
            remarkContainer.style.paddingTop = 6;
            remarkContainer.style.paddingBottom = 6;
            remarkContainer.style.borderBottomWidth = 1;
            remarkContainer.style.borderBottomColor = new StyleColor(new Color(0, 0, 0, 0.3f));

            remarkLabel = new Label();
            remarkLabel.name = "remark-label";
            remarkLabel.style.whiteSpace = WhiteSpace.Normal;
            remarkLabel.style.fontSize = 10;
            remarkLabel.style.color = new StyleColor(new Color(0.75f, 0.75f, 0.75f));
            remarkLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            remarkContainer.Add(remarkLabel);

            // Add to extensionContainer and refresh
            extensionContainer.Add(remarkContainer);
            RefreshExpandedState();

            UpdateRemarkDisplay();
        }

        public void UpdateRemarkDisplay()
        {
            if (remarkLabel == null || remarkContainer == null) return;

            if (string.IsNullOrEmpty(node.remark))
            {
                remarkContainer.style.display = DisplayStyle.None;
            }
            else
            {
                remarkContainer.style.display = DisplayStyle.Flex;
                remarkLabel.text = node.remark;
            }
        }

        private void SetupClasses()
        {
            if (node is ActionNode)
            {
                AddToClassList("action");
            }
            else if (node is CompositeNode)
            {
                AddToClassList("composite");
            }
            else if (node is DecoratorNode)
            {
                AddToClassList("decorator");
            }
            else if (node is RootNode)
            {
                AddToClassList("root");
            }
        }

        private Color GetNodeTypeColor()
        {
            if (node is ActionNode) return ActionColor;
            if (node is CompositeNode) return CompositeColor;
            if (node is DecoratorNode) return DecoratorColor;
            if (node is RootNode) return RootColor;
            return DefaultPortColor;
        }

        private void CreateInputPorts()
        {
            if (node is ActionNode)
            {
                input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            }
            else if (node is CompositeNode)
            {
                input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            }
            else if (node is DecoratorNode)
            {
                input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            }
            else if (node is RootNode)
            {
                // Root node receives no input port visually in this setup
            }

            if (input != null)
            {
                input.portName = "";
                input.portColor = DefaultPortColor;
                // Connector on the LEFT side
                input.style.flexDirection = FlexDirection.Row;
                inputContainer.Add(input);
            }
        }

        private void CreateOutputPorts()
        {
            if (node is ActionNode)
            {
                // Actions have no children
            }
            else if (node is CompositeNode)
            {
                output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            }
            else if (node is DecoratorNode)
            {
                output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            }
            else if (node is RootNode)
            {
                output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            }

            if (output != null)
            {
                output.portName = "";
                // Use node type color for output ports
                output.portColor = GetNodeTypeColor();
                // Connector on the RIGHT side
                output.style.flexDirection = FlexDirection.RowReverse;
                outputContainer.Add(output);
            }
        }

        private void SetupDescription()
        {
            // Add description area if node has a description field via reflection
            var descField = node.GetType().GetField("description",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (descField != null)
            {
                var descValue = descField.GetValue(node) as string;
                if (!string.IsNullOrEmpty(descValue))
                {
                    var descContainer = new VisualElement();
                    descContainer.name = "description";

                    var descLabel = new Label(descValue);
                    descLabel.style.whiteSpace = WhiteSpace.Normal;
                    descContainer.Add(descLabel);

                    mainContainer.Add(descContainer);
                }
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            UnityEditor.Undo.RecordObject(node, "Behavior Tree (Set Position)");
            node.position.x = newPos.xMin;
            node.position.y = newPos.yMin;
            UnityEditor.EditorUtility.SetDirty(node);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            if (OnNodeSelected != null)
            {
                OnNodeSelected.Invoke(this);
            }
        }

        public void UpdateState()
        {
            RemoveFromClassList("running");
            RemoveFromClassList("failure");
            RemoveFromClassList("success");

            if (Application.isPlaying)
            {
                switch (node.state)
                {
                    case NodeState.Running:
                        if (node.started) AddToClassList("running");
                        break;
                    case NodeState.Failure:
                        AddToClassList("failure");
                        break;
                    case NodeState.Success:
                        AddToClassList("success");
                        break;
                }
            }
        }
    }
}
