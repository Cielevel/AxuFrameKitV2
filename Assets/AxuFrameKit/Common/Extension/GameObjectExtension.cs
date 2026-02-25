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

        // ==================== 激活状态控制 ====================

        /// <summary>
        /// 设置组件所在物体的激活状态
        /// </summary>
        public static void SetActive(this Component comp, bool active)
        {
            if (comp != null && comp.gameObject != null)
                comp.gameObject.SetActive(active);
        }

        /// <summary>
        /// 激活组件所在物体
        /// </summary>
        public static void Activate(this Component comp)
        {
            comp.SetActive(true);
        }

        /// <summary>
        /// 停用组件所在物体
        /// </summary>
        public static void Deactivate(this Component comp)
        {
            comp.SetActive(false);
        }

        /// <summary>
        /// 切换组件所在物体的激活状态
        /// </summary>
        public static void ToggleActive(this Component comp)
        {
            if (comp != null && comp.gameObject != null)
                comp.gameObject.SetActive(!comp.gameObject.activeSelf);
        }

        // ==================== 安全销毁 ====================

        /// <summary>
        /// 安全销毁 GameObject (自动区分运行时和编辑器模式)
        /// </summary>
        public static void DestroySelf(this GameObject go)
        {
            if (go == null) return;

            if (Application.isPlaying)
                Object.Destroy(go);
            else
                Object.DestroyImmediate(go);
        }

        /// <summary>
        /// 安全销毁 Component 所在的 GameObject
        /// </summary>
        public static void DestroyGameObject(this Component comp)
        {
            if (comp != null && comp.gameObject != null)
                comp.gameObject.DestroySelf();
        }

        /// <summary>
        /// 延迟销毁 GameObject
        /// </summary>
        public static void DestroySelf(this GameObject go, float delay)
        {
            if (go == null) return;

            if (Application.isPlaying)
                Object.Destroy(go, delay);
            else
                Object.DestroyImmediate(go);
        }

        // ==================== 层级操作 ====================

        /// <summary>
        /// 检查物体是否在指定的 LayerMask 中
        /// </summary>
        public static bool IsInLayerMask(this GameObject go, LayerMask layerMask)
        {
            return ((1 << go.layer) & layerMask.value) != 0;
        }

        /// <summary>
        /// 检查物体是否在指定的 LayerMask 中 (Component 扩展)
        /// </summary>
        public static bool IsInLayerMask(this Component comp, LayerMask layerMask)
        {
            return comp.gameObject.IsInLayerMask(layerMask);
        }

        /// <summary>
        /// 设置物体层级
        /// </summary>
        public static void SetLayer(this GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
        }

        /// <summary>
        /// 设置物体层级 (通过层级名称)
        /// </summary>
        public static void SetLayer(this GameObject go, string layerName)
        {
            if (go == null) return;
            go.layer = LayerMask.NameToLayer(layerName);
        }

        /// <summary>
        /// 递归设置物体及其所有子物体的层级
        /// </summary>
        public static void SetLayerRecursive(this GameObject go, int layer)
        {
            if (go == null) return;

            go.layer = layer;

            Transform trans = go.transform;
            for (int i = 0; i < trans.childCount; i++)
            {
                trans.GetChild(i).gameObject.SetLayerRecursive(layer);
            }
        }

        /// <summary>
        /// 递归设置物体及其所有子物体的层级 (通过层级名称)
        /// </summary>
        public static void SetLayerRecursive(this GameObject go, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"GameObjectExtension.SetLayerRecursive: 层级名称 '{layerName}' 不存在");
                return;
            }
            go.SetLayerRecursive(layer);
        }

        /// <summary>
        /// 递归设置物体及其所有子物体的层级 (Component 扩展)
        /// </summary>
        public static void SetLayerRecursive(this Component comp, int layer)
        {
            if (comp != null && comp.gameObject != null)
                comp.gameObject.SetLayerRecursive(layer);
        }

        // ==================== 查找与遍历 ====================

        /// <summary>
        /// 获取物体的完整层级路径 (从根节点到当前物体)
        /// </summary>
        public static string GetHierarchyPath(this GameObject go)
        {
            if (go == null) return string.Empty;

            string path = go.name;
            Transform current = go.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        /// <summary>
        /// 获取或创建子物体
        /// </summary>
        public static GameObject GetOrCreateChild(this GameObject go, string childName)
        {
            Transform child = go.transform.Find(childName);

            if (child == null)
            {
                GameObject newChild = new GameObject(childName);
                newChild.transform.SetParent(go.transform);
                return newChild;
            }

            return child.gameObject;
        }

        /// <summary>
        /// 查找子物体 (支持路径)
        /// </summary>
        public static GameObject FindChild(this GameObject go, string path)
        {
            Transform child = go.transform.Find(path);
            return child?.gameObject;
        }

        // ==================== 标签与命名 ====================

        /// <summary>
        /// 检查物体是否具有指定标签
        /// </summary>
        public static bool HasTag(this GameObject go, string tag)
        {
            return go != null && go.CompareTag(tag);
        }

        /// <summary>
        /// 检查物体是否具有指定标签 (Component 扩展)
        /// </summary>
        public static bool HasTag(this Component comp, string tag)
        {
            return comp != null && comp.gameObject.HasTag(tag);
        }

        /// <summary>
        /// 设置物体名称 (链式调用)
        /// </summary>
        public static GameObject SetName(this GameObject go, string name)
        {
            if (go != null)
                go.name = name;
            return go;
        }

        /// <summary>
        /// 设置物体标签
        /// </summary>
        public static GameObject SetTag(this GameObject go, string tag)
        {
            if (go != null)
                go.tag = tag;
            return go;
        }
    }
}
