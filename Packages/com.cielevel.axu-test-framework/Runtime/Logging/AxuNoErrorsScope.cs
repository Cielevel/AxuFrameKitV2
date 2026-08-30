using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace AxuTestFramework
{
    /// <summary>
    /// "无错误"作用域：构造时开始监视日志，<see cref="Dispose"/> 时若捕获到 Error / Exception / Assert，
    /// 经 Assert.Fail 令测试失败并附汇总消息。配合 using 使用：
    /// <code>
    /// using (AxuLogWatch.ExpectNoErrors())
    /// {
    ///     DoSomethingThatMustNotError();
    /// }
    /// </code>
    /// </summary>
    public sealed class AxuNoErrorsScope : IDisposable
    {
        private readonly AxuLogWatch _watch;

        /// <summary>底层监视器（可读取 <see cref="AxuLogWatch.Errors"/> 明细而不终止测试）。</summary>
        public AxuLogWatch Watch
        {
            get { return _watch; }
        }

        /// <summary>创建作用域并开始监视日志。</summary>
        public AxuNoErrorsScope()
            : this(new AxuLogWatch())
        {
        }

        /// <summary>用外部创建的监视器创建作用域（ Dispose 时一并反挂）。</summary>
        /// <param name="watch">日志监视器，不能为 null。</param>
        public AxuNoErrorsScope(AxuLogWatch watch)
        {
            _watch = watch ?? throw new ArgumentNullException(nameof(watch));
        }

        /// <summary>
        /// 结束作用域：先反挂监视回调，再检查捕获结果——存在错误则 Assert.Fail（最多列出 10 条样例）。
        /// </summary>
        public void Dispose()
        {
            IReadOnlyList<LogEntry> errors = _watch.Errors;
            _watch.Dispose();

            if (errors.Count == 0)
                return;

            StringBuilder message = new StringBuilder();
            message.AppendLine($"AxuNoErrorsScope：作用域内捕获到 {errors.Count} 条未预期的错误日志：");
            int shown = Math.Min(errors.Count, 10);
            for (int i = 0; i < shown; i++)
            {
                message.Append("  [").Append(errors[i].Type).Append("] ").AppendLine(errors[i].Message);
            }
            if (errors.Count > shown)
                message.AppendLine($"  …以及另外 {errors.Count - shown} 条");
            if (_watch.DroppedCount > 0)
                message.AppendLine($"  另有 {_watch.DroppedCount} 条因超出捕获上限被丢弃");

            Assert.Fail(message.ToString());
        }
    }
}
