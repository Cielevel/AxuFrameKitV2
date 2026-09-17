using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.UtilityGroup
{
    /// <summary>
    /// 单个缺失脚本组件条目。
    /// </summary>
    public sealed class MissingScriptEntry
    {
        /// <summary>组件所在 GameObject 的层级路径（Prefab 相对其根物体 / 场景相对场景根，如 "Root/Child"）。</summary>
        public string GameObjectPath;

        /// <summary>组件在 GetComponents 结果中的下标。</summary>
        public int ComponentIndex;

        /// <summary>组件归属资产路径：可移除时为当前扫描资产；嵌套实例上时为其源 prefab 路径。</summary>
        public string OwningAssetPath;

        /// <summary>是否可在当前资产上移除（嵌套 prefab 实例上的缺失组件归属源资产，此处不可移除）。</summary>
        public bool Removable;
    }

    /// <summary>
    /// 单个资产的缺失脚本报告（仅含有缺失的资产会出现在扫描结果中）。
    /// </summary>
    public sealed class MissingScriptReport
    {
        /// <summary>资产路径（Assets/...）。</summary>
        public string AssetPath;

        /// <summary>资产类型：<see cref="MissingScriptScanner.TypePrefab"/> / <see cref="MissingScriptScanner.TypeScene"/>。</summary>
        public string AssetType;

        /// <summary>缺失组件条目列表。</summary>
        public readonly List<MissingScriptEntry> Entries = new List<MissingScriptEntry>();

        /// <summary>可在当前资产上直接移除的缺失组件数。</summary>
        public int RemovableCount
        {
            get
            {
                int n = 0;
                foreach (var entry in Entries)
                    if (entry.Removable) n++;
                return n;
            }
        }

        /// <summary>嵌套 prefab 实例上的缺失组件数（由其源资产负责清理）。</summary>
        public int NestedCount
        {
            get { return Entries.Count - RemovableCount; }
        }
    }

    /// <summary>
    /// 一次扫描的统计摘要（供页面展示）。
    /// </summary>
    public sealed class MissingScriptScanSummary
    {
        /// <summary>实际扫描（未命中缓存）的资产数。</summary>
        public int ScannedCount;

        /// <summary>命中会话缓存而跳过的资产数。</summary>
        public int CacheHitCount;

        /// <summary>总耗时（秒）。</summary>
        public double ElapsedSeconds;

        /// <summary>是否被外部取消（此时结果可能不完整）。</summary>
        public bool Cancelled;
    }

    /// <summary>
    /// 一次清扫的统计摘要（供页面展示）。
    /// </summary>
    public sealed class MissingScriptRemoveSummary
    {
        /// <summary>实际处理（发生移除并落盘/保存）的资产数。</summary>
        public int ProcessedAssetCount;

        /// <summary>移除的缺失组件总数。</summary>
        public int RemovedCount;

        /// <summary>跳过的资产数（无可移除项、资产缺失或处理失败）。</summary>
        public int SkippedCount;

        /// <summary>已打开场景中发生移除但未保存（仅标脏，需用户手动保存）的场景数。</summary>
        public int ModifiedOpenSceneCount;
    }

    /// <summary>
    /// 缺脚本扫描器（纯静态逻辑，无 UI）：扫描工程 Assets 下 Prefab / Scene 资产中的 Missing Script
    /// （GetComponents 结果中的 null 条目；Unity 6000.0.23f1 实测为真 null，旧版为 fake-null，
    /// 与官方计数 API GameObjectUtility.GetMonoBehavioursWithMissingScriptCount 口径一致），
    /// 输出资产级报告与逐组件条目；
    /// 支持会话级"干净缓存"（依赖 hash 未变且上次干净则跳过）与一键移除
    /// （Prefab 经 LoadPrefabContents → GameObjectUtility.RemoveMonoBehavioursWithMissingScript → SaveAsPrefabAsset 直接落盘，
    /// 已打开场景就地移除后仅标脏不保存（防静默提交用户无关未保存修改），未打开场景 Additive 打开后保存再关闭）。
    /// KitBox 缺脚本清扫器页面与单元测试共用；Packages/ 下资产一律不扫描。
    /// </summary>
    public static class MissingScriptScanner
    {
        // ───────────────────── 常量 ─────────────────────

        /// <summary>资产类型标识：Prefab。</summary>
        public const string TypePrefab = "Prefab";

        /// <summary>资产类型标识：Scene。</summary>
        public const string TypeScene = "Scene";

        /// <summary>默认扫描根目录（folders 为 null/空时兜底）。</summary>
        private const string DefaultFolder = "Assets";

        // ───────────────────── 会话缓存 ─────────────────────

        private sealed class CacheEntry
        {
            public Hash128 Hash;
            public bool Clean;
        }

        /// <summary>资产路径 → (依赖 hash, 上次是否干净)。仅当"hash 未变且上次干净"时跳过扫描。</summary>
        private static readonly Dictionary<string, CacheEntry> CleanCache =
            new Dictionary<string, CacheEntry>(StringComparer.Ordinal);

        /// <summary>清空会话"干净缓存"（下次扫描全量重扫）。</summary>
        public static void ClearCache()
        {
            CleanCache.Clear();
        }

        // ───────────────────── 扫描 ─────────────────────

        /// <summary>
        /// 扫描指定目录下的 Prefab / Scene 资产，返回含有 Missing Script 的资产报告（仅含有缺失的资产）。
        /// folders 仅接受 "Assets" 与 "Assets/..."（统一斜杠、去尾斜杠），null/空列表时兜底默认 "Assets"，
        /// Packages/ 等其他输入一律过滤不扫描（全部无效时返回空结果，不兜底全扫）。
        /// summary 输出统计（实际扫描数 / 缓存命中数 / 耗时 / 是否取消）；progress 回调 (当前序号[1 起], 总数, 资产路径)；
        /// cancelled 回调返回 true 时中断扫描（summary.Cancelled 置真，已扫部分照常返回）。
        /// 会话缓存：以资产依赖 hash 为键记录"上次干净"结论，hash 未变且上次干净则跳过；上次有缺失的资产始终重扫。
        /// </summary>
        public static List<MissingScriptReport> Scan(
            IList<string> folders,
            bool includePrefabs,
            bool includeScenes,
            out MissingScriptScanSummary summary,
            Action<int, int, string> progress = null,
            Func<bool> cancelled = null)
        {
            var watch = Stopwatch.StartNew();
            summary = new MissingScriptScanSummary();
            var results = new List<MissingScriptReport>();

            var normalized = NormalizeFolders(folders);
            if (normalized.Count == 0 || (!includePrefabs && !includeScenes))
            {
                summary.ElapsedSeconds = watch.Elapsed.TotalSeconds;
                return results;
            }

            // 收集目标资产（路径 → 类型），按路径排序保证进度与结果顺序稳定
            var targets = new List<KeyValuePair<string, string>>();
            if (includePrefabs) CollectTargets("t:Prefab", TypePrefab, normalized, targets);
            if (includeScenes) CollectTargets("t:Scene", TypeScene, normalized, targets);
            targets.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

            for (int i = 0; i < targets.Count; i++)
            {
                // 外部取消（页面可取消进度条）则中断
                if (cancelled != null && cancelled())
                {
                    summary.Cancelled = true;
                    break;
                }

                var target = targets[i];
                string path = target.Key;
                if (progress != null) progress(i + 1, targets.Count, path);

                // 会话缓存：hash 未变且上次"干净"则跳过（上次"有缺失"的资产始终重扫）
                var hash = AssetDatabase.GetAssetDependencyHash(path);
                if (CleanCache.TryGetValue(path, out var cached) && cached.Clean && cached.Hash == hash)
                {
                    summary.CacheHitCount++;
                    continue;
                }

                bool clean;
                try
                {
                    var report = target.Value == TypePrefab ? ScanPrefab(path) : ScanScene(path);
                    clean = report == null;
                    if (report != null) results.Add(report);
                }
                catch (Exception ex)
                {
                    // 单个资产失败不中断整体；按"不干净"记缓存，下次仍会重扫
                    UnityEngine.Debug.LogWarning($"[缺脚本清扫器] 资产扫描失败（下次仍会重扫）: {path}\n{ex.Message}");
                    clean = false;
                }

                CleanCache[path] = new CacheEntry { Hash = hash, Clean = clean };
                summary.ScannedCount++;
            }

            summary.ElapsedSeconds = watch.Elapsed.TotalSeconds;
            return results;
        }

        // ───────────────────── 清扫 ─────────────────────

        /// <summary>
        /// 按报告逐资产移除"可移除"的缺失脚本组件（执行时在资产内重新检测，不依赖扫描时的瞬时下标）：
        /// Prefab → LoadPrefabContents → GameObjectUtility.RemoveMonoBehavioursWithMissingScript → SaveAsPrefabAsset 直接落盘 → Unload；
        /// 已打开场景 → 官方移除 API + 仅 MarkSceneDirty（不 SaveScene，避免把用户场景里无关的未保存修改一并静默提交，
        /// 统计的 ModifiedOpenSceneCount 提示用户手动保存）；未打开场景 → Additive 打开 → 移除 → SaveScene → 关闭（不重复保存）。
        /// 嵌套 prefab 实例上的缺失组件（Removable=false）不在当前资产移除，由其源资产负责。
        /// 返回统计（处理资产数 / 移除处数 / 跳过数 / 已打开场景修改未保存数）；处理过的资产会清除会话缓存结论，下次扫描全量重检。
        /// </summary>
        public static MissingScriptRemoveSummary RemoveMissingScripts(
            List<MissingScriptReport> reports,
            Action<int, int, string> progress = null)
        {
            var summary = new MissingScriptRemoveSummary();
            if (reports == null || reports.Count == 0) return summary;

            for (int i = 0; i < reports.Count; i++)
            {
                var report = reports[i];
                if (progress != null) progress(i + 1, reports.Count, report.AssetPath);

                // 清除该资产的缓存结论，强制下次全量重扫
                CleanCache.Remove(report.AssetPath);

                if (report.RemovableCount == 0 || AssetDatabase.LoadMainAssetAtPath(report.AssetPath) == null)
                {
                    summary.SkippedCount++;
                    continue;
                }

                try
                {
                    // modifiedOpenScene：该资产是"已打开场景"且发生了移除（未落盘，需用户手动保存）
                    bool modifiedOpenScene = false;
                    int removed = report.AssetType == TypePrefab
                        ? RemoveFromPrefab(report.AssetPath)
                        : RemoveFromScene(report.AssetPath, out modifiedOpenScene);

                    if (removed > 0)
                    {
                        summary.ProcessedAssetCount++;
                        summary.RemovedCount += removed;
                        if (modifiedOpenScene) summary.ModifiedOpenSceneCount++;
                    }
                    else
                    {
                        summary.SkippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[缺脚本清扫器] 清扫失败: {report.AssetPath}\n{ex}");
                    summary.SkippedCount++;
                }
            }

            return summary;
        }

        // ───────────────────── 内部：扫描 ─────────────────────

        /// <summary>扫描单个 prefab：LoadPrefabContents 临时加载，遍历检测后必定 Unload（不落盘）。</summary>
        private static MissingScriptReport ScanPrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var report = new MissingScriptReport { AssetPath = path, AssetType = TypePrefab };
                WalkHierarchy(root, root.name, (go, goPath) => DetectMissingComponents(go, goPath, path, report));
                return report.Entries.Count > 0 ? report : null;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>扫描单个场景：已打开则直接遍历；未打开则 Additive 打开、扫完即关（不保存）。</summary>
        private static MissingScriptReport ScanScene(string path)
        {
            var scene = EditorSceneManager.GetSceneByPath(path);
            bool wasOpen = scene.IsValid() && scene.isLoaded;
            if (!wasOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            try
            {
                var report = new MissingScriptReport { AssetPath = path, AssetType = TypeScene };
                foreach (var rootGo in scene.GetRootGameObjects())
                    WalkHierarchy(rootGo, rootGo.name, (go, goPath) => DetectMissingComponents(go, goPath, path, report));
                return report.Entries.Count > 0 ? report : null;
            }
            finally
            {
                if (!wasOpen && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>检测单个 GameObject 上的缺失脚本组件，写入报告。</summary>
        private static void DetectMissingComponents(GameObject go, string goPath, string assetPath, MissingScriptReport report)
        {
            // 嵌套 prefab 实例上的物体：缺失组件归属其源资产，此处仅记录为不可移除条目
            bool nested = TryGetPrefabSourcePath(go, out string sourcePath);

            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                var comp = comps[i];
                // 关键判定：GetComponents 对缺失脚本槽位返回 null 条目——数组对有效组件绝不返回 null，
                // 故 == null 即缺失脚本。Unity 6000.0.23f1 实测该条目为"真 null"（ReferenceEquals 亦为 null），
                // 社区流传的 fake-null 判定（额外要求 ReferenceEquals 非 null）在本版本恒为 false，勿用。
                if (comp == null)
                {
                    report.Entries.Add(new MissingScriptEntry
                    {
                        GameObjectPath = goPath,
                        ComponentIndex = i,
                        OwningAssetPath = nested ? sourcePath : assetPath,
                        Removable = !nested,
                    });
                }
            }
        }

        /// <summary>深度优先遍历层级（含未激活物体），visit 参数为 (物体, 层级路径)。</summary>
        private static void WalkHierarchy(GameObject go, string path, Action<GameObject, string> visit)
        {
            visit(go, path);
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                WalkHierarchy(child.gameObject, path + "/" + child.name, visit);
            }
        }

        /// <summary>该物体是否为嵌套 prefab 实例的一部分；是则输出其源 prefab 资产路径。</summary>
        private static bool TryGetPrefabSourcePath(GameObject go, out string sourcePath)
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (src == null)
            {
                sourcePath = null;
                return false;
            }

            sourcePath = AssetDatabase.GetAssetPath(src);
            return true;
        }

        // ───────────────────── 内部：清扫 ─────────────────────

        /// <summary>移除单个 prefab 上的可移除缺失组件（有移除才 SaveAsPrefabAsset 落盘）。</summary>
        private static int RemoveFromPrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int removed = RemoveMissingInHierarchy(root);
                if (removed > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                return removed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 移除单个场景上的可移除缺失组件。
        /// 已打开场景：只移除 + MarkSceneDirty，不 SaveScene——保存会把用户在该场景里的
        /// 全部无关未保存修改一并静默提交，故交由用户确认后手动保存（modifiedOpenScene 置真供上层提示）；
        /// 未打开场景：保持 Additive 打开 → 移除 → SaveScene 落盘 → 关闭 的既有流程。
        /// </summary>
        private static int RemoveFromScene(string path, out bool modifiedOpenScene)
        {
            var scene = EditorSceneManager.GetSceneByPath(path);
            bool wasOpen = scene.IsValid() && scene.isLoaded;
            if (!wasOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            modifiedOpenScene = false;
            try
            {
                int removed = 0;
                foreach (var rootGo in scene.GetRootGameObjects())
                    removed += RemoveMissingInHierarchy(rootGo);

                if (removed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (wasOpen)
                        modifiedOpenScene = true; // 已打开场景仅标脏，等用户手动保存
                    else
                        EditorSceneManager.SaveScene(scene);
                }
                return removed;
            }
            finally
            {
                if (!wasOpen && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>遍历层级移除全部可移除的缺失脚本组件，返回移除数（嵌套实例上的不动）。</summary>
        private static int RemoveMissingInHierarchy(GameObject root)
        {
            int removed = 0;
            WalkHierarchy(root, root.name, (go, goPath) =>
            {
                // 嵌套 prefab 实例上的缺失组件归属源资产，不在本资产移除
                if (TryGetPrefabSourcePath(go, out _)) return;

                // 官方移除 API：按移除本物体上全部缺失脚本并返回数量。Unity 6000.0.23f1 缺失槽位在
                // GetComponents 中为真 null，无法 DestroyImmediate（真 null 传参直接抛异常），必须走此 API
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            });
            return removed;
        }

        // ───────────────────── 内部：目录与目标收集 ─────────────────────

        /// <summary>
        /// 归一化输入目录：统一斜杠、去空白与尾斜杠，仅保留 "Assets" 与 "Assets/..."；
        /// null/空列表兜底 ["Assets"]，全部无效时返回空列表（语义：什么都不扫，而非兜底全扫）。
        /// </summary>
        private static List<string> NormalizeFolders(IList<string> folders)
        {
            var result = new List<string>();
            if (folders == null || folders.Count == 0)
            {
                result.Add(DefaultFolder);
                return result;
            }

            foreach (var raw in folders)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var folder = raw.Trim().Replace('\\', '/').TrimEnd('/');
                if (folder.Length == 0) continue;
                if (folder == DefaultFolder || folder.StartsWith(DefaultFolder + "/", StringComparison.Ordinal))
                    result.Add(folder);
            }
            return result;
        }

        /// <summary>按 FindAssets 查询收集目标资产路径（仅 Assets/ 下），追加到 targets（路径 → 类型）。</summary>
        private static void CollectTargets(string query, string assetType, List<string> folders, List<KeyValuePair<string, string>> targets)
        {
            foreach (var guid in AssetDatabase.FindAssets(query, folders.ToArray()))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                targets.Add(new KeyValuePair<string, string>(path, assetType));
            }
        }
    }
}
