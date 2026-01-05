using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxV2.Core;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Groups.CopierGroup
{
    /// <summary>
    /// 材质球关联复制页面
    /// </summary>
    public class MaterialCopierPage : IKitPageV2
    {
        public string Title => "材质球关联";
        
        private IKitBoxV2 _kitBox;
        private GameObject _goOriginal;
        private GameObject _goTarget;
        private Vector2 _logScrollPos;
        private string _logMessage = "";
        
        public void Init(IKitBoxV2 kitBox)
        {
            _kitBox = kitBox;
        }
        
        public void Draw()
        {
            KitBoxStyles.DrawSectionHeader("单个模型材质球复制");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("选择场景内的模型，确保子物体命名一一对应。", EditorStyles.wordWrappedMiniLabel);
            
            GUILayout.Space(10);
            
            // 原材质模型
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📦 原材质模型", EditorStyles.boldLabel);
            
            if (_goOriginal == null)
            {
                EditorGUILayout.HelpBox("请选择原材质模型", MessageType.Warning);
            }
            
            _goOriginal = (GameObject)EditorGUILayout.ObjectField(_goOriginal, typeof(GameObject), true);
            EditorGUILayout.EndVertical();
            
            // 箭头指示
            GUILayout.Space(5);
            GUIStyle arrowStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 0.7f, 0.9f) }
            };
            EditorGUILayout.LabelField("▼▼▼", arrowStyle, GUILayout.Height(30));
            GUILayout.Space(5);
            
            // 目标模型
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎯 目标模型", EditorStyles.boldLabel);
            
            if (_goTarget == null)
            {
                EditorGUILayout.HelpBox("请选择目标模型", MessageType.Warning);
            }
            
            _goTarget = (GameObject)EditorGUILayout.ObjectField(_goTarget, typeof(GameObject), true);
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(15);
            
            // 复制按钮
            EditorGUI.BeginDisabledGroup(_goOriginal == null || _goTarget == null);
            
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
            
            if (GUILayout.Button("复制材质球关联", GUILayout.Height(35)))
            {
                CopyMaterials();
            }
            
            GUI.backgroundColor = originalBg;
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            // 日志区域
            if (!string.IsNullOrEmpty(_logMessage))
            {
                GUILayout.Space(10);
                KitBoxStyles.DrawSectionHeader("操作日志");
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(120));
                _logScrollPos = EditorGUILayout.BeginScrollView(_logScrollPos);
                
                GUIStyle logStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    richText = true,
                    fontSize = 11
                };
                EditorGUILayout.LabelField(_logMessage, logStyle, GUILayout.ExpandHeight(true));
                
                EditorGUILayout.EndScrollView();
                
                if (GUILayout.Button("清除日志", EditorStyles.miniButton))
                {
                    _logMessage = "";
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void CopyMaterials()
        {
            if (_goOriginal == null || _goTarget == null)
            {
                _logMessage = "<color=red>错误：请先选择原材质模型和目标模型！</color>";
                return;
            }
            
            _logMessage = "";
            int successCount = 0;
            int failCount = 0;
            
            var originalRenderers = _goOriginal.GetComponentsInChildren<Renderer>();
            
            foreach (var renderer in originalRenderers)
            {
                var targetTransform = _goTarget.transform.Find(renderer.gameObject.name);
                
                if (targetTransform == null)
                {
                    _logMessage += $"<color=yellow>⚠ 未找到：{renderer.gameObject.name}</color>\n";
                    failCount++;
                    continue;
                }
                
                var targetRenderer = targetTransform.GetComponent<Renderer>();
                if (targetRenderer != null)
                {
                    targetRenderer.sharedMaterials = renderer.sharedMaterials;
                    _logMessage += $"<color=green>✓ 成功复制：{renderer.gameObject.name}</color>\n";
                    successCount++;
                }
                else
                {
                    _logMessage += $"<color=red>✗ 无Renderer：{renderer.gameObject.name}</color>\n";
                    failCount++;
                }
            }
            
            _logMessage += $"\n<color=cyan>完成！成功：{successCount}，失败：{failCount}</color>";
            
            Debug.Log($"[MaterialCopier] 材质复制完成 - 成功：{successCount}，失败：{failCount}");
        }
        
        public void Refresh()
        {
            _goOriginal = null;
            _goTarget = null;
            _logMessage = "";
        }
    }
}
