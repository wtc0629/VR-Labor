namespace MazeEscape
{
    public struct MazeCell
    {
        public bool WallNorth;
        public bool WallSouth;
        public bool WallEast;
        public bool WallWest;
        public bool Visited;

        public static MazeCell Default => new MazeCell
        {
            WallNorth = true,
            WallSouth = true,
            WallEast = true,
            WallWest = true,
            Visited = false
        };
    }
}
