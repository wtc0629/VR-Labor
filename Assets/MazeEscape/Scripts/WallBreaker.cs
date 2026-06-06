using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace MazeEscape
{
    public class WallBreaker : MonoBehaviour
    {
        [HideInInspector] public Transform RightControllerOrigin;
        [HideInInspector] public MinimapController Minimap;
        [HideInInspector] public Transform HMDCamera;
        public LayerMask WallLayer;
        public float RayDistance = 15f;

        private bool _hasPowerUp;
        private WallData _selected;
        private Color _originalColor;
        private GameObject _indicator;

        private InputAction _selectAction;
        private InputAction _destroyAction;

        void Awake()
        {
            // primaryButton = A, secondaryButton = B on right Quest controller
            _selectAction = new InputAction("SelectWall", InputActionType.Button);
            _selectAction.AddBinding("<XRController>{RightHand}/primaryButton");

            _destroyAction = new InputAction("DestroyWall", InputActionType.Button);
            _destroyAction.AddBinding("<XRController>{RightHand}/secondaryButton");
        }

        void OnEnable()
        {
            _selectAction.Enable();
            _destroyAction.Enable();
        }

        void OnDisable()
        {
            _selectAction.Disable();
            _destroyAction.Disable();
        }

        public void GivePowerUp()
        {
            _hasPowerUp = true;
            CreateIndicator();
        }

        void Update()
        {
            if (!_hasPowerUp || RightControllerOrigin == null) return;

            if (_selectAction.WasPressedThisFrame())
                DoSelect();

            if (_destroyAction.WasPressedThisFrame())
                DoDestroy();
        }

        private void CreateIndicator()
        {
            if (HMDCamera == null) return;

            _indicator = new GameObject("WallBreakerIndicator");
            _indicator.transform.SetParent(HMDCamera, false);

            // Minimap sits at LocalOffset (default 0.12, 0.08, 0.25) and is ~0.1m wide.
            // Place indicator ~0.07m to the left of the minimap centre → closer to FOV centre.
            Vector3 mapOffset = Minimap != null
                ? Minimap.LocalOffset
                : new Vector3(0.12f, 0.08f, 0.25f);

            _indicator.transform.localPosition = new Vector3(
                mapOffset.x - 0.07f,
                mapOffset.y,
                mapOffset.z);
            _indicator.transform.localRotation = Quaternion.identity;

            // Canvas: 100 × 40 px → 0.04 m wide in world space
            const float cW = 100f, cH = 40f;
            _indicator.transform.localScale = Vector3.one * (0.04f / cW);

            var canvas = _indicator.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            _indicator.AddComponent<CanvasScaler>();
            _indicator.GetComponent<RectTransform>().sizeDelta = new Vector2(cW, cH);

            // Dark semi-transparent background
            var bg = new GameObject("BG", typeof(Image));
            bg.transform.SetParent(_indicator.transform, false);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            FullRect(bg.GetComponent<RectTransform>());

            // Gold circle (Unity built-in Knob sprite)
            var circle = new GameObject("Circle", typeof(Image));
            circle.transform.SetParent(_indicator.transform, false);
            var circleImg = circle.GetComponent<Image>();
            circleImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            circleImg.color = new Color(1f, 0.8f, 0f);
            var circleRt = circle.GetComponent<RectTransform>();
            circleRt.anchorMin = new Vector2(0.06f, 0.12f);
            circleRt.anchorMax = new Vector2(0.40f, 0.88f);
            circleRt.offsetMin = circleRt.offsetMax = Vector2.zero;

            // "×1" label
            var labelGo = new GameObject("Label", typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(_indicator.transform, false);
            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "×1";   // ×1
            tmp.fontSize = 22;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.44f, 0f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

            // Sync with minimap visibility
            var minimapCanvas = Minimap?.GetComponent<Canvas>();
            if (minimapCanvas != null && !minimapCanvas.enabled)
                canvas.enabled = false;
        }

        public void SetIndicatorVisible(bool visible)
        {
            if (_indicator == null) return;
            var c = _indicator.GetComponent<Canvas>();
            if (c != null) c.enabled = visible;
        }

        private void DoSelect()
        {
            int mask = WallLayer.value != 0 ? WallLayer.value : ~0;
            var ray = new Ray(RightControllerOrigin.position, RightControllerOrigin.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, RayDistance, mask))
            {
                var wd = hit.collider.GetComponent<WallData>();
                if (wd != null && !wd.IsBoundary())
                {
                    Deselect();
                    _selected = wd;
                    var rend = hit.collider.GetComponent<Renderer>();
                    _originalColor = rend.material.color;
                    rend.material.color = Color.red;
                    return;
                }
            }
            Deselect();
        }

        private void DoDestroy()
        {
            if (_selected == null) return;

            int cx = _selected.CellX;
            int cy = _selected.CellY;
            WallDirection dir = _selected.Direction;
            WallData twin = _selected.Twin;

            Minimap?.RemoveWall(cx, cy, dir);
            Destroy(_selected.gameObject);
            if (twin != null) Destroy(twin.gameObject);

            _selected = null;
            _hasPowerUp = false;

            if (_indicator != null)
            {
                Destroy(_indicator);
                _indicator = null;
            }
        }

        private void Deselect()
        {
            if (_selected == null) return;
            var rend = _selected.GetComponent<Renderer>();
            if (rend != null) rend.material.color = _originalColor;
            _selected = null;
        }

        private static void FullRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
