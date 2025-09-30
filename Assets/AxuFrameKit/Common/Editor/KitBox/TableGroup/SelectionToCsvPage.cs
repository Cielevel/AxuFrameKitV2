using System;
using System.IO;
using System.Linq;
using AxuFrameKit._Core.Extension;
using AxuFrameKit.Common.Editor.Utility.IOX;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Editor.KitBox.TableGroup
{
    public class SelectionToCsvPage : IKitPage
    {
        public string Title => "选项名称生成csv";
        
        private string CsvTitle => $"资产名称,资产路径,资产加载路径,GUID{Environment.NewLine}assetName,assetPath,assetLoadPath,assetGuid{Environment.NewLine}string,string,string,string";

        private string _folderPath = Application.dataPath;

        private string _fileName = "CsvAsset.csv";

        private Vector2 _scrollPosition;

        public void Show()
        {
            GUIStyle titleLabelLayout = new()
            {
                fontSize = 15,
                normal = new GUIStyleState
                {
                    textColor = Color.white
                },
            };
            
            GUI.backgroundColor = Color.black;
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField(_folderPath);
            if (GUILayout.Button("📂", GUILayout.Width(40)))
            {
                _folderPath = EditorUtility.OpenFolderPanel("选择一个保存路径", "", _fileName);
            }
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.LabelField("当前选中的资产", titleLabelLayout);
            GUI.backgroundColor = Color.green;
            _scrollPosition     = EditorGUILayout.BeginScrollView(_scrollPosition, "box", GUILayout.Height(300));
            string[] selectedPaths = Selection.assetGUIDs;
            for (int i = 0; i < selectedPaths.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(selectedPaths[i]);
                EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<Object>(path), typeof(Object), false);
            }
            EditorGUILayout.EndScrollView();
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("导出非文件夹的资产名称到Csv"))
            {
                ExportSelectedAssets();
            }
            
            EditorGUILayout.LabelField("增量导出时，如果目标路径已有csv，将在末尾继续增加内容，而不会覆盖");
            if (GUILayout.Button("增量导出"))
            {

            }
        }

        private void ExportSelectedAssets()
        {
            // 获取选中的资产
            var selectedAssets = Selection.objects.Where(obj => obj != null).ToList();
            
            if (selectedAssets.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择至少一个非文件夹的资产！", "OK");
                return;
            }
            
            foreach (var asset in selectedAssets)
            {
                if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(asset)))
                {
                    EditorUtility.DisplayDialog("提示", "请不要选择文件夹！", "确认");
                    return;
                }
            }
            
            string suffix = Path.GetExtension(_fileName).Equals(".csv") ? "" : ".csv";

            string path = (_folderPath + "/" + _fileName + suffix).MakeSureFilePath();
            
            if (string.IsNullOrEmpty(path))
                return;

            var csvUnits = selectedAssets.Select(e => new CsvRowUnit(e.name, AssetDatabase.GetAssetPath(e), AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e))));

            if (CsvIOUtil.WriteCsv(path, CsvTitle, csvUnits))
                EditorUtility.DisplayDialog("成功", "已查重并导出到文件！", "OK");
            else
                EditorUtility.DisplayDialog("错误", $"导出失败", "OK");

            AssetDatabase.Refresh();
        }

        public void Refresh() { }
    }
}