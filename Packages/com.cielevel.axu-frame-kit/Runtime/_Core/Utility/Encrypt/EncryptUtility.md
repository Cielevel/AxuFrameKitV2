# AxuFrameKit 加密模块使用指南

> 纯 C# 可移植加密解密工具集，不依赖 Unity，可运行于任何 .NET 环境。

---

## 目录

- [快速开始](#快速开始)
- [哈希函数](#哈希函数)
- [AES 对称加密](#aes-对称加密)
- [RSA 非对称加密](#rsa-非对称加密)
- [编码工具](#编码工具)
- [密钥派生](#密钥派生)
- [HMAC 消息认证](#hmac-消息认证)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

---

## 快速开始

### 引入命名空间

```csharp
using AxuFrameKit._Core.Utility.Encrypt;
```

### 核心 API 一览

| 功能 | 方法 | 说明 |
|------|------|------|
| SHA256 哈希 | `EncryptHelper.HashSHA256(input)` | 返回十六进制字符串 |
| AES 加密 | `EncryptHelper.AesEncrypt(plain, key)` | 旧口令直传格式，仅用于兼容既有密文 |
| AES 解密 | `EncryptHelper.AesDecrypt(cipher, key)` | 配套旧格式解密 |
| AES 加密（推荐） | `EncryptHelper.AesEncryptWithPbkdf2(plain, pwd)` | 口令经 PBKDF2 派生密钥，返回 Base64 密文 |
| AES 解密（推荐） | `EncryptHelper.AesDecryptWithPbkdf2(cipher, pwd)` | 迭代次数自密文头读取 |
| RSA 密钥对 | `EncryptHelper.GenerateRsaKeyPair()` | 生成公钥/私钥 |
| Base64 编码 | `EncryptHelper.ToBase64(data)` | 字节数组转字符串 |
| 十六进制编码 | `EncryptHelper.ToHex(data)` | 字节数组转字符串 |
| 密钥派生 | `EncryptHelper.DeriveKey(pwd, salt, len)` | 从密码生成密钥 |
| HMAC 计算 | `EncryptHelper.ComputeHMAC(msg, key)` | 消息认证码 |

---

## 哈希函数

### SHA256（推荐）

适用于密码存储、数据完整性校验、数字签名等安全场景。

```csharp
// 字符串哈希
string hash = EncryptHelper.HashSHA256("Hello World");
// 输出: a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e

// 字节数组哈希
byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello World");
byte[] hashBytes = EncryptHelper.SHA256.ComputeHash(data);

// 文件哈希
byte[] fileHash = EncryptHelper.SHA256.ComputeFileHash(@"C:\path\to\file.txt");
string fileHashStr = BitConverter.ToString(fileHash).Replace("-", "").ToLower();
```

### SHA512（更安全）

需要更高安全性时使用，输出 128 字符的十六进制字符串。

```csharp
string hash = EncryptHelper.HashSHA512("Hello World");
// 输出: 2c74fd17edafd80e8447b0d46741ee243b7eb74dd2149a0...
```

### MD5（仅用于校验）

**警告**：MD5 存在碰撞漏洞，仅适用于非安全场景（如文件校验），不要用于密码存储。

```csharp
string hash = EncryptHelper.HashMD5("Hello World");
// 输出: b10a8db164e0754105b7a99be72e3fe5
```

---

## AES 对称加密

### 基本用法

AES 使用 CBC 模式 + 随机 IV，每次加密相同明文会产生不同密文。口令场景用 PBKDF2 版本（口令经派生后作密钥，避免弱密钥风险）：

```csharp
// 加密
string plainText = "这是敏感数据";
string password = "我的口令";
string encrypted = EncryptHelper.AesEncryptWithPbkdf2(plainText, password);

// 解密
string decrypted = EncryptHelper.AesDecryptWithPbkdf2(encrypted, password);
// decrypted == "这是敏感数据"
```

`AesEncrypt` / `AesDecrypt` 是口令直传的旧格式，**保留仅为解既有密文**，不要用于新数据。

### 高级用法

```csharp
// 生成随机密钥（32字节）
byte[] key = EncryptHelper.GenerateAesKey();

// 生成随机 IV（16字节）
byte[] iv = EncryptHelper.GenerateAesIV();

// 使用字节数组加密
byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes("数据");
var result = EncryptHelper.AES.Encrypt(plainBytes, key);
// result.CipherData - 加密后的数据
// result.IV - 使用的 IV

// 解密
byte[] decrypted = EncryptHelper.AES.Decrypt(result.CipherData, key, result.IV);
```

### 密文格式说明

`EncryptString` 返回的密文格式为：`Base64(IV[16字节] + 密文)`

解密时自动从密文中提取 IV，无需单独存储。

---

## XOR 对称加密（按位异或）

### 基本用法

简单的轻量级对称加密方法，**安全性极低**，仅适用于需要非常轻量的防简单查看或数据混淆场景中。不推荐用于高敏感数据保存。

```csharp
// 加密
string plainText = "需要混淆的数据";
string key = "XorKey";
string encrypted = EncryptHelper.XorEncrypt(plainText, key);

// 解密
string decrypted = EncryptHelper.XorDecrypt(encrypted, key);
// decrypted == "需要混淆的数据"
```

### 高级用法

也可以直接访问底层的字节数组方法。加解密不需要 IV。

```csharp
// 使用字节数组加密
byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes("数据");
byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes("XorKey");
var result = EncryptHelper.XOR.Encrypt(plainBytes, keyBytes);
// result.CipherData - 加密后的混淆数据
// result.IV - 始终为空数组 byte[0]

// 解密
byte[] decrypted = EncryptHelper.XOR.Decrypt(result.CipherData, keyBytes);
```

### 缺点警告
- XOR 没有扩散机制：相同明文配合相同的短密钥将具有明显规律。
- 若明文全部为 `0` 字节，加密后的就是密钥循环序列本身。

---

## RSA 非对称加密

### 生成密钥对

```csharp
// 生成 2048 位密钥对（默认）
var (publicKey, privateKey) = EncryptHelper.GenerateRsaKeyPair();

// 生成 4096 位密钥对（更安全，但更慢）
var (publicKey, privateKey) = EncryptHelper.GenerateRsaKeyPair(4096);
```

### 加密与解密

```csharp
string plainText = "机密信息";

// 使用公钥加密
string encrypted = EncryptHelper.RsaEncrypt(plainText, publicKey);

// 使用私钥解密
string decrypted = EncryptHelper.RsaDecrypt(encrypted, privateKey);
```

### 签名与验证

```csharp
byte[] data = System.Text.Encoding.UTF8.GetBytes("要签名的数据");

// 使用私钥签名
byte[] signature = EncryptHelper.RSA.Sign(data, privateKey);

// 使用公钥验证
bool isValid = EncryptHelper.RSA.Verify(data, signature, publicKey);
// isValid == true 表示签名有效
```

### 使用场景

| 场景 | 使用密钥 |
|------|----------|
| 加密通信 | 对方公钥加密，己方私钥解密 |
| 数字签名 | 己方私钥签名，对方公钥验证 |
| 密钥交换 | 双方交换公钥，安全传输对称密钥 |

---

## 编码工具

### Base64

```csharp
byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello");

// 标准编码
string base64 = EncryptHelper.ToBase64(data);
// "SGVsbG8="

// 解码
byte[] decoded = EncryptHelper.FromBase64(base64);

// URL 安全编码（用于 URL 参数）
string urlSafe = EncryptHelper.ToBase64UrlSafe(data);
// "SGVsbG8"（无填充，+/- 替换）

// URL 安全解码
byte[] urlSafeDecoded = EncryptHelper.FromBase64UrlSafe(urlSafe);
```

### 十六进制

```csharp
byte[] data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };

// 编码
string hex = EncryptHelper.ToHex(data);
// "48656c6c6f"

// 解码
byte[] decoded = EncryptHelper.FromHex("48656c6c6f");
```

---

## 密钥派生

### PBKDF2

从用户密码安全地派生加密密钥。

```csharp
string password = "用户密码123";

// 生成随机盐值
byte[] salt = EncryptHelper.GenerateSalt(16);

// 派生 32 字节密钥（10000 次迭代）
byte[] key = EncryptHelper.DeriveKey(password, salt, 32, 10000);

// 便捷方法：同时生成盐值和密钥
var (derivedKey, newSalt) = EncryptHelper.DeriveKeyWithSalt(password, 32, 16);
```

### 存储密码的最佳实践

```csharp
// 注册时
string password = "用户密码";
var (key, salt) = EncryptHelper.DeriveKeyWithSalt(password, 32, 16);

// 存储 salt 和 key（都是 byte[]，可转为 Base64）
string saltStr = EncryptHelper.ToBase64(salt);
string keyStr = EncryptHelper.ToBase64(key);
// 将 saltStr 和 keyStr 存入数据库

// 登录验证时
string inputPassword = "用户输入的密码";
byte[] storedSalt = EncryptHelper.FromBase64(saltStr);
byte[] derivedKey = EncryptHelper.DeriveKey(inputPassword, storedSalt, 32, 10000);

// 比较派生的密钥与存储的密钥
bool valid = derivedKey.SequenceEqual(EncryptHelper.FromBase64(keyStr));
```

---

## HMAC 消息认证

用于验证消息的完整性和真实性。

```csharp
string message = "重要消息";
string key = "共享密钥";

// 计算 HMAC
string hmac = EncryptHelper.ComputeHMAC(message, key);
// 输出: 64 字符的十六进制字符串

// 验证 HMAC
bool isValid = EncryptHelper.VerifyHMAC(message, key, hmac);
// isValid == true 表示消息未被篡改
```

### 应用场景

- API 请求签名验证
- 消息完整性校验
- 防止数据篡改

---

## 最佳实践

### 密钥管理

- **不要硬编码密钥**：密钥应从配置文件、环境变量或密钥管理服务获取
- **定期轮换密钥**：建议定期更换加密密钥
- **不同用途使用不同密钥**：加密、签名、HMAC 应使用不同密钥

### 安全建议

| 场景 | 推荐方案 |
|------|----------|
| 密码存储 | PBKDF2 + SHA256，迭代 10000+ 次 |
| 敏感数据加密 | AES-256-CBC |
| 密钥传输 | RSA 加密对称密钥 |
| 消息验证 | HMAC-SHA256 |
| 文件校验 | SHA256 或 SHA512 |

### 不要做的事

- 不要使用 MD5 存储密码
- 不要使用 ECB 模式（相同明文产生相同密文）
- 不要在日志中输出密钥或密文
- 不要使用简单字符串作为密钥（应使用 PBKDF2 派生）

---

## 常见问题

### Q: AES 加密后密文长度是多少？

A: 密文长度 = `16字节IV` + `向上取整(明文长度/16) * 16`，然后 Base64 编码。

### Q: RSA 加密的数据长度有限制吗？

A: 有限制。2048 位密钥的最长加密尺寸由使用的填充模式（即运行平台）决定：
- 在 Unity 环境下（默认使用向下兼容的 PKCS#1 填充）：最多可加密 245 字节。
- 在标准 .NET 环境下（默认使用更高安全性的 OAEP-SHA256 填充）：最多可加密 190 字节。
大数据应始终使用对称加密（如 AES）处理，然后再用 RSA 加密该对称密钥。

### Q: 如何选择哈希算法？

A:
- 安全场景（密码、签名）：SHA256 或 SHA512
- 非安全场景（文件校验）：MD5 或 SHA256

### Q: 迭代次数设多少合适？

A: PBKDF2 建议至少 10000 次，可根据性能需求调整。更多迭代 = 更安全但更慢。

### Q: 代码可以在非 Unity 环境运行吗？

A: 可以。本模块仅依赖 .NET BCL，可在任何 .NET 环境（.NET Framework、.NET Core、.NET 5+）运行。

---

## 异常处理

所有加密操作失败时会抛出 `EncryptException`：

```csharp
using AxuFrameKit._Core.Utility.Encrypt.Core;

try
{
    string decrypted = EncryptHelper.AesDecrypt(cipherText, key);
}
catch (EncryptException ex)
{
    // ex.Operation - 操作类型（Encrypt, Decrypt, Hash 等）
    // ex.Message - 错误详情
    Console.WriteLine($"加密操作失败: [{ex.Operation}] {ex.Message}");
}
```

---

## 命名空间汇总

```csharp
using AxuFrameKit._Core.Utility.Encrypt;              // EncryptHelper
using AxuFrameKit._Core.Utility.Encrypt.Core;         // 接口和异常
using AxuFrameKit._Core.Utility.Encrypt.Hash;         // 哈希实现
using AxuFrameKit._Core.Utility.Encrypt.Symmetric;    // AES
using AxuFrameKit._Core.Utility.Encrypt.Asymmetric;   // RSA
using AxuFrameKit._Core.Utility.Encrypt.Encoding;     // Base64, Hex
using AxuFrameKit._Core.Utility.Encrypt.KeyDerivation; // PBKDF2
using AxuFrameKit._Core.Utility.Encrypt.Mac;          // HMAC
```
