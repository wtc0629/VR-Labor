using System.Collections.Generic;
using UnityEngine;

namespace MazeEscape
{
    public static class MazeGenerator
    {
        private static readonly Vector2Int[] Directions = {
            Vector2Int.up,    // North
            Vector2Int.down,  // South
            Vector2Int.right, // East
            Vector2Int.left   // West
        };

        public static MazeCell[,] Generate(int width, int height, int seed = -1)
        {
            if (seed >= 0) Random.InitState(seed);

            var cells = new MazeCell[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    cells[x, y] = MazeCell.Default;

            var stack = new Stack<Vector2Int>();
            var start = Vector2Int.zero;
            cells[start.x, start.y].Visited = true;
            stack.Push(start);

            while (stack.Count > 0)
            {
                var current = stack.Peek();
                var neighbors = GetUnvisitedNeighbors(cells, current, width, height);

                if (neighbors.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                var next = neighbors[Random.Range(0, neighbors.Count)];
                RemoveWall(ref cells[current.x, current.y], ref cells[next.x, next.y], next - current);
                cells[next.x, next.y].Visited = true;
                stack.Push(next);
            }

            return cells;
        }

        private static List<Vector2Int> GetUnvisitedNeighbors(MazeCell[,] cells, Vector2Int pos, int width, int height)
        {
            var result = new List<Vector2Int>(4);
            foreach (var dir in Directions)
            {
                var neighbor = pos + dir;
                if (neighbor.x >= 0 && neighbor.x < width &&
                    neighbor.y >= 0 && neighbor.y < height &&
                    !cells[neighbor.x, neighbor.y].Visited)
                {
                    result.Add(neighbor);
                }
            }
            return result;
        }

        private static void RemoveWall(ref MazeCell from, ref MazeCell to, Vector2Int direction)
        {
            if (direction == Vector2Int.up)    { from.WallNorth = false; to.WallSouth = false; }
            else if (direction == Vector2Int.down)  { from.WallSouth = false; to.WallNorth = false; }
            else if (direction == Vector2Int.right) { from.WallEast  = false; to.WallWest  = false; }
            else if (direction == Vector2Int.left)  { from.WallWest  = false; to.WallEast  = false; }
        }
    }
}
