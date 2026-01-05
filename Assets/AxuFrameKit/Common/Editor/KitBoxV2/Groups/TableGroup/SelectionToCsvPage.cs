using System;
using System.IO;
using System.Linq;
using AxuFrameKit._Core.Extension;
using AxuFrameKit.Common.Editor.KitBoxV2.Core;
using AxuFrameKit.Common.Editor.Utility.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Groups.TableGroup
{
    /// <summary>
    /// 选项名称生成 CSV 页面
    /// </summary>
    public class SelectionToCsvPage : IKitPageV2
    {
        public string Title => "资产导出CSV";
        
        private IKitBoxV2 _kitBox;
        
        private string CsvTitle => $"资产名称,资产路径,资产加载路径,GUID{Environment.NewLine}assetName,assetPath,assetLoadPath,assetGuid{Environment.NewLine}string,string,string,string";
        
        private string _folderPath = Application.dataPath;
        private string _fileName = "CsvAsset.csv";
        private Vector2 _scrollPosition;
        
        public void Init(IKitBoxV2 kitBox)
        {
            _kitBox = kitBox;
        }
        
        public void Draw()
        {
            KitBoxStyles.DrawSectionHeader("选中资产导出为 CSV");
            
            // 保存路径设置
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("📁 保存路径", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            _folderPath = EditorGUILayout.TextField(_folderPath);
            
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
            if (GUILayout.Button("浏览...", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel("选择保存路径", _folderPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    _folderPath = selected;
                }
            }
            GUI.backgroundColor = originalBg;
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("文件名：", GUILayout.Width(50));
            _fileName = EditorGUILayout.TextField(_fileName);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // 当前选中的资产列表
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("📦 当前选中的资产", EditorStyles.boldLabel);
            
            string[] selectedGuids = Selection.assetGUIDs;
            
            if (selectedGuids.Length == 0)
            {
                EditorGUILayout.HelpBox("请在 Project 窗口中选择要导出的资产", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"已选择 {selectedGuids.Length} 个资产", EditorStyles.miniLabel);
                
                GUILayout.Space(5);
                
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
                
                foreach (var guid in selectedGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    // 图标和名称
                    Texture icon = AssetDatabase.GetCachedIcon(path);
                    GUIContent content = new GUIContent(" " + (obj != null ? obj.name : "(Missing)"), icon);
                    
                    if (GUILayout.Button(content, EditorStyles.label, GUILayout.Height(20), GUILayout.ExpandWidth(true)))
                    {
                        if (obj != null)
                        {
                            EditorGUIUtility.PingObject(obj);
                        }
                    }
                    
                    // 类型标签
                    string typeName = obj != null ? obj.GetType().Name : "Unknown";
                    GUIStyle typeStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                    };
                    EditorGUILayout.LabelField(typeName, typeStyle, GUILayout.Width(80));
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(15);
            
            // 导出按钮
            EditorGUI.BeginDisabledGroup(selectedGuids.Length == 0);
            
            originalBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.4f);
            
            if (GUILayout.Button("📤 导出非文件夹资产到 CSV", GUILayout.Height(35)))
            {
                ExportSelectedAssets();
            }
            
            GUI.backgroundColor = originalBg;
            EditorGUI.EndDisabledGroup();
            
            GUILayout.Space(5);
            
            EditorGUILayout.HelpBox("增量导出时，如果目标路径已有 CSV，将在末尾继续增加内容，而不会覆盖。", MessageType.Info);
            
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.7f);
            if (GUILayout.Button("📥 增量导出", GUILayout.Height(28)))
            {
                // TODO: 实现增量导出
                EditorUtility.DisplayDialog("提示", "增量导出功能开发中...", "确定");
            }
            GUI.backgroundColor = originalBg;
        }
        
        private void ExportSelectedAssets()
        {
            var selectedAssets = Selection.objects.Where(obj => obj != null).ToList();
            
            if (selectedAssets.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择至少一个非文件夹的资产！", "确定");
                return;
            }
            
            // 检查是否有文件夹
            foreach (var asset in selectedAssets)
            {
                if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(asset)))
                {
                    EditorUtility.DisplayDialog("提示", "请不要选择文件夹！", "确定");
                    return;
                }
            }
            
            string suffix = Path.GetExtension(_fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase) ? "" : ".csv";
            string path = (_folderPath + "/" + _fileName + suffix).MakeSureFilePath();
            
            if (string.IsNullOrEmpty(path))
                return;
            
            var csvUnits = selectedAssets.Select(e => new CsvRowUnit(
                e.name, 
                AssetDatabase.GetAssetPath(e), 
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e))
            ));
            
            if (CsvIOUtil.WriteCsv(path, CsvTitle, csvUnits))
            {
                EditorUtility.DisplayDialog("成功", "已查重并导出到文件！", "确定");
                Debug.Log($"[SelectionToCsv] 导出成功：{path}");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "导出失败", "确定");
            }
            
            AssetDatabase.Refresh();
        }
        
        public void Refresh()
        {
            _folderPath = Application.dataPath;
            _fileName = "CsvAsset.csv";
        }
    }
}
