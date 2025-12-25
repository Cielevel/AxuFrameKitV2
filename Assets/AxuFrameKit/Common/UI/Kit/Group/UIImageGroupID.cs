using System.Linq;
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
                    _imgDict = _images.ToDictionary(e => e.id);

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

            foreach (var img in _images)
                img.image.gameObject.SetActive(false);
            
            ImgDict[id].image.gameObject.SetActive(true);
        }
        
        public void ShowMulti(params string[] ids)
        {
            foreach (var img in ImgDict.Values)
                img.image.gameObject.SetActive(false);

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