using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace MazeEscape
{
    public class HelpPanel : MonoBehaviour
    {
        private InputAction _toggleAction;
        private Canvas _canvas;

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

            var panelGo = new GameObject("HelpPanel");
            panelGo.transform.SetParent(leftHand, false);

            panelGo.transform.localPosition = localPosition;
            panelGo.transform.localRotation = Quaternion.Euler(localEulerAngles);

            const float cW = 280f, cH = 160f;
            panelGo.transform.localScale = Vector3.one * (0.14f / cW);

            _canvas = panelGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            panelGo.AddComponent<CanvasScaler>();
            panelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(cW, cH);

            var bg = new GameObject("BG", typeof(Image));
            bg.transform.SetParent(panelGo.transform, false);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

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
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 8f);
            textRt.offsetMax = new Vector2(-8f, -8f);
        }

        void Update()
        {
            if (_canvas != null && _toggleAction.WasPressedThisFrame())
                _canvas.enabled = !_canvas.enabled;
        }
    }
}
