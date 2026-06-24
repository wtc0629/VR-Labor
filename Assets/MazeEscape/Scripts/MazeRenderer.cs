using System.Collections.Generic;
using UnityEngine;

namespace MazeEscape
{
    public class MazeRenderer : MonoBehaviour
    {
        [Header("Dimensions")]
        public float CellSize = 4f;
        public float WallHeight = 3f;
        public float WallThickness = 0.3f;

        [Header("Layer")]
        [Tooltip("Layer assigned to walls and corner posts. Must match CameraFade's WallLayers.")]
        public string WallLayerName = "Wall";
        [Tooltip("Layer assigned to floor tiles.")]
        public string FloorLayerName = "Floor";
        [Tooltip("Layer assigned to the exit marker.")]
        public string ExitLayerName = "Exit";

        [Header("Materials")]
        public Material WallMaterial;
        public Material CornerPostMaterial;
        public Material FloorMaterial;
        public Material ExitMaterial;

        private int _width;
        private int _height;

        public void Render(MazeCell[,] cells)
        {
            _width = cells.GetLength(0);
            _height = cells.GetLength(1);

            // Clear previous geometry
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            var wallDict = new Dictionary<(int, int, WallDirection), WallData>();

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector3 cellOrigin = new Vector3(x * CellSize, 0f, y * CellSize);
                    BuildFloor(cellOrigin, x, y);
                    BuildWalls(cells[x, y], cellOrigin, x, y, wallDict);
                }
            }

            // Link twins — shared-border walls that overlap
            foreach (var kv in wallDict)
            {
                (int cx, int cy, WallDirection dir) = kv.Key;
                (int ax, int ay, WallDirection ad) = AdjacentKey(cx, cy, dir);
                if (wallDict.TryGetValue((ax, ay, ad), out WallData twin))
                    kv.Value.Twin = twin;
            }

            BuildCornerPosts();
            MarkExit();
        }

        private void BuildFloor(Vector3 origin, int x, int y)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = $"Floor_{x}_{y}";
            floor.transform.SetParent(transform);
            floor.transform.localPosition = origin + new Vector3(CellSize / 2f, -0.05f, CellSize / 2f);
            floor.transform.localScale = new Vector3(CellSize, 0.1f, CellSize);
            if (FloorMaterial) floor.GetComponent<Renderer>().material = FloorMaterial;
            int layer = LayerMask.NameToLayer(FloorLayerName);
            if (layer >= 0) floor.layer = layer;
        }

        private void BuildWalls(MazeCell cell, Vector3 origin, int cx, int cy,
            Dictionary<(int, int, WallDirection), WallData> dict)
        {
            float half = CellSize / 2f;

            if (cell.WallNorth)
            {
                var w = CreateWall(origin + new Vector3(half, WallHeight / 2f, CellSize), 0f,
                    new Vector3(CellSize, WallHeight, WallThickness));
                RegisterWall(w, cx, cy, WallDirection.North, dict);
            }
            if (cell.WallSouth)
            {
                var w = CreateWall(origin + new Vector3(half, WallHeight / 2f, 0f), 0f,
                    new Vector3(CellSize, WallHeight, WallThickness));
                RegisterWall(w, cx, cy, WallDirection.South, dict);
            }
            if (cell.WallEast)
            {
                var w = CreateWall(origin + new Vector3(CellSize, WallHeight / 2f, half), 0f,
                    new Vector3(WallThickness, WallHeight, CellSize));
                RegisterWall(w, cx, cy, WallDirection.East, dict);
            }
            if (cell.WallWest)
            {
                var w = CreateWall(origin + new Vector3(0f, WallHeight / 2f, half), 0f,
                    new Vector3(WallThickness, WallHeight, CellSize));
                RegisterWall(w, cx, cy, WallDirection.West, dict);
            }
        }

        private void RegisterWall(GameObject wall, int cx, int cy, WallDirection dir,
            Dictionary<(int, int, WallDirection), WallData> dict)
        {
            var wd = wall.AddComponent<WallData>();
            wd.CellX = cx; wd.CellY = cy; wd.Direction = dir;
            wd.MazeWidth = _width; wd.MazeHeight = _height;
            dict[(cx, cy, dir)] = wd;
        }

        private static (int, int, WallDirection) AdjacentKey(int cx, int cy, WallDirection dir)
        {
            return dir switch
            {
                WallDirection.North => (cx, cy + 1, WallDirection.South),
                WallDirection.South => (cx, cy - 1, WallDirection.North),
                WallDirection.East  => (cx + 1, cy, WallDirection.West),
                _                   => (cx - 1, cy, WallDirection.East),
            };
        }

        private void BuildCornerPosts()
        {
            for (int x = 0; x <= _width; x++)
            {
                for (int y = 0; y <= _height; y++)
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = "CornerPost";
                    post.transform.SetParent(transform);
                    post.transform.localPosition = new Vector3(x * CellSize, WallHeight / 2f, y * CellSize);
                    post.transform.localScale = new Vector3(WallThickness + 0.001f, WallHeight, WallThickness + 0.001f);
                    var postMat = CornerPostMaterial ?? WallMaterial;
                    if (postMat) post.GetComponent<Renderer>().material = postMat;
                    int layer = LayerMask.NameToLayer(WallLayerName);
                    if (layer >= 0) post.layer = layer;
                }
            }
        }

        private GameObject CreateWall(Vector3 position, float rotY, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(transform);
            wall.transform.localPosition = position;
            wall.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            wall.transform.localScale = scale;
            if (WallMaterial) wall.GetComponent<Renderer>().material = WallMaterial;
            int layer = LayerMask.NameToLayer(WallLayerName);
            if (layer >= 0) wall.layer = layer;
            return wall;
        }

        private void MarkExit()
        {
            float ex = (_width - 1) * CellSize;
            float ey = (_height - 1) * CellSize;
            var exitMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitMarker.name = "ExitMarker";
            exitMarker.transform.SetParent(transform);
            exitMarker.transform.localPosition = new Vector3(ex + CellSize / 2f, -0.04f, ey + CellSize / 2f);
            exitMarker.transform.localScale = new Vector3(CellSize, 0.12f, CellSize);

            var mat = ExitMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0f, 0.8f, 0.2f) };
            exitMarker.GetComponent<Renderer>().material = mat;
            Destroy(exitMarker.GetComponent<Collider>());

            int layer = LayerMask.NameToLayer(ExitLayerName);
            if (layer >= 0) exitMarker.layer = layer;
        }

        public Vector3 GetStartPosition() =>
            new Vector3(CellSize / 2f, 0f, CellSize / 2f);

        public Vector3 GetExitPosition() =>
            new Vector3((_width - 1) * CellSize + CellSize / 2f, 0f, (_height - 1) * CellSize + CellSize / 2f);
    }
}
