using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// <see cref="AxuTestRunnerWindow"/> 的"开始使用 / 按目标运行"卡（阶段 2 Task B，独立 partial 文件）。
    /// <para>面向第一次打开看板的人：①醒目的"运行自检"大按钮（临时自检套件，验证环境通不通，不动 Assets）；
    /// ②readiness 横幅——环境没就绪时给出"为什么"与一键修复（开启 AXU_TEST_AGENT / 退出 Play / 等待空闲 /
    /// 配置构建场景），编译期间全部发起按钮禁用；③"按目标运行"区（单元测试直达 / 场景示例 / 性能示例，
    /// 点击行展开说明与操作，同时只展开一个）；④自检结论行（本次发起的运行到终态后回显一次）。</para>
    /// <para>样式全部 inline + 复用既有 USS 类（不动 AxuTestTokens.uss），与 Overview 卡同规则。
    /// 数据契约由阶段 2 Task A 提供（同程序集，命名空间 <c>AxuTestFramework.Editor</c>）：
    /// <see cref="RunReadinessCheck"/> / <see cref="FirstRunSuiteCatalog"/> / <see cref="FirstRunPresenter"/>。
    /// 运行统一走 <see cref="ScenarioRunMenu.TryRunSuite(string, string, out string)"/>（看板标准入口，
    /// 与菜单/Scenario 页同一条启动-收尾契约）；结论认领按运行标识与状态文件对齐，
    /// 域重载后发起标识丢失时按"最近一次终态属于自检套件"兜底回显。</para>
    /// </summary>
    public partial class AxuTestRunnerWindow
    {
        /// <summary>selfcheck 目标在 <see cref="FirstRunSuiteCatalog.Goals"/> 里的目标 id（契约钉死）；
        /// 该目标由卡顶部的大按钮承担，目标区不重复渲染。</summary>
        private const string SelfCheckGoalId = "selfcheck";

        private VisualElement _firstRunCard;
        private Button _firstRunSelfCheckButton;
        private VisualElement _firstRunBanner;
        private Label _firstRunBannerLabel;
        private VisualElement _firstRunBannerActions;
        private VisualElement _firstRunGoalsContainer;
        private Label _firstRunOutcomeLabel;

        /// <summary>800ms 轮询句柄（BuildFirstRunCard 末尾自挂一次；窗口重建时随元素自然重建，
        /// 卡的幂等构建守卫保证不会重复注册）。</summary>
        private IVisualElementScheduledItem _firstRunPollSchedule;

        /// <summary>当前展开的目标 id（同一时间只展开一个；null = 全部收起）。</summary>
        private string _firstRunExpandedGoalId;

        /// <summary>目标行的展开设置区 / 展开箭头 / 主操作按钮，按目标 id 键控（构建一次，刷新只改可用性）。</summary>
        private readonly Dictionary<string, VisualElement> _firstRunGoalSettings = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Label> _firstRunGoalChevrons = new Dictionary<string, Label>();
        private readonly Dictionary<string, Button> _firstRunGoalActionButtons = new Dictionary<string, Button>();

        /// <summary>"单元测试"目标所选模式："editor" / "playmode"（带到 Tests 页）。</summary>
        private string _firstRunCodeMode = "editor";
        private Button _firstRunCodeEditButton;
        private Button _firstRunCodePlayButton;

        /// <summary>本次发起的自检运行标识（发起成功后从运行记录读出；null = 没有在途的本次发起）。</summary>
        private string _firstRunPendingRunId;

        /// <summary>最近一次已渲染结论的运行标识（终态只渲染一次，防 800ms 轮询反复刷写）。</summary>
        private string _firstRunLastDeliveredRunId;

        // ------------------------------------------------------------------ 构建与刷新（Task C 接线入口）

        /// <summary>构建"开始使用"整卡；构建完成后立即刷新一次 readiness 并自挂 800ms 轮询
        /// （域重载重建窗口后会重新走到这里，readiness 自然重查）。重复调用幂等，返回已构建的卡。</summary>
        internal VisualElement BuildFirstRunCard()
        {
            if (_firstRunCard != null)
                return _firstRunCard;

            var card = new VisualElement { name = "ax-firstrun-card" };
            card.AddToClassList("ax-overview-card");
            _firstRunCard = card;

            // ① 标题
            var title = new Label("开始使用");
            title.AddToClassList("ax-pane-header");
            card.Add(title);

            // ② 自检大按钮 + 副标题
            _firstRunSelfCheckButton = new Button(RunSelfCheck) { text = "运行自检" };
            _firstRunSelfCheckButton.AddToClassList("ax-run-button");
            _firstRunSelfCheckButton.style.height = 36f;
            _firstRunSelfCheckButton.style.fontSize = 14f;
            _firstRunSelfCheckButton.style.paddingLeft = 16f;
            _firstRunSelfCheckButton.style.paddingRight = 16f;
            _firstRunSelfCheckButton.style.marginBottom = 4f;
            card.Add(_firstRunSelfCheckButton);

            var disclaimer = new Label(FirstRunPresenter.SelfCheckDisclaimer);
            disclaimer.AddToClassList("ax-overview-note");
            card.Add(disclaimer);

            // ③ readiness 横幅（默认隐藏；刷新时按 ReadinessBannerText 与 Issue 修复项重建内容）
            _firstRunBanner = new VisualElement { name = "ax-firstrun-banner" };
            _firstRunBanner.style.marginTop = 8f;
            _firstRunBanner.style.marginBottom = 4f;
            _firstRunBanner.style.paddingTop = 6f;
            _firstRunBanner.style.paddingBottom = 6f;
            _firstRunBanner.style.paddingLeft = 8f;
            _firstRunBanner.style.paddingRight = 8f;
            _firstRunBanner.style.borderLeftWidth = 3f;
            _firstRunBanner.style.borderTopRightRadius = 4f;
            _firstRunBanner.style.borderBottomRightRadius = 4f;
            _firstRunBanner.style.display = DisplayStyle.None;

            _firstRunBannerLabel = new Label();
            _firstRunBannerLabel.AddToClassList("ax-help-note");
            _firstRunBanner.Add(_firstRunBannerLabel);

            _firstRunBannerActions = new VisualElement { name = "ax-firstrun-banner-actions" };
            _firstRunBannerActions.style.flexDirection = FlexDirection.Row;
            _firstRunBannerActions.style.marginTop = 4f;
            _firstRunBanner.Add(_firstRunBannerActions);
            card.Add(_firstRunBanner);

            // ④ 按目标运行区
            var goalsHeader = new Label("按目标运行");
            goalsHeader.AddToClassList("ax-pane-header");
            goalsHeader.style.marginTop = 8f;
            card.Add(goalsHeader);

            var goalsHint = new Label("第一次使用建议先点上面的自检；点击任一目标展开说明与操作。");
            goalsHint.AddToClassList("ax-help-note");
            card.Add(goalsHint);

            _firstRunGoalsContainer = new VisualElement { name = "ax-firstrun-goals" };
            card.Add(_firstRunGoalsContainer);

            if (FirstRunSuiteCatalog.Goals != null)
            {
                foreach (var goal in FirstRunSuiteCatalog.Goals)
                {
                    if (goal == null || goal.Id == SelfCheckGoalId)
                        continue; // 自检语义由顶部大按钮承担，目标区不重复渲染
                    _firstRunGoalsContainer.Add(BuildFirstRunGoalRow(goal));
                }
            }
            UpdateFirstRunGoalExpansion();

            // ⑤ 自检结论区（发起后显示；到终态回显一次结论）
            _firstRunOutcomeLabel = new Label();
            _firstRunOutcomeLabel.AddToClassList("ax-summary");
            _firstRunOutcomeLabel.style.marginTop = 6f;
            _firstRunOutcomeLabel.style.display = DisplayStyle.None;
            card.Add(_firstRunOutcomeLabel);

            // 构建完成后立即查一次（不用等首个轮询 tick），再自挂 800ms 轮询
            RefreshFirstRunCard();
            _firstRunPollSchedule = card.schedule.Execute(RefreshFirstRunCard).Every(800);

            return card;
        }

        /// <summary>刷新首用卡：GatherInput → Evaluate（按自检目标参数）→ 大按钮文案与可用性 /
        /// 横幅与修复按钮 / 各目标行可用性 / 结论行。800ms 轮询与 Task C 主动刷新共用本入口。</summary>
        internal void RefreshFirstRunCard()
        {
            if (_firstRunCard == null)
                return;

            var input = RunReadinessCheck.GatherInput();
            var readiness = RunReadinessCheck.Evaluate(input, requireAgentDefine: true, requireBuildScenes: false);

            // 运行中判定与 Scenario 页同口径：Play 中，或任一运行在途（互斥闸）——此时禁止再发起
            var busy = EditorApplication.isPlaying
                || ScenarioRunService.IsScenarioRunInFlight()
                || AxuTestRunService.IsAxuRunInFlight();

            _firstRunSelfCheckButton.text = FirstRunPresenter.RunButtonLabel(readiness);
            _firstRunSelfCheckButton.tooltip = FirstRunPresenter.RunButtonTooltip(readiness);
            _firstRunSelfCheckButton.SetEnabled(readiness.IsReady && !busy && !input.IsCompiling);

            RenderFirstRunBanner(readiness, input);

            if (FirstRunSuiteCatalog.Goals != null)
            {
                foreach (var goal in FirstRunSuiteCatalog.Goals)
                {
                    if (goal == null || goal.Id == SelfCheckGoalId)
                        continue;
                    if (!_firstRunGoalActionButtons.TryGetValue(goal.Id, out var actionButton) || actionButton == null)
                        continue;

                    if (goal.Id == "code")
                    {
                        // 只做页面跳转：运行与否由 Tests 页自己把关；运行中/编译中一样不给点
                        actionButton.SetEnabled(!busy && !input.IsCompiling);
                    }
                    else
                    {
                        // 场景/性能目标按各自前提门控（如场景目标要求构建场景列表里有场景）
                        var gate = RunReadinessCheck.Evaluate(
                            input, requireAgentDefine: goal.NeedsAgentDefine, requireBuildScenes: goal.NeedsBuildScenes);
                        actionButton.SetEnabled(gate.IsReady && !busy && !input.IsCompiling);
                    }
                }
            }

            RefreshFirstRunOutcome();
        }

        /// <summary>自检入口（本卡大按钮与 Scenario 页空态共用）：确保临时自检套件就绪后按看板来源发起运行。</summary>
        internal void RunSelfCheck()
        {
            var suitePath = FirstRunSuiteCatalog.EnsureSelfCheckSuiteFile(out var error);
            if (string.IsNullOrEmpty(suitePath))
            {
                UpdateStatus("无法开始自检：" + (string.IsNullOrEmpty(error) ? "未知原因" : error));
                return;
            }
            StartFirstRunSuite(suitePath, trackOutcome: true);
        }

        // ------------------------------------------------------------------ 目标行

        /// <summary>单个目标行（标题 + 说明可点击展开；设置行按构建时的展开状态显隐）。</summary>
        private VisualElement BuildFirstRunGoalRow(FirstRunGoal goal)
        {
            var container = new VisualElement { name = "ax-firstrun-goal-" + goal.Id };
            container.AddToClassList("ax-overview-trend-row"); // 复用可点击行的内边距与 hover 高亮
            container.style.flexDirection = FlexDirection.Column;
            container.style.marginBottom = 2f;

            var titleRow = new VisualElement { name = "ax-firstrun-goal-header" };
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;

            var chevron = new Label("▸");
            chevron.style.width = 14f;
            var titleLabel = new Label(goal.Title);
            titleLabel.AddToClassList("ax-overview-subtitle");
            titleRow.Add(chevron);
            titleRow.Add(titleLabel);
            container.Add(titleRow);

            var description = new Label(goal.Description);
            description.AddToClassList("ax-overview-note");
            container.Add(description);

            var settings = new VisualElement { name = "ax-firstrun-goal-settings-" + goal.Id };
            settings.style.marginLeft = 14f;
            settings.style.marginTop = 4f;
            settings.style.paddingLeft = 8f;
            settings.style.borderLeftWidth = 2f;
            settings.style.borderLeftColor = ParseColor("#3a3a3a");
            settings.style.display = DisplayStyle.None;

            if (goal.NeedsAgentDefine)
                AddFirstRunNote(settings, "前提：需要开启 AXU_TEST_AGENT（脚本开关，未开启时上方横幅可一键开启）。");
            if (goal.NeedsBuildScenes)
                AddFirstRunNote(settings, "前提：Build Settings / Build Profiles 的场景列表里至少加入一个场景。");

            switch (goal.Id)
            {
                case "code":
                    BuildCodeGoalSettings(settings);
                    break;
                case "scene":
                case "perf":
                    BuildSuiteGoalSettings(goal.Id, settings);
                    break;
                default:
                    // 未识别的目标：只展示说明，不给操作入口（避免误触未知行为）
                    break;
            }

            container.Add(settings);

            // 展开点击只挂在标题与说明上——设置行内的按钮不受牵连（不会点了按钮反而折叠）
            var capturedGoalId = goal.Id;
            titleRow.RegisterCallback<PointerDownEvent>(_ => ToggleFirstRunGoal(capturedGoalId));
            titleRow.tooltip = "点击展开/收起说明与操作";
            description.RegisterCallback<PointerDownEvent>(_ => ToggleFirstRunGoal(capturedGoalId));

            _firstRunGoalSettings[goal.Id] = settings;
            _firstRunGoalChevrons[goal.Id] = chevron;
            return container;
        }

        /// <summary>"单元测试"目标：模式二选 + 直达 Tests 页（带上所选模式）。</summary>
        private void BuildCodeGoalSettings(VisualElement parent)
        {
            AddFirstRunNote(parent, "单元测试不需要示例套件与 AXU_TEST_AGENT，选一种模式后直达单元测试页。");

            var modeRow = new VisualElement { name = "ax-firstrun-code-mode-row" };
            modeRow.style.flexDirection = FlexDirection.Row;
            modeRow.style.marginTop = 4f;

            _firstRunCodeEditButton = new Button(() => SetFirstRunCodeMode("editor")) { text = "EditMode" };
            _firstRunCodeEditButton.AddToClassList("ax-mode-button");
            _firstRunCodePlayButton = new Button(() => SetFirstRunCodeMode("playmode")) { text = "PlayMode" };
            _firstRunCodePlayButton.AddToClassList("ax-mode-button");
            modeRow.Add(_firstRunCodeEditButton);
            modeRow.Add(_firstRunCodePlayButton);
            SetFirstRunCodeMode(_firstRunCodeMode); // 回填选中态

            var goButton = new Button(GoToTestsPageWithMode) { text = "转到单元测试页运行" };
            goButton.AddToClassList("ax-run-button");
            goButton.tooltip = "跳到「单元测试」页签并带上所选模式；在该页顶部点「Run 全部」即可运行。";
            _firstRunGoalActionButtons["code"] = goButton;

            parent.Add(modeRow);
            parent.Add(goButton);
        }

        /// <summary>"场景 / 性能"目标：运行（首次自动准备示例套件）+ 保存为可编辑套件。</summary>
        private void BuildSuiteGoalSettings(string goalId, VisualElement parent)
        {
            var actionRow = new VisualElement { name = "ax-firstrun-goal-actions-" + goalId };
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.marginTop = 4f;

            var runButton = new Button(() => RunGoalSuite(goalId)) { text = "▶ 运行" };
            runButton.AddToClassList("ax-run-button");
            runButton.tooltip = goalId == "scene"
                ? "进入 Play 模式自动运行一份场景示例（首次会自动准备）；跑完自动退出 Play，结果在「场景套件」页签查看。"
                : "采集当前场景的性能数据（首次会自动准备示例）；跑完自动退出 Play，结果在「场景套件」页签查看，跑两次以上可在「趋势」页签看变化。";
            _firstRunGoalActionButtons[goalId] = runButton;

            var saveButton = new Button(() => SaveGoalSuite(goalId)) { text = "保存为可编辑套件" };
            saveButton.AddToClassList("ax-secondary-button");
            saveButton.tooltip = "在 Assets 里生成一份可编辑的示例套件；重名自动加序号、不覆盖已有文件，之后可在「场景套件」页签修改并复跑。";

            actionRow.Add(runButton);
            actionRow.Add(saveButton);
            parent.Add(actionRow);
        }

        /// <summary>展开/收起目标行（同一时间只展开一个；再次点击已展开的行即收起）。</summary>
        private void ToggleFirstRunGoal(string goalId)
        {
            _firstRunExpandedGoalId = _firstRunExpandedGoalId == goalId ? null : goalId;
            UpdateFirstRunGoalExpansion();
        }

        /// <summary>按展开状态回填各目标设置行显隐与箭头方向。</summary>
        private void UpdateFirstRunGoalExpansion()
        {
            foreach (var pair in _firstRunGoalSettings)
            {
                var expanded = pair.Key == _firstRunExpandedGoalId;
                pair.Value.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                if (_firstRunGoalChevrons.TryGetValue(pair.Key, out var chevron) && chevron != null)
                    chevron.text = expanded ? "▾" : "▸";
            }
        }

        /// <summary>"单元测试"目标选模式（回填两个模式按钮的选中态）。</summary>
        private void SetFirstRunCodeMode(string mode)
        {
            _firstRunCodeMode = mode == "playmode" ? "playmode" : "editor";
            _firstRunCodeEditButton?.EnableInClassList("ax-mode-active", _firstRunCodeMode == "editor");
            _firstRunCodePlayButton?.EnableInClassList("ax-mode-active", _firstRunCodeMode == "playmode");
        }

        /// <summary>带上所选模式跳到 Tests 页（SetMode 会按新模式重取测试列表）。</summary>
        private void GoToTestsPageWithMode()
        {
            SetPage("tests");
            SetMode(_firstRunCodeMode);
            UpdateStatus("已切到单元测试页——点顶部「Run 全部」即可运行，进度与结果都在本页显示。");
        }

        // ------------------------------------------------------------------ 发起运行

        /// <summary>目标行"运行"：准备（或复用）该目标的示例套件后按看板来源发起（结论不在本卡回显，
        /// 由「场景套件」页签的既有回显逻辑负责）。</summary>
        private void RunGoalSuite(string goalId)
        {
            if (!FirstRunSuiteCatalog.TryCreateGoalSuite(goalId, null, out var assetPath, out var error))
            {
                UpdateStatus("未能准备示例：" + (string.IsNullOrEmpty(error) ? "未知原因" : error));
                return;
            }
            StartFirstRunSuite(assetPath, trackOutcome: false);
        }

        /// <summary>目标行"保存为可编辑套件"：只落一份可编辑资产并提示位置，不发起运行。</summary>
        private void SaveGoalSuite(string goalId)
        {
            if (!FirstRunSuiteCatalog.TryCreateGoalSuite(goalId, null, out var assetPath, out var error))
            {
                UpdateStatus("示例套件创建失败：" + (string.IsNullOrEmpty(error) ? "未知原因" : error));
                return;
            }
            UpdateStatus($"已创建 {Relativize(AxuTestReportPaths.ProjectRootFullPath, assetPath)}，" +
                         "可在「场景套件」页签编辑与复跑。");
        }

        /// <summary>统一的发起入口（看板来源）：发起成功后按 trackOutcome 决定是否在本卡跟踪结论
        /// （本次发起的运行标识在发起后立即从运行记录读出，终态据此认领）。</summary>
        private void StartFirstRunSuite(string suiteAbsolutePath, bool trackOutcome)
        {
            if (!ScenarioRunMenu.TryRunSuite(suiteAbsolutePath, ScenarioRunRecord.SourceDashboard, out var error))
            {
                UpdateStatus("未能启动运行：" + error);
                return;
            }

            if (trackOutcome && _firstRunOutcomeLabel != null)
            {
                _firstRunPendingRunId = ScenarioRunRecord.TryRead()?.RunId;
                _firstRunLastDeliveredRunId = null;
                _firstRunOutcomeLabel.style.display = DisplayStyle.Flex;
                SetFirstRunOutcomeTone("ax-tone-neutral");
                _firstRunOutcomeLabel.text = "自检已发起——进入 Play 模式执行，跑完自动退出 Play 并在这里给出结论。";
            }

            UpdateStatus(trackOutcome
                ? "自检运行中——跑完自动退出 Play 并回显结论。"
                : "示例套件运行中——跑完自动退出 Play，可在「场景套件」页签查看进度与结果。");
        }

        // ------------------------------------------------------------------ readiness 横幅

        /// <summary>横幅：文案来自 presenter（就绪返回 null → 隐藏）；未就绪=警示色、仅提示=中性色。
        /// 修复区按 Issue 的 FixKind 重建（开启脚本开关 / 退 Play / 等待文案 / 配场景指引 / 重备自检文件）。
        /// 编译期间追加"正在编译"提示，且所有会触发动作的按钮禁用。</summary>
        private void RenderFirstRunBanner(RunReadiness readiness, RunReadinessInput input)
        {
            var text = FirstRunPresenter.ReadinessBannerText(readiness);
            if (input.IsCompiling)
                text = string.IsNullOrEmpty(text)
                    ? "正在编译脚本，请稍候——编译完成后按钮会恢复可用。"
                    : text + "\n正在编译脚本，请稍候。";

            if (string.IsNullOrEmpty(text))
            {
                _firstRunBanner.style.display = DisplayStyle.None;
                return;
            }

            var blocking = !readiness.IsReady;
            _firstRunBanner.style.display = DisplayStyle.Flex;
            _firstRunBannerLabel.text = text;
            _firstRunBannerLabel.EnableInClassList("ax-tone-warn", blocking);
            _firstRunBannerLabel.EnableInClassList("ax-tone-neutral", !blocking);
            _firstRunBanner.style.borderLeftColor = blocking ? ParseColor("#ffc107") : ParseColor("#6e6e6e");
            var tint = blocking ? ParseColor("#ffc107") : ParseColor("#6e6e6e");
            tint.a = 0.10f; // 对齐主题 token 的 warn-soft 弱底色
            _firstRunBanner.style.backgroundColor = tint;

            _firstRunBannerActions.Clear();
            if (readiness.Issues == null)
                return;

            foreach (var issue in readiness.Issues)
            {
                if (issue == null)
                    continue;
                switch (issue.FixKind)
                {
                    case ReadinessFixKind.EnableAgentDefine:
                    {
                        var button = new Button(AgentDefineMenu.Enable) { text = "启用 AXU_TEST_AGENT" };
                        button.AddToClassList("ax-secondary-button");
                        button.tooltip = string.IsNullOrEmpty(issue.FixHint)
                            ? "开启运行所需的脚本开关；会触发一次脚本重编译，完成后即可发起。"
                            : issue.FixHint;
                        button.SetEnabled(!input.IsCompiling);
                        _firstRunBannerActions.Add(button);
                        break;
                    }
                    case ReadinessFixKind.ExitPlayMode:
                    {
                        var button = new Button(() => EditorApplication.isPlaying = false) { text = "退出 Play 模式" };
                        button.AddToClassList("ax-secondary-button");
                        button.tooltip = string.IsNullOrEmpty(issue.FixHint)
                            ? "结束当前 Play 会话后即可发起运行。"
                            : issue.FixHint;
                        _firstRunBannerActions.Add(button);
                        break;
                    }
                    case ReadinessFixKind.WaitUntilIdle:
                    {
                        var wait = new Label("运行中，结束后可发起。");
                        wait.AddToClassList("ax-overview-note");
                        _firstRunBannerActions.Add(wait);
                        break;
                    }
                    case ReadinessFixKind.ConfigureBuildScenes:
                    {
                        var guide = new Label(string.IsNullOrEmpty(issue.FixHint)
                            ? "请在顶部菜单 File → Build Profiles（旧版为 Build Settings）的场景列表里至少加入一个场景。"
                            : issue.FixHint);
                        guide.AddToClassList("ax-overview-note");
                        _firstRunBannerActions.Add(guide);

                        var open = new Button(TryOpenBuildProfiles) { text = "打开 Build Profiles" };
                        open.AddToClassList("ax-secondary-button");
                        open.tooltip = "尝试直接打开 Build Profiles 窗口；打不开时请按上面的文字路径手动打开。";
                        _firstRunBannerActions.Add(open);
                        break;
                    }
                    case ReadinessFixKind.CreateSuiteFile:
                    {
                        var button = new Button(() =>
                        {
                            FirstRunSuiteCatalog.EnsureSelfCheckSuiteFile(out var ensureError);
                            UpdateStatus(string.IsNullOrEmpty(ensureError)
                                ? "自检所需的文件已重新就绪，可再次点击运行按钮。"
                                : "自检文件准备失败：" + ensureError);
                        }) { text = "重新准备自检" };
                        button.AddToClassList("ax-secondary-button");
                        button.tooltip = string.IsNullOrEmpty(issue.FixHint)
                            ? "重新生成自检所需的临时文件。"
                            : issue.FixHint;
                        button.SetEnabled(!input.IsCompiling);
                        _firstRunBannerActions.Add(button);
                        break;
                    }
                }
            }
        }

        /// <summary>尝试打开 Build Profiles（旧版退回 Build Settings）；打不开就静默降级为纯文字指引
        /// （横幅里已写明手动路径，不打断用户）。</summary>
        private static void TryOpenBuildProfiles()
        {
            try
            {
                if (EditorApplication.ExecuteMenuItem("File/Build Profiles"))
                    return;
                EditorApplication.ExecuteMenuItem("File/Build Settings");
            }
            catch
            {
                // 静默降级：横幅文字已给出手动路径
            }
        }

        // ------------------------------------------------------------------ 自检结论

        /// <summary>结论行刷新（随 800ms 轮询）：认领本次发起的运行（运行标识一致），运行中给进度、
        /// 到终态用 presenter 文案渲染一次结论（成功=正常色、失败=警示色）并清零认领——此后其他运行
        /// 不会把已交付的结论误报成"没等到结论"。域重载后发起标识丢失时，按"最近一次终态属于
        /// 自检套件"兜底回显并标注（历史）（与总览页"（历史报告）"同口径）；本次发起的正常认领不加标注。</summary>
        private void RefreshFirstRunOutcome()
        {
            if (_firstRunOutcomeLabel == null)
                return;

            var status = AxuScenarioRunStatus.TryRead();
            var hasPending = !string.IsNullOrEmpty(_firstRunPendingRunId);

            if (hasPending &&
                (status == null || !string.Equals(status.RunId, _firstRunPendingRunId, StringComparison.Ordinal)))
            {
                // 状态文件还不属于本次发起（等 Agent 写出 / 已被别的运行取代）：认领凭据以运行记录为准，
                // 记录不再属于本次发起（被其他运行顶替，或那次运行已被看门狗收尾删除）时作废认领，
                // 避免结论行永久卡在"已发起"
                var record = ScenarioRunRecord.TryRead();
                var recordStillOurs = record != null &&
                                      string.Equals(record.RunId, _firstRunPendingRunId, StringComparison.Ordinal);
                if (!recordStillOurs)
                {
                    _firstRunPendingRunId = null;
                    _firstRunOutcomeLabel.text = "这次自检没有等到结论（期间发起了其他运行）——可再点一次上方的自检按钮。";
                    SetFirstRunOutcomeTone("ax-tone-neutral");
                }
                return;
            }

            if (!hasPending &&
                (status == null ||
                 !string.Equals(status.SuiteId, FirstRunSuiteCatalog.SelfCheckSuiteId, StringComparison.OrdinalIgnoreCase)))
                return; // 没有在途的本次发起，也没有可兜底回显的自检终态 → 不动结论行

            _firstRunOutcomeLabel.style.display = DisplayStyle.Flex;

            if (status.IsRunning)
            {
                _firstRunOutcomeLabel.text = $"自检运行中（第 {status.CompletedCount}/{status.TotalCount} 项）…";
                SetFirstRunOutcomeTone("ax-tone-neutral");
                return;
            }

            if (!status.IsTerminal)
                return;

            var runId = status.RunId ?? string.Empty;
            if (_firstRunLastDeliveredRunId == runId)
                return; // 同一终态只渲染一次

            _firstRunLastDeliveredRunId = runId;
            _firstRunPendingRunId = null; // 本次已交付：清零认领，此后其他运行不再触发"没等到结论"的误报

            var succeeded = status.IsCompleted && !ScenarioRunStatusPresenter.IsFailureVerdict(status);
            var conclusion = FirstRunPresenter.DescribeSelfCheckOutcome(status);
            if (!hasPending)
                conclusion += "（历史）"; // 兜底回显的是更早的终态，注明历史；本次发起的正常认领不加
            _firstRunOutcomeLabel.text = conclusion;
            SetFirstRunOutcomeTone(succeeded ? "ax-tone-ok" : "ax-tone-bad");
        }

        /// <summary>结论行着色（ok/warn/bad/neutral 四选一，写法与 Scenario 页进度行一致）。</summary>
        private void SetFirstRunOutcomeTone(string toneClass)
        {
            foreach (var tone in new[] { "ax-tone-ok", "ax-tone-warn", "ax-tone-bad", "ax-tone-neutral" })
                _firstRunOutcomeLabel.EnableInClassList(tone, tone == toneClass);
        }

        // ------------------------------------------------------------------ 小工具

        private static void AddFirstRunNote(VisualElement parent, string text)
        {
            var note = new Label(text);
            note.AddToClassList("ax-overview-note");
            parent.Add(note);
        }
    }
}
