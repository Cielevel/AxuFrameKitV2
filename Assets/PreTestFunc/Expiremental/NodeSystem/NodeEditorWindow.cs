using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class NodeEditorWindow : EditorWindow
{
    private List<Node> nodes = new List<Node>();

    private Vector2 panOffset;
    private float zoom = 1f;

    private const float GRID_SMALL = 20f;
    private const float GRID_LARGE = 100f;

    [MenuItem("Tools/IMGUI Node Editor")]
    public static void Open()
    {
        GetWindow<NodeEditorWindow>("IMGUI Node");
    }

    private void OnEnable()
    {
        nodes.Add(new Node(new Rect(100, 100, 180, 80), "Start"));
        nodes.Add(new Node(new Rect(400, 200, 200, 100), "Action"));
    }

    private void OnGUI()
    {
        HandleZoom(Event.current);

        DrawGrid(GRID_SMALL, 0.2f, Color.gray);
        DrawGrid(GRID_LARGE, 0.4f, Color.gray);

        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(panOffset, Quaternion.identity, Vector3.one * zoom);

        DrawConnections();
        DrawNodes();

        GUI.matrix = oldMatrix;

        ProcessEvents(Event.current);

        if (GUI.changed)
            Repaint();
    }

    #region Draw

    private void DrawNodes()
    {
        foreach (var node in nodes)
            node.Draw();
    }

    private void DrawConnections()
    {
        Handles.BeginGUI();
        foreach (var node in nodes)
            node.DrawConnections();
        Handles.EndGUI();
    }

    private void DrawGrid(float spacing, float opacity, Color color)
    {
        int width = Mathf.CeilToInt(position.width / spacing);
        int height = Mathf.CeilToInt(position.height / spacing);

        Handles.BeginGUI();
        Handles.color = new Color(color.r, color.g, color.b, opacity);

        Vector3 offset = new Vector3(panOffset.x % spacing, panOffset.y % spacing);

        for (int i = 0; i < width; i++)
            Handles.DrawLine(
                new Vector3(spacing * i, 0, 0) + offset,
                new Vector3(spacing * i, position.height, 0) + offset);

        for (int j = 0; j < height; j++)
            Handles.DrawLine(
                new Vector3(0, spacing * j, 0) + offset,
                new Vector3(position.width, spacing * j, 0) + offset);

        Handles.EndGUI();
    }

    #endregion

    #region Events

    private void ProcessEvents(Event e)
    {
        if (e.type == EventType.MouseDrag && e.button == 2)
        {
            panOffset += e.delta;
            GUI.changed = true;
        }
    }

    private void HandleZoom(Event e)
    {
        if (e.type != EventType.ScrollWheel)
            return;

        float zoomDelta = -e.delta.y * 0.01f;
        float oldZoom = zoom;
        zoom = Mathf.Clamp(zoom + zoomDelta, 0.5f, 2f);

        Vector2 mouse = e.mousePosition;
        Vector2 delta = mouse - panOffset;
        Vector2 offset = delta - delta * (zoom / oldZoom);
        panOffset += offset;

        e.Use();
    }

    #endregion
}
