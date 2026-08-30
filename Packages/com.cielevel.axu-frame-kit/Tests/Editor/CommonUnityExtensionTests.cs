using AxuFrameKit.Common.Extension;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Common/Util] Common 层 Unity 类型扩展抽查：Color / Vector / RectTransform。
    /// </summary>
    [Category("Axu/Common/Util")]
    public class CommonUnityExtensionTests
    {
        [Test]
        public void ColorExtension_Hex往返_无效输入回退白色()
        {
            // Arrange
            var color = new Color(1f, 85f / 255f, 0f, 170f / 255f);

            // Act & Assert：ToHexString → "#RRGGBBAA"
            Assert.That(color.ToHexString(), Is.EqualTo("#FF5500AA"));
            Assert.That(color.ToHexString(false), Is.EqualTo("#FF5500"));

            // FromHexString 往返（Color32 量化后比较）
            var parsed = ColorExtension.FromHexString("#FF5500AA");
            Assert.That((Color32)parsed, Is.EqualTo(new Color32(0xFF, 0x55, 0x00, 0xAA)));

            // 无效格式（长度不符）→ 回退白色并输出告警（预期日志须声明，匹配完整原文）
            LogAssert.Expect(LogType.Warning, "ColorExtension.FromHexString: 无效的十六进制格式 '12345'");
            Assert.That(ColorExtension.FromHexString("12345"), Is.EqualTo(Color.white));
        }

        [Test]
        public void Vector与RectTransform扩展_分量修改与尺寸锚点()
        {
            // Arrange & Act：Vector3 链式分量修改与 XZ 平面距离
            var v = new Vector3(1f, 2f, 3f).WithX(9f);
            float flat = new Vector3(1f, 0f, 4f).FlatDistanceXZ(new Vector3(4f, 9f, 0f));

            // Assert：WithX 只改 X；FlatDistanceXZ = sqrt(3^2 + 4^2) = 5
            Assert.That(v, Is.EqualTo(new Vector3(9f, 2f, 3f)));
            Assert.That(flat, Is.EqualTo(5f).Within(1e-4f));

            // RectTransform：SetSize 与 SetAnchorsCenter
            var go = new GameObject("rt-axu-test", typeof(RectTransform));
            try
            {
                var rt = go.GetComponent<RectTransform>();
                rt.SetSize(200f, 100f);
                Assert.That(rt.rect.width, Is.EqualTo(200f).Within(0.01f));
                Assert.That(rt.rect.height, Is.EqualTo(100f).Within(0.01f));

                rt.SetAnchorsCenter();
                Assert.That(rt.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(rt.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(rt.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
