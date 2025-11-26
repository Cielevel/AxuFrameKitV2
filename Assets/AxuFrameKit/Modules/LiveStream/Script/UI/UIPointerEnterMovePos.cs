using UnityEngine;
using UnityEngine.EventSystems;
using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation;

namespace AxuFrameKit.Modules.LiveStream.Script.UI
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
        
        private AxuTweener _currentTween;
        
        private void Start() => _rtf = GetComponent<RectTransform>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            _currentTween?.Kill();
            
            if (_lockX && _lockY == false)
                _currentTween = _rtf.AnchoredMoveX(_onPointerEnterPos.y, _durationOnEnter).Start();
            if (_lockY && _lockX == false)
                _currentTween = _rtf.AnchoredMoveY(_onPointerEnterPos.x, _durationOnEnter).Start();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _currentTween?.Kill();
            
            if (_lockX && _lockY == false)
                _currentTween = _rtf.AnchoredMoveY(_onPointerExitPos.y, _durationOnExit).Start();
            if (_lockY && _lockX == false)
                _currentTween = _rtf.AnchoredMoveX(_onPointerExitPos.x, _durationOnExit).Start();
        }
    }
}