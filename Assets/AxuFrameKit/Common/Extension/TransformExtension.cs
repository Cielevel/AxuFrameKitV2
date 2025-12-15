using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit._Core.Enum;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Extension
{
    public static class TransformExtension
    {
        /// <summary>
        /// 一键重置位置、旋转和缩放 (常用于对象池回收时)
        /// </summary>
        public static void ResetTransformation(this Transform trans)
        {
            trans.position      = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale    = Vector3.one;
        }
        
        public static void ResetLocalPosition(this Transform self) => self.localPosition = Vector3.zero;
        public static void ResetPosition(this Transform self) => self.position = Vector3.zero;

        public static void SetParentAndResetPos(this Transform self, Transform parent)
        {
            self.SetParent(parent, false);
            self.localPosition = Vector3.zero;
        }

        public static Transform[] GetChildren(this Transform self)
        {
            Transform[] result = new Transform[self.childCount];
            for (int i = 0; i < self.childCount; i++)
            {
                result[i] = self.GetChild(i);
            }

            return result;
        }

        public static void ClearChildren(this Transform self)
        {
            if (Application.isPlaying)
                for (int i = 0; i < self.childCount; i++)
                    Object.Destroy(self.GetChild(i).gameObject);
            else
                for (int i = 0; i < self.childCount; i++)
                    Object.DestroyImmediate(self.GetChild(i).gameObject);
        }

        public static List<T> GetChildrenComponents<T>(this Transform self, bool includeInactive) where T : Component
        {
            return self.GetComponentsInChildren<T>(includeInactive).ToList();
        }
        
        /// <summary>
        /// 显示所有子物体
        /// </summary>
        public static void ShowChildren(this Transform self)
        {
            for (int i = 0; i < self.childCount; i++)
                self.GetChild(i).gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 显示前n个子物体，n最多不超过所有子物体个数
        /// </summary>
        public static void ShowChildren(this Transform self, int count)
        {
            int correctCount = Math.Min(self.childCount, count);
            
            for (int i = 0; i < correctCount; i++)
                self.GetChild(i).gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 显示前n个子物体，n最多不超过所有子物体个数，后续的物体则隐藏
        /// </summary>
        public static void ShowChildAndHideRest(this Transform self, int count)
        {
            int correctCount = Math.Min(self.childCount, count);
            
            for (int i = 0; i < correctCount; i++)
                self.GetChild(i).gameObject.SetActive(true);

            for (int i = correctCount; i < self.childCount; i++)
                self.GetChild(i).gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 隐藏所有子物体
        /// </summary>
        public static void HideChildren(this Transform self)
        {
            for (int i = 0; i < self.childCount; i++)
                self.GetChild(i).gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 隐藏前n个子物体，n最多不超过所有子物体个数
        /// </summary>
        public static void HideChildren(this Transform self, int count)
        {
            int correctCount = Math.Min(self.childCount, count);
            
            for (int i = 0; i < correctCount; i++)
                self.GetChild(i).gameObject.SetActive(false);
        }

        public static List<Transform> GetChildrenAsList(this Transform self)
        {
            List<Transform> result = new(self.childCount);
            for (int i = 0; i < self.childCount; i++)
            {
                result.Add(self.GetChild(i));
            }

            return result;
        }

        /// <summary>
        /// 递归查询子物体，将返回第一个名称匹配的物体
        /// </summary>
        public static Transform FindIncludeChild(this Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                Transform result = FindIncludeChild(child, name); // 递归查找
                if (result != null)
                    return result;
            }

            return null;
        }
        
        /// <summary>
        /// 递归查询子物体，将返回第一个名称匹配的物体
        /// </summary>
        public static Transform FindIncludeChild(this Transform parent, string name, Func<string, string, bool> match)
        {
            if (match == null)
            {
                Debug.LogError("TransformExtension.FindIncludeChild: match function cannot be null.");
                return null;
            }
            
            foreach (Transform child in parent)
            {
                if (match(child.name, name))
                    return child;

                Transform result = FindIncludeChild(child, name, match); // 递归查找
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// 查询离当前最近的transform，可自定轴面
        /// </summary>
        public static Transform FindNearest(this Transform self, WorldAxis worldAxis, Transform[] targets)
        {
            Transform closest = null;

            float minSqrDistance = Mathf.Infinity;

            foreach (var target in targets)
            {
                if (target == null || target == self) continue; // 排除自身和空对象

                Vector3 delta = target.position - self.position;
                float   dx    = (worldAxis.HasFlag(WorldAxis.X)) ? delta.x : 0f;
                float   dy    = (worldAxis.HasFlag(WorldAxis.Y)) ? delta.y : 0f;
                float   dz    = (worldAxis.HasFlag(WorldAxis.Z)) ? delta.z : 0f;

                float sqrDistance = dx * dx + dy * dy + dz * dz;

                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closest        = target;
                }
            }

            return closest;
        }

        /// <summary>
        /// 查询离当前最近的transform，可自定轴面（对碰撞检测时的碰撞体集合进行优化，不必select出transform）
        /// </summary>
        public static Transform FindNearest(this Transform self, WorldAxis worldAxis, Collider[] targets)
        {
            Transform closest = null;

            float minSqrDistance = Mathf.Infinity;

            foreach (var target in targets)
            {
                if (target == null || target.transform == self) continue; // 排除自身和空对象

                Vector3 delta = target.transform.position - self.position;
                float   dx    = (worldAxis.HasFlag(WorldAxis.X)) ? delta.x : 0f;
                float   dy    = (worldAxis.HasFlag(WorldAxis.Y)) ? delta.y : 0f;
                float   dz    = (worldAxis.HasFlag(WorldAxis.Z)) ? delta.z : 0f;

                float sqrDistance = dx * dx + dy * dy + dz * dz;

                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closest        = target.transform;
                }
            }

            return closest;
        }

        public static T FindNearest<T>(this T self, WorldAxis worldAxis, T[] targets) where T : Component
        {
            T closest = null;

            float minSqrDistance = Mathf.Infinity;

            foreach (var target in targets)
            {
                if (target == null || target == self) continue; // 排除自身和空对象

                Vector3 delta = target.transform.position - self.transform.position;
                float   dx    = (worldAxis.HasFlag(WorldAxis.X)) ? delta.x : 0f;
                float   dy    = (worldAxis.HasFlag(WorldAxis.Y)) ? delta.y : 0f;
                float   dz    = (worldAxis.HasFlag(WorldAxis.Z)) ? delta.z : 0f;

                float sqrDistance = dx * dx + dy * dy + dz * dz;

                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closest        = target;
                }
            }

            return closest;
        }

        #region Look(Axis)

        /// <summary>
        /// 使物体仅绕 Y 轴旋转，Z 轴朝向目标（保持 X 和 Z 轴旋转不变）
        /// </summary>
        public static void LookAtYAxis(this Transform transform, Vector3 targetPosition)
        {
            // 计算目标方向（仅水平方向，忽略 Y 轴高度差）
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f; // 忽略 Y 轴差异

            // 如果方向为 0（目标与自身重合），则跳过
            if (direction == Vector3.zero) return;

            // 计算仅绕 Y 轴的旋转
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 保留当前 X 和 Z 轴旋转，仅应用 Y 轴旋转
            Vector3 euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(euler.x, targetRotation.eulerAngles.y, euler.z);
        }

        /// <summary>
        /// 重载：直接传入目标 Transform
        /// </summary>
        public static void LookAtYAxis(this Transform transform, Transform target)
        {
            if (target != null)
                transform.LookAtYAxis(target.position);
        }

        #endregion

        #region Raycast

        /// <summary>
        /// 检测A到B之间是否有障碍物（使用射线检测）
        /// </summary>
        /// <param name="origin">发射点Transform</param>
        /// <param name="target">目标点Transform</param>
        /// <param name="layerMask">障碍物层级（默认为所有层）</param>
        /// <param name="checkLineOfSight">是否检查直接视线（忽略触发器）</param>
        /// <returns>true表示有障碍物，false表示无障碍物</returns>
        public static (bool isHit, Transform hitTf) HasObstacleBetween(this Transform origin, Transform target, LayerMask layerMask = default, bool checkLineOfSight = true)
        {
            if (origin == null || target == null)
            {
                Debug.LogError("Transform参数不能为null！");
                return (false, null);
            }

            Vector3 direction = target.position - origin.position;
            float   distance  = direction.magnitude;

            // 调整射线起点（从A的中心略微偏移，避免自碰撞）
            Vector3 rayStart = origin.position + direction.normalized * 0.1f;

            // 避免浮点误差导致检测到目标本身
            bool isHit = Physics.Raycast(rayStart, direction.normalized, out var hit, distance * 0.99f,layerMask, checkLineOfSight ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide);

            // 调试可视化（仅在Editor中显示）
#if UNITY_EDITOR
            Debug.DrawRay(rayStart, direction.normalized * distance, isHit ? Color.red : Color.green, 1f);
#endif

            return (isHit, hit.transform);
        }
        
        public static bool HasObstacleBetweenWithRadius(this Transform origin, Transform target, float radius, LayerMask layerMask)
        {
            Vector3 direction = target.position - origin.position;
            float   distance  = direction.magnitude;

            RaycastHit[] hits = Physics.SphereCastAll(origin.position, radius, direction.normalized, distance, layerMask, QueryTriggerInteraction.Ignore);

            // 过滤掉目标自身（如果有碰撞器）
            foreach (var hit in hits)
            {
                if (hit.transform != target && hit.transform != origin)
                    return true;
            }
            return false;
        }

        #endregion
    }
}