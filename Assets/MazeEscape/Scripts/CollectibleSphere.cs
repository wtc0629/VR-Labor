using UnityEngine;

namespace MazeEscape
{
    public class CollectibleSphere : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CharacterController>() == null) return;
            var sfx = GameManager.Instance != null ? GameManager.Instance.CollectiblePickupSfx : null;
            if (sfx != null) AudioSource.PlayClipAtPoint(sfx, transform.position);
            CollectibleManager.Instance?.Collect();
            Destroy(gameObject);
        }
    }
}
