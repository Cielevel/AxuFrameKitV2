using UnityEngine;
using AxuFrameKit._Core.Utility.Computed;

namespace AxuFrameKit.TestAndSample.Sample_Core.Utility.Computed
{
    /// <summary>
    /// Computed 模块使用示例。
    /// 本示例展示了如何创建源数据 (ComputedSource)、定义计算派生值 (Computed) 以及它们的自动依赖追踪与惰性求值特性。
    /// </summary>
    public class Sample_Computed : MonoBehaviour
    {
        // 1. 定义源数据 (Sources)
        // 它们是整个响应式图表的叶子节点，可以手动修改 Value。
        private readonly ComputedSource<int> _width = new(10);
        private readonly ComputedSource<int> _height = new(20);
        private readonly ComputedSource<string> _label = new("Rect");

        // 2. 定义派生数据 (Computed values)
        // 它们依赖于其他 Computed 或 ComputedSource，会在依赖变化时自动失效，并在下一次读取时惰性重算。
        private Computed<int> _area;
        private Computed<string> _description;

        private void Awake()
        {
            // 初始化计算派生值
            // 注意：构造函数只接受求值函数，此时并不会立即执行，直到首次访问 .Value
            _area = new Computed<int>(() =>
            {
                Debug.Log($"<color=cyan>[Computed]</color> Calculating Area... ({_width.Value} x {_height.Value})");
                return _width.Value * _height.Value;
            });

            _description = new Computed<string>(() =>
            {
                Debug.Log($"<color=yellow>[Computed]</color> Calculating Description...");
                // 会自动建立对 _label 和 _area 的双重依赖
                return $"{_label.Value}: Area is {_area.Value}";
            });
        }

        private void Start()
        {
            Debug.Log("<color=green>--- 首次读取 (First Read) ---</color>");
            // 首次读取触发计算，并输出日志
            Debug.Log($"Initial Area: {_area.Value}");
            Debug.Log($"Initial Description: {_description.Value}");

            Debug.Log("<color=green>--- 静默修改 (Silent Modification) ---</color>");
            // 修改源数据，这会触发依赖通知，但受限于惰性特性 (Lazy)，并不会立即执行求值逻辑
            _width.Value = 30;
            Debug.Log("Modified _width.Value to 30. No re-calculation should have happened yet.");

            Debug.Log("<color=green>--- 再次读取 (Second Read) ---</color>");
            // 此时读取触发重新计算
            Debug.Log($"Updated Area: {_area.Value}");
            Debug.Log($"Updated Description: {_description.Value}");

            Debug.Log("<color=green>--- 链式响应测试 (Chained Reaction) ---</color>");
            _label.Value = "Circle";
            // 此时只修改了 label，读取 Description 时，Area 不应该被重新计算 (因为 Area 依赖的 width/height 没变)
            Debug.Log($"Final Description: {_description.Value}");
            
            Debug.Log("<color=grey>Check logs above: only Description logic ran, Area should still be cached.</color>");
        }

        private void OnDestroy()
        {
            // 良好的习惯：不再使用时 Dispose，清理掉在源数据中注册的失效回调，避免内存泄漏/残留引用。
            _area?.Dispose();
            _description?.Dispose();
        }
    }
}
