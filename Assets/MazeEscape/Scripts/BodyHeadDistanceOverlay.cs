using TMPro;
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

        [Tooltip("Optional canvas group for the overlay root.")]
        public CanvasGroup OverlayCanvasGroup;

        [Tooltip("Optional image component used for the overlay background.")]
        public Image OverlayImage;

        [Tooltip("Optional text component used for the overlay message.")]
        public TextMeshProUGUI OverlayText;

        private void Awake()
        {
            if (HeadTransform == null)
                HeadTransform = transform;

            if (OverlayCanvasGroup == null)
                OverlayCanvasGroup = GetComponent<CanvasGroup>();

            if (OverlayImage == null)
                OverlayImage = GetComponent<Image>();

            if (OverlayText == null)
                OverlayText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void Update()
        {
            Vector3 headPosition = HeadTransform != null ? HeadTransform.position : transform.position;
            Vector3 bodyPosition = GetBodyPosition();
            float distance = Vector3.Distance(headPosition, bodyPosition);
            bool showOverlay = distance > Threshold;
            float alpha = showOverlay ? 1f : 0f;

            if (OverlayCanvasGroup != null)
            {
                OverlayCanvasGroup.alpha = alpha;
            }
            else
            {
                if (OverlayImage != null)
                {
                    Color imageColor = OverlayImage.color;
                    imageColor.a = alpha;
                    OverlayImage.color = imageColor;
                }

                if (OverlayText != null)
                {
                    Color textColor = OverlayText.color;
                    textColor.a = alpha;
                    OverlayText.color = textColor;
                }
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
