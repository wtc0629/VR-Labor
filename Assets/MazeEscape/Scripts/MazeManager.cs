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

            var exit = new GameObject("ExitTrigger");
            exit.transform.SetParent(transform);
            var exitPos = _renderer.GetExitPosition();
            exit.transform.position = new Vector3(exitPos.x, 1f, exitPos.z);

            var col = exit.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(_renderer.CellSize * 0.9f, 2f, _renderer.CellSize * 0.9f);

            exit.AddComponent<ExitTrigger>();
        }

#if UNITY_EDITOR
        [ContextMenu("Regenerate Maze")]
        void RegenerateMaze() => BuildMaze();
#endif
    }
}
