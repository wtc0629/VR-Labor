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

            // Fallback: find WallBreaker in scene if Init() reference was lost
            var wb = _wallBreaker != null
                ? _wallBreaker
                : Object.FindFirstObjectByType<WallBreaker>();
            wb?.GivePowerUp();
            Destroy(gameObject);
        }
    }
}
