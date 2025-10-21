using UnityEngine;

namespace AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool
{
    public class PoolableStr : MonoBehaviour
    {
        [field: SerializeField]
        public string ObjName { get; private set; }
        
        private GameObjectStrPools _strPoolMgr;

        public void Set(GameObjectStrPools strPoolMgr, string objName)
        {
            _strPoolMgr = strPoolMgr;
            ObjName     = objName;
        }
        
        public void Release()
        {
            if (gameObject.activeSelf == false)
                return;
            
            _strPoolMgr?.Release(this);
        }
    }
}