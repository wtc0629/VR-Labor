using UnityEngine;
using UnityEngine.UI;

namespace MazeEscape
{
    public class BodyHeadDistanceOverlay : MonoBehaviour
    {
        [Tooltip("Distance between head and body that should trigger the overlay.")]
        public float Threshold = 0.5f;

        [Tooltip("Body reference point. Leave empty to use the parent CharacterController bounds center.")]
        public Transform BodyTransform;

        [Tooltip("Head reference point. Leave empty to use this camera transform.")]
        public Transform HeadTransform;

        [Tooltip("Optional image component used for the overlay background.")]
        public Image OverlayImage;

        private void Awake()
        {
            if (HeadTransform == null)
                HeadTransform = transform;

            if (OverlayImage == null)
                OverlayImage = GetComponent<Image>();
        }

        private void Update()
        {
            Vector3 headPosition = HeadTransform != null ? HeadTransform.position : transform.position;
            Vector3 bodyPosition = GetBodyPosition();
            float distance = Vector3.Distance(headPosition, bodyPosition);
            bool showOverlay = distance > Threshold;
            float alpha = showOverlay ? 1f : 0f;

            if (OverlayImage != null)
            {
                Color imageColor = OverlayImage.color;
                imageColor.a = alpha;
                OverlayImage.color = imageColor;
            }
        }

        private Vector3 GetBodyPosition()
        {
            if (BodyTransform != null)
                return BodyTransform.position;

            var controller = GetComponentInParent<CharacterController>();
            if (controller != null)
                return controller.bounds.center;

            Transform parent = transform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent(out CharacterController characterController))
                    return characterController.bounds.center;

                parent = parent.parent;
            }

            return transform.position;
        }
    }
}
