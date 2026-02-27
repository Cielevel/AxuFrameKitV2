using UnityEditor;
using UnityEngine;

// 告诉 Unity 这个编辑器是针对 PathController 脚本的
namespace AxuExperimental.SceneCurveDraw.Editor
{
    [CustomEditor(typeof(PathController))]
    public class PathControllerEditor : UnityEditor.Editor
    {
        private PathController pathController;

        private void OnEnable()
        {
            // 获取目标组件
            pathController = (PathController)target;
        }

        // 核心函数：在 Scene View 中绘制 Handles
        private void OnSceneGUI()
        {
            // 确保数据有效
            if (pathController.controlPoints == null || pathController.controlPoints.Count == 0)
                return;

            // 确保修改被记录，以便支持撤销 (Ctrl+Z)
            // 必须在修改任何序列化对象之前调用
            Undo.RecordObject(pathController, "Move Bezier Point");

            // ------------------ 绘制控制点和交互式拖柄 ------------------
            for (int i = 0; i < pathController.controlPoints.Count; i++)
            {
                // 1. 设置 Handle 的颜色
                Handles.color = (i == 0 || i == pathController.controlPoints.Count - 1) ? Color.cyan : Color.red;

                // 2. 将本地坐标转换为世界坐标（如果控制点是相对于组件的本地坐标）
                // 注意：在这个例子中，controlPoints 存储的是世界坐标。
                Vector3 worldPosition = pathController.controlPoints[i];

                // 3. 绘制一个可拖动的**位置拖柄**
                // Handles.PositionHandle() 会返回拖柄被拖动后的新位置。
                Vector3 newWorldPosition = Handles.PositionHandle(worldPosition, Quaternion.identity);

                // 4. 绘制一个球体来标记控制点 (可选，让点更容易点击)
                float handleSize = HandleUtility.GetHandleSize(worldPosition) * 0.1f;
                Handles.SphereHandleCap(i, worldPosition, Quaternion.identity, handleSize, EventType.Repaint);

                // 5. 如果位置发生了变化，更新组件的数据
                if (newWorldPosition != worldPosition)
                {
                    // 将拖动后的新位置存回列表
                    pathController.controlPoints[i] = newWorldPosition;
                    // 标记对象为“已修改”，确保数据被保存到场景文件
                    EditorUtility.SetDirty(pathController);
                }
            }

            // ------------------ 绘制曲线本身 (使用 Handles 而不是 Gizmos) ------------------
            Handles.color = Color.yellow;
            if (pathController.controlPoints.Count >= 4)
            {
                // 绘制贝塞尔曲线（可以简化为直接使用 Handles.DrawBezier）
                // P0, P3 是锚点，P1, P2 是控制柄
                Handles.DrawBezier(
                                   pathController.controlPoints[0],
                                   pathController.controlPoints[3],
                                   pathController.controlPoints[1],
                                   pathController.controlPoints[2],
                                   Color.yellow,
                                   null,
                                   2f // 曲线宽度
                                  );
                
                Handles.DrawLine(pathController.controlPoints[0], pathController.controlPoints[1]);
                Handles.DrawLine(pathController.controlPoints[3], pathController.controlPoints[2]);
            }
        }
    }
}