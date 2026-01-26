using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    public class UIBindActive : UIBindComponentGeneric<bool>
    {
        [SerializeField] private GameObject _targetObject;

        public override void SetData(bool data)
        {
            var target = _targetObject != null ? _targetObject : gameObject;
            target.SetActive(data);
        }

        private void OnValidate()
        {
            // 如果未指定目标对象，默认使用自身
            if (_targetObject == null)
                _targetObject = gameObject;
        }
    }
}
