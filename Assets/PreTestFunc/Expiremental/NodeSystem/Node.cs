using UnityEngine;
using UnityEditor;

public class Node
{
    public Rect rect;
    public string title;

    private bool isDragging;

    public Node(Rect rect, string title)
    {
        this.rect = rect;
        this.title = title;
    }

    public void Draw()
    {
        DrawNodeBody();
        DrawTitle();
    }

    private void DrawNodeBody()
    {
        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
    }

    private void DrawTitle()
    {
        Rect titleRect = new Rect(rect.x, rect.y, rect.width, 20);
        GUI.Label(titleRect, title, EditorStyles.boldLabel);
    }

    public void DrawConnections()
    {
        // 示例连线占位
    }

    public void ProcessEvents(Event e)
    {
        if (rect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                isDragging = true;
                GUI.changed = true;
            }
        }

        if (e.type == EventType.MouseDrag && e.button == 0 && isDragging)
        {
            rect.position += e.delta;
            e.Use();
        }

        if (e.type == EventType.MouseUp)
        {
            isDragging = false;
        }
    }
}
