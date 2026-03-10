using AxuFrameKit.Common.Utility.Save;
using UnityEngine;
using AxuFrameKit.Common.Utility.Serializer;
using AxuFrameKit._Core.Utility.Encrypt.Symmetric;

namespace TestAndSample.Sample_Common.Utility.Save
{
    // 一个简单的测试数据类
    [System.Serializable]
    public class UserProfile
    {
        public string username;
        public int gold;
    }

    public class SampleCommonSaveUtil : MonoBehaviour
    {
        // 定义两个不同的存档器，用于演示多实例
        private SaveUtil _defaultSaver;
        private SaveUtil _secureSaver;

        void Start()
        {
            // ---------------------------------------------------------
            // 场景 1: 普通的 Json 存储
            // 注入 UnityJsonSerializer 和 LocalFileStorage
            // ---------------------------------------------------------
            _defaultSaver = new SaveUtilBuilder()
                            .UseSerializer(new UnityJsonSerializer())  // 注入序列化方式
                            .UseStorage(new LocalFileStorage()) // 注入存储位置
                            .Build();

            // ---------------------------------------------------------
            // 场景 2: 高度安全的加密存储
            // 注入了加密模块，且可以复用上面的Serializer，或者换成 Newtonsoft
            // ---------------------------------------------------------
            _secureSaver = new SaveUtilBuilder()
                           .UseSerializer(new UnityJsonSerializer())
                           .UseStorage(new LocalFileStorage(".dat"))               // 使用不同的后缀
                           .UseEncryption(new AesCrypto(), "MySuperSecretKey123456") // 注入加密
                           .Build();

            Debug.Log("Save Utils Initialized!");
        }

        void Update()
        {
            // 按 A 保存普通档
            if (Input.GetKeyDown(KeyCode.A))
            {
                var data = new UserProfile { username = "CasualPlayer", gold = 100 };
                _defaultSaver.Save("user_save", data);
                Debug.Log("Saved Normal JSON.");
            }

            // 按 B 保存加密档
            if (Input.GetKeyDown(KeyCode.B))
            {
                var data = new UserProfile { username = "VIP_Player", gold = 999999 };
                _secureSaver.Save("vip_save", data);
                Debug.Log("Saved Encrypted Data.");
            }

            // 按 L 读取
            if (Input.GetKeyDown(KeyCode.L))
            {
                var normal = _defaultSaver.Load<UserProfile>("user_save");
                var secure = _secureSaver.Load<UserProfile>("vip_save");

                if (normal != null) Debug.Log($"Loaded Normal: {normal.username}, Gold: {normal.gold}");
                if (secure != null) Debug.Log($"Loaded Secure: {secure.username}, Gold: {secure.gold}");
            }
        }
    }
}