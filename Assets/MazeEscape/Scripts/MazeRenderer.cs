using UnityEngine;

namespace MazeEscape
{
    public class MazeRenderer : MonoBehaviour
    {
        [Header("Dimensions")]
        public float CellSize = 4f;
        public float WallHeight = 3f;
        public float WallThickness = 0.3f;

        [Header("Materials")]
        public Material WallMaterial;
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

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector3 cellOrigin = new Vector3(x * CellSize, 0f, y * CellSize);
                    BuildFloor(cellOrigin, x, y);
                    BuildWalls(cells[x, y], cellOrigin);
                }
            }

            // Mark exit
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
        }

        private void BuildWalls(MazeCell cell, Vector3 origin)
        {
            float half = CellSize / 2f;

            // North wall (top edge, +Z side)
            if (cell.WallNorth)
                CreateWall(origin + new Vector3(half, WallHeight / 2f, CellSize), 0f, new Vector3(CellSize + WallThickness, WallHeight, WallThickness));

            // South wall (bottom edge)
            if (cell.WallSouth)
                CreateWall(origin + new Vector3(half, WallHeight / 2f, 0f), 0f, new Vector3(CellSize + WallThickness, WallHeight, WallThickness));

            // East wall (+X side)
            if (cell.WallEast)
                CreateWall(origin + new Vector3(CellSize, WallHeight / 2f, half), 0f, new Vector3(WallThickness, WallHeight, CellSize + WallThickness));

            // West wall (-X side)
            if (cell.WallWest)
                CreateWall(origin + new Vector3(0f, WallHeight / 2f, half), 0f, new Vector3(WallThickness, WallHeight, CellSize + WallThickness));
        }

        private void CreateWall(Vector3 position, float rotY, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(transform);
            wall.transform.localPosition = position;
            wall.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            wall.transform.localScale = scale;
            if (WallMaterial) wall.GetComponent<Renderer>().material = WallMaterial;
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
        }

        public Vector3 GetStartPosition() =>
            new Vector3(CellSize / 2f, 0f, CellSize / 2f);

        public Vector3 GetExitPosition() =>
            new Vector3((_width - 1) * CellSize + CellSize / 2f, 0f, (_height - 1) * CellSize + CellSize / 2f);
    }
}
