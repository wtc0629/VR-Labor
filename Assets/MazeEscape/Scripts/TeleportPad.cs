using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace MazeEscape
{
    public class TeleportPad : MonoBehaviour
    {
        [HideInInspector] public TeleportPad OtherPad;
        [HideInInspector] public CharacterController CC;
        [HideInInspector] public Transform XROrigin;

        public float ProximityRadius = 3f;
        public float Cooldown = 1.5f;

        private GameObject _menu;
        private float _cooldownTimer;

        void Start() => BuildMenu();

        void Update()
        {
            _cooldownTimer -= Time.deltaTime;

            Vector3 playerFlat = new Vector3(XROrigin.position.x, transform.position.y, XROrigin.position.z);
            bool near = Vector3.Distance(playerFlat, transform.position) < ProximityRadius;
            _menu.SetActive(near);

            if (near)
            {
                Vector3 toPlayer = XROrigin.position - _menu.transform.position;
                toPlayer.y = 0f;
                // Negate so Canvas -Z faces player (Canvas front is the -Z side)
                if (toPlayer.sqrMagnitude > 0.001f)
                    _menu.transform.rotation = Quaternion.LookRotation(-toPlayer);
            }
        }

        public void OnTeleportPressed()
        {
            if (_cooldownTimer > 0f || OtherPad == null) return;

            CC.enabled = false;
            XROrigin.position = new Vector3(OtherPad.transform.position.x,
                                             XROrigin.position.y,
                                             OtherPad.transform.position.z);
            CC.enabled = true;

            _cooldownTimer = Cooldown;
            OtherPad._cooldownTimer = Cooldown;
            _menu.SetActive(false);
            OtherPad._menu.SetActive(false);
        }

        private void BuildMenu()
        {
            _menu = new GameObject("TeleportMenu");
            _menu.transform.SetParent(transform);
            _menu.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            // --- Visual layer: Canvas (background + text only, no interactable) ---
            // Canvas world size: 300*0.003 = 0.9m wide, 120*0.003 = 0.36m tall
            var canvasGo = new GameObject("Visual");
            canvasGo.transform.SetParent(_menu.transform, false);
            canvasGo.transform.localScale = Vector3.one * 0.003f;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 120f);

            // Dark background
            var bg = new GameObject("BG", typeof(Image));
            bg.transform.SetParent(canvasGo.transform, false);
            bg.GetComponent<Image>().color = new Color(0.1f, 0.05f, 0.3f, 0.92f);
            SetFullRect(bg.GetComponent<RectTransform>());

            // "Teleport?" label (top half)
            var titleGo = new GameObject("Title", typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(canvasGo.transform, false);
            var title = titleGo.GetComponent<TextMeshProUGUI>();
            title.text = "Teleport?"; title.fontSize = 32;
            title.alignment = TextAlignmentOptions.Center; title.color = Color.white;
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f); titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;

            // Blue button visual (bottom 40%)
            var btnVis = new GameObject("BtnVisual", typeof(Image));
            btnVis.transform.SetParent(canvasGo.transform, false);
            btnVis.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f);
            var btnVisRt = btnVis.GetComponent<RectTransform>();
            btnVisRt.anchorMin = new Vector2(0.1f, 0.05f); btnVisRt.anchorMax = new Vector2(0.9f, 0.45f);
            btnVisRt.offsetMin = btnVisRt.offsetMax = Vector2.zero;

            // "Yes" label on button
            var btnLabelGo = new GameObject("BtnLabel", typeof(TextMeshProUGUI));
            btnLabelGo.transform.SetParent(canvasGo.transform, false);
            var btnText = btnLabelGo.GetComponent<TextMeshProUGUI>();
            btnText.text = "Yes"; btnText.fontSize = 28;
            btnText.alignment = TextAlignmentOptions.Center; btnText.color = Color.white;
            var btnLabelRt = btnLabelGo.GetComponent<RectTransform>();
            btnLabelRt.anchorMin = new Vector2(0.1f, 0.05f); btnLabelRt.anchorMax = new Vector2(0.9f, 0.45f);
            btnLabelRt.offsetMin = btnLabelRt.offsetMax = Vector2.zero;

            // --- Interaction layer: invisible 3D collider + XRSimpleInteractable ---
            // Covers the button visual area (world coords relative to _menu):
            //   X: -0.36m to +0.36m, Y: -0.162m to -0.018m, center Y = -0.09m
            // Placed at localZ = -0.005 so XR rays hit it before the canvas plane
            var hitZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hitZone.name = "HitZone";
            hitZone.transform.SetParent(_menu.transform, false);
            hitZone.transform.localPosition = new Vector3(0f, -0.09f, -0.005f);
            hitZone.transform.localScale = new Vector3(0.72f, 0.144f, 0.005f);
            hitZone.GetComponent<Renderer>().enabled = false; // invisible — canvas shows the visuals

            var interactable = hitZone.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

            interactable.selectEntered.AddListener((_) => OnTeleportPressed());

            _menu.SetActive(false);
        }

        private static void SetFullRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
