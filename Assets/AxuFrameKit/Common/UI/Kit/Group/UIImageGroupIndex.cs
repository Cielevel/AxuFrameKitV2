using UnityEngine;
using UnityEngine.UI;
using AxuFrameKit.Common.UI.Framework;

namespace AxuFrameKit.Common.UI.Kit.Group
{
    /// <summary>
    /// 图组的图片切换组件，可以通过索引显示单个或多个图片。
    /// </summary>
    public class UIImageGroupIndex : UIBaseUnit
    {
        [SerializeField] private Image[] _images;

        public void ShowOne(int index)
        {
            if (IsIndexValid(index) == false)
            {
                Debug.LogError("UIImageGroupIndex: Index不在合理范围内，index：" + index, gameObject);
                return;
            }

            for (int i = 0; i < _images.Length; i++)
                _images[i].gameObject.SetActive(i == index);
        }
        
        public void ShowMulti(params int[] indexes)
        {
            foreach (var img in _images)
                img.gameObject.SetActive(false);

            foreach (var index in indexes)
            {
                if (IsIndexValid(index) == false)
                {
                    Debug.LogError("UIImageGroupIndex: Index不在合理范围内，index：" + index, gameObject);
                    continue;
                }
                
                _images[index].gameObject.SetActive(true);
            }
        }
        
        private bool IsIndexValid(int index)
        {
            return 0 <= index && index < _images.Length;
        }
    }
}