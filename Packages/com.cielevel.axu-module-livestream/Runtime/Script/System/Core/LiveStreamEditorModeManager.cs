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

        /// <summary>
        /// 是否已完成初始化且持有有效的触发器集合（运行时安全入口，不引用任何 Editor 类型）。
        /// 编辑器侧的 LiveStreamEditorHooks（Editor 程序集）依据它决定是否把
        /// LiveStreamSimulatorWindow 的发送事件桥接到本管理器。
        /// </summary>
        public bool IsReady => _isInited && _tirggerCollectionSo != null;

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
            }
#endif

            // 与 LiveStreamSimulatorWindow 的事件接线已迁移至 Editor 程序集（LiveStreamEditorHooks），
            // Runtime 程序集不得引用任何 Editor 类型。
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

    }
}