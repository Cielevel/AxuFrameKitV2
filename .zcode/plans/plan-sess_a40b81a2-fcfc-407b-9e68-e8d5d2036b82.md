# LiveStream 运行时游戏内面板（评论/点赞/礼物）实施计划

## 背景结论（已调研确认）

- 发送链路可整体复用：`LiveStreamEditorModeManager → TirggerCollectionSo.Trigger(UserDataUnit)` 是纯 Runtime 代码，新面板作为**新数据源**直连 `TirggerCollectionSo` 即可，下游触发器体系零改动。
- `UserConfigSo` / `GiftConfigSo` 的数据列表是 private 字段（模拟器窗口用反射读，仅限 Editor）。运行时面板需给这两个 SO 各加一个**公开只读访问器**（约 4 行，向后兼容）。
- `GiftIndex` 语义 = 礼物在 `GiftConfigSo` 列表中的下标；面板按同一列表顺序展示，天然对齐。
- 预制体上的三个配置引用**留空**（不能预挂 `Samples~/SO/` 里的资产——其他工程未导入 Sample 会变 Missing），由使用者 Inspector 手动挂载，Manager 启动时校验并提示。这正是你要的"手动挂载"模式。
- Unity Editor 正在运行、Pipeline 服务心跳正常；实时命令验证列为第 0 步。

## 交付物（全部在 `Packages/com.cielevel.axu-module-livestream` 包内，随包同步）

### 1. Runtime 代码（`Runtime/Script/` 下，新增 4 文件 + 改 2 文件）

| 文件 | 内容 |
|---|---|
| `UI/Console/UIPanelLiveStreamConsole.cs` | uGUI 面板：持有三栏 UI 引用（序列化），`SetupUsers()/SetupGifts()` 动态实例化条目，收集选中用户/礼物(含数量)/评论文本/点赞数；收起/展开开关 |
| `UI/Console/UIConsoleUserItem.cs` | 用户条目：Toggle 多选 + 头像 + 昵称 + UID，持 `UserDataUnit` |
| `UI/Console/UIConsoleGiftItem.cs` | 礼物条目：Toggle 多选 + 图标 + 名称/价格 + 数量输入框，持 `GiftDataUnit` + 列表下标（即 GiftIndex） |
| `UI/Console/LiveStreamConsoleManager.cs` | Manager（挂预制体根，普通 MonoBehaviour）：`[SerializeField]` 手动挂 `UserConfigSo`/`GiftConfigSo`/`TirggerCollectionSo`；Start 校验配置（缺失则 LogError 并停用）→ 喂面板 → 发送方法构造 `UserDataComment/Like/Gift` 调 `_tirggerCollectionSo.Trigger()`；空选择 LogWarning（与模拟器一致） |
| `UserConfigSo.cs`（改） | + `public IReadOnlyList<UserDataUnit> Users => _userDataUnitList;` |
| `GiftConfigSo.cs`（改） | + `public IReadOnlyList<GiftDataUnit> Gifts => _giftDataUnitList;` |

纯 Runtime、零 Editor 依赖（不引用 AssetDatabase/UnityEditor），可进 Player 构建。

### 2. UI 预制体（Pipeline 在 Editor 中实际搭建）

- 层级：自带 Canvas（Screen Space Overlay + CanvasScaler + GraphicRaycaster）→ 标题栏（含收起按钮）→ 三栏（用户列表 / 礼物列表 / 操作区：评论输入+发送、点赞数量+按钮、赠送礼物按钮）。
- 两个条目子预制体（用户条目 / 礼物条目），滚动用 ScrollRect + 动态实例化（条目量级为配置资产规模，无需虚拟化；不使用已 Obsolete 的 `UILoopVerticalScrollRect`）。
- 字体用包内已有 SmileySans TMP 字体；配色风格贴近模拟器窗口（深色半透明底）。
- 产物：`Runtime/Art/Prefab/PF UI LiveStream Console.prefab`（含两个条目 prefab，同目录）——挂到任意场景即用。

### 3. 验证（Editor 实操）

- **第 0 步（能力实测）**：`/api/status` + `/api/commands` 核对 `create_script / create_gameobject / add_component / set_property / create_prefab / save_scene / capture_game_view / 进 Play` 等命令的实际参数形态；个别能力缺失则改用 pipeline eval/console 兜底方案，仍不可行则停下报告。
- 搭建后：临时验证场景挂预制体 + 临时 User/Gift 配置 SO（用包内示例图片填充）→ **进 Play** → 截图确认布局 → 模拟点击发送 → Console 确认触发链路输出（示例触发器打 Debug.Log）→ 退出 Play。
- 验证场景按工程惯例保留到 `Assets/TestAndSample/Sample_PackageDev/` 作参考示例。
- EditMode 测试（`Tests/Editor/`，类别 `Axu/Module/LiveStream`，沿用包内"反射注入 private 字段"测试纪律）：覆盖配置访问器、Manager 三类发送构造与分发、空配置校验；经 `axu_test_run` 跑过。

### 4. 收尾

- `package.json` 0.1.0 → 0.2.0 + `CHANGELOG.md` 新增条目；`README.md` 增「运行时控制台面板」章节（挂载三步：拖预制体进场景 → Inspector 挂三个 SO → 运行）。
- 最终报告含截图与测试结果。

## 执行分工

unity-runtime-dev 写 Runtime 代码 → unity-pipeline 搭预制体 + Play 验证 → unity-tester 写/跑测试 → 收尾文档。全程不碰 `Tirgger` 既有拼写等既有 API；不动 Editor 模拟器窗口。

## 不做（明确出范围）

发送记录/日志区（你已选不带）、弹幕展示侧 UI（现有 UIPanelUser/Gift/Comment 不动）、自动搜索配置（明确不要）、Player 真机构建验证（如需可后续加）。