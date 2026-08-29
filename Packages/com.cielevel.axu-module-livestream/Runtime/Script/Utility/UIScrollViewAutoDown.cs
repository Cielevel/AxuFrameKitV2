using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Modules.LiveStream
{
    [RequireComponent(typeof(ScrollRect))]
    public class UIScrollViewAutoDown : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;

        [SerializeField] private RectTransform _content;

        [SerializeField] private float _refreshRate = 0.1f;

        [SerializeField] private float _tweenDuration = 0.5f;

        private float _timer;

        private int _lastContentChildCount;

        private AxuTweener<object> _tween;

        private void Start()
        {
            if (_scrollRect == null)
            {
                _scrollRect = GetComponent<ScrollRect>();

                _content = _scrollRect?.content;

                if (_content != null)
                    _lastContentChildCount = _content.childCount;
            }
        }

        private void Update()
        {
            if (_content != null)
            {
                _timer += Time.deltaTime;
                if (_refreshRate < _timer)
                {
                    _timer = 0f;

                    if (_lastContentChildCount != _content.childCount)
                        Refresh();
                }
            }
        }

        private void Refresh()
        {
            _lastContentChildCount = _content.childCount;

            _tween.Kill();

            _tween = AxuTween.Custom(_scrollRect.verticalNormalizedPosition, 0f, _tweenDuration, value => _scrollRect.verticalNormalizedPosition = value);

            _tween.Start();
        }

        private void OnValidate()
        {
            _scrollRect = GetComponent<ScrollRect>();

            _content = _scrollRect?.content;
        }
    }
}