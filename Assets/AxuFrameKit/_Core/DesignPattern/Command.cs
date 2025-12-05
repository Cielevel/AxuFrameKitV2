using System.Collections.Generic;

namespace AxuFrameKit._Core.DesignPattern
{
    /// <summary>
    /// 命令模式接口
    /// </summary>
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    /// <summary>
    /// 支持撤销/重做的通用命令调用者 (Command Invoker)
    /// </summary>
    public class CommandInvoker
    {
        private const int DefaultCapacity = 8;
        
        private readonly Stack<ICommand> _undoStack = new(DefaultCapacity);
        private readonly Stack<ICommand> _redoStack = new(DefaultCapacity);

        /// <summary>
        /// 执行一个命令并将其推入历史记录
        /// </summary>
        public void Execute(ICommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); // 执行新操作时清空重做栈
        }

        /// <summary>
        /// 撤销上一个命令
        /// </summary>
        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
            }
        }

        /// <summary>
        /// 重做上一个撤销的命令
        /// </summary>
        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var command = _redoStack.Pop();
                command.Execute();
                _undoStack.Push(command);
            }
        }

        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
    }
}
