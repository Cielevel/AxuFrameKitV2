using UnityEngine;
using AxuFrameKit._Core.DesignPattern;

namespace TestAndSample.Sample_Core_DesignPattern
{
    public class SampleCommand : MonoBehaviour
    {
        private CommandInvoker _invoker;
        private int _value = 0;

        private void Start()
        {
            _invoker = new CommandInvoker();
            Debug.Log("Sample_Command Started. Use A: Add 1, S: Add 10, U: Undo, R: Redo");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                var cmd = new AddValueCommand(this, 1);
                _invoker.Execute(cmd);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                var cmd = new AddValueCommand(this, 10);
                _invoker.Execute(cmd);
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                if (_invoker.CanUndo)
                {
                    _invoker.Undo();
                    Debug.Log($"Undo performed. Current Value: {_value}");
                }
                else
                {
                    Debug.Log("Nothing to undo.");
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                if (_invoker.CanRedo)
                {
                    _invoker.Redo();
                    Debug.Log($"Redo performed. Current Value: {_value}");
                }
                else
                {
                    Debug.Log("Nothing to redo.");
                }
            }
        }

        public void Add(int amount)
        {
            _value += amount;
            Debug.Log($"Value added: {amount}. Current Value: {_value}");
        }

        public void Subtract(int amount)
        {
            _value -= amount;
            Debug.Log($"Value subtracted: {amount}. Current Value: {_value}");
        }

        // Concrete Command
        private class AddValueCommand : ICommand
        {
            private readonly SampleCommand _receiver;
            private readonly int _amount;

            public AddValueCommand(SampleCommand receiver, int amount)
            {
                _receiver = receiver;
                _amount = amount;
            }

            public void Execute()
            {
                _receiver.Add(_amount);
            }

            public void Undo()
            {
                _receiver.Subtract(_amount);
            }
        }
    }
}
