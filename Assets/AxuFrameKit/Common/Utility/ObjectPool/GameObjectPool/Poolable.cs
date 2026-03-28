using System;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool
{
    public class Poolable<T> : MonoBehaviour
    {
        public T ObjId { get; private set; }

        private Action<Poolable<T>> _releaseAction;

        private bool _isReleased;

        public void Set(T objId, Action<Poolable<T>> onRelease)
        {
            ObjId = objId;
            _isReleased = false;
            _releaseAction = onRelease;
        }

        public void Release()
        {
            if (_isReleased)
                return;

            _isReleased = true;
            _releaseAction?.Invoke(this);
        }
    }
}