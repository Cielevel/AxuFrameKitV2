using System.Collections.Generic;

namespace AxuFrameKit._Core.DesignPattern
{
    /// <summary>
    /// Command Interface
    /// </summary>
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    /// <summary>
    /// Generic Command Invoker with Undo/Redo support
    /// </summary>
    public class CommandInvoker
    {
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();

        /// <summary>
        /// Executes a command and pushes it to the history
        /// </summary>
        public void Execute(ICommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); // Clear redo stack on new action
        }

        /// <summary>
        /// Undoes the last command
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
        /// Redoes the last undone command
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
