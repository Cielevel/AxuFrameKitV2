using System;

namespace AxuFrameKit._Core.Utility.Encrypt
{
    /// <summary>
    /// 加密操作类型枚举
    /// </summary>
    public enum EncryptOperation
    {
        /// <summary>哈希操作</summary>
        Hash,
        /// <summary>加密操作</summary>
        Encrypt,
        /// <summary>解密操作</summary>
        Decrypt,
        /// <summary>签名操作</summary>
        Sign,
        /// <summary>验证操作</summary>
        Verify,
        /// <summary>编码操作</summary>
        Encode,
        /// <summary>解码操作</summary>
        Decode,
        /// <summary>密钥派生操作</summary>
        KeyDerivation
    }

    /// <summary>
    /// 加密模块统一异常类
    /// </summary>
    public class EncryptException : Exception
    {
        /// <summary>
        /// 加密操作类型
        /// </summary>
        public EncryptOperation Operation { get; }

        /// <summary>
        /// 创建加密异常
        /// </summary>
        public EncryptException(string message) : base(message) { }

        /// <summary>
        /// 创建加密异常（含内部异常）
        /// </summary>
        public EncryptException(string message, Exception innerException)
            : base(message, innerException) { }

        /// <summary>
        /// 创建加密异常（含操作类型）
        /// </summary>
        public EncryptException(EncryptOperation operation, string message)
            : base($"[{operation}] {message}")
        {
            Operation = operation;
        }

        /// <summary>
        /// 创建加密异常（含操作类型和内部异常）
        /// </summary>
        public EncryptException(EncryptOperation operation, string message, Exception innerException)
            : base($"[{operation}] {message}", innerException)
        {
            Operation = operation;
        }
    }
}
