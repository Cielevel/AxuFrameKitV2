using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Structure
{
    /// <summary>
    /// 只读 ReactiveProperty 接口
    /// </summary>
    public interface IReadOnlyReactiveProperty<T>
    {
        T Value { get; }

        /// <summary>
        /// 订阅值变更事件。
        /// </summary>
        /// <param name="onChanged">值变更时的回调，参数为新值。</param>
        /// <param name="notifyImmediately">
        /// 是否在订阅时立即触发一次回调，默认为 true。
        /// 注意：立即触发与后续触发行为一致，同样禁止在回调中修改此属性的值。
        /// 若立即触发期间抛出异常，异常将在订阅处冒泡，而非赋值处。
        /// </param>
        /// <returns>取消订阅的句柄，Dispose 后停止接收通知。</returns>
        /// <remarks>
        /// 不要在回调中修改此属性的值，这会导致 <see cref="InvalidOperationException"/>。
        /// 如需根据当前值派生另一个值，请使用独立的 ReactiveProperty。
        /// </remarks>
        IDisposable Subscribe(Action<T> onChanged, bool notifyImmediately = true);

        /// <summary>
        /// 订阅值变更事件。
        /// </summary>
        /// <param name="onChanged">值变更时的回调，参数为新值。</param>
        /// <param name="notifyImmediately">
        /// 是否在订阅时立即触发一次回调，默认为 true。
        /// 注意：立即触发与后续触发行为一致，同样禁止在回调中修改此属性的值。
        /// 若立即触发期间抛出异常，异常将在订阅处冒泡，而非赋值处。
        /// </param>
        /// <returns>取消订阅的句柄，Dispose 后停止接收通知。</returns>
        /// <remarks>
        /// 不要在回调中修改此属性的值，这会导致 <see cref="InvalidOperationException"/>。
        /// 如需根据当前值派生另一个值，请使用独立的 ReactiveProperty。
        /// </remarks>
        IDisposable SubscribeWithPrevious(Action<T, T> onChanged, bool notifyImmediately = true);
    }

    /// <summary>
    /// 可写 ReactiveProperty 接口
    /// </summary>
    public interface IReactiveProperty<T> : IReadOnlyReactiveProperty<T>
    {
        new T Value { get; set; }
    }

    /// <summary>
    /// 与 Unity 无关的 ReactiveProperty 核心实现。
    /// </summary>
    /// <remarks>
    /// 线程安全：不支持多线程并发访问。
    /// 重入限制：不允许在值变更回调中修改此属性的值，违反将抛出 <see cref="InvalidOperationException"/>。
    /// </remarks>
    public class ReactiveProperty<T> : IReactiveProperty<T>
    {
        private T _value;
        private T _previousValue;
        private bool _isNotifying;

        public event Action<T> OnValueChanged;
        public event Action<T, T> OnValueChangedWithPrevious;

        public T Value
        {
            get => _value;
            set => SetValue(value);
        }

        protected virtual void SetValue(T value)
        {
            if (_isNotifying)
                throw new InvalidOperationException(
                    $"[ReactiveProperty] 不允许在值变更回调中修改属性值。" +
                    $"当前值：{_value}，尝试设置的值：{value}。" +
                    $"如需派生值，请使用独立的 ReactiveProperty。");

            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                RaiseValueChanged();
            }
        }

        protected virtual void RaiseValueChanged()
        {
            _isNotifying = true;
            var current = _value;
            var previous = _previousValue;
            try
            {
                OnValueChanged?.Invoke(current);
                OnValueChangedWithPrevious?.Invoke(current, previous);
            }
            finally
            {
                _previousValue = current;
                _isNotifying = false;
            }
        }

        public ReactiveProperty() { }

        public ReactiveProperty(T initialValue)
        {
            _value = initialValue;
            _previousValue = initialValue;
        }

        public IDisposable Subscribe(Action<T> onChanged, bool notifyImmediately = true)
        {
            OnValueChanged += onChanged;
            if (notifyImmediately)
                onChanged(_value);
            return new Unsubscriber(() => OnValueChanged -= onChanged);
        }

        public IDisposable SubscribeWithPrevious(Action<T, T> onChanged, bool notifyImmediately = true)
        {
            OnValueChangedWithPrevious += onChanged;
            if (notifyImmediately)
                onChanged(_value, _previousValue);
            return new Unsubscriber(() => OnValueChangedWithPrevious -= onChanged);
        }

        public static ReactiveProperty<T> From(T value) => new(value);

        private class Unsubscriber : IDisposable
        {
            private readonly Action _unsubscribe;
            public Unsubscriber(Action unsubscribe) => _unsubscribe = unsubscribe;
            public void Dispose() => _unsubscribe();
        }
    }
}