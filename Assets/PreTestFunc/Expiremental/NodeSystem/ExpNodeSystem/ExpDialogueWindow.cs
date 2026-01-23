using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExpNodeSystem
{
    public class ExpDialogueWindow : EditorWindow
    {
        private List<ExpDialogueNode> nodes = new();
        private List<NodeConnection> connections = new();

        private Vector2 panOffset;
        private float zoom = 1f;

        private ExpDialogueNode selectedNode;
        private NodeConnection selectedConnection;

        // Drag connection
        private ExpDialogueNode draggingFrom;
        private bool isDraggingConnection;

        [MenuItem("Tools/Exp Dialogue Node Editor")]
        public static void Open()
        {
            var wnd = GetWindow<ExpDialogueWindow>();
            wnd.titleContent = new GUIContent("Exp Dialogue Editor");
            wnd.minSize = new Vector2(800, 600);
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;

            var toolbar = new ExpToolbar();
            root.Add(toolbar);

            toolbar.Add(new ExpToolbarButton(() => AddNode(NodeType.Dialogue)) { text = "Add Dialogue" });
            toolbar.Add(new ExpToolbarButton(() => AddNode(NodeType.Choice)) { text = "Add Choice" });
            toolbar.Add(new ExpToolbarButton(ClearAll) { text = "Clear All" });

            var imgui = new IMGUIContainer(OnIMGUI);
            imgui.style.flexGrow = 1;
            root.Add(imgui);

            if (nodes.Count == 0)
                AddNode(NodeType.Start, new Vector2(100, 300));
        }

        private void OnIMGUI()
        {
            Event e = Event.current;

            HandleZoom(e);

            // === Grid (Screen Space but calculated in Canvas) ===
            DrawGrid(20f, 0.1f, Color.gray);
            DrawGrid(100f, 0.2f, Color.gray);

            // === Draw connections FIRST (Screen Space) ===
            DrawConnectionsScreen();

            // === Nodes (Canvas Space) ===
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(panOffset, Quaternion.identity, Vector3.one * zoom);

            DrawNodes();
            ProcessEvents(e);

            GUI.matrix = oldMatrix;

            // === Dragging temp connection ===
            if (isDraggingConnection && draggingFrom != null)
            {
                Vector2 start = CanvasToScreen(draggingFrom.GetOutputPortRect().center);
                Vector2 end = e.mousePosition;
                DrawBezierScreen(start, end, Color.white, 4f);
                GUI.changed = true;
            }

            if (GUI.changed)
                Repaint();
        }

        #region Draw

        private void DrawNodes()
        {
            foreach (var node in nodes)
                node.Draw(node == selectedNode);
        }

        private void DrawConnectionsScreen()
        {
            Handles.BeginGUI();
            Handles.matrix = Matrix4x4.identity;

            foreach (var c in connections)
            {
                Color color = (c == selectedConnection) ? Color.yellow : Color.cyan;

                Vector2 start = CanvasToScreen(c.from.GetOutputPortRect().center);
                Vector2 end = CanvasToScreen(c.to.GetInputPortRect().center);

                DrawBezierScreen(start, end, color, 6f);
            }

            Handles.EndGUI();
        }

        private void DrawBezierScreen(Vector2 start, Vector2 end, Color color, float width)
        {
            float tangent = 60f;
            Vector2 startTan = start + Vector2.right * tangent;
            Vector2 endTan = end + Vector2.left * tangent;

            Handles.DrawBezier(start, end, startTan, endTan, color, null, width);
        }

        private void DrawGrid(float spacing, float opacity, Color color)
        {
            Handles.BeginGUI();
            Handles.color = new Color(color.r, color.g, color.b, opacity);

            float scaledSpacing = spacing * zoom;
            float offsetX = panOffset.x % scaledSpacing;
            float offsetY = panOffset.y % scaledSpacing;

            for (float x = offsetX; x < position.width; x += scaledSpacing)
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, position.height));

            for (float y = offsetY; y < position.height; y += scaledSpacing)
                Handles.DrawLine(new Vector3(0, y), new Vector3(position.width, y));

            Handles.EndGUI();
        }

        #endregion

        #region Events

        private void ProcessEvents(Event e)
        {
            Vector2 canvasMouse = ScreenToCanvas(e.mousePosition);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                selectedNode = null;
                selectedConnection = null;

                bool clickedPort = false;
                bool clickedNode = false;

                // Ports first
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    if (nodes[i].GetOutputPortRect().Contains(canvasMouse))
                    {
                        draggingFrom = nodes[i];
                        isDraggingConnection = true;
                        clickedPort = true;
                        e.Use();
                        break;
                    }
                }

                // Nodes (only if no port was clicked)
                if (!clickedPort)
                {
                    for (int i = nodes.Count - 1; i >= 0; i--)
                    {
                        if (nodes[i].rect.Contains(canvasMouse))
                        {
                            selectedNode = nodes[i];
                            clickedNode = true;
                            break;
                        }
                    }
                }

                // Connections (only if no node or port was clicked)
                if (!clickedPort && !clickedNode)
                {
                    foreach (var c in connections)
                    {
                        if (IsPointNearBezier(
                            e.mousePosition,
                            CanvasToScreen(c.from.GetOutputPortRect().center),
                            CanvasToScreen(c.to.GetInputPortRect().center)))
                        {
                            selectedConnection = c;
                            e.Use();
                            return;
                        }
                    }
                }

                // If nothing was clicked, clear selection
                if (!clickedPort && !clickedNode && selectedConnection == null)
                {
                    GUI.changed = true;
                }
            }

            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
            {
                if (selectedConnection != null)
                {
                    connections.Remove(selectedConnection);
                    selectedConnection = null;
                    GUI.changed = true;
                    e.Use();
                }
            }

            if (e.type == EventType.MouseUp && isDraggingConnection)
            {
                foreach (var node in nodes)
                {
                    if (node != draggingFrom &&
                        node.type != NodeType.Start &&
                        node.GetInputPortRect().Contains(canvasMouse))
                    {
                        CreateConnection(draggingFrom, node);
                        break;
                    }
                }

                draggingFrom = null;
                isDraggingConnection = false;
                e.Use();
            }

            if (e.type == EventType.MouseDrag &&
                (e.button == 2 || (e.button == 0 && e.alt)))
            {
                panOffset += e.delta;
                e.Use();
            }

            if (!isDraggingConnection)
            {
                foreach (var node in nodes)
                {
                    Event evt = new Event(e);
                    evt.mousePosition = canvasMouse;
                    node.ProcessEvents(evt, zoom);
                }
            }
        }

        private void HandleZoom(Event e)
        {
            if (e.type != EventType.ScrollWheel) return;

            float oldZoom = zoom;
            zoom = Mathf.Clamp(zoom - e.delta.y * 0.01f, 0.25f, 2f);

            Vector2 mouse = e.mousePosition;
            panOffset = mouse - (mouse - panOffset) * (zoom / oldZoom);

            e.Use();
            GUI.changed = true;
        }

        #endregion

        #region Utils

        private Vector2 CanvasToScreen(Vector2 canvas)
        {
            return canvas * zoom + panOffset;
        }

        private Vector2 ScreenToCanvas(Vector2 screen)
        {
            return (screen - panOffset) / zoom;
        }

        private bool IsPointNearBezier(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 startTan = start + Vector2.right * 60f;
            Vector2 endTan = end + Vector2.left * 60f;

            const int samples = 16;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 p = GetBezierPoint(start, startTan, endTan, end, t);
                if (Vector2.Distance(point, p) < 8f)
                    return true;
            }
            return false;
        }

        private static Vector2 GetBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        private void AddNode(NodeType type, Vector2? pos = null)
        {
            Vector2 spawn = pos ?? ScreenToCanvas(new Vector2(200, 200));
            nodes.Add(new ExpDialogueNode(spawn, type.ToString(), type));
            GUI.changed = true;
        }

        private void CreateConnection(ExpDialogueNode from, ExpDialogueNode to)
        {
            if (!connections.Any(c => c.from == from && c.to == to))
                connections.Add(new NodeConnection { from = from, to = to });
        }

        private void ClearAll()
        {
            nodes.Clear();
            connections.Clear();
            AddNode(NodeType.Start, new Vector2(100, 300));
        }

        #endregion
    }

    [System.Serializable]
    public class NodeConnection
    {
        public ExpDialogueNode from;
        public ExpDialogueNode to;
    }

    // Custom Toolbar to avoid conflict with UIElements.Toolbar
    public class ExpToolbar : VisualElement
    {
        public ExpToolbar()
        {
            style.flexDirection = FlexDirection.Row;
            style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            style.height = 25;
            style.paddingLeft = 5;
            style.alignItems = Align.Center;
        }
    }

    public class ExpToolbarButton : Button
    {
        public ExpToolbarButton(System.Action clickEvent) : base(clickEvent)
        {
            style.height = 20;
            style.marginRight = 5;
            style.paddingLeft = 10;
            style.paddingRight = 10;
        }
    }
}
