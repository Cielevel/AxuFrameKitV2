namespace AxuFrameKit._Core.Utility.Encrypt.Core
{
    /// <summary>
    /// 密钥派生接口
    /// </summary>
    public interface IKeyDerivation
    {
        /// <summary>
        /// 从密码派生密钥
        /// </summary>
        /// <param name="password">密码</param>
        /// <param name="salt">盐值</param>
        /// <param name="keyLength">派生密钥长度（字节）</param>
        /// <param name="iterations">迭代次数，默认10000</param>
        /// <returns>派生的密钥</returns>
        byte[] DeriveKey(string password, byte[] salt, int keyLength, int iterations = 10000);

        /// <summary>
        /// 生成随机盐值
        /// </summary>
        /// <param name="saltLength">盐值长度（字节），默认16</param>
        /// <returns>随机盐值</returns>
        byte[] GenerateSalt(int saltLength = 16);
    }
}
