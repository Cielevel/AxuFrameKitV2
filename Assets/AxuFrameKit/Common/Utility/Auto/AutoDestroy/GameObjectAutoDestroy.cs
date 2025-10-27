using UnityEngine;

namespace AxuFrameKit.Common.Utility.Auto.AutoDestroy
{
    public class GameObjectAutoDestroy : MonoBehaviour
    {
        [field: SerializeField]
        public float Duration { get; private set; } = 1f;
        
        [field: SerializeField]
        public bool AutoStart { get; private set; } = true;
        
        private float _timer;
        
        private bool _isRunning;
        
        private void Start()
        {
            if (AutoStart)
                _isRunning = true;
        }

        private void Update()
        {
            if (_isRunning == false)
                return;
            
            _timer += Time.deltaTime;
            if (_timer < Duration)
                return;
            
            Destroy(gameObject);
            _isRunning = false;
        }
    }
}