using UnityEngine;

namespace MazeEscape
{
    public class PowerUpSphere : MonoBehaviour
    {
        private WallBreaker _wallBreaker;

        public void Init(WallBreaker wb) => _wallBreaker = wb;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CharacterController>() == null) return;
            var sfx = GameManager.Instance != null ? GameManager.Instance.PowerUpPickupSfx : null;
            if (sfx != null) AudioSource.PlayClipAtPoint(sfx, transform.position);
            var wb = _wallBreaker != null
                ? _wallBreaker
                : Object.FindFirstObjectByType<WallBreaker>();
            wb?.GivePowerUp();
            Destroy(gameObject);
        }
    }
}
