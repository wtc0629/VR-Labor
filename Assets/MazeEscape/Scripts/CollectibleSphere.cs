using UnityEngine;

namespace MazeEscape
{
    public class CollectibleSphere : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CharacterController>() == null) return;
            CollectibleManager.Instance?.Collect();
            Destroy(gameObject);
        }
    }
}
