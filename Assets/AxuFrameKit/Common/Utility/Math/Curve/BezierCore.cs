using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Curve
{
    /// <summary>
    /// 提供不依赖 MonoBehaviour 的 Bezier 曲线核心计算。
    /// 所有计算基于 Vector3，可用于 2D 或 3D 场景。
    /// </summary>
    public static class BezierCore
    {
        /// <summary>
        /// 计算线性 Bezier 曲线（2个点）在参数 t 处的点。
        /// 相当于 Vector3.Lerp。
        /// B(t) = (1 - t)P0 + tP1
        /// </summary>
        /// <param name="p0">起始点 (P0)。</param>
        /// <param name="p1">终点 (P1)。</param>
        /// <param name="t">参数 [0, 1]。</param>
        /// <returns>曲线上 t 处的点。</returns>
        public static Vector3 GetLinearPoint(Vector3 p0, Vector3 p1, float t)
        {
            // Vector3.Lerp 本身就是线性 Bezier 曲线
            return Vector3.Lerp(p0, p1, t);
        }

        /// <summary>
        /// 计算二次 Bezier 曲线（3个点）在参数 t 处的点。
        /// B(t) = (1-t)^2 P0 + 2(1-t)t P1 + t^2 P2
        /// </summary>
        /// <param name="p0">起始点 (P0)。</param>
        /// <param name="p1">控制点 (P1)。</param>
        /// <param name="p2">终点 (P2)。</param>
        /// <param name="t">参数 [0, 1]。</param>
        /// <returns>曲线上 t 处的点。</returns>
        public static Vector3 GetQuadraticPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            // 使用 De Casteljau 算法
            Vector3 q0 = Vector3.Lerp(p0, p1, t);
            Vector3 q1 = Vector3.Lerp(p1, p2, t);

            // 在两个线性插值结果之间再进行一次线性插值
            return Vector3.Lerp(q0, q1, t);
        }

        /// <summary>
        /// 计算三次 Bezier 曲线（4个点）在参数 t 处的点。
        /// B(t) = (1-t)^3 P0 + 3(1-t)^2 t P1 + 3(1-t) t^2 P2 + t^3 P3
        /// </summary>
        /// <param name="p0">起始点 (P0)。</param>
        /// <param name="p1">第一个控制点 (P1)。</param>
        /// <param name="p2">第二个控制点 (P2)。</param>
        /// <param name="p3">终点 (P3)。</param>
        /// <param name="t">参数 [0, 1]。</param>
        /// <returns>曲线上 t 处的点。</returns>
        public static Vector3 GetCubicPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            // 使用 De Casteljau 算法：
            // 第一次插值（Q0, Q1, Q2）
            Vector3 q0 = Vector3.Lerp(p0, p1, t);
            Vector3 q1 = Vector3.Lerp(p1, p2, t);
            Vector3 q2 = Vector3.Lerp(p2, p3, t);

            // 第二次插值（R0, R1）
            Vector3 r0 = Vector3.Lerp(q0, q1, t);
            Vector3 r1 = Vector3.Lerp(q1, q2, t);

            // 最终插值（B(t)）
            return Vector3.Lerp(r0, r1, t);
        }

        /// <summary>
        /// 计算三次 Bezier 曲线在参数 t 处的切线方向（一阶导数）。
        /// 切线方向可用于确定物体沿曲线移动时的朝向。
        /// </summary>
        /// <param name="p0">起始点 (P0)。</param>
        /// <param name="p1">第一个控制点 (P1)。</param>
        /// <param name="p2">第二个控制点 (P2)。</param>
        /// <param name="p3">终点 (P3)。</param>
        /// <param name="t">参数 [0, 1]。</param>
        /// <returns>切线方向（未归一化）。</returns>
        public static Vector3 GetCubicTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float oneMinusT = 1f - t;
            float oneMinusT2 = oneMinusT * oneMinusT;

            // B'(t) = 3 * (1-t)^2 * (P1 - P0) + 6 * (1-t) * t * (P2 - P1) + 3 * t^2 * (P3 - P2)

            Vector3 term1 = 3f * oneMinusT2 * (p1 - p0);
            Vector3 term2 = 6f * oneMinusT * t * (p2 - p1);
            Vector3 term3 = 3f * t2 * (p3 - p2);

            return term1 + term2 + term3;
        }

        /// <summary>
        /// 将三次 Bezier 曲线离散化为一系列点，以便于渲染或路径跟随。
        /// 曲线将返回 resolution 个点，包括起点和终点。
        /// </summary>
        /// <param name="p0">起始点 (P0)。</param>
        /// <param name="p1">第一个控制点 (P1)。</param>
        /// <param name="p2">第二个控制点 (P2)。</param>
        /// <param name="p3">终点 (P3)。</param>
        /// <param name="resolution">曲线点的数量（例如：resolution=100 将返回 100 个点）。</param>
        /// <returns>包含 resolution 个 Vector3 的数组，表示曲线上的点。</returns>
        public static Vector3[] GetCubicPoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int resolution)
        {
            // 确保分辨率有效，至少需要 2 个点（起点和终点）
            if (resolution < 2)
            {
                Debug.LogWarning("Bezier resolution must be at least 2. Returning array with P0 and P3.");
                return new Vector3[] { p0, p3 };
            }

            Vector3[] points = new Vector3[resolution];
            
            // 确定步长。如果 resolution=10，则有 9 个步长，t 从 0, 1/9, 2/9, ..., 9/9 变化。
            float stepSize = 1f / (resolution - 1); 

            for (int i = 0; i < resolution; i++)
            {
                float t = i * stepSize;
                
                // 使用现有的 GetCubicPoint 方法
                points[i] = GetCubicPoint(p0, p1, p2, p3, t);
            }

            return points;
        }
    }
}