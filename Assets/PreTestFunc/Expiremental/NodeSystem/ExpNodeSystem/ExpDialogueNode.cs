using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ExpNodeSystem
{
    public enum NodeType
    {
        Start,
        Dialogue,
        Choice
    }

    [Serializable]
    public class ExpDialogueNode
    {
        public string id;
        public Rect rect;
        public string title;
        public NodeType type;

        // Dialogue specific data
        public string speakerName;
        public string content;

        // Port management
        public List<string> outputIds = new List<string>();

        [NonSerialized] private bool isDragging;
        [NonSerialized] private bool isSelected;

        public ExpDialogueNode(Vector2 position, string title, NodeType type)
        {
            this.id = Guid.NewGuid().ToString();
            this.rect = new Rect(position.x, position.y, 200, 140);
            this.title = title;
            this.type = type;
            this.speakerName = "Speaker";
            this.content = "Enter dialogue text here...";
        }

        public void Draw(bool selected)
        {
            isSelected = selected;
            DrawNodeBody();
            DrawTitle();
            DrawContent();
            DrawPorts();
        }

        private void DrawNodeBody()
        {
            if (isSelected)
            {
                Rect highlightRect = new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4);
                EditorGUI.DrawRect(highlightRect, new Color(0.3f, 0.5f, 0.8f, 0.8f));
            }

            // Draw opaque background
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f, 1f));

            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
        }

        private void DrawTitle()
        {
            Rect titleRect = new Rect(rect.x, rect.y, rect.width, 24);

            Color titleColor = type switch
            {
                NodeType.Start => new Color(0.1f, 0.5f, 0.1f),
                NodeType.Dialogue => new Color(0.1f, 0.3f, 0.5f),
                NodeType.Choice => new Color(0.5f, 0.3f, 0.1f),
                _ => Color.gray
            };

            EditorGUI.DrawRect(titleRect, titleColor);

            // Use EditorGUI.DropShadowLabel - it doesn't capture mouse events
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.white;
            titleStyle.hover.textColor = Color.white;
            titleStyle.onNormal.textColor = Color.white;
            titleStyle.onHover.textColor = Color.white;

            EditorGUI.DropShadowLabel(titleRect, title, titleStyle);
        }

        private void DrawContent()
        {
            float padding = 8;
            float currentY = rect.y + 28;

            if (type == NodeType.Dialogue || type == NodeType.Choice)
            {
                EditorGUI.DropShadowLabel(new Rect(rect.x + padding, currentY, rect.width - padding * 2, 16), "Speaker:", EditorStyles.miniLabel);
                currentY += 16;
                speakerName = EditorGUI.TextField(new Rect(rect.x + padding, currentY, rect.width - padding * 2, 18), speakerName);
                currentY += 22;

                EditorGUI.DropShadowLabel(new Rect(rect.x + padding, currentY, rect.width - padding * 2, 16), "Content:", EditorStyles.miniLabel);
                currentY += 16;
                content = EditorGUI.TextArea(new Rect(rect.x + padding, currentY, rect.width - padding * 2, 60), content, EditorStyles.textArea);
            }
            else if (type == NodeType.Start)
            {
                Rect textRect = new Rect(rect.x + padding, currentY, rect.width - padding * 2, 18);
                EditorGUI.DropShadowLabel(textRect, "Starting Point of Graph", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawPorts()
        {
            // Input Port (Left)
            if (type != NodeType.Start)
            {
                Rect inPort = GetInputPortRect();
                GUI.Box(inPort, "", EditorStyles.radioButton);
            }

            // Output Port (Right)
            Rect outPort = GetOutputPortRect();
            GUI.Box(outPort, "", EditorStyles.radioButton);
        }

        public Rect GetInputPortRect()
        {
            return new Rect(rect.x - 12, rect.y + rect.height * 0.5f - 6, 12, 12);
        }

        public Rect GetOutputPortRect()
        {
            return new Rect(rect.x + rect.width, rect.y + rect.height * 0.5f - 6, 12, 12);
        }

        public void ProcessEvents(Event e, float zoom = 1f)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && rect.Contains(e.mousePosition))
                    {
                        isDragging = true;
                        GUI.changed = true;
                        e.Use();
                    }
                    else if (e.button == 1 && rect.Contains(e.mousePosition))
                    {
                        // Right click menu for node?
                    }
                    break;

                case EventType.MouseUp:
                    isDragging = false;
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && isDragging)
                    {
                        // e.delta is in screen space, need to convert to canvas space
                        rect.position += e.delta / zoom;
                        GUI.changed = true;
                        e.Use();
                    }
                    break;
            }
        }
    }
}
