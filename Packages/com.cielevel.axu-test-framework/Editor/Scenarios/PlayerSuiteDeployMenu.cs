using System.IO;
using UnityEditor;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// suite JSON → StreamingAssets 一键部署菜单（方案 §3.3：Player 形态经
    /// <c>StreamingAssets/AxuSuites/</c> 发现套件）。复制走纯文件 IO（StreamingAssets 目标目录
    /// 在 Assets 下，落盘后统一 <see cref="AssetDatabase.Refresh"/> 生成 .meta；已存在则覆盖）。
    /// </summary>
    public static class PlayerSuiteDeployMenu
    {
        /// <summary>部署目标目录（Assets 下，Player 侧经 StreamingAssets 读取）。</summary>
        public const string DeployFolder = "Assets/StreamingAssets/AxuSuites";

        private const string DeployMenuPath = "AxuTestFramework/Deploy Suite To StreamingAssets";
        private const string RemoveMenuPath = "AxuTestFramework/Remove Deployed Suites";

        [MenuItem(DeployMenuPath, false)]
        public static void DeploySuite()
        {
            var start = AssetDatabase.IsValidFolder(ScenarioJsonMenu.ScenariosFolder)
                ? ScenarioJsonMenu.ScenariosFolder
                : "Assets";
            var selected = EditorUtility.OpenFilePanel("选择要部署的 Suite JSON", start, "json");
            if (string.IsNullOrEmpty(selected) || !File.Exists(selected))
                return;

            var deployFullPath = ToFullPath(DeployFolder);
            if (!Directory.Exists(deployFullPath))
                Directory.CreateDirectory(deployFullPath);

            var destination = Path.Combine(deployFullPath, Path.GetFileName(selected));
            File.Copy(selected, destination, overwrite: true);
            AssetDatabase.Refresh();

            Debug.Log($"[AxuTestFramework] Suite 已部署到 StreamingAssets: {destination}（覆盖旧文件如有）。");
            EditorUtility.DisplayDialog("Deploy Suite", $"已部署：\n{destination}", "好");
        }

        [MenuItem(RemoveMenuPath, false)]
        public static void RemoveDeployed()
        {
            var deployFullPath = ToFullPath(DeployFolder);
            if (!Directory.Exists(deployFullPath) ||
                Directory.GetFiles(deployFullPath, "*.json", SearchOption.TopDirectoryOnly).Length == 0)
            {
                EditorUtility.DisplayDialog("Remove Deployed Suites", "StreamingAssets/AxuSuites 下没有已部署的 suite JSON。", "好");
                return;
            }

            if (!EditorUtility.DisplayDialog("Remove Deployed Suites",
                $"将删除 {DeployFolder} 下的全部 suite JSON（目录本身保留）。继续？", "删除", "取消"))
                return;

            foreach (var file in Directory.GetFiles(deployFullPath, "*.json", SearchOption.TopDirectoryOnly))
                File.Delete(file);
            AssetDatabase.Refresh();

            Debug.Log("[AxuTestFramework] 已清理 StreamingAssets/AxuSuites 下的部署文件。");
        }

        /// <summary>相对工程根路径 → 完整物理路径（复用报告路径工具的根推导）。</summary>
        private static string ToFullPath(string relativePath)
        {
            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(AxuTestReportPaths.ProjectRootFullPath, normalized);
        }
    }
}
