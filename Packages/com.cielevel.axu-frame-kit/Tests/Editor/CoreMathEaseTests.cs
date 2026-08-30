using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit._Core.Utility.Math.Interpolation.Ease;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Core/Util] Interpolation/Ease 抽查：边界与中点值、EaseFactory 映射。
    /// </summary>
    [Category("Axu/Core/Util")]
    public class CoreMathEaseTests
    {
        [Test]
        public void Ease_边界与中点_返回预期值()
        {
            // Arrange
            var linear = new Linear();
            var inQuad = new InQuad();
            var outQuad = new OutQuad();

            // Act & Assert：边界
            Assert.That(inQuad.Evaluate(0f), Is.EqualTo(0f));
            Assert.That(inQuad.Evaluate(1f), Is.EqualTo(1f));
            Assert.That(outQuad.Evaluate(1f), Is.EqualTo(1f));

            // 中点：InQuad=t^2=0.25，OutQuad=t(2-t)=0.75，Linear=t
            Assert.That(inQuad.Evaluate(0.5f), Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(outQuad.Evaluate(0.5f), Is.EqualTo(0.75f).Within(1e-6f));
            Assert.That(linear.Evaluate(0.3f), Is.EqualTo(0.3f).Within(1e-6f));

            // EaseFactory 映射正确且缓动单例可用
            Assert.That(EaseFactory.Get(EaseType.InQuad), Is.InstanceOf<InQuad>());
            Assert.That(EaseFactory.Get(EaseType.Linear).Evaluate(0.25f), Is.EqualTo(0.25f).Within(1e-6f));
        }
    }
}
