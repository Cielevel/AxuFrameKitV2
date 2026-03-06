using System.Collections.Generic;

namespace AxuFrameKit._Core.DesignPattern
{
    /// <summary>
    /// 命令模式接口，定义执行与撤销的基本契约
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 执行命令逻辑
        /// </summary>
        void Execute();

        /// <summary>
        /// 撤销命令逻辑
        /// </summary>
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
        /// 执行一个新的命令并记录到历史记录（会清空当前的重做栈）
        /// </summary>
        /// <param name="command">要执行的命令实例</param>
        public void Execute(ICommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); // 执行新操作时清空重做栈
        }

        /// <summary>
        /// 撤销上一个执行成功的命令
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
        /// 重复执行上一个被撤销的命令
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

        /// <summary>
        /// 清空所有操作的历史记录
        /// </summary>
        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        /// <summary>
        /// 是否有可撤销的操作
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// 是否有可重做的操作
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;
    }
}
