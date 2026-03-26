using System;
using UnityEngine;
using AxuFrameKit._Core.Structure;

namespace TestAndSample.Sample_Core.Structure
{
    public class SampleReactiveProperty : MonoBehaviour
    {
        private void Start()
        {
            RunCorrectUsage();
            RunIncorrectUsage_Reentrancy();
        }

        #region 正常使用

        private void RunCorrectUsage()
        {
            Debug.Log("=== 正常使用示例 ===\n");

            // 1. 基础订阅
            Debug.Log("-- 基础订阅 --");
            var hp = new ReactiveProperty<int>(100);
            var disposable = hp.Subscribe(newValue => Debug.Log($"[订阅] HP: {newValue}"));
            hp.Value = 80;
            hp.Value = 60;
            disposable.Dispose();
            hp.Value = 40; // 订阅已释放，不再收到通知
            Debug.Log($"[订阅释放后] HP = {hp.Value}");

            // 2. 订阅带旧值
            Debug.Log("\n-- SubscribeWithPrevious --");
            var gold = new ReactiveProperty<int>(100);
            gold.SubscribeWithPrevious((newVal, oldVal) =>
                Debug.Log($"[旧值] Gold: {oldVal} -> {newVal}"));
            gold.Value = 150;
            gold.Value = 200;

            // 3. 订阅时禁用立即通知
            Debug.Log("\n-- notifyImmediately=false --");
            var score = new ReactiveProperty<int>(0);
            score.Subscribe(
                v => Debug.Log($"[立即通知] Score: {v}"),
                notifyImmediately: false);
            score.Value = 10; // 只有这里才收到通知
            score.Value = 20;

            // 4. 静态工厂方法
            Debug.Log("\n-- From 工厂方法 --");
            var rp = ReactiveProperty<string>.From("Hello");
            rp.Subscribe(v => Debug.Log($"[From] {v}"));

            // 5. 接口引用
            Debug.Log("\n-- IReactiveProperty 接口 --");
            IReactiveProperty<string> named = new ReactiveProperty<string>("Alice");
            named.Subscribe(v => Debug.Log($"[接口] Name: {v}"));
            named.Value = "Bob";

            // 6. 正确派生模式：使用独立 ReactiveProperty
            Debug.Log("\n-- 派生值（正确做法）--");
            var hpMax = new ReactiveProperty<int>(100);
            var hpWarning = new ReactiveProperty<bool>(false);

            hpMax.Subscribe(max =>
            {
                hpWarning.Value = max < 30;
                Debug.Log($"[派生] Max={max}, Warning={hpWarning.Value}");
            });

            hpMax.Value = 20; // 触发警告
            hpMax.Value = 50; // 解除警告
        }

        #endregion

        #region 错误使用

        private void RunIncorrectUsage_Reentrancy()
        {
            Debug.Log("\n=== 错误使用示例：重入（回调中修改值）===\n");

            var counter = new ReactiveProperty<int>(0);

            counter.SubscribeWithPrevious((newVal, oldVal) =>
            {
                Debug.Log($"[错误] Notify: {oldVal} -> {newVal}");
                counter.Value = 999; // 错误：在回调中修改自身值
            }, false);

            try
            {
                counter.Value = 1;
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError($"[错误捕获] {ex.Message}");
            }

            Debug.Log($"[错误] 最终值 = {counter.Value}（应为 999，因为修改发生在异常前）");
        }

        #endregion
    }
}