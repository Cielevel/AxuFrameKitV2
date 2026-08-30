namespace AxuTestFramework
{
    /// <summary>
    /// 报告的测试过滤条件描述（schema <c>axu-test-report/1</c>）。
    /// </summary>
    public class FilterInfo
    {
        /// <summary>过滤类型："assembly" / "testName" / "category" / "fullPath"；无过滤时为 "none"。</summary>
        public string Type { get; set; }

        /// <summary>过滤值；无过滤时为 null 或空字符串。</summary>
        public string Value { get; set; }
    }
}
