using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MazeEscape
{
    public class CollectibleManager : MonoBehaviour
    {
        public static CollectibleManager Instance { get; private set; }

        private int _total;
        private int _collected;
        private TextMeshProUGUI _countText;
        private Canvas _indicatorCanvas;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(int total, Transform attachpoint, Vector3 mapOffset, Vector3 mapEulerAngles)
        {
            _total = total;
            _collected = 0;

            if (_indicatorCanvas != null)
                Destroy(_indicatorCanvas.gameObject);

            CreateIndicator(attachpoint, mapOffset, mapEulerAngles);
        }

        public void Collect()
        {
            _collected++;
            if (_countText != null)
                _countText.text = $"×{_collected}/{_total}";
        }

        public string GetStatsText() => $"{_collected}/{_total}";

        public void SetVisible(bool visible)
        {
            if (_indicatorCanvas != null)
                _indicatorCanvas.enabled = visible;
        }

        private void CreateIndicator(Transform attachpoint, Vector3 mapOffset, Vector3 mapEulerAngles)
        {
            if (attachpoint == null) return;

            var indicator = new GameObject("CollectibleIndicator");
            indicator.transform.SetParent(attachpoint, false);
            indicator.transform.localPosition = new Vector3(
                mapOffset.x,
                mapOffset.y - 0.06f,
                mapOffset.z + 0.02f);
            indicator.transform.localRotation = Quaternion.Euler(mapEulerAngles);

            const float cW = 100f, cH = 40f;
            indicator.transform.localScale = Vector3.one * (0.04f / cW);

            _indicatorCanvas = indicator.AddComponent<Canvas>();
            _indicatorCanvas.renderMode = RenderMode.WorldSpace;
            var canvas = _indicatorCanvas;
            indicator.AddComponent<CanvasScaler>();
            indicator.GetComponent<RectTransform>().sizeDelta = new Vector2(cW, cH);

            var bg = new GameObject("BG", typeof(Image));
            bg.transform.SetParent(indicator.transform, false);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            FullRect(bg.GetComponent<RectTransform>());

            var circle = new GameObject("Circle", typeof(Image));
            circle.transform.SetParent(indicator.transform, false);
            var circleImg = circle.GetComponent<Image>();
            circleImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            circleImg.color = new Color(0.2f, 0.6f, 1f);
            var circleRt = circle.GetComponent<RectTransform>();
            circleRt.anchorMin = new Vector2(0.06f, 0.12f);
            circleRt.anchorMax = new Vector2(0.40f, 0.88f);
            circleRt.offsetMin = circleRt.offsetMax = Vector2.zero;

            var labelGo = new GameObject("Label", typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(indicator.transform, false);
            _countText = labelGo.GetComponent<TextMeshProUGUI>();
            _countText.text = $"×0/{_total}";
            _countText.fontSize = 22;
            _countText.color = Color.white;
            _countText.alignment = TextAlignmentOptions.MidlineLeft;
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.44f, 0f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
        }

        private static void FullRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
