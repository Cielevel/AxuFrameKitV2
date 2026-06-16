using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.DesignPattern
{
    /*
     * 可用于实现撤销/重做功能，或保存对象状态以便后续恢复；
     * 比如：游戏中存档之间的切换。
     */

    /// <summary>
    /// 备忘录模式：表示对象在某一时刻的快照 (Memento)
    /// </summary>
    /// <typeparam name="TState">快照包含的状态数据类型</typeparam>
    public class Memento<TState>
    {
        /// <summary>
        /// 持有的快照数据
        /// </summary>
        public TState State { get; private set; }

        /// <summary>
        /// 快照生成的时间戳
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// 初始化快照
        /// </summary>
        /// <param name="state">状态数据</param>
        public Memento(TState state)
        {
            State = state;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 原发器 (Originator) 接口，定义对象如何保存和恢复其内部状态
    /// </summary>
    /// <typeparam name="TState">状态类型</typeparam>
    public interface IOriginator<TState>
    {
        /// <summary>
        /// 生成当前状态的备忘录
        /// </summary>
        /// <returns>包含当前状态的备忘录对象</returns>
        Memento<TState> Save();

        /// <summary>
        /// 根据备忘录恢复状态
        /// </summary>
        /// <param name="memento">存有历史状态的备忘录对象</param>
        void Restore(Memento<TState> memento);
    }

    /// <summary>
    /// 负责人 (Caretaker) 管理备忘录的历史记录
    /// </summary>
    /// <typeparam name="TState">状态类型</typeparam>
    public class Caretaker<TState>
    {
        private readonly List<Memento<TState>> _history = new(8);
        private readonly IOriginator<TState> _originator;

        /// <summary>
        /// 初始化负责人
        /// </summary>
        /// <param name="originator">关联的原发器对象</param>
        public Caretaker(IOriginator<TState> originator)
        {
            _originator = originator;
        }

        /// <summary>
        /// 立即保存原发器的当前状态
        /// </summary>
        public void Backup()
        {
            _history.Add(_originator.Save());
        }

        /// <summary>
        /// 撤销至上一个保存的状态
        /// </summary>
        public void Undo()
        {
            if (_history.Count == 0) return;

            var memento = _history[^1];
            _history.RemoveAt(_history.Count - 1);

            _originator.Restore(memento);
        }

        /// <summary>
        /// 获取最近一次保存的快照
        /// </summary>
        /// <returns>快照对象或 null</returns>
        public Memento<TState> GetLast()
        {
            return _history.Count == 0 ? null : _history[^1];
        }

        /// <summary>
        /// 清空快照存储历史
        /// </summary>
        public void Clear()
        {
            _history.Clear();
        }
    }
}
