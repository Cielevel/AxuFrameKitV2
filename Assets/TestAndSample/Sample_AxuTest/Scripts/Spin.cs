using UnityEngine;

namespace TestAndSample.AxuTest
{
    /// <summary>
    /// AxuTestFramework v2 Phase 3 验证靶场用自转组件。
    /// </summary>
    public class Spin : MonoBehaviour
    {
        public float DegreesPerSecond = 90f;

        private void Update()
        {
            transform.Rotate(0f, DegreesPerSecond * Time.deltaTime, 0f, Space.World);
        }
    }
}
