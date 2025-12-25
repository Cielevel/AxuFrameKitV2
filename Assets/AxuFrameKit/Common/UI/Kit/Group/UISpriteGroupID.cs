using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using AxuFrameKit.Common.UI.Framework;

namespace AxuFrameKit.Common.UI.Kit.Group
{
    /// <summary>
    /// 针对一个携带Image组件的物体，根据ID切换Sprite
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UISpriteGroupID : UIBaseUnit
    {
        [SerializeField] private Image _image;
        
        [SerializeField] private SpriteWrapper[] _spriteWrappers;

        public void SetSprite(string id)
        {
            var wrapper = _spriteWrappers.FirstOrDefault(e => e.id == id);

            if (wrapper == null)
            {
                Debug.LogError("UISpriteGroupID: 未找到对应ID的Sprite：" + id, gameObject);
                return;
            }
            
            _image.sprite = wrapper.sprite;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (_image == null)
                _image = GetComponent<Image>();
            
            var hs = new HashSet<string>(_spriteWrappers.Length);

            foreach (var wrapper in _spriteWrappers)
            {
                if (hs.Add(wrapper.id) == false)
                    Debug.LogError("UIImageGroupID: 存在重复的ID：" + wrapper.id, gameObject);
            }
        }

        [System.Serializable]
        private class SpriteWrapper
        {
            public Sprite sprite;
            public string id;
        }
    }
}