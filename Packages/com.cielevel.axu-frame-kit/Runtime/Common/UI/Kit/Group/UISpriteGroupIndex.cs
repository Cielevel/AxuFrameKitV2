using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using AxuFrameKit.Common.UI.Framework;

namespace AxuFrameKit.Common.UI.Kit.Group
{
    /// <summary>
    /// 针对一个携带Image组件的UI单元，根据传入的索引设置不同的Sprite。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UISpriteGroupIndex : UIBaseUnit
    {
        [SerializeField] private Image _image;
        
        [SerializeField] private Sprite[] _sprites;

        public void SetSprite(int index)
        {
            if (IsIndexValid(index) == false)
            {
                Debug.LogError("UISpriteGroupIndex: index不在范围内，index：" + index, gameObject);
                return;
            }

            _image.sprite = _sprites[index];
        }
      
        private bool IsIndexValid(int index)
        {
            return 0 <= index && index < _sprites.Length;
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();

            if (_image == null)
                _image = GetComponent<Image>();
        }

        [System.Serializable]
        private class SpriteWrapper
        {
            public Sprite sprite;
            public string id;
        }
    }
}