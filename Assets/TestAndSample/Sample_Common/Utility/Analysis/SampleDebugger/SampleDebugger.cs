using UnityEngine;
using AxuFrameKit.Common.Utility.Analysis;
using System.Threading.Tasks;
using System;

namespace TestAndSample.Sample_Common.Utility.Analysis.SampleDebugger
{
    public class SampleDebugger : MonoBehaviour
    {
        private async void Start()
        {
            await TestAsyncCall();
            await TestManualTiming();
        }

        private async Task Setup()
        {
            Debug.Log("This is a debug.");
            Debug.Log("Begin to wait...");
            await Task.Delay(TimeSpan.FromSeconds(1));
            Debug.Log("End.");
        }

        private async Task TestAsyncCall()
        {
            // 自动计时：适合 lexical scope (代码块) 
            using (Debugger.LogMethodScope("Scope Profiling Test"))
            {
                await Setup();
            }
        }

        private async Task TestManualTiming()
        {
            // 手动计时：适合需要自定义计时逻辑或不方便使用 using 的场景
            float start = Time.realtimeSinceStartup;

            // 模拟一些同步或异步逻辑
            // ... (这里假设执行了一些逻辑)
            await Task.Delay(TimeSpan.FromSeconds(1));

            float elapsed = Time.realtimeSinceStartup - start;

            // 仅仅是格式化报告
            Debugger.LogTiming("Manual Sync Operation", elapsed);
        }
    }
}