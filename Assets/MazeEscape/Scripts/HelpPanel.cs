using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;
using TMPro;

namespace MazeEscape
{
    public class HelpPanel : MonoBehaviour
    {
        private InputAction _toggleAction;
        private Canvas _canvas;

        // Slider visual range, in px, relative to the handle's anchor point
        private const float HandleRangePx = 117.6f;

        // Maze-size state (always adjustable via right thumbstick)
        private int _mazeSize;
        private const int MinSize = 4;
        private const int MaxSize = 20;
        private const float Deadzone = 0.4f;
        private const float StepInterval = 0.25f;
        private float _stepTimer;

        private TextMeshProUGUI _sizeLabel;
        private RectTransform _handleRt;
        private MazeManager _mazeManager;

        // Right controller state read via XR subsystem (bypasses Input System
        // binding resolution issues with OpenXR thumbstick controls).
        private UnityEngine.XR.InputDevice _rightController;
        private bool _rightHoverRestart;
        private bool _prevTrigger;

        void Awake()
        {
            _toggleAction = new InputAction("ToggleHelp", InputActionType.Button);
            _toggleAction.AddBinding("<XRController>{LeftHand}/secondaryButton");
            _toggleAction.Enable();
        }

        void OnDisable() => _toggleAction?.Disable();

        public void Initialize(Transform hmdCamera, Transform leftHand, Vector3 localPosition, Vector3 localEulerAngles)
        {
            if (hmdCamera == null || leftHand == null) return;

            _mazeManager = FindFirstObjectByType<MazeManager>();
            _mazeSize = _mazeManager != null ? _mazeManager.MazeWidth : 10;
            _mazeSize = Mathf.Clamp(_mazeSize, MinSize, MaxSize);

            var panelGo = new GameObject("HelpPanel");
            panelGo.transform.SetParent(leftHand, false);
            panelGo.transform.localPosition = localPosition;
            panelGo.transform.localRotation = Quaternion.Euler(localEulerAngles);

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

            var textGo = new GameObject("Text", typeof(TextMeshProUGUI));
            textGo.transform.SetParent(panelGo.transform, false);
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text =
                "<color=#FFD700><b>Controls</b></color>\n" +
                "<color=#AADDFF>[Y]</color>         Toggle this panel\n" +
                "<color=#AADDFF>[x]</color>         Toggle the minimap and indicators\n" +
                "<color=#AADDFF>[R Grip]</color>    Confirm teleport\n" +
                "<color=#AADDFF>[R Trigger]</color> Restart (aim at btn)\n" +
                "<color=#AADDFF>[R Stick]</color>   Adjust maze size\n" +
                "<color=#FFD700>[Gold ●]</color>    Wall breaker item\n" +
                "<color=#AADDFF>[A]</color>         Select wall (right ray)\n" +
                "<color=#AADDFF>[B]</color>         Confirm destroy";
            tmp.fontSize = 12;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0.40f);
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 4f);
            textRt.offsetMax = new Vector2(-8f, -4f);

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

        // ── Maze-size slider (visual only — input handled in Update) ─────────
        private void BuildSizeSlider(Transform panelRoot)
        {
            _sizeLabel = CreateLabel(panelRoot, "SizeLabel", $"Maze Size: {_mazeSize}",
                new Vector2(0.05f, 0.305f), new Vector2(0.95f, 0.375f), 14, FontStyles.Normal);

            var track = new GameObject("Track", typeof(Image));
            track.transform.SetParent(panelRoot, false);
            track.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);
            var trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0.08f, 0.225f);
            trackRt.anchorMax = new Vector2(0.92f, 0.275f);
            trackRt.offsetMin = trackRt.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle", typeof(Image));
            handle.transform.SetParent(panelRoot, false);
            var handleImg = handle.GetComponent<Image>();
            handleImg.color = new Color(0.2f, 0.6f, 1f);
            _handleRt = handle.GetComponent<RectTransform>();
            _handleRt.anchorMin = _handleRt.anchorMax = new Vector2(0.5f, 0.25f);
            _handleRt.pivot = new Vector2(0.5f, 0.5f);
            _handleRt.sizeDelta = new Vector2(16f, 16f);

            UpdateSliderVisual();
        }

        private void UpdateSliderVisual()
        {
            if (_sizeLabel == null) return;
            _sizeLabel.text = $"Maze Size: {_mazeSize}";
            float t = Mathf.InverseLerp(MinSize, MaxSize, _mazeSize);
            _handleRt.anchoredPosition = new Vector2(Mathf.Lerp(-HandleRangePx, HandleRangePx, t), 0f);
        }

        // ── Restart button ───────────────────────────────────────────────────
        // Uses XRI hover detection (reliable) combined with XR subsystem
        // trigger reading (bypasses the selectEntered blocking issue caused
        // by the left hand's near-interactor always hovering its own panel).
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

            // Parent the hitzone to leftHand (panelRoot's parent), NOT to the
            // canvas, because the canvas scale (~0.0005) would shrink the
            // collider to sub-millimetre size, making it undetectable by
            // the far-ray interactor.
            var leftHand = panelRoot.parent;
            var hitZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hitZone.name = "RestartHitZone";
            hitZone.transform.SetParent(leftHand, false);
            hitZone.transform.localPosition = panelRoot.localPosition
                + panelRoot.localRotation * new Vector3(0f, -0.0462f, -0.002f);
            hitZone.transform.localRotation = panelRoot.localRotation;
            hitZone.transform.localScale = new Vector3(0.126f, 0.018f, 0.01f);
            hitZone.GetComponent<Renderer>().enabled = false;
            Destroy(hitZone.GetComponent<Collider>());
            var col = hitZone.AddComponent<BoxCollider>();
            col.size = Vector3.one;

            var interactable = hitZone.AddComponent<XRSimpleInteractable>();
            interactable.selectMode = InteractableSelectMode.Multiple;
            interactable.hoverEntered.AddListener(args =>
            {
                if (args.interactorObject.handedness == InteractorHandedness.Right)
                    _rightHoverRestart = true;
            });
            interactable.hoverExited.AddListener(args =>
            {
                if (args.interactorObject.handedness == InteractorHandedness.Right)
                    _rightHoverRestart = false;
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

            if (!_rightController.isValid)
            {
                var devices = new List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                    UnityEngine.XR.InputDeviceCharacteristics.Right |
                    UnityEngine.XR.InputDeviceCharacteristics.Controller,
                    devices);
                if (devices.Count > 0) _rightController = devices[0];
                if (!_rightController.isValid) return;
            }

            // Right thumbstick → step maze size (always active)
            _stepTimer -= Time.deltaTime;
            if (_stepTimer <= 0f)
            {
                _rightController.TryGetFeatureValue(
                    UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 stick);
                if (Mathf.Abs(stick.x) >= Deadzone)
                {
                    int newSize = Mathf.Clamp(
                        _mazeSize + (stick.x > 0 ? 1 : -1), MinSize, MaxSize);
                    if (newSize != _mazeSize)
                    {
                        _mazeSize = newSize;
                        UpdateSliderVisual();
                    }
                    _stepTimer = StepInterval;
                }
            }

            // Right trigger + hover on restart button → restart
            _rightController.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.triggerButton, out bool trigger);
            bool triggerDown = trigger && !_prevTrigger;
            _prevTrigger = trigger;
            if (_rightHoverRestart && triggerDown)
                _mazeManager?.Restart(_mazeSize);
        }

        private static void FullRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
