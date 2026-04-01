using AxuFrameKit._Core.Extension;
using AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool;
using AxuFrameKit.Common.Utility.Timer;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TestAndSample.Sample_Common.Utility.ObjectPool
{
    public class SampleGameObjectPools : MonoBehaviour
    {
        [SerializeField] private GameObjectIntPools _intPoolMgr;
        [SerializeField] private GameObjectStrPools _strPoolMgr;

        [SerializeField] private int[] _intIDs;
        [SerializeField] private string[] _strIDs;

        private void Awake()
        {
            _intPoolMgr.Initialize(e => e.gameObject.SetActive(true), e => e.gameObject.SetActive(false), transform);
            _strPoolMgr.Initialize(e => e.gameObject.SetActive(true), e => e.gameObject.SetActive(false), transform);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                var obj = _intPoolMgr.Get(_intIDs.GetRandom());

                obj.transform.position = new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));

                Timers.StartCd(5).OnComplete(() => obj.Release());
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                var obj = _strPoolMgr.Get(_strIDs.GetRandom());

                obj.transform.position = new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));

                Timers.StartCd(5).OnComplete(() => obj.Release());
            }
        }
    }
}
