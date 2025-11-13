#if PRIME_TWEEN
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AxU_NextGen._DevUtils.LiveStream.Script.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UIPointerEnterMovePos : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Vector2 _onPointerEnterPos;
        [SerializeField] private float _durationOnEnter = 1;
        
        [Space]
        
        [SerializeField] private Vector2 _onPointerExitPos;
        [SerializeField] private float _durationOnExit = 1;

        [Space]
        
        [SerializeField] private bool _lockX;
        [SerializeField] private bool _lockY;

        private RectTransform _rtf;
        
        private Tween _currentTween;
        
        private void Start() => _rtf = GetComponent<RectTransform>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            _currentTween.Stop();

            if (_lockX && _lockY == false)
                _currentTween = Tween.UIAnchoredPositionY(_rtf, _onPointerEnterPos.y, _durationOnEnter);
            if (_lockY && _lockX == false)
                _currentTween = Tween.UIAnchoredPositionX(_rtf, _onPointerEnterPos.x, _durationOnEnter);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _currentTween.Stop();
            
            if (_lockX && _lockY == false)
                _currentTween = Tween.UIAnchoredPositionY(_rtf, _onPointerExitPos.y, _durationOnExit);
            if (_lockY && _lockX == false)
                _currentTween = Tween.UIAnchoredPositionX(_rtf, _onPointerExitPos.x, _durationOnExit);
        }
    }
}
#endif