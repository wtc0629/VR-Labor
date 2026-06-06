using UnityEngine;

namespace MazeEscape
{
    public enum WallDirection { North, South, East, West }

    public class WallData : MonoBehaviour
    {
        public int CellX, CellY, MazeWidth, MazeHeight;
        public WallDirection Direction;
        public WallData Twin;

        public bool IsBoundary() =>
            (Direction == WallDirection.North && CellY == MazeHeight - 1) ||
            (Direction == WallDirection.South && CellY == 0) ||
            (Direction == WallDirection.East  && CellX == MazeWidth  - 1) ||
            (Direction == WallDirection.West  && CellX == 0);
    }
}
