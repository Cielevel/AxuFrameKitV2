using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AxuFrameKit.Common.UI.Framework;

namespace AxuFrameKit.Common.UI.Kit.Group
{
    /// <summary>
    /// 图组的图片切换组件，可以通过ID显示单个或多个图片。
    /// </summary>
    public class UIImageGroupID : UIBaseUnit
    {
        [SerializeField] private Img[] _images;

        private Dictionary<string, Img> ImgDict
        {
            get
            {
                if (_imgDict == null || _imgDict.Count == 0)
                {
                    // 手动构建字典：重复 ID 与未绑定 image 的条目只告警并跳过
                    // （原 ToDictionary 遇重复键会直接抛异常，OnValidate 只在编辑器兜底，运行时挡不住）
                    _imgDict = new Dictionary<string, Img>(_images.Length);
                    foreach (var img in _images)
                    {
                        if (img == null || img.image == null)
                        {
                            Debug.LogWarning("UIImageGroupID: 存在未绑定 Image 的条目，已跳过", gameObject);
                            continue;
                        }

                        if (_imgDict.ContainsKey(img.id))
                        {
                            Debug.LogError("UIImageGroupID: 存在重复的ID：" + img.id, gameObject);
                            continue;
                        }

                        _imgDict[img.id] = img;
                    }
                }

                return _imgDict;
            }
        }
        
        private Dictionary<string, Img> _imgDict;

        public void ShowOne(string id)
        {
            if (IsIndexValid(id) == false)
            {
                Debug.LogError("UIImageGroupID，ID不存在，id：" + id, gameObject);
                return;
            }

            // 与 ShowMulti 统一遍历口径：按 _images 顺序关闭全部（跳过未绑定条目），再开启目标
            foreach (var img in _images)
            {
                if (img == null || img.image == null)
                    continue;

                img.image.gameObject.SetActive(false);
            }

            ImgDict[id].image.gameObject.SetActive(true);
        }

        public void ShowMulti(params string[] ids)
        {
            // 与 ShowOne 统一遍历口径：按 _images 顺序关闭全部（跳过未绑定条目）
            foreach (var img in _images)
            {
                if (img == null || img.image == null)
                    continue;

                img.image.gameObject.SetActive(false);
            }

            foreach (var id in ids)
            {
                if (IsIndexValid(id) == false)
                {
                    Debug.LogError("UIImageGroupID，ID不存在，id：" + id, gameObject);
                    continue;
                }

                ImgDict[id].image.gameObject.SetActive(true);
            }
        }

        private bool IsIndexValid(string id) => ImgDict.ContainsKey(id);

        protected override void OnValidate()
        {
            base.OnValidate();
            
            var hs = new HashSet<string>(_images.Length);

            foreach (var img in _images)
            {
                if (hs.Add(img.id) == false)
                    Debug.LogError("UIImageGroupID: 存在重复的ID：" + img.id, gameObject);
            }
        }

        [System.Serializable]
        private class Img
        {
            public Image image;
            public string id;
        }
    }
}