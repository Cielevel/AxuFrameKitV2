using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.UtilityGroup
{
    /// <summary>
    /// 多场景组合预设：一个命名组合对应一组场景路径。
    /// ScenePaths 顺序即 Additive 叠加顺序，第一个元素为激活场景。
    /// </summary>
    [Serializable]
    public class SceneSetPreset
    {
        public string Name;
        public string[] ScenePaths;
    }

    /// <summary>落盘文件根对象（JsonUtility 序列化）。</summary>
    [Serializable]
    public class SceneSetPresetFile
    {
        public List<SceneSetPreset> Presets = new List<SceneSetPreset>();
    }

    /// <summary>
    /// 多场景组合预设的存储/校验/恢复纯逻辑层（无 UI）。
    /// JSON 落盘 ProjectSettings/AxuSceneSets.json：随工程走、不入 Assets（不产生资产导入），
    /// ProjectSettings 目录本身正常入库，组合预设可随版本库团队共享。
    /// 所有存取方法提供可选 path 参数（默认 DefaultPath），测试用临时路径注入。
    /// 文件损坏时把原文件改名 .bak 后从空档重建，不向上抛异常。
    /// </summary>
    public static class SceneSetPresetStore
    {
        /// <summary>默认存档路径（相对 Unity 进程工作目录，即工程根）。</summary>
        public const string DefaultPath = "ProjectSettings/AxuSceneSets.json";

        #region 存取

        /// <summary>读取预设列表。文件不存在或损坏时返回空列表（损坏文件先备份为 .bak）。</summary>
        public static List<SceneSetPreset> LoadPresets(string path = null)
        {
            path = path ?? DefaultPath;
            try
            {
                if (!File.Exists(path))
                    return new List<SceneSetPreset>();

                var file = JsonUtility.FromJson<SceneSetPresetFile>(File.ReadAllText(path));
                if (file == null || file.Presets == null)
                {
                    // 反序列化结果残缺同样视同损坏（防御 JsonUtility 宽松解析出空壳对象）
                    BackupCorruptedFile(path);
                    return new List<SceneSetPreset>();
                }

                return file.Presets;
            }
            catch (Exception)
            {
                // JSON 非法（如手改坏）等读取异常：备份原文件后从空档重建，不向上抛
                BackupCorruptedFile(path);
                return new List<SceneSetPreset>();
            }
        }

        /// <summary>写入预设列表（整文件覆盖式保存）。目录不存在时自动创建；presets 为 null 视为空列表。</summary>
        public static void SavePresets(List<SceneSetPreset> presets, string path = null)
        {
            path = path ?? DefaultPath;

            var dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var file = new SceneSetPresetFile { Presets = presets ?? new List<SceneSetPreset>() };
            File.WriteAllText(path, JsonUtility.ToJson(file, true));
        }

        /// <summary>损坏文件备份：改名 *.bak（旧备份直接覆盖）。备份失败静默，不阻断"从空档重建"。</summary>
        private static void BackupCorruptedFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                var bak = path + ".bak";
                if (File.Exists(bak))
                    File.Delete(bak);

                File.Move(path, bak);
            }
            catch (Exception)
            {
                // 备份本身失败也静默：目标是"Load 永远可用"，不能因备份动作卡死
            }
        }

        #endregion

        #region 当前编辑器场景状态

        /// <summary>
        /// 把当前打开的场景集合（EditorSceneManager.GetSceneManagerSetup）转为预设。
        /// 保持打开顺序，并把激活场景稳定排到首位（契约：ScenePaths[0] 即激活场景）；
        /// Prefab Stage 子场景与从未保存过的临时场景（无资产路径，无法恢复）会被跳过。
        /// </summary>
        public static SceneSetPreset CaptureCurrentSetup()
        {
            var paths = new List<string>();
            string activePath = null;

            foreach (var setup in EditorSceneManager.GetSceneManagerSetup())
            {
                if (setup.isSubScene)
                    continue; // Prefab Stage 子场景不属于工程场景组合

                if (string.IsNullOrEmpty(setup.path))
                    continue; // 未保存过的临时场景没有资产路径，恢复不了

                if (setup.isActive)
                    activePath = setup.path;

                paths.Add(setup.path);
            }

            if (!string.IsNullOrEmpty(activePath) && paths.Count > 0 && paths[0] != activePath)
            {
                paths.Remove(activePath);
                paths.Insert(0, activePath);
            }

            return new SceneSetPreset { Name = string.Empty, ScenePaths = paths.ToArray() };
        }

        /// <summary>返回当前有未保存修改的场景名列表（供 UI 层在恢复前弹确认框）。</summary>
        public static List<string> GetDirtyOpenSceneNames()
        {
            var names = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                    names.Add(scene.name);
            }

            return names;
        }

        #endregion

        #region 校验与恢复

        /// <summary>
        /// 校验预设：返回其中已不存在的场景路径列表（空列表 = 全部可用）。
        /// 判定为"存在"需双重确认：物理文件在（File.Exists）且资产库识别为 SceneAsset。
        /// </summary>
        public static List<string> ValidatePreset(SceneSetPreset preset)
        {
            var missing = new List<string>();
            if (preset == null || preset.ScenePaths == null)
                return missing;

            foreach (var path in preset.ScenePaths)
            {
                if (!ScenePathExists(path))
                    missing.Add(path);
            }

            return missing;
        }

        /// <summary>
        /// 恢复预设：关闭当前场景集合并按预设重建（缺失路径跳过，全部缺失则失败返回 false）。
        /// 优先走 EditorSceneManager.RestoreSceneManagerSetup —— 直接按 setup 数组重建，
        /// 数组第一个场景即激活场景（Unity 6 的 SceneSetup 直接暴露可写 string path）。
        /// 注意其不会自动保存任何未保存场景，恢复会丢弃未存修改，
        /// 须由 UI 层先经 GetDirtyOpenSceneNames 弹确认后再调用。
        /// </summary>
        public static bool ApplyPreset(SceneSetPreset preset)
        {
            if (preset == null || preset.ScenePaths == null || preset.ScenePaths.Length == 0)
                return false;

            var setups = new List<SceneSetup>();
            foreach (var path in preset.ScenePaths)
            {
                if (!ScenePathExists(path))
                    continue; // 缺失场景跳过而非整体失败（UI 层恢复前已弹过缺失提醒）

                setups.Add(new SceneSetup { path = path, isLoaded = true, isActive = setups.Count == 0 });
            }

            if (setups.Count == 0)
                return false;

            EditorSceneManager.RestoreSceneManagerSetup(setups.ToArray());
            return true;
        }

        /// <summary>场景路径双重存在性校验：物理文件 + 资产库 SceneAsset。</summary>
        private static bool ScenePathExists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (!File.Exists(path))
                return false; // 物理层面（相对 Unity 工作目录 = 工程根）

            // 资产库层面：确为已导入识别的 SceneAsset（排除未导入/非法文件）
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
        }

        #endregion
    }
}
