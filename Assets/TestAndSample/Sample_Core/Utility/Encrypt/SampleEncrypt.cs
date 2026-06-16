using UnityEngine;
using AxuFrameKit._Core.Utility.Encrypt;
using System.Text;
using System;

namespace TestAndSample.Sample_Core.Utility.Encrypt
{
    /// <summary>
    /// 加密模块运行时测试示例
    /// 在 Unity 运行时按下数字键 1-6 触发测试
    /// </summary>
    public class SampleEncrypt : MonoBehaviour
    {
        private string _testInput = "Hello AxuFrameKit Encrypt!";
        private string _testKey = "AxuFrameKit-Secret-Key";
        private (string publicKey, string privateKey) _rsaKeys;

        private void Start()
        {
            Debug.Log("<color=green>[Encrypt Sample]</color> 脚本已就绪。请在运行时按下键盘数字键 1-6 进行测试：");
            Debug.Log("<b>1:</b> 哈希测试 (SHA256, MD5)");
            Debug.Log("<b>2:</b> AES 对称加解密测试");
            Debug.Log("<b>3:</b> RSA 非对称加解密测试");
            Debug.Log("<b>4:</b> 编码转换测试 (Base64, Hex)");
            Debug.Log("<b>5:</b> 密钥派生测试 (PBKDF2)");
            Debug.Log("<b>6:</b> HMAC 消息验证测试");
            Debug.Log("<b>7:</b> XOR 轻量加解密测试");

            // 预生成 RSA 密钥对
            _rsaKeys = EncryptHelper.GenerateRsaKeyPair(2048);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) TestHash();
            if (Input.GetKeyDown(KeyCode.Alpha2)) TestAES();
            if (Input.GetKeyDown(KeyCode.Alpha3)) TestRSA();
            if (Input.GetKeyDown(KeyCode.Alpha4)) TestEncoding();
            if (Input.GetKeyDown(KeyCode.Alpha5)) TestKeyDerivation();
            if (Input.GetKeyDown(KeyCode.Alpha6)) TestHMAC();
            if (Input.GetKeyDown(KeyCode.Alpha7)) TestXOR();
        }

        private void TestHash()
        {
            Debug.Log("<color=yellow>--- [1] 哈希测试 ---</color>");
            string sha256 = EncryptHelper.HashSHA256(_testInput);
            string md5 = EncryptHelper.HashMD5(_testInput);

            Debug.Log($"输入文本: {_testInput}");
            Debug.Log($"SHA256: <color=white>{sha256}</color>");
            Debug.Log($"MD5: <color=white>{md5}</color>");
        }

        private void TestAES()
        {
            Debug.Log("<color=yellow>--- [2] AES 对称加解密测试 ---</color>");
            try
            {
                string encrypted = EncryptHelper.AesEncrypt(_testInput, _testKey);
                string decrypted = EncryptHelper.AesDecrypt(encrypted, _testKey);

                Debug.Log($"原始文本: {_testInput}");
                Debug.Log($"AES 密钥: {_testKey}");
                Debug.Log($"AES 密文 (Base64+IV): <color=white>{encrypted}</color>");
                Debug.Log($"AES 解密结果: <color=white>{decrypted}</color>");
                Debug.Log($"结果匹配: <color={(decrypted == _testInput ? "green" : "red")}>{decrypted == _testInput}</color>");
            }
            catch (EncryptException ex)
            {
                Debug.LogError($"AES 测试失败: {ex.Message}");
            }
        }

        private void TestRSA()
        {
            Debug.Log("<color=yellow>--- [3] RSA 非对称加解密测试 ---</color>");
            try
            {
                string encrypted = EncryptHelper.RsaEncrypt(_testInput, _rsaKeys.publicKey);
                string decrypted = EncryptHelper.RsaDecrypt(encrypted, _rsaKeys.privateKey);

                Debug.Log($"原始文本: {_testInput}");
                Debug.Log($"RSA 密文: <color=white>{(encrypted.Length > 50 ? encrypted.Substring(0, 50) + "..." : encrypted)}</color>");
                Debug.Log($"RSA 解密结果: <color=white>{decrypted}</color>");
                Debug.Log($"结果匹配: <color={(decrypted == _testInput ? "green" : "red")}>{decrypted == _testInput}</color>");

                // 签名测试
                byte[] data = Encoding.UTF8.GetBytes(_testInput);
                byte[] signature = EncryptHelper.RSA.Sign(data, _rsaKeys.privateKey);
                bool isValid = EncryptHelper.RSA.Verify(data, signature, _rsaKeys.publicKey);
                Debug.Log($"数字签名验证: <color={(isValid ? "green" : "red")}>{isValid}</color>");
            }
            catch (EncryptException ex)
            {
                Debug.LogError($"RSA 测试失败: {ex.Message}");
            }
        }

        private void TestEncoding()
        {
            Debug.Log("<color=yellow>--- [4] 编码转换测试 ---</color>");
            byte[] data = Encoding.UTF8.GetBytes(_testInput);

            string base64 = EncryptHelper.ToBase64(data);
            string hex = EncryptHelper.ToHex(data);
            string base64Url = EncryptHelper.ToBase64UrlSafe(data);

            Debug.Log($"输入文本: {_testInput}");
            Debug.Log($"Base64: <color=white>{base64}</color>");
            Debug.Log($"Hex: <color=white>{hex}</color>");
            Debug.Log($"Base64 URL Safe: <color=white>{base64Url}</color>");

            string decoded = Encoding.UTF8.GetString(EncryptHelper.FromBase64(base64));
            Debug.Log($"Base64 解码还原: {decoded}");
        }

        private void TestKeyDerivation()
        {
            Debug.Log("<color=yellow>--- [5] 密钥派生测试 (PBKDF2) ---</color>");
            string pwd = "SecurePassword123";
            var (derivedKey, salt) = EncryptHelper.DeriveKeyWithSalt(pwd, 32, 16);

            Debug.Log($"用户原始密码: {pwd}");
            Debug.Log($"生成的随机盐值 (16b): <color=white>{EncryptHelper.ToHex(salt)}</color>");
            Debug.Log($"派生的 256 位密钥: <color=white>{EncryptHelper.ToHex(derivedKey)}</color>");
        }

        private void TestHMAC()
        {
            Debug.Log("<color=yellow>--- [6] HMAC 消息验证测试 ---</color>");
            string hmac = EncryptHelper.ComputeHMAC(_testInput, _testKey);
            bool isValid = EncryptHelper.VerifyHMAC(_testInput, _testKey, hmac);

            Debug.Log($"输入消息: {_testInput}");
            Debug.Log($"HMAC-SHA256: <color=white>{hmac}</color>");
            Debug.Log($"认证签名验证: <color={(isValid ? "green" : "red")}>{isValid}</color>");

            // 模拟篡改测试
            bool isTamperedValid = EncryptHelper.VerifyHMAC(_testInput + "!", _testKey, hmac);
            Debug.Log($"篡改文本校验 (预期 false): <color={(!isTamperedValid ? "green" : "red")}>{isTamperedValid}</color>");
        }

        private void TestXOR()
        {
            Debug.Log("<color=yellow>--- [7] XOR 轻量加解密测试 ---</color>");
            try
            {
                string encrypted = EncryptHelper.XorEncrypt(_testInput, _testKey);
                string decrypted = EncryptHelper.XorDecrypt(encrypted, _testKey);

                Debug.Log($"原始文本: {_testInput}");
                Debug.Log($"XOR 密钥: {_testKey}");
                Debug.Log($"XOR 密文 (Base64): <color=white>{encrypted}</color>");
                Debug.Log($"XOR 解密结果: <color=white>{decrypted}</color>");
                Debug.Log($"结果匹配: <color={(decrypted == _testInput ? "green" : "red")}>{decrypted == _testInput}</color>");
            }
            catch (EncryptException ex)
            {
                Debug.LogError($"XOR 测试失败: {ex.Message}");
            }
        }
    }
}