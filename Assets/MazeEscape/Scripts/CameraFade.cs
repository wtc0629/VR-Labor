using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MazeEscape
{
    // Fades to black when the HMD camera enters wall geometry.
    // Attach to the Main Camera (inside XR Origin > Camera Offset).
    public class CameraFade : MonoBehaviour
    {
        [Tooltip("Sphere radius to check for wall collision around the camera")]
        public float CheckRadius = 0.15f;

        [Tooltip("Layer mask for wall geometry only. Do not include the player body layer here.")]
        public LayerMask WallLayers = LayerMask.GetMask("Wall");

        [Tooltip("Layer name assigned to walls and corner posts. Must match MazeRenderer.")]
        public string WallLayerName = "Wall";

        [Tooltip("Distance from the player body to a wall that should force a transition to black")]
        public float BodyCheckDistance = 0.1f;

        [Tooltip("Detection radius for fade2 proximity mode (should be larger than CheckRadius)")]
        public float FadeRadius = 0.8f;


        [Tooltip("What to do if contact with wall")]
        public enum transition {
        cut,
        fade
        };
        public transition tansition;
        public Image _overlay;
        private float _targetAlpha;

        void Awake()
        {
            int wallLayer = LayerMask.NameToLayer(WallLayerName);
            if (wallLayer >= 0)
                WallLayers = 1 << wallLayer;
        }

        private HashSet<Collider> GetPlayerColliders()
        {
            var controller = GetComponentInParent<CharacterController>();
            if (controller == null)
                return new HashSet<Collider>();

            return new HashSet<Collider>(controller.GetComponentsInChildren<Collider>());
        }

        private LayerMask GetWallLayerMask()
        {
            if (WallLayers.value != 0)
                return WallLayers;

            LayerMask mask = LayerMask.GetMask(WallLayerName);
            return mask != 0 ? mask : WallLayers;
        }

        private float GetBodyFadeAlpha(HashSet<Collider> playerColliders, float maxDistance, LayerMask wallMask)
        {
            if (playerColliders.Count == 0)
                return 0f;

            float minDistance = maxDistance;
            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider == null)
                    continue;

                Collider[] hits = Physics.OverlapSphere(playerCollider.bounds.center, maxDistance, wallMask);
                foreach (Collider hit in hits)
                {
                    if (playerColliders.Contains(hit))
                        continue;

                    float distance = Vector3.Distance(playerCollider.bounds.center, hit.ClosestPoint(playerCollider.bounds.center));
                    if (distance < minDistance)
                        minDistance = distance;
                }
            }

            if (minDistance >= maxDistance)
                return 0f;

            return Mathf.InverseLerp(maxDistance, 0f, minDistance);
        }

        void Update()
        {
            Vector3 cameraPosition = transform.position;

            var playerColliders = GetPlayerColliders();
            LayerMask wallMask = GetWallLayerMask();
            float bodyAlpha = GetBodyFadeAlpha(playerColliders, BodyCheckDistance, wallMask);

            if (tansition == transition.cut)
            {
                bool insideWall = Physics.CheckSphere(cameraPosition, CheckRadius, wallMask);
                _targetAlpha = (insideWall || bodyAlpha > 0f) ? 1f : 0f;
                Color currentColor = _overlay.color;

                currentColor.a = _targetAlpha;
                _overlay.color = currentColor;
            }
            if (tansition == transition.fade)
            {
                Collider[] hits = Physics.OverlapSphere(cameraPosition, FadeRadius, wallMask)
                    .Where(hit => !playerColliders.Contains(hit))
                    .ToArray();

                float minDistance = FadeRadius;
                foreach (Collider hit in hits)
                {
                    float distance = Vector3.Distance(cameraPosition, hit.ClosestPoint(cameraPosition));
                    if (distance < minDistance)
                        minDistance = distance;
                }

                float cameraAlpha = 0f;
                if (hits.Length > 0)
                    cameraAlpha = Mathf.InverseLerp(FadeRadius, FadeRadius * 0.2f, minDistance);

                float alpha = Mathf.Max(cameraAlpha, bodyAlpha);
                Color currentColor = _overlay.color;
                currentColor.a = Mathf.Clamp01(alpha);
                _overlay.color = currentColor;
            }
            
        
        }
    }
}
