using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TMPro;

namespace MazeEscape
{
    public class HelpPanel : MonoBehaviour
    {
        private InputAction _toggleAction;
        private Canvas _canvas;

        // Slider visual range, in px, relative to the handle's anchor point
        private const float HandleRangePx = 117.6f;

        private MazeSizeSlider _sizeSlider;
        private TextMeshProUGUI _sizeLabel;
        private RectTransform _handleRt;
        private MazeManager _mazeManager;

        void Awake()
        {
            _toggleAction = new InputAction("ToggleHelp", InputActionType.Button);
            _toggleAction.AddBinding("<XRController>{LeftHand}/secondaryButton");
            _toggleAction.Enable();
        }

        void OnDisable() => _toggleAction?.Disable();

        public void Initialize(Transform hmdCamera, Transform leftHand, Vector3 localPosition, Vector3 localEulerAngles)
        {
            if (hmdCamera == null) return;
            if (leftHand == null) return;

            _mazeManager = FindFirstObjectByType<MazeManager>();

            var panelGo = new GameObject("HelpPanel");
            panelGo.transform.SetParent(leftHand, false);

            panelGo.transform.localPosition = localPosition;
            panelGo.transform.localRotation = Quaternion.Euler(localEulerAngles);

            // Canvas: 280 x 240 px — controls text on top, maze-size slider +
            // restart button in the lower portion.
            const float cW = 280f, cH = 240f;
            panelGo.transform.localScale = Vector3.one * (0.14f / cW);

            _canvas = panelGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            panelGo.AddComponent<CanvasScaler>();
            panelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(cW, cH);

            var bg = new GameObject("BG", typeof(Image));
            bg.transform.SetParent(panelGo.transform, false);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            FullRect(bg.GetComponent<RectTransform>());

            // Controls text — top portion of the canvas
            var textGo = new GameObject("Text", typeof(TextMeshProUGUI));
            textGo.transform.SetParent(panelGo.transform, false);
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text =
                "<color=#FFD700><b>Controls</b></color>\n" +
                "<color=#AADDFF>[X]</color>        Toggle minimap\n" +
                "<color=#AADDFF>[Y]</color>        Toggle this panel\n" +
                "<color=#AADDFF>[Trigger]</color>  Confirm teleport\n" +
                "<color=#FFD700>[Gold ●]</color>   Wall breaker item\n" +
                "<color=#AADDFF>[A]</color>        Select wall (right ray)\n" +
                "<color=#AADDFF>[B]</color>        Confirm destroy";
            tmp.fontSize = 13;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0.40f);
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 4f);
            textRt.offsetMax = new Vector2(-8f, -4f);

            // Divider line
            var divGo = new GameObject("Divider", typeof(Image));
            divGo.transform.SetParent(panelGo.transform, false);
            divGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
            var divRt = divGo.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.03f, 0.385f);
            divRt.anchorMax = new Vector2(0.97f, 0.395f);
            divRt.offsetMin = divRt.offsetMax = Vector2.zero;

            BuildSizeSlider(panelGo.transform);
            BuildRestartButton(panelGo.transform);
        }

        // ── Maze-size slider ──────────────────────────────────────────────────
        // Track spans anchors x:[0.08,0.92] y:[0.225,0.275] → centred at canvas
        // local (0, -0.03)m, half-width 0.0588m. The 3-D hit-zone covers this
        // area for right-hand hover detection (see MazeSizeSlider).
        private void BuildSizeSlider(Transform panelRoot)
        {
            int initialValue = _mazeManager != null ? _mazeManager.MazeWidth : 10;

            _sizeLabel = CreateLabel(panelRoot, "SizeLabel", $"Maze Size: {initialValue}",
                new Vector2(0.05f, 0.305f), new Vector2(0.95f, 0.375f), 14, FontStyles.Normal);

            // Track background bar
            var track = new GameObject("Track", typeof(Image));
            track.transform.SetParent(panelRoot, false);
            track.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);
            var trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0.08f, 0.225f);
            trackRt.anchorMax = new Vector2(0.92f, 0.275f);
            trackRt.offsetMin = trackRt.offsetMax = Vector2.zero;

            // Handle knob
            var handle = new GameObject("Handle", typeof(Image));
            handle.transform.SetParent(panelRoot, false);
            var handleImg = handle.GetComponent<Image>();
            handleImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            handleImg.color = new Color(0.2f, 0.6f, 1f);
            _handleRt = handle.GetComponent<RectTransform>();
            _handleRt.anchorMin = _handleRt.anchorMax = new Vector2(0.5f, 0.25f);
            _handleRt.pivot = new Vector2(0.5f, 0.5f);
            _handleRt.sizeDelta = new Vector2(16f, 16f);

            // Invisible 3-D hit zone covering the track for ray-drag input
            var hitZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hitZone.name = "SliderHitZone";
            hitZone.transform.SetParent(panelRoot, false);
            hitZone.transform.localPosition = new Vector3(0f, -0.024f, -0.001f);
            hitZone.transform.localScale = new Vector3(0.1176f, 0.018f, 0.005f);
            hitZone.GetComponent<Renderer>().enabled = false;
            Destroy(hitZone.GetComponent<Collider>());
            var col = hitZone.AddComponent<BoxCollider>();
            col.size = Vector3.one;

            hitZone.AddComponent<XRSimpleInteractable>();

            _sizeSlider = hitZone.AddComponent<MazeSizeSlider>();
            _sizeSlider.Setup(initialValue);
            _sizeSlider.OnValueChanged += UpdateSliderVisual;

            UpdateSliderVisual(_sizeSlider.Value);
        }

        private void UpdateSliderVisual(int value)
        {
            _sizeLabel.text = $"Maze Size: {value}";
            float t = Mathf.InverseLerp(_sizeSlider.MinValue, _sizeSlider.MaxValue, value);
            _handleRt.anchoredPosition = new Vector2(Mathf.Lerp(-HandleRangePx, HandleRangePx, t), 0f);
        }

        // ── Restart button ───────────────────────────────────────────────────
        private void BuildRestartButton(Transform panelRoot)
        {
            var btnBg = new GameObject("RestartBtnBG", typeof(Image));
            btnBg.transform.SetParent(panelRoot, false);
            btnBg.GetComponent<Image>().color = new Color(0.1f, 0.55f, 0.45f, 0.95f);
            var btnBgRt = btnBg.GetComponent<RectTransform>();
            btnBgRt.anchorMin = new Vector2(0.05f, 0.04f);
            btnBgRt.anchorMax = new Vector2(0.95f, 0.19f);
            btnBgRt.offsetMin = btnBgRt.offsetMax = Vector2.zero;

            CreateLabel(panelRoot, "RestartBtnLabel", "Restart",
                new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.19f), 16, FontStyles.Bold);

            var hitZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hitZone.name = "RestartHitZone";
            hitZone.transform.SetParent(panelRoot, false);
            hitZone.transform.localPosition = new Vector3(0f, -0.0462f, -0.001f);
            hitZone.transform.localScale = new Vector3(0.126f, 0.018f, 0.005f);
            hitZone.GetComponent<Renderer>().enabled = false;
            Destroy(hitZone.GetComponent<Collider>());
            var col = hitZone.AddComponent<BoxCollider>();
            col.size = Vector3.one;

            var interactable = hitZone.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(args =>
            {
                if (args.interactorObject.handedness == InteractorHandedness.Right)
                    _mazeManager?.Restart(_sizeSlider.Value);
            });
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, float fontSize, FontStyles style)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return tmp;
        }

        void Update()
        {
            if (_canvas != null && _toggleAction.WasPressedThisFrame())
                _canvas.enabled = !_canvas.enabled;
        }

        private static void FullRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
