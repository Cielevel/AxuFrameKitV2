using System;

namespace AxuFrameKit.Modules.Config.Model
{
    /// <summary>
    /// 配置条目
    /// 用于封装单个配置项，提供缓存、脏标记和变更事件
    /// </summary>
    /// <typeparam name="T">配置值类型</typeparam>
    public class ConfigEntry<T>
    {
        /// <summary>配置键名</summary>
        public string Key { get; }

        /// <summary>当前值</summary>
        public T Value { get; private set; }

        /// <summary>默认值</summary>
        public T DefaultValue { get; }

        /// <summary>值是否已从存储加载</summary>
        public bool IsLoaded { get; private set; }

        /// <summary>值是否已修改（未保存）</summary>
        public bool IsDirty { get; private set; }

        /// <summary>值变更事件</summary>
        public event Action<T, T> OnValueChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key">配置键名</param>
        /// <param name="defaultValue">默认值</param>
        public ConfigEntry(string key, T defaultValue = default)
        {
            Key = key;
            DefaultValue = defaultValue;
            Value = defaultValue;
            IsLoaded = false;
            IsDirty = false;
        }

        /// <summary>
        /// 设置值（标记为脏数据，不立即保存）
        /// </summary>
        /// <param name="newValue">新值</param>
        public void SetValue(T newValue)
        {
            if (!Equals(Value, newValue))
            {
                var oldValue = Value;
                Value = newValue;
                IsDirty = true;
                OnValueChanged?.Invoke(oldValue, newValue);
            }
        }

        /// <summary>
        /// 标记为已加载（从存储读取后调用）
        /// </summary>
        /// <param name="loadedValue">加载的值</param>
        public void MarkAsLoaded(T loadedValue)
        {
            Value = loadedValue;
            IsLoaded = true;
            IsDirty = false;
        }

        /// <summary>
        /// 标记为已保存（保存到存储后调用）
        /// </summary>
        public void MarkAsSaved()
        {
            IsDirty = false;
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void Reset()
        {
            SetValue(DefaultValue);
        }

        /// <summary>
        /// 隐式转换为值类型
        /// </summary>
        public static implicit operator T(ConfigEntry<T> entry)
        {
            return entry != null ? entry.Value : default;
        }
    }
}
