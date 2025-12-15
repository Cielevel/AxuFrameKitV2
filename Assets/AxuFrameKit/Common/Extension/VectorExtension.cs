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
        /// 获取平面距离 (忽略Y轴高度差，常用于判定水平攻击范围)
        /// </summary>
        public static float FlatDistance(this Vector3 a, Vector3 b)
        {
            Vector2 aFlat = new Vector2(a.x, a.z);
            Vector2 bFlat = new Vector2(b.x, b.z);
            return Vector2.Distance(aFlat, bFlat);
        }
    }
}