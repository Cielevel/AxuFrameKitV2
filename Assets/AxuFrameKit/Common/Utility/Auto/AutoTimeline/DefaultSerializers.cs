using UnityEngine;

namespace AxuFrameKit.Common.Utility.Auto.AutoTimeline
{
    /// <summary>
    /// 使用 Unity JsonUtility 的默认数据序列化器
    /// 注意：JsonUtility 对复杂类型支持有限，建议需要时使用 Newtonsoft.Json 等
    /// </summary>
    public class UnityJsonDataSerializer : ITimelineDataSerializer
    {
        public string SerializeItem<T>(T item)
        {
            return JsonUtility.ToJson(item);
        }

        public T DeserializeItem<T>(string data)
        {
            return JsonUtility.FromJson<T>(data);
        }
    }
}
