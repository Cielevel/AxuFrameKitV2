using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.DesignPattern
{
    /// <summary>
    /// 表示状态的快照 (Memento)
    /// </summary>
    public class Memento<TState>
    {
        public TState State { get; private set; }
        public DateTime Timestamp { get; private set; }

        public Memento(TState state)
        {
            State = state;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 原发器 (Originator) 接口，即需要保存状态的对象
    /// </summary>
    public interface IOriginator<TState>
    {
        Memento<TState> Save();
        void Restore(Memento<TState> memento);
    }

    /// <summary>
    /// 负责人 (Caretaker) 用于管理备忘录的历史记录
    /// </summary>
    public class Caretaker<TState>
    {
        private readonly List<Memento<TState>> _history = new List<Memento<TState>>();
        private readonly IOriginator<TState> _originator;

        public Caretaker(IOriginator<TState> originator)
        {
            _originator = originator;
        }

        public void Backup()
        {
            _history.Add(_originator.Save());
        }

        public void Undo()
        {
            if (_history.Count == 0) return;

            var memento = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);

            _originator.Restore(memento);
        }

        public Memento<TState> GetLast()
        {
             if (_history.Count == 0) return null;
             return _history[_history.Count - 1];
        }

        public void Clear()
        {
            _history.Clear();
        }
    }
}
