using System;
using System.Collections.Generic;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>
    /// 压力对象生成器（stress 能力共用）：按计数增删实体并跟踪总量。
    /// prefab 解析语义：prefabPath 为 Resources 相对路径（不含 "Resources/" 前缀与扩展名），
    /// Editor 与 Player 一致——Player 侧只能加载随构建打包的 Resources 资产；
    /// 路径为空或加载失败时回落内置 Primitive Cube 替身（模板去除 Collider，避免大量重叠碰撞体
    /// 把压力测试变成物理测试），无任何游戏资产也能跑。
    /// </summary>
    public class StressSpawner
    {
        const int GridSide = 32;

        readonly IAgentLog _log;
        readonly List<GameObject> _spawned = new List<GameObject>(256);
        readonly string _prefabPath;
        GameObject _template;
        bool _templateResolved;
        bool _ownsTemplate;

        /// <summary>累计实例化数（跨 Clear 保留，churn/spawner 总量指标来源）。</summary>
        public long InstantiatedTotal { get; private set; }

        /// <summary>累计销毁数（跨 Clear 保留）。</summary>
        public long DestroyedTotal { get; private set; }

        /// <summary>当前存活实体数。</summary>
        public int CurrentCount => _spawned.Count;

        /// <summary>历史峰值并发实体数。</summary>
        public int MaxConcurrent { get; private set; }

        /// <param name="prefabPath">Resources 相对路径（空 = 内置 Primitive 替身）。</param>
        /// <param name="log">宿主日志（可空）。</param>
        public StressSpawner(string prefabPath, IAgentLog log)
        {
            _prefabPath = prefabPath;
            _log = log;
        }

        /// <summary>增补实体到 count 个（增量实例化，网格散布防重叠）。</summary>
        public void Add(int count)
        {
            if (count <= 0) return;

            var template = ResolveTemplate();
            if (template == null) return;

            for (var i = 0; i < count; i++)
            {
                var index = _spawned.Count;
                var clone = UnityEngine.Object.Instantiate(template,
                    GridPosition(index), Quaternion.identity);
                clone.name = "AxuStress_Clone";
                clone.SetActive(true);
                _spawned.Add(clone);
                InstantiatedTotal++;
            }

            if (_spawned.Count > MaxConcurrent) MaxConcurrent = _spawned.Count;
        }

        /// <summary>把实体数削减到 target（从尾部销毁；target ≥ 当前数时 no-op）。</summary>
        public void RemoveDownTo(int target)
        {
            if (target < 0) target = 0;
            for (var i = _spawned.Count - 1; i >= target; i--)
            {
                var go = _spawned[i];
                if (go != null) UnityEngine.Object.Destroy(go);
                DestroyedTotal++;
            }

            if (target == 0) _spawned.Clear();
            else if (_spawned.Count > target) _spawned.RemoveRange(target, _spawned.Count - target);
        }

        /// <summary>清场（销毁全部实体；模板保留供复用）。</summary>
        public void Clear()
        {
            RemoveDownTo(0);
        }

        /// <summary>释放模板等自有资源（phase 结束时调用；幂等）。</summary>
        public void Dispose()
        {
            Clear();
            if (_template != null && _ownsTemplate)
            {
                // 仅销毁自建的 Primitive 替身模板；Resources.Load 来的是资产，Destroy 资产不被允许
                UnityEngine.Object.Destroy(_template);
            }
            _template = null;
            _templateResolved = false;
            _ownsTemplate = false;
        }

        // -------------------------------------------------------------- 内部

        GameObject ResolveTemplate()
        {
            if (_templateResolved) return _template;
            _templateResolved = true;

            if (!string.IsNullOrWhiteSpace(_prefabPath))
            {
                try
                {
                    _template = Resources.Load<GameObject>(_prefabPath.Trim());
                }
                catch (Exception ex)
                {
                    _log?.Warning($"[StressSpawner] prefab 加载异常（{_prefabPath}）：{ex.Message}，回落内置替身");
                    _template = null;
                }

                if (_template == null)
                    _log?.Warning($"[StressSpawner] prefab 加载失败：Resources/{_prefabPath}（Player 侧须随构建打包），回落内置 Primitive 替身");
            }

            if (_template == null)
            {
                // 内置替身模板：Primitive Cube 克隆一份、去 Collider、隐藏待实例化
                _template = UnityEngine.Object.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube));
                _template.name = "AxuStress_PrimitiveTemplate";
                var collider = _template.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.Destroy(collider);
                _template.SetActive(false);
                _ownsTemplate = true;
            }

            return _template;
        }

        static Vector3 GridPosition(int index)
        {
            var x = index % GridSide;
            var z = (index / GridSide) % GridSide;
            var y = index / (GridSide * GridSide);
            return new Vector3((x - GridSide * 0.5f) * 2f, y * 2f, (z - GridSide * 0.5f) * 2f);
        }
    }
}
