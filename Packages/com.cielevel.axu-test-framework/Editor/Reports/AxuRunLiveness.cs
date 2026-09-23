namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 运行在途判定（互斥闸）的共享策略常量：UTF 运行、scenario 运行、Editor 侧运行记录三者
    /// 共用同一陈旧阈值——三处各写一个字面量会随时间漂移成"状态已判陈旧、记录仍判在途"的静默分叉。
    /// <para>阈值的含义：<c>running</c> 状态的**最后活动时间**（心跳 &gt; 状态文件写入时间 &gt; 启动时间）
    /// 距今超过该时长即视为中断残留并放行新的运行。长跑测试只要还在心跳就恒判在途。</para>
    /// </summary>
    public static class AxuRunLiveness
    {
        /// <summary>运行状态的中断残留阈值（分钟）。</summary>
        public const int StaleMinutes = 15;

        /// <summary>运行发起后的启动宽限期（秒）：进 Play 的转换窗口（域重载/场景装载）内
        /// 看门狗不把"尚未真正开始"的运行判为中断。</summary>
        public const int StartGraceSeconds = 10;
    }
}
