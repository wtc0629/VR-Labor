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

        [Tooltip("How fast the fade transitions")]
        public float FadeSpeed = 8f;

        private Image _overlay;
        private float _targetAlpha;

        void Awake()
        {
            // Create a full-screen black quad as child of this camera
            var canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 2f);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            canvasGo.transform.localScale = Vector3.one;

            var imgGo = new GameObject("Overlay", typeof(Image));
            imgGo.transform.SetParent(canvasGo.transform, false);
            _overlay = imgGo.GetComponent<Image>();
            _overlay.color = new Color(0f, 0f, 0f, 0f);
            var imgRt = imgGo.GetComponent<RectTransform>();
            imgRt.anchorMin = Vector2.zero;
            imgRt.anchorMax = Vector2.one;
            imgRt.offsetMin = imgRt.offsetMax = Vector2.zero;
        }

        void Update()
        {
            bool insideWall = Physics.CheckSphere(transform.position, CheckRadius, WallLayers);
            _targetAlpha = insideWall ? 1f : 0f;

            var c = _overlay.color;
            c.a = Mathf.MoveTowards(c.a, _targetAlpha, FadeSpeed * Time.deltaTime);
            _overlay.color = c;
        }
    }
}
