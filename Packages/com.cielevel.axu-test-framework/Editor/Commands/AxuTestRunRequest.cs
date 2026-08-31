namespace AxuTestFramework.Editor
{
    using System.Collections.Generic;

    /// <summary>
    /// axu_test_run 的运行请求（pipeline 命令与看板窗口共用的入参模型）。
    /// </summary>
    public sealed class AxuTestRunRequest
    {
        /// <summary>运行模式："editor" / "playmode" / "all"。默认 "editor"。</summary>
        public string Mode { get; set; } = "editor";

        /// <summary>过滤值（与 <see cref="FilterType"/> 配合；testName 为大小写不敏感的包含匹配）。</summary>
        public string Filter { get; set; } = "";

        /// <summary>过滤类型："testName" / "assembly" / "category"。默认 "testName"。</summary>
        public string FilterType { get; set; } = "testName";

        /// <summary>类别过滤便捷参数（等价 filter_type=category）；与 Filter 同时给定时两者都生效。</summary>
        public string Category { get; set; } = "";

        /// <summary>程序集过滤便捷参数（等价 filter_type=assembly）；与 Filter 同时给定时两者都生效。</summary>
        public string Assembly { get; set; } = "";

        /// <summary>
        /// "选定测试"精确全名清单。非空时启用 UTF Filter.testNames 通道（逐字全名匹配，参数化用例含括号段），
        /// 优先级高于 Filter/FilterType 的 testName 子串通道（此时后者不参与，避免两组全名条件叠加出 0 匹配）；
        /// 与安全程序集清单（assemblyNames）为 AND 语义，越界名单自动无效。
        /// </summary>
        public List<string> TestNames { get; set; }

        /// <summary>运行前清空 Editor Console 窗口。默认 true。</summary>
        public bool ClearConsole { get; set; } = true;

        /// <summary>是否落盘报告 JSON。默认 true。</summary>
        public bool Report { get; set; } = true;

        /// <summary>报告文件路径覆盖（相对工程根或绝对路径）；空为默认目录。</summary>
        public string OutPath { get; set; } = "";

        /// <summary>EditMode 完成等待预算（毫秒）。默认 120000。</summary>
        public int TimeoutMs { get; set; } = 120000;
    }
}
