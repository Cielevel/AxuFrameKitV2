using UnityEngine;

namespace AxuFrameKit.Common.Extension
{
    public static class VectorExtension
    {
        // ---链式修改分量---
        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        /// <summary>
        /// 链式修改 X 和 Y 分量
        /// </summary>
        public static Vector3 WithXY(this Vector3 v, float x, float y) => new Vector3(x, y, v.z);

        /// <summary>
        /// 链式修改 X 和 Z 分量
        /// </summary>
        public static Vector3 WithXZ(this Vector3 v, float x, float z) => new Vector3(x, v.y, z);

        /// <summary>
        /// 链式修改 Y 和 Z 分量
        /// </summary>
        public static Vector3 WithYZ(this Vector3 v, float y, float z) => new Vector3(v.x, y, z);

        /// <summary>
        /// 获取平面距离 (忽略Y轴高度差，常用于判定水平攻击范围)
        /// </summary>
        public static float FlatDistance(this Vector3 a, Vector3 b)
        {
            Vector2 aFlat = new Vector2(a.x, a.z);
            Vector2 bFlat = new Vector2(b.x, b.z);
            return Vector2.Distance(aFlat, bFlat);
        }

        // ==================== 转换为整数向量 ====================

        /// <summary>
        /// 将 Vector3 转换为 Vector2Int (截断取整)
        /// </summary>
        public static Vector2Int ToVector2Int(this Vector3 v) => new Vector2Int((int)v.x, (int)v.y);

        /// <summary>
        /// 将 Vector3 转换为 Vector3Int (截断取整)
        /// </summary>
        public static Vector3Int ToVector3Int(this Vector3 v) => new Vector3Int((int)v.x, (int)v.y, (int)v.z);

        /// <summary>
        /// 将 Vector3 转换为 Vector2Int (四舍五入)
        /// </summary>
        public static Vector2Int ToVector2IntRound(this Vector3 v) => new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));

        /// <summary>
        /// 将 Vector3 转换为 Vector3Int (四舍五入)
        /// </summary>
        public static Vector3Int ToVector3IntRound(this Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));

        /// <summary>
        /// 将 Vector2 转换为 Vector2Int (截断取整)
        /// </summary>
        public static Vector2Int ToVector2Int(this Vector2 v) => new Vector2Int((int)v.x, (int)v.y);

        /// <summary>
        /// 将 Vector2 转换为 Vector2Int (四舍五入)
        /// </summary>
        public static Vector2Int ToVector2IntRound(this Vector2 v) => new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));

        // ==================== 向量工具 ====================

        /// <summary>
        /// 限制向量的最大长度
        /// </summary>
        /// <param name="v">原向量</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>限制长度后的向量</returns>
        public static Vector3 ClampMagnitude(this Vector3 v, float maxLength)
        {
            return Vector3.ClampMagnitude(v, maxLength);
        }

        /// <summary>
        /// 限制向量的最大长度 (Vector2版本)
        /// </summary>
        public static Vector2 ClampMagnitude(this Vector2 v, float maxLength)
        {
            return Vector2.ClampMagnitude(v, maxLength);
        }

        /// <summary>
        /// 在圆内获取随机点 (XZ平面)
        /// </summary>
        /// <param name="center">圆心</param>
        /// <param name="radius">半径</param>
        /// <returns>圆内的随机点</returns>
        public static Vector3 RandomPointInCircleXZ(this Vector3 center, float radius)
        {
            Vector2 randomPoint = Random.insideUnitCircle * radius;
            return new Vector3(center.x + randomPoint.x, center.y, center.z + randomPoint.y);
        }

        /// <summary>
        /// 在圆内获取随机点 (XY平面)
        /// </summary>
        public static Vector3 RandomPointInCircleXY(this Vector3 center, float radius)
        {
            Vector2 randomPoint = Random.insideUnitCircle * radius;
            return new Vector3(center.x + randomPoint.x, center.y + randomPoint.y, center.z);
        }

        /// <summary>
        /// 在圆环上获取随机点 (XZ平面)
        /// </summary>
        /// <param name="center">圆心</param>
        /// <param name="radius">半径</param>
        /// <returns>圆环上的随机点</returns>
        public static Vector3 RandomPointOnCircleXZ(this Vector3 center, float radius)
        {
            Vector2 randomPoint = Random.onUnitSphere * radius;
            // 投影到XZ平面并归一化
            Vector2 normalized = randomPoint.normalized * radius;
            return new Vector3(center.x + normalized.x, center.y, center.z + normalized.y);
        }

        /// <summary>
        /// 在球内获取随机点
        /// </summary>
        public static Vector3 RandomPointInSphere(this Vector3 center, float radius)
        {
            return center + Random.insideUnitSphere * radius;
        }

        /// <summary>
        /// 在球面上获取随机点
        /// </summary>
        public static Vector3 RandomPointOnSphere(this Vector3 center, float radius)
        {
            return center + Random.onUnitSphere * radius;
        }

        // ==================== 向量检测 ====================

        /// <summary>
        /// 检测向量是否包含 NaN 或 Infinity
        /// </summary>
        public static bool IsNaN(this Vector3 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
        }

        /// <summary>
        /// 检测向量是否包含 NaN 或 Infinity (Vector2版本)
        /// </summary>
        public static bool IsNaN(this Vector2 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y);
        }

        /// <summary>
        /// 检测向量是否有效 (不包含 NaN 或 Infinity)
        /// </summary>
        public static bool IsValid(this Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
                   !float.IsNaN(v.y) && !float.IsInfinity(v.y) &&
                   !float.IsNaN(v.z) && !float.IsInfinity(v.z);
        }

        /// <summary>
        /// 检测向量是否为零向量
        /// </summary>
        public static bool IsZero(this Vector3 v, float tolerance = 0.0001f)
        {
            return v.sqrMagnitude < tolerance * tolerance;
        }

        /// <summary>
        /// 检测两个向量是否近似相等
        /// </summary>
        public static bool Approximately(this Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) < 0.0001f;
        }

        /// <summary>
        /// 检测两个向量是否近似相等 (Vector2版本)
        /// </summary>
        public static bool Approximately(this Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b) < 0.0001f;
        }
    }
}
