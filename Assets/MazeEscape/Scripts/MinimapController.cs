using UnityEngine;
using UnityEngine.UI;

namespace MazeEscape
{
    [RequireComponent(typeof(Canvas))]
    public class MinimapController : MonoBehaviour
    {
        [Header("Minimap Settings")]
        public int PixelsPerCell = 5;
        public int WallPixels = 1;
        public int DisplaySize = 180;

        [Header("Colors")]
        public Color FogColor = Color.black;
        public Color FloorColor = new Color(0.75f, 0.75f, 0.75f);
        public Color WallColor = new Color(0.25f, 0.25f, 0.25f);
        public Color PlayerColor = Color.cyan;
        public Color ExitColor = new Color(0f, 0.9f, 0.3f);

        [Header("HMD Attachment")]
        public Vector3 LocalOffset = new Vector3(0.12f, 0.08f, 0.25f);

        private MazeCell[,] _cells;
        private float _cellSize;
        private int _width, _height;
        private bool[,] _explored;
        private Texture2D _tex;
        private RawImage _rawImage;
        private Transform _player;

        private int _texWidth, _texHeight;
        private int _prevCx = -1, _prevCy = -1;
        private int _playerDotCx = -1, _playerDotCy = -1;

        public void Initialize(MazeCell[,] cells, float cellSize, Transform hmdCamera, Transform player)
        {
            _cells = cells;
            _cellSize = cellSize;
            _width = cells.GetLength(0);
            _height = cells.GetLength(1);
            _player = player;
            _explored = new bool[_width, _height];

            _texWidth  = (PixelsPerCell + WallPixels) * _width  + WallPixels;
            _texHeight = (PixelsPerCell + WallPixels) * _height + WallPixels;

            _tex = new Texture2D(_texWidth, _texHeight, TextureFormat.RGB24, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            // Fill everything with fog
            Color[] fog = new Color[_texWidth * _texHeight];
            for (int i = 0; i < fog.Length; i++) fog[i] = FogColor;
            _tex.SetPixels(fog);
            _tex.Apply();

            SetupCanvas(hmdCamera);
            MarkExit();
        }

        private void SetupCanvas(Transform hmdCamera)
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(DisplaySize, DisplaySize);

            float worldScale = 0.1f / DisplaySize;
            transform.localScale = Vector3.one * worldScale;
            transform.SetParent(hmdCamera, false);
            transform.localPosition = LocalOffset;
            transform.localRotation = Quaternion.identity;

            // Background
            var bg = new GameObject("Background", typeof(Image));
            bg.transform.SetParent(transform, false);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.6f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            // Map image
            var mapGo = new GameObject("Map", typeof(RawImage));
            mapGo.transform.SetParent(transform, false);
            _rawImage = mapGo.GetComponent<RawImage>();
            _rawImage.texture = _tex;
            var mapRt = mapGo.GetComponent<RectTransform>();
            mapRt.anchorMin = new Vector2(0.05f, 0.05f);
            mapRt.anchorMax = new Vector2(0.95f, 0.95f);
            mapRt.offsetMin = mapRt.offsetMax = Vector2.zero;
        }

        private void MarkExit()
        {
            // Paint exit cell with exit color but keep it under fog (revealed only when visited)
            // We'll paint it as a special color when revealed
        }

        void Update()
        {
            if (_cells == null || _player == null) return;

            int cx = Mathf.FloorToInt(_player.position.x / _cellSize);
            int cy = Mathf.FloorToInt(_player.position.z / _cellSize);
            cx = Mathf.Clamp(cx, 0, _width - 1);
            cy = Mathf.Clamp(cy, 0, _height - 1);

            if (cx != _prevCx || cy != _prevCy)
            {
                RevealCell(cx, cy);
                _prevCx = cx;
                _prevCy = cy;
            }

            DrawPlayerDot(cx, cy);
        }

        private void RevealCell(int cx, int cy)
        {
            if (_explored[cx, cy]) return;
            _explored[cx, cy] = true;

            bool isExit = (cx == _width - 1 && cy == _height - 1);
            Color floor = isExit ? ExitColor : FloorColor;

            int tx = CellToTexX(cx);
            int ty = CellToTexY(cy);

            // Fill floor pixels
            FillRect(tx, ty, PixelsPerCell, PixelsPerCell, floor);

            ref var cell = ref _cells[cx, cy];

            // Open passages to neighbours (reveal corridor pixels between cells)
            if (!cell.WallNorth && cy + 1 < _height)
                FillRect(tx, ty + PixelsPerCell, PixelsPerCell, WallPixels, FloorColor);
            if (!cell.WallSouth && cy - 1 >= 0)
                FillRect(tx, ty - WallPixels, PixelsPerCell, WallPixels, FloorColor);
            if (!cell.WallEast && cx + 1 < _width)
                FillRect(tx + PixelsPerCell, ty, WallPixels, PixelsPerCell, FloorColor);
            if (!cell.WallWest && cx - 1 >= 0)
                FillRect(tx - WallPixels, ty, WallPixels, PixelsPerCell, FloorColor);

            // Fill wall pixels (closed walls → dark gray, visible once cell is revealed)
            if (cell.WallNorth)
                FillRect(tx, ty + PixelsPerCell, PixelsPerCell, WallPixels, WallColor);
            if (cell.WallSouth)
                FillRect(tx, ty - WallPixels, PixelsPerCell, WallPixels, WallColor);
            if (cell.WallEast)
                FillRect(tx + PixelsPerCell, ty, WallPixels, PixelsPerCell, WallColor);
            if (cell.WallWest)
                FillRect(tx - WallPixels, ty, WallPixels, PixelsPerCell, WallColor);

            _tex.Apply();
        }

        private void DrawPlayerDot(int cx, int cy)
        {
            bool changed = (cx != _playerDotCx || cy != _playerDotCy);
            if (!changed) return;

            // Erase old dot
            if (_playerDotCx >= 0 && _explored[_playerDotCx, _playerDotCy])
            {
                bool wasExit = (_playerDotCx == _width - 1 && _playerDotCy == _height - 1);
                int ox = CellToTexX(_playerDotCx) + PixelsPerCell / 2;
                int oy = CellToTexY(_playerDotCy) + PixelsPerCell / 2;
                _tex.SetPixel(ox, oy, wasExit ? ExitColor : FloorColor);
            }

            // Draw new dot
            int nx = CellToTexX(cx) + PixelsPerCell / 2;
            int ny = CellToTexY(cy) + PixelsPerCell / 2;
            _tex.SetPixel(nx, ny, PlayerColor);
            _tex.Apply();

            _playerDotCx = cx;
            _playerDotCy = cy;
        }

        private void FillRect(int startX, int startY, int w, int h, Color color)
        {
            for (int x = startX; x < startX + w; x++)
                for (int y = startY; y < startY + h; y++)
                    if (x >= 0 && x < _texWidth && y >= 0 && y < _texHeight)
                        _tex.SetPixel(x, y, color);
        }

        private int CellToTexX(int cx) => WallPixels + cx * (PixelsPerCell + WallPixels);
        private int CellToTexY(int cy) => WallPixels + cy * (PixelsPerCell + WallPixels);
    }
}
