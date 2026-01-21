using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using AxuFrameKit.BehaviorTree;

namespace AxuFrameKit.BehaviorTree.Editor
{
    public class BehaviorTreeGraphView : GraphView
    {
        // Removed UxmlFactory as we are using pure code construction
        // public new class UxmlFactory : UxmlFactory<BehaviorTreeGraphView, GraphView.UxmlTraits> { }

        public BehaviorTree tree;
        public Action<NodeView> OnNodeSelected;

        public BehaviorTreeGraphView()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var styleSheet = Resources.Load<StyleSheet>("BehaviorTreeEditor");
            styleSheets.Add(styleSheet);

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            PopulateView(tree);
            AssetDatabase.SaveAssets();
        }

        public NodeView FindNodeView(Node node)
        {
            return GetNodeByGuid(node.guid) as NodeView;
        }

        public void PopulateView(BehaviorTree tree)
        {
            this.tree = tree;

            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList()); // ToList() is crucial for modification during iteration
            graphViewChanged += OnGraphViewChanged;

            if (tree.rootNode == null)
            {
                tree.rootNode = tree.CreateNode(typeof(RootNode)) as RootNode;
                EditorUtility.SetDirty(tree);
                AssetDatabase.SaveAssets();
            }

            // Create Node Views
            tree.nodes.ForEach(n => CreateNodeView(n));

            // Create Edges
            tree.nodes.ForEach(n =>
            {
                var children = tree.GetChildren(n);
                children.ForEach(c =>
                {
                    NodeView parentView = FindNodeView(n);
                    NodeView childView = FindNodeView(c);

                    Edge edge = parentView.output.ConnectTo(childView.input);
                    AddElement(edge);
                });
            });

            // Update sequence indices for all nodes
            UpdateSequenceIndices();
        }

        /// <summary>
        /// Update sequence index display for all children of SequencerNode
        /// </summary>
        public void UpdateSequenceIndices()
        {
            if (tree == null) return;

            // First, clear all indices
            foreach (var node in tree.nodes)
            {
                var nodeView = FindNodeView(node);
                if (nodeView != null)
                {
                    nodeView.SetSequenceIndex(-1);
                }
            }

            // Then set indices for children of SequencerNode
            foreach (var node in tree.nodes)
            {
                if (node is SequencerNode sequencer)
                {
                    for (int i = 0; i < sequencer.children.Count; i++)
                    {
                        var childNode = sequencer.children[i];
                        var childView = FindNodeView(childNode);
                        if (childView != null)
                        {
                            childView.SetSequenceIndex(i + 1); // 1-based index
                        }
                    }
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
            {
                return endPort.direction != startPort.direction &&
                       endPort.node != startPort.node;
            }).ToList();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                graphViewChange.elementsToRemove.ForEach(elem =>
                {
                    if (elem is NodeView nodeView)
                    {
                        tree.DeleteNode(nodeView.node);
                    }

                    if (elem is Edge edge)
                    {
                        NodeView parentView = edge.output.node as NodeView;
                        NodeView childView = edge.input.node as NodeView;
                        tree.RemoveChild(parentView.node, childView.node);
                    }
                });
            }

            if (graphViewChange.edgesToCreate != null)
            {
                graphViewChange.edgesToCreate.ForEach(edge =>
                {
                    NodeView parentView = edge.output.node as NodeView;
                    NodeView childView = edge.input.node as NodeView;
                    tree.AddChild(parentView.node, childView.node);
                });
            }

            // Update sequence indices after edge changes
            if (graphViewChange.elementsToRemove != null || graphViewChange.edgesToCreate != null)
            {
                // Schedule update to run after the graph view has finished processing
                schedule.Execute(UpdateSequenceIndices).ExecuteLater(10);
            }

            return graphViewChange;
        }

        // This is where context menu is built
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (tree == null) return;

            // base.BuildContextualMenu(evt); // We can keep standard copy/paste etc if we want

            // Simple approach: Add actions to create specific Node types
            // In a real app, use SearchWindowProvider for a nicer dropdown
            {
                var types = TypeCache.GetTypesDerivedFrom<ActionNode>();
                foreach (var type in types)
                {
                    evt.menu.AppendAction($"[Action]/{type.Name}", (a) => CreateNode(type, a.eventInfo.localMousePosition));
                }
            }
            {
                var types = TypeCache.GetTypesDerivedFrom<CompositeNode>();
                foreach (var type in types)
                {
                    evt.menu.AppendAction($"[Composite]/{type.Name}", (a) => CreateNode(type, a.eventInfo.localMousePosition));
                }
            }
            {
                var types = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
                foreach (var type in types)
                {
                    evt.menu.AppendAction($"[Decorator]/{type.Name}", (a) => CreateNode(type, a.eventInfo.localMousePosition));
                }
            }
        }

        void CreateNode(System.Type type, Vector2 position)
        {
            if (tree == null) return;
            Node node = tree.CreateNode(type);
            node.position = position;
            CreateNodeView(node);
        }

        void CreateNodeView(Node node)
        {
            NodeView nodeView = new NodeView(node);
            nodeView.OnNodeSelected = OnNodeSelected;
            AddElement(nodeView);
        }
    }
}
