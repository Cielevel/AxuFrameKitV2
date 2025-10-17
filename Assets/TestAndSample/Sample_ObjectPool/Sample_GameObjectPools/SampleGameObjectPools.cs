using System;
using AxuFrameKit._Core.Extension;
using AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool;
using AxuFrameKit.Common.Utility.Timer;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TestAndSample.Sample_ObjectPool.Sample_GameObjectPools
{
    public class SampleGameObjectPools : MonoBehaviour
    {
        [SerializeField] private GameObjectIntPools _intPoolMgr;
        [SerializeField] private GameObjectStrPools _strPoolMgr;

        [SerializeField] private int[] _intIDs;
        [SerializeField] private string[] _strIDs;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                var obj = _intPoolMgr.GetObj(_intIDs.GetRandom());
                
                obj.transform.position = new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));

                CountDownU.Get().StartTik(5).OnComplete += () => obj.Release();
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                var obj = _strPoolMgr.GetObj(_strIDs.GetRandom());
                
                obj.transform.position = new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));

                CountDownU.Get().StartTik(5).OnComplete += () => obj.Release();
            }
        }
    }
}
