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

        [Header("Systems")]
        public MinimapController Minimap;

        private MazeRenderer _renderer;

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

            if (Minimap != null && HMDCamera != null && XROrigin != null)
                Minimap.Initialize(cells, _renderer.CellSize, HMDCamera, XROrigin);

            var exit = new GameObject("ExitTrigger");
            exit.transform.SetParent(transform);
            var exitPos = _renderer.GetExitPosition();
            exit.transform.position = new Vector3(exitPos.x, 1f, exitPos.z);

            var col = exit.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(_renderer.CellSize * 0.9f, 2f, _renderer.CellSize * 0.9f);

            exit.AddComponent<ExitTrigger>();

            CreateTeleportPads();
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
            return pad;
        }

#if UNITY_EDITOR
        [ContextMenu("Regenerate Maze")]
        void RegenerateMaze() => BuildMaze();
#endif
    }
}
