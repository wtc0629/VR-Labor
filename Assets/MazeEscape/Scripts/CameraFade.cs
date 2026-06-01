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

        [Tooltip("Layer mask for wall geometry (set to 'Default' if walls have no special layer)")]
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


        void Update()
        {
            if (tansition == transition.cut)
            {
                bool insideWall = Physics.CheckSphere(transform.position, CheckRadius, WallLayers);
                _targetAlpha = insideWall ? 1f : 0f;
                Color currentColor = _overlay.color;

                currentColor.a = _targetAlpha;
           
                _overlay.color = currentColor;
            }
            if (tansition == transition.fade)
            {
                float minDistance = FadeRadius;
                Collider[] hits = Physics.OverlapSphere(transform.position, FadeRadius, WallLayers);
                foreach (Collider hit in hits)
                {
                    float distance = Vector3.Distance(transform.position, hit.ClosestPoint(transform.position));
                    if (distance < minDistance) minDistance = distance;
                }

                Color currentColor = _overlay.color;
                currentColor.a = Mathf.InverseLerp(FadeRadius, FadeRadius*0.2f, minDistance);
                _overlay.color = currentColor;
            }
            
        
        }
    }
}
