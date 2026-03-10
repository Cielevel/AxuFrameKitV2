# EncryptHelper API 参考

## 哈希 API

### HashSHA256

计算字符串的 SHA256 哈希值。

```csharp
public static string HashSHA256(string input)
```

**参数**
- `input`: 输入字符串

**返回**
- 64 字符的十六进制哈希值（小写）

**异常**
- `EncryptException`: 输入为 null 时

---

### HashSHA512

计算字符串的 SHA512 哈希值。

```csharp
public static string HashSHA512(string input)
```

**返回**
- 128 字符的十六进制哈希值（小写）

---

### HashMD5

计算字符串的 MD5 哈希值。

```csharp
public static string HashMD5(string input)
```

**返回**
- 32 字符的十六进制哈希值（小写）

**警告**: MD5 仅适用于数据校验，不应用于安全敏感场景。

---

### IHasher 接口

```csharp
public interface IHasher
{
    byte[] ComputeHash(byte[] data);
    string ComputeHash(string input);
    byte[] ComputeFileHash(string filePath);
}
```

**实例访问**
- `EncryptHelper.SHA256`
- `EncryptHelper.SHA512`
- `EncryptHelper.MD5`

---

## AES 对称加密 API

### AesEncrypt

使用 AES-CBC 模式加密字符串。

```csharp
public static string AesEncrypt(string plainText, string key)
```

**参数**
- `plainText`: 明文字符串
- `key`: 密钥字符串（任意长度，自动调整为 32 字节）

**返回**
- Base64 编码的密文（格式：IV + 密文）

**异常**
- `EncryptException`: 明文或密钥为空时

---

### AesDecrypt

解密 AES 加密的字符串。

```csharp
public static string AesDecrypt(string cipherText, string key)
```

**参数**
- `cipherText`: Base64 编码的密文
- `key`: 密钥字符串（必须与加密时相同）

**返回**
- 解密后的明文

**异常**
- `EncryptException`: 密文格式无效或解密失败时

---

### GenerateAesKey / GenerateAesIV

```csharp
public static byte[] GenerateAesKey()  // 32 字节
public static byte[] GenerateAesIV()   // 16 字节
```

---

### ISymmetricCrypto 接口

```csharp
public interface ISymmetricCrypto
{
    SymmetricCryptoResult Encrypt(byte[] plainData, byte[] key, byte[] iv = null);
    byte[] Decrypt(byte[] cipherData, byte[] key, byte[] iv);
    string EncryptString(string plainText, string key);
    string DecryptString(string cipherText, string key);
    byte[] GenerateKey();
    byte[] GenerateIV();
}
```

**实例访问**: `EncryptHelper.AES`

---

## RSA 非对称加密 API

### GenerateRsaKeyPair

生成 RSA 密钥对。

```csharp
public static (string publicKey, string privateKey) GenerateRsaKeyPair(int keySize = 2048)
```

**参数**
- `keySize`: 密钥位数，默认 2048，可选 1024、2048、4096

**返回**
- 公钥和私钥元组（XML 格式）

---

### RsaEncrypt / RsaDecrypt

```csharp
public static string RsaEncrypt(string plainText, string publicKey)
public static string RsaDecrypt(string cipherText, string privateKey)
```

---

### IAsymmetricCrypto 接口

```csharp
public interface IAsymmetricCrypto
{
    (string publicKey, string privateKey) GenerateKeyPair(int keySize = 2048);
    byte[] Encrypt(byte[] data, string publicKey);
    byte[] Decrypt(byte[] cipherData, string privateKey);
    byte[] Sign(byte[] data, string privateKey);
    bool Verify(byte[] data, byte[] signature, string publicKey);
}
```

**实例访问**: `EncryptHelper.RSA`

---

## 编码 API

### Base64

```csharp
public static string ToBase64(byte[] data)
public static byte[] FromBase64(string encoded)
public static string ToBase64UrlSafe(byte[] data)
public static byte[] FromBase64UrlSafe(string encoded)
```

---

### Hex

```csharp
public static string ToHex(byte[] data)
public static byte[] FromHex(string hex)
```

---

## 密钥派生 API

### DeriveKey

从密码派生加密密钥。

```csharp
public static byte[] DeriveKey(string password, byte[] salt, int keyLength = 32, int iterations = 10000)
```

**参数**
- `password`: 用户密码
- `salt`: 盐值（建议 16 字节以上）
- `keyLength`: 派生密钥长度（字节），默认 32
- `iterations`: 迭代次数，默认 10000（建议 10000+）

**返回**
- 派生的密钥字节数组

---

### GenerateSalt

```csharp
public static byte[] GenerateSalt(int saltLength = 16)
```

---

### DeriveKeyWithSalt

便捷方法，同时生成盐值和派生密钥。

```csharp
public static (byte[] key, byte[] salt) DeriveKeyWithSalt(
    string password,
    int keyLength = 32,
    int saltLength = 16)
```

---

## HMAC API

### ComputeHMAC

计算消息的 HMAC-SHA256。

```csharp
public static string ComputeHMAC(string input, string key)
```

**返回**
- 64 字符的十六进制 HMAC 值

---

### VerifyHMAC

验证 HMAC 是否有效。

```csharp
public static bool VerifyHMAC(string input, string key, string expectedHash)
```

**返回**
- `true`: 验证通过
- `false`: 验证失败（消息被篡改或密钥错误）

---

## 异常类

### EncryptException

```csharp
public class EncryptException : Exception
{
    public EncryptOperation Operation { get; }
}

public enum EncryptOperation
{
    Hash, Encrypt, Decrypt, Sign, Verify, Encode, Decode, KeyDerivation
}
```

**使用示例**

```csharp
try
{
    var result = EncryptHelper.AesDecrypt(cipherText, key);
}
catch (EncryptException ex)
{
    Console.WriteLine($"操作: {ex.Operation}, 错误: {ex.Message}");
}
```
