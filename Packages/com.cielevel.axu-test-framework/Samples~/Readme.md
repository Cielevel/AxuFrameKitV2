# AxuTestFramework Samples

两个最小示例，演示 AxuTestFramework Runtime 工具层的标准用法与测试编写规范。

## 导入方式

1. Package Manager（Window → Package Manager）→ 左上切换为 **In Project** 或 **All**（Unity 6 为 "In Project" 列表）→ 选中 **AxuTestFramework**；
2. 详情页展开 **Samples**，按需 **Import**：
   - **SampleEditMode**：EditMode 基础示例（命名规范、纯断言、`AxuLogWatch`、`LogAssert.Expect`、`AxuTestState`）；
   - **SamplePlayMode**：PlayMode 基础示例（`[UnitySetUp]` 中 `AxuTestState.ResetStatics()` + `AxuWait.ForFrames`、`AxuWait.Until`、`AxuCapture.ScreenshotCoroutine`、`AxuNoErrorsScope`；无需场景资产，UTF 自动空场景即可跑）。
3. 导入后位于 `Assets/Samples/com.cielevel.axu-test-framework/0.1.0/<示例名>/`，在 Test Runner 中即可运行。

## 说明

- 示例程序集 `defineConstraints: [UNITY_INCLUDE_TESTS]`：只在测试上下文编译，不影响正常出包。
- PlayMode 示例中的截图测试要求 GameView 存在（经 Unity Pipeline 驱动或编辑器内运行均满足）。
