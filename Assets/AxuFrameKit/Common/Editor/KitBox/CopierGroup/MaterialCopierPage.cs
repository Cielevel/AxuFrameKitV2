using System.Collections.Generic;
using System.Linq;
using AxuFrameKit.Common.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBox.CopierGroup
{
    public class ModelMaterialCopy : IKitPage
    {
        public string Title => "材质球关联";

        private GameObject _goOriginal;
        private GameObject _goTarget;
        
        public void Show()
        {
            Single();
            return;
            
            void Single()
            {
                TitleDrawerEditor.DrawTitle(Title, Color.white);
                
                EditorGUILayout.LabelField("单个模型材质球复制，选择场景内的模型，保证子物体命名一一对应。");
                
                if (_goOriginal == null)
                    EditorGUILayout.HelpBox("请选择原材质模型", MessageType.Error);
                _goOriginal = (GameObject)EditorGUILayout.ObjectField("原材质模型", _goOriginal, typeof(GameObject), true);
                
                EditorGUILayout.LabelField("▼▼▼▼");
                
                if (_goTarget == null)
                    EditorGUILayout.HelpBox("请选择目标模型", MessageType.Error);
                _goTarget = (GameObject)EditorGUILayout.ObjectField("目标模型", _goTarget, typeof(GameObject), true);

                if (GUILayout.Button("复制材质球关联"))
                {
                    _goOriginal.GetComponentsInChildren<Renderer>().ToList().ForEach(e =>
                    {
                        var targetSk = _goTarget.transform.Find(e.gameObject.name);
                        if (!targetSk)
                            Debug.Log($"无法查询到：{targetSk.name}。");
                        else
                            targetSk.GetComponent<Renderer>().sharedMaterials = e.sharedMaterials;
                    });
                }
            }
        }

        public void Refresh() { }

        public List<GameObject> GoOriginals { get; private set; }
        public List<GameObject> GoTargets { get; private set; }

        public void CopyAll()
        {
            foreach (var goOriginal in GoOriginals)
            {
                var goTarget = GoTargets.Find(e => e.name.Equals(goOriginal.name));

                if (goTarget)
                {
                    goOriginal.GetComponentsInChildren<Renderer>().ToList().ForEach(e =>
                    {
                        var targetSk = goTarget.transform.Find(e.gameObject.name);
                        if (!targetSk)
                            Debug.Log($"无法查询到：{e.gameObject.name}。");
                        else
                        {
                            var renderer = targetSk.GetComponent<Renderer>();
                            if (renderer)
                                renderer.sharedMaterials = e.sharedMaterials;
                        }
                    });
                }
                else
                    Debug.Log($"Originals中的[{goOriginal.name}]在Targets中无法找到！");
            }
        }
    }
}