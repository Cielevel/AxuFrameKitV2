using System.Text;
using AxuFrameKit._Core.Utility.Encrypt;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Core/Util] Encrypt 工具集抽查：AES 往返、MD5/SHA256 已知向量、Base64/Hex 编码。
    /// </summary>
    [Category("Axu/Core/Util")]
    public class CoreEncryptTests
    {
        [Test]
        public void AesCrypto_字符串加密解密往返_还原原文()
        {
            // Arrange
            var aes = new AesCrypto();
            const string plain = "阿修框架 AxuFrameKit 123!";
            const string key = "axu-test-key";

            // Act
            var cipher1 = aes.EncryptString(plain, key);
            var cipher2 = aes.EncryptString(plain, key);

            // Assert：密文非原文、两次加密随机 IV 不同、往返还原
            Assert.That(cipher1, Is.Not.EqualTo(plain));
            Assert.That(cipher1, Is.Not.EqualTo(cipher2), "随机 IV 下同一明文两次加密应产生不同密文");
            Assert.That(aes.DecryptString(cipher1, key), Is.EqualTo(plain));
            Assert.That(aes.DecryptString(cipher2, key), Is.EqualTo(plain));
        }

        [Test]
        public void MD5与SHA256_标准向量_摘要一致()
        {
            // Arrange & Act & Assert：RFC 1321 / NIST 标准向量（UTF8）
            Assert.That(new MD5Hasher().ComputeHash("abc"),
                Is.EqualTo("900150983cd24fb0d6963f7d28e17f72"));
            Assert.That(new MD5Hasher().ComputeHash(string.Empty),
                Is.EqualTo("d41d8cd98f00b204e9800998ecf8427e"));
            Assert.That(new SHA256Hasher().ComputeHash("abc"),
                Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        }

        [Test]
        public void Base64与Hex_已知向量_往返正确()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Axu!");

            // Assert：Base64 已知向量与往返
            Assert.That(Base64Encoder.Encode(data), Is.EqualTo("QXh1IQ=="));
            CollectionAssert.AreEqual(data, Base64Encoder.Decode("QXh1IQ=="));

            // URL 安全变体：不含 +/=/，且可还原
            var tricky = new byte[] { 0xFB, 0xEF, 0xBE, 0xBF };
            var urlSafe = Base64Encoder.EncodeUrlSafe(tricky);
            Assert.That(urlSafe, Does.Not.Contain("+").And.Not.Contain("/").And.Not.Contain("="));
            CollectionAssert.AreEqual(tricky, Base64Encoder.DecodeUrlSafe(urlSafe));

            // Hex 已知向量、大小写不敏感解码、格式校验
            Assert.That(HexEncoder.Encode(data), Is.EqualTo("41787521"));
            CollectionAssert.AreEqual(data, HexEncoder.Decode("41787521"));
            CollectionAssert.AreEqual(data, HexEncoder.Decode("41787521".ToUpperInvariant()));
            Assert.That(HexEncoder.IsValidHex("DEADBEEF"), Is.True);
            Assert.That(HexEncoder.IsValidHex("XYZ"), Is.False);
        }
    }
}
