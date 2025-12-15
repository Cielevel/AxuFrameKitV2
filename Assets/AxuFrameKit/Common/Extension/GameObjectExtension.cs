using UnityEngine;

namespace AxuFrameKit.Common.Extension
{
    public static class GameObjectExtensions
    {
        /// <summary>
        /// 尝试获取组件，如果没有则添加一个
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }
            return component;
        }
    
        // 针对 Transform 的重载版本
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<T>();
        }

        /// <summary>
        /// 检查层级蒙版是否包含某个层 (位运算封装)
        /// </summary>
        public static bool Contains(this LayerMask mask, int layer)
        {
            return (mask.value & (1 << layer)) != 0;
        }
    }
}