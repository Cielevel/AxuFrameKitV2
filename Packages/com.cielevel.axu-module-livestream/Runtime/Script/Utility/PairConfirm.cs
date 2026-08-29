using System;
using System.Linq;
using System.Collections.Generic;
using AxuFrameKit.Common.Utility.Singleton;

namespace AxuFrameKit.Modules.LiveStream
{
    /// <summary>
    /// 简易-配对确认
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PairConfirm<T> : SingletonMonoCrDs<PairConfirm<T>> where T : IEquatable<T>
    {
        private const int DefaultCapacity = 16;

        private List<Pair> _pairList = new(DefaultCapacity);

        /// <summary>
        /// 如果双方互相请求，该flag将决定如何处理：如果为true，相互请求将confirm；如果为false，相互请求不会confirm。
        /// </summary>
        protected abstract bool ReverseConfirm { get; }

        public void OnRequest(T requester, T responder)
        {
            if (_pairList.Any(e => e.IsSame(requester, responder)))
                return;

            if (ReverseConfirm && _pairList.Any(e => e.IsReverseSame(requester, responder)))
            {
                var reversePair = _pairList.FirstOrDefault(e => e.IsReverseSame(requester, responder));
                if (reversePair != null)
                {
                    _pairList.Remove(reversePair);
                    OnPairConfirm(requester, responder);
                    return;
                }
            }

            // 3. 如果未确认，则添加新的请求
            _pairList.Add(new Pair(requester, responder));
        }

        public void OnResponse(T responder)
        {
            var pair = _pairList.FirstOrDefault(e => e.Responder.Equals(responder));
            if (pair != null)
            {
                _pairList.Remove(pair);
                OnPairConfirm(pair.Requester, pair.Responder);
            }
        }

        protected void ClearWhereRequesterEquals(T requester) => _pairList.RemoveAll(e => e.Requester.Equals(requester));
        protected void ClearWhereResponserEquals(T responder) => _pairList.RemoveAll(e => e.Responder.Equals(responder));
        protected void ClearWhereContains(T identity) => _pairList.RemoveAll(e => e.Contains(identity));

        // Confirm后的操作按照需求制定；如清理Requester，Responser
        protected abstract void OnPairConfirm(T requester, T responder);

        private class Pair
        {
            public T Requester { get; private set; }
            public T Responder { get; private set; }

            public Pair(T requester, T responder)
            {
                Requester = requester;
                Responder = responder;
            }

            public bool Contains(T target) => Requester.Equals(target) || Responder.Equals(target);

            /// <summary>
            /// 完全相同
            /// </summary>
            public bool IsSame(T requester, T responder) => Requester.Equals(requester) && Responder.Equals(responder);

            /// <summary>
            /// 反向相同；请求者变成答应者，答应者变成请求者
            /// </summary>
            public bool IsReverseSame(T requester, T responder) => Requester.Equals(responder) && Responder.Equals(requester);
        }
    }
}