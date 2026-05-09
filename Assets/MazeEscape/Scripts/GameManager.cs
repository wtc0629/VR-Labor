using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MazeEscape
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("HMD Reference")]
        [Tooltip("Main Camera inside XR Origin")]
        public Transform HMDCamera;

        private float _startTime;
        private bool _won;
        private GameObject _victoryPanel;
        private TextMeshProUGUI _victoryText;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _startTime = Time.time;
            BuildVictoryPanel();
        }

        private void BuildVictoryPanel()
        {
            // Canvas
            _victoryPanel = new GameObject("VictoryPanel");
            var canvas = _victoryPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            _victoryPanel.AddComponent<CanvasScaler>();
            _victoryPanel.AddComponent<GraphicRaycaster>();

            var rt = _victoryPanel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 200f);

            // Scale: 0.4m wide (400px * 0.001 = 0.4m)
            _victoryPanel.transform.localScale = Vector3.one * 0.001f;

            // Fix to HMD center view
            if (HMDCamera != null)
            {
                _victoryPanel.transform.SetParent(HMDCamera, false);
                _victoryPanel.transform.localPosition = new Vector3(0f, 0f, 0.6f);
                _victoryPanel.transform.localRotation = Quaternion.identity;
            }

            // Background
            var bg = new GameObject("Background", typeof(Image));
            bg.transform.SetParent(_victoryPanel.transform, false);
            bg.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.2f, 0.92f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            // Text
            var textGo = new GameObject("VictoryText", typeof(TextMeshProUGUI));
            textGo.transform.SetParent(_victoryPanel.transform, false);
            _victoryText = textGo.GetComponent<TextMeshProUGUI>();
            _victoryText.text = "Escaped!";
            _victoryText.fontSize = 48;
            _victoryText.alignment = TextAlignmentOptions.Center;
            _victoryText.color = new Color(1f, 0.85f, 0.2f);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;

            _victoryPanel.SetActive(false);
        }

        public void TriggerWin()
        {
            if (_won) return;
            _won = true;

            float elapsed = Time.time - _startTime;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);

            _victoryText.text = $"Escaped!\n{minutes:00}:{seconds:00}";
            _victoryPanel.SetActive(true);
            Debug.Log($"[MazeEscape] Win! Time: {minutes:00}:{seconds:00}");
        }
    }
}
