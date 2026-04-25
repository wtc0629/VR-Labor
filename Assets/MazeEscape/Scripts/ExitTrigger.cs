using UnityEngine;
using UnityEngine.Events;

namespace MazeEscape
{
    public class ExitTrigger : MonoBehaviour
    {
        public UnityEvent OnMazeComplete;

        private bool _triggered;

        void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag("Player")) return;

            _triggered = true;
            Debug.Log("[MazeEscape] Maze Complete!");
            OnMazeComplete?.Invoke();
        }
    }
}
