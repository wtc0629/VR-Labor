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
        public LayerMask WallLayers = ~0;

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

        private HashSet<Collider> GetPlayerColliders()
        {
            var controller = GetComponentInParent<CharacterController>();
            if (controller == null)
                return new HashSet<Collider>();

            return new HashSet<Collider>(controller.GetComponentsInChildren<Collider>());
        }

        void Update()
        {
            Vector3 cameraPosition = transform.position;

            if (tansition == transition.cut)
            {
                bool insideWall = Physics.CheckSphere(cameraPosition, CheckRadius, WallLayers);
                _targetAlpha = insideWall ? 1f : 0f;
                Color currentColor = _overlay.color;

                currentColor.a = _targetAlpha;
           
                _overlay.color = currentColor;
            }
            if (tansition == transition.fade)
            {
                var playerColliders = GetPlayerColliders();
                Collider[] hits = Physics.OverlapSphere(cameraPosition, FadeRadius, WallLayers)
                    .Where(hit => !playerColliders.Contains(hit))
                    .ToArray();

                float minDistance = FadeRadius;
                foreach (Collider hit in hits)
                {
                    float distance = Vector3.Distance(cameraPosition, hit.ClosestPoint(cameraPosition));
                    if (distance < minDistance)
                        minDistance = distance;
                }

                float alpha = 0f;
                if (hits.Length > 0)
                    alpha = Mathf.InverseLerp(FadeRadius, FadeRadius * 0.2f, minDistance);

                Color currentColor = _overlay.color;
                currentColor.a = Mathf.Clamp01(alpha);
                _overlay.color = currentColor;
            }
            
        
        }
    }
}
