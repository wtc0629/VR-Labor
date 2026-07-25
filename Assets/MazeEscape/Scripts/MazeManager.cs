using System.Collections.Generic;
using UnityEngine;

namespace MazeEscape
{
    [RequireComponent(typeof(MazeRenderer))]
    public class MazeManager : MonoBehaviour
    {
        [Header("Maze Settings")]
        public int MazeWidth = 10;
        public int MazeHeight = 10;
        [Tooltip("-1 = random each run")]
        public int Seed = -1;

        [Header("Player")]
        [Tooltip("Assign the XR Origin GameObject here")]
        public Transform XROrigin;
        [Tooltip("Main Camera inside XR Origin (for minimap HMD attachment)")]
        public Transform HMDCamera;
        [Tooltip("Right Controller Transform")]
        public Transform RightController;

        [Header("Minimap local offset and rotation")]
        public Vector3 MinimapLocalOffset = new Vector3(0.12f, 0.08f, 0.25f);
        public Vector3 MinimapLocalEulerAngles = new Vector3(0f, 270f, 0f);

        [Header("Systems")]
        public MinimapController Minimap;

        [Header("Wall Breaker")]
        [Tooltip("Assign the Right Controller Transform here")]
        public Transform RightControllerOrigin;

        private MazeRenderer _renderer;
        private readonly HashSet<(int, int)> _usedCells = new HashSet<(int, int)>();

        void Awake()
        {
            _renderer = GetComponent<MazeRenderer>();
            BuildMaze();
        }

        public void BuildMaze()
        {
            var cells = MazeGenerator.Generate(MazeWidth, MazeHeight, Seed);
            _renderer.Render(cells);

            if (XROrigin != null)
            {
                var startPos = _renderer.GetStartPosition();
                XROrigin.position = new Vector3(startPos.x, XROrigin.position.y, startPos.z);
            }

            if (Minimap != null && HMDCamera != null && XROrigin != null && RightController != null)
                Minimap.Initialize(cells, _renderer.CellSize, HMDCamera, XROrigin, RightController, MinimapLocalOffset, MinimapLocalEulerAngles);

            var exit = new GameObject("ExitTrigger");
            exit.transform.SetParent(transform);
            var exitPos = _renderer.GetExitPosition();
            exit.transform.position = new Vector3(exitPos.x, 1f, exitPos.z);

            var col = exit.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(_renderer.CellSize * 0.9f, 2f, _renderer.CellSize * 0.9f);

            exit.AddComponent<ExitTrigger>();

            CreateTeleportPads();
            SetupWallBreaker();

            _usedCells.Clear();
            SetupCollectibleManager();
            CreatePowerUpSphere();
            CreateCollectibleSpheres();
        }

        private void CreateTeleportPads()
        {
            float cs = _renderer.CellSize;
            var padA = CreateOnePad("TeleportPadA",
                new Vector3((MazeWidth - 1) * cs + cs / 2f, 0f, cs / 2f));
            var padB = CreateOnePad("TeleportPadB",
                new Vector3(cs / 2f, 0f, (MazeHeight - 1) * cs + cs / 2f));
            padA.OtherPad = padB;
            padB.OtherPad = padA;
        }

        private TeleportPad CreateOnePad(string padName, Vector3 worldPos)
        {
            var go = new GameObject(padName);
            go.transform.SetParent(transform);
            go.transform.localPosition = worldPos;

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.SetParent(go.transform, false);
            marker.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            marker.transform.localScale = new Vector3(_renderer.CellSize, 0.12f, _renderer.CellSize);
            marker.GetComponent<Renderer>().material =
                new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.5f, 0.1f, 0.9f) };
            Destroy(marker.GetComponent<Collider>());

            var pad = go.AddComponent<TeleportPad>();
            pad.XROrigin = XROrigin;
            pad.CC = XROrigin.GetComponent<CharacterController>();

            int teleportLayer = LayerMask.NameToLayer(pad.TeleportLayerName);
            if (teleportLayer >= 0)
            {
                go.layer = teleportLayer;
                marker.layer = teleportLayer;
            }

            return pad;
        }

        private void SetupWallBreaker()
        {
            if (XROrigin == null) return;
            var wb = XROrigin.GetComponent<WallBreaker>()
                  ?? XROrigin.gameObject.AddComponent<WallBreaker>();
            wb.Minimap = Minimap;
            wb.attatchmentPoint = RightController;
            wb.MinimapLocalOffset = MinimapLocalOffset;
            wb.MinimapLocalEulerAngles = MinimapLocalEulerAngles;
            wb.RightControllerOrigin = RightControllerOrigin;
            wb.WallLayer = LayerMask.GetMask(_renderer.WallLayerName);
        }

        private void SetupCollectibleManager()
        {
            var cm = GetComponent<CollectibleManager>()
                  ?? gameObject.AddComponent<CollectibleManager>();
            int n = Mathf.Max(1, Mathf.Min(MazeWidth, MazeHeight) / 5);
            cm.Initialize(n, RightController, MinimapLocalOffset, MinimapLocalEulerAngles);
        }

        private void CreatePowerUpSphere()
        {
            if (XROrigin == null) return;
            float cs = _renderer.CellSize;

            var (cx, cy) = PickUniqueCell(MazeWidth, MazeHeight, avoidCorners: true);

            // Sichtbare Kugel – nur Optik
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PowerUpSphere";
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(cx * cs + cs / 2f, 0.5f, cy * cs + cs / 2f);
            go.transform.localScale = Vector3.one * 0.4f;
            go.GetComponent<Renderer>().material =
                new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(1f, 0.8f, 0f) };
            Destroy(go.GetComponent<Collider>());

            // Unsichtbare Reichweiten-Sphere
            var pickupRange = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickupRange.name = "PickupRange";
            pickupRange.transform.SetParent(go.transform);
            pickupRange.transform.localPosition = Vector3.zero;
            pickupRange.transform.localScale = Vector3.one * 7f;
            pickupRange.GetComponent<Renderer>().enabled = false;
            pickupRange.GetComponent<Collider>().isTrigger = true;
            pickupRange.AddComponent<PowerUpSphere>()
               .Init(XROrigin.GetComponent<WallBreaker>());
        }


        private void CreateCollectibleSpheres()
        {
            int n = Mathf.Max(1, Mathf.Min(MazeWidth, MazeHeight) / 5);
            float cs = _renderer.CellSize;
            for (int i = 0; i < n; i++)
            {
                var (cx, cy) = PickUniqueCell(MazeWidth, MazeHeight, avoidCorners: true);

                // Sichtbare Kugel – nur Optik
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "CollectibleSphere";
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(cx * cs + cs / 2f, 0.5f, cy * cs + cs / 2f);
                go.transform.localScale = Vector3.one * 0.35f;
                go.GetComponent<Renderer>().material =
                    new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.2f, 0.6f, 1f) };
                Destroy(go.GetComponent<Collider>());

                // Unsichtbare Reichweiten-Sphere
                var pickupRange = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pickupRange.name = "PickupRange";
                pickupRange.transform.SetParent(go.transform);
                pickupRange.transform.localPosition = Vector3.zero;
                pickupRange.transform.localScale = Vector3.one * 7f;
                pickupRange.GetComponent<Renderer>().enabled = false;
                pickupRange.GetComponent<Collider>().isTrigger = true;
                pickupRange.AddComponent<CollectibleSphere>();
            }
        }


        private (int, int) PickUniqueCell(int w, int h, bool avoidCorners = true)
        {
            int cx, cy;
            do
            {
                cx = Random.Range(0, w);
                cy = Random.Range(0, h);
            }
            while (_usedCells.Contains((cx, cy)) ||
                   (avoidCorners && IsCorner(cx, cy, w, h)));
            _usedCells.Add((cx, cy));
            return (cx, cy);
        }

        private static bool IsCorner(int cx, int cy, int w, int h) =>
            (cx == 0 && cy == 0) || (cx == w - 1 && cy == h - 1) ||
            (cx == w - 1 && cy == 0) || (cx == 0 && cy == h - 1);

        // Re-generate the maze as a square of the given size and reset
        // dependent systems (wall breaker power-up, win state).
        public void Restart(int newSize)
        {
            newSize = Mathf.Max(3, newSize);
            MazeWidth = newSize;
            MazeHeight = newSize;

            if (XROrigin != null && XROrigin.TryGetComponent<WallBreaker>(out var wb))
                wb.ResetState();
            GameManager.Instance?.ResetGame();

            BuildMaze();
        }

#if UNITY_EDITOR
        [ContextMenu("Regenerate Maze")]
        void RegenerateMaze() => BuildMaze();
#endif
    }
}
