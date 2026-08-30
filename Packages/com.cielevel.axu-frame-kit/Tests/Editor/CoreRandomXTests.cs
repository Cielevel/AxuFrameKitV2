using AxuFrameKit._Core.Utility.RandomX;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Core/Util] RandomX 抽查：AliasSampler 分布收敛、RandomId 唯一性与字符集。
    /// 注：AliasMethod/RandomId 内部使用无参 System.Random（不可注入种子），
    /// 故分布断言采用大样本 + 宽容差保证稳定性。
    /// </summary>
    [Category("Axu/Core/Util")]
    public class CoreRandomXTests
    {
        [Test]
        public void AliasSampler_万次采样_频率收敛至权重()
        {
            // Arrange：权重 1:3 → 期望频率 25% / 75%
            var items = new[] { "A", "B" };
            var sampler = new AliasSampler<string>(items, new double[] { 1.0, 3.0 });
            const int n = 20000;

            // Act
            var samples = sampler.Sample(n);
            int countA = 0;
            foreach (var s in samples)
                if (s == "A")
                    countA++;

            // Assert：±2% 容差（约 6.5 个标准差，运行间极稳定）
            Assert.That(samples.Length, Is.EqualTo(n));
            Assert.That((float)countA / n, Is.EqualTo(0.25f).Within(0.02f));
            Assert.That((float)(n - countA) / n, Is.EqualTo(0.75f).Within(0.02f));
        }

        [Test]
        public void RandomId_批量生成_唯一且符合字符集()
        {
            // Arrange
            const string charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            // Act
            var ids = RandomId.Get(12, 200);

            // Assert：长度、字符集、唯一性
            Assert.That(ids.Length, Is.EqualTo(200));
            Assert.That(new System.Collections.Generic.HashSet<string>(ids).Count, Is.EqualTo(200),
                "200 个长度 12 的随机 ID 不应出现碰撞");
            foreach (var id in ids)
            {
                Assert.That(id.Length, Is.EqualTo(12));
                foreach (var c in id)
                    Assert.That(charset.IndexOf(c), Is.GreaterThanOrEqualTo(0), $"非法字符 '{c}'");
            }
        }
    }
}
