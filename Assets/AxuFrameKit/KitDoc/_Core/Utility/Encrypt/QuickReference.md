# 加密模块快速参考卡片

## 哈希函数

```csharp
EncryptHelper.HashSHA256("text")    // → 64字符十六进制
EncryptHelper.HashSHA512("text")    // → 128字符十六进制
EncryptHelper.HashMD5("text")       // → 32字符十六进制（仅校验用）
```

## AES 对称加密

```csharp
// 加密解密
EncryptHelper.AesEncrypt("明文", "密钥")    // → Base64密文
EncryptHelper.AesDecrypt("密文", "密钥")    // → 明文

// 生成密钥
EncryptHelper.GenerateAesKey()              // → 32字节密钥
EncryptHelper.GenerateAesIV()               // → 16字节IV
```

## RSA 非对称加密

```csharp
// 生成密钥对
var (pub, priv) = EncryptHelper.GenerateRsaKeyPair()

// 加密解密
EncryptHelper.RsaEncrypt("明文", publicKey)   // → Base64密文
EncryptHelper.RsaDecrypt("密文", privateKey)  // → 明文

// 签名验证
byte[] sig = EncryptHelper.RSA.Sign(data, privateKey)
bool ok = EncryptHelper.RSA.Verify(data, sig, publicKey)
```

## 编码转换

```csharp
// Base64
EncryptHelper.ToBase64(bytes)           // → Base64字符串
EncryptHelper.FromBase64(str)           // → byte[]
EncryptHelper.ToBase64UrlSafe(bytes)    // → URL安全Base64

// 十六进制
EncryptHelper.ToHex(bytes)              // → "48656c6c6f"
EncryptHelper.FromHex("48656c6c6f")     // → byte[]
```

## 密钥派生

```csharp
// 生成盐值
byte[] salt = EncryptHelper.GenerateSalt(16)

// 派生密钥
byte[] key = EncryptHelper.DeriveKey("密码", salt, 32, 10000)

// 便捷方法
var (key, salt) = EncryptHelper.DeriveKeyWithSalt("密码", 32, 16)
```

## HMAC 消息认证

```csharp
string hmac = EncryptHelper.ComputeHMAC("消息", "密钥")
bool valid = EncryptHelper.VerifyHMAC("消息", "密钥", hmac)
```

---

## 使用场景速查

| 需求 | 推荐方案 | 示例 |
|------|----------|------|
| 密码存储 | PBKDF2 | `DeriveKey(password, salt, 32)` |
| 敏感数据 | AES | `AesEncrypt/AesDecrypt` |
| 密钥传输 | RSA | `RsaEncrypt/RsaDecrypt` |
| 数据完整性 | SHA256 | `HashSHA256` |
| 消息验证 | HMAC | `ComputeHMAC/VerifyHMAC` |
| 文件校验 | MD5/SHA256 | `MD5/SHA256.ComputeFileHash` |
