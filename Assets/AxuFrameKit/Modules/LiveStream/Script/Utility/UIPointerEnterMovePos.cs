using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AxuFrameKit.Modules.LiveStream.Script.Utility
{
    [RequireComponent(typeof(RectTransform))]
    public class UIPointerEnterMovePos : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Vector2 _onPointerEnterPos;
        [SerializeField] private float _durationOnEnter = 1;
        [SerializeField] private EaseType _easeIn = EaseType.InCubic;
        
        [Space]
        
        [SerializeField] private Vector2 _onPointerExitPos;
        [SerializeField] private float _durationOnExit = 1;
        [SerializeField] private EaseType _easeOut = EaseType.InCubic;

        [Space]
        
        [SerializeField] private bool _lockX;
        [SerializeField] private bool _lockY;

        private RectTransform _rtf;
        
        private AxuTweener<RectTransform> _currentTween;

        private void Start() => _rtf = GetComponent<RectTransform>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            _currentTween?.Kill();
            
            if (_lockX && _lockY == false)
                _currentTween = _rtf.ToAnchoredPosY(_onPointerEnterPos.y, _durationOnEnter).SetEase(_easeIn).Start();
            if (_lockY && _lockX == false)
                _currentTween = _rtf.ToAnchoredPosX(_onPointerEnterPos.x, _durationOnEnter).SetEase(_easeIn).Start();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _currentTween?.Kill();
            
            if (_lockX && _lockY == false)
                _currentTween = _rtf.ToAnchoredPosY(_onPointerExitPos.y, _durationOnExit).SetEase(_easeOut).Start();
            if (_lockY && _lockX == false)
                _currentTween = _rtf.ToAnchoredPosX(_onPointerExitPos.x, _durationOnExit).SetEase(_easeOut).Start();
        }
    }
}