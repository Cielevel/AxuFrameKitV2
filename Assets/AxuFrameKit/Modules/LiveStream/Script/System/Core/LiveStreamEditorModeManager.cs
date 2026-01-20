using System.Collections.Generic;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream
{
    public class LiveStreamEditorModeManager : SingletonMono<LiveStreamEditorModeManager>
    {
        [SerializeField] private TirggerCollectionSo _tirggerCollectionSo;

        [SerializeField] private bool _autoInit;

        [SerializeField] private bool _isInited;

        private void Start()
        {
            if (_autoInit)
                Init();
        }

        public void Init(TirggerCollectionSo tirggerCollectionSo = null)
        {
            if (_isInited)
                return;

            _isInited = true;

            if (_tirggerCollectionSo == null && tirggerCollectionSo != null)
                _tirggerCollectionSo = tirggerCollectionSo;

#if UNITY_EDITOR
            if (_tirggerCollectionSo == null)
            {
                Debug.LogError("TirggerCollectionSo is null");
                return;
            }

            LiveStreamSimulatorWindow.ClearActions();

            LiveStreamSimulatorWindow.OnCommentsSent += TriggerComment;
            LiveStreamSimulatorWindow.OnGiftsSent += TriggerGift;
            LiveStreamSimulatorWindow.OnLikesSent += TriggerLike;
#endif
        }

        public void TriggerComment(IReadOnlyList<UserDataComment> comments)
        {
            foreach (var comment in comments)
                _tirggerCollectionSo.Trigger(comment);
        }

        public void TriggerGift(IReadOnlyList<UserDataGift> gifts)
        {
            foreach (var gift in gifts)
                _tirggerCollectionSo.Trigger(gift);
        }

        public void TriggerLike(IReadOnlyList<UserDataLike> likes)
        {
            foreach (var like in likes)
                _tirggerCollectionSo.Trigger(like);
        }

        private void OnDestroy()
        {
            LiveStreamSimulatorWindow.OnCommentsSent -= TriggerComment;
            LiveStreamSimulatorWindow.OnGiftsSent -= TriggerGift;
            LiveStreamSimulatorWindow.OnLikesSent -= TriggerLike;

            LiveStreamSimulatorWindow.ClearActions();
        }
    }
}