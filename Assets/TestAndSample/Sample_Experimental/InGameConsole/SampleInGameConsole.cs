using UnityEngine;

namespace Sample_Expiremental.InGameConsole
{
    public class SampleInGameConsole : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
                Debug.Log("这是一个测试日志，按下空格键触发。");

            if (Input.GetKeyDown(KeyCode.W))
                Debug.LogWarning("这是一个测试日志Warning，按下空格键触发。");

            if (Input.GetKeyDown(KeyCode.E))
                Debug.LogError("这是一个测试日志Error，按下空格键触发。");
        }
    }
}