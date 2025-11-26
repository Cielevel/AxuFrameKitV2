using System.Collections.Generic;
using UnityEngine;

namespace PreTestFunc.SceneCurveDraw
{
    public class PathController : MonoBehaviour
    {
        // 存储曲线的控制点（世界坐标）
        public List<Vector3> controlPoints = new List<Vector3>()
        {
            new Vector3(-5, 0, 0),
            new Vector3(-2, 3, 0),
            new Vector3(2, -3, 0),
            new Vector3(5, 0, 0)
        };

        // 绘制曲线的 Gizmos (可选，用于运行时可视化)
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            if (controlPoints.Count < 4) return;
        
            // 示例：绘制三次贝塞尔曲线
            int     segmentCount  = 20;
            Vector3 previousPoint = GetBezierPoint(0f);
        
            for (int i = 1; i <= segmentCount; i++)
            {
                float   t            = (float)i / segmentCount;
                Vector3 currentPoint = GetBezierPoint(t);
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }

        // 简单的三次贝塞尔曲线计算
        // P0: Start, P1: Handle A, P2: Handle B, P3: End
        public Vector3 GetBezierPoint(float t)
        {
            if (controlPoints.Count < 4) return Vector3.zero;

            Vector3 p0 = controlPoints[0];
            Vector3 p1 = controlPoints[1];
            Vector3 p2 = controlPoints[2];
            Vector3 p3 = controlPoints[3];

            float u   = 1 - t;
            float uu  = u * u;
            float uuu = u * u * u;
            float tt  = t * t;
            float ttt = t * t * t;

            // B(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃
            return uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
        }
    }
}