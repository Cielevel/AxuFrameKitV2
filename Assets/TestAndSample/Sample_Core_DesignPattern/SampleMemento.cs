using UnityEngine;
using AxuFrameKit._Core.DesignPattern;

namespace TestAndSample.Sample_Core_DesignPattern
{
    public class SampleMemento : MonoBehaviour
    {
        private PlayerCharacter _player;
        private Caretaker<PlayerState> _caretaker;

        private void Start()
        {
            _player = new PlayerCharacter();
            _caretaker = new Caretaker<PlayerState>(_player);

            Debug.Log("Sample_Memento Started.");
            Debug.Log("Current State: " + _player);
            Debug.Log("Press Z: Level Up (Change State)");
            Debug.Log("Press X: Save State (Backup)");
            Debug.Log("Press C: Undo (Restore State)");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                _player.LevelUp();
                Debug.Log($"Player Leveled Up! New State: {_player}");
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                _caretaker.Backup();
                Debug.Log("State Saved!");
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("Restoring State...");
                _caretaker.Undo();
                Debug.Log($"State Restored: {_player}");
            }
        }

        // State Object
        [System.Serializable]
        public class PlayerState
        {
            public int Level { get; set; }
            public int Health { get; set; }
            public string Name { get; set; }

            public override string ToString()
            {
                return $"[Level: {Level}, Health: {Health}, Name: {Name}]";
            }
        }

        // Originator
        private class PlayerCharacter : IOriginator<PlayerState>
        {
            private int _level = 1;
            private int _health = 100;
            private string _name = "Hero";

            public void LevelUp()
            {
                _level++;
                _health += 10;
            }

            public override string ToString()
            {
                return $"Level: {_level}, Health: {_health}, Name: {_name}";
            }

            public Memento<PlayerState> Save()
            {
                // Create a deep copy or new instance of state
                return new Memento<PlayerState>(new PlayerState
                {
                    Level = _level,
                    Health = _health,
                    Name = _name
                });
            }

            public void Restore(Memento<PlayerState> memento)
            {
                var state = memento.State;
                _level  = state.Level;
                _health = state.Health;
                _name   = state.Name;
            }
        }
    }
}
