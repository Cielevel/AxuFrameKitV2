using UnityEngine;

namespace AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool
{
    public class PoolableInt : MonoBehaviour
    {
        [field: SerializeField]
        public int ObjID { get; private set; }

        private GameObjectIntPools _strPoolMgr;

        public void Set(GameObjectIntPools strPoolMgr, int objID)
        {
            _strPoolMgr = strPoolMgr;
            ObjID       = objID;
        }
        
        public void Release()
        {
            _strPoolMgr?.Release(this);
        }
    }
}