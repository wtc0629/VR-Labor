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

        // Stellschrauben für „mehr Verzweigungen“
        private const float JitterResumeProbability = 0.12f; // Chance, nicht vom Stack weiterzumachen, sondern „seitlich“ neu zu starten
        private const float TurnBias = 0.25f;                 // Bonus auf Richtungen, die nicht der letzten Bewegung entsprechen

        public static MazeCell[,] Generate(int width, int height, int seed = -1)
        {
            if (seed >= 0) Random.InitState(seed);

            var cells = new MazeCell[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    cells[x, y] = MazeCell.Default;

            var stack = new Stack<Vector2Int>();
            var visitedOrder = new List<Vector2Int>(width * height);

            var start = Vector2Int.zero;
            cells[start.x, start.y].Visited = true;
            stack.Push(start);
            visitedOrder.Add(start);

            // Merkt sich pro Schritt die letzte Bewegungsrichtung (für TurnBias)
            Vector2Int lastDir = Vector2Int.zero;

            while (stack.Count > 0)
            {
                // Mit kleiner Wahrscheinlichkeit „springe“ zu einer anderen besuchten Zelle,
                // die noch unbesuchte Nachbarn hat (erzeugt neue Seitenarme).
                Vector2Int current;
                if (Random.value < JitterResumeProbability)
                {
                    if (TryPickVisitedWithUnvisitedNeighbor(cells, width, height, visitedOrder, out var picked))
                    {
                        current = picked;
                        // lastDir zurücksetzen, damit an der neuen Stelle Richtungen neutral behandelt werden
                        lastDir = Vector2Int.zero;
                    }
                    else
                    {
                        current = stack.Peek();
                    }
                }
                else
                {
                    current = stack.Peek();
                }

                var neighbors = GetUnvisitedNeighbors(cells, current, width, height);
                if (neighbors.Count == 0)
                {
                    // normales DFS-Backtracking
                    if (stack.Count > 0 && current == stack.Peek())
                        stack.Pop();
                    continue;
                }

                // Richte eine zufällige, aber leicht „kurvenfreundliche“ Auswahl der nächsten Richtung ein
                var next = PickNextWithTurnBias(neighbors, current, lastDir);

                RemoveWall(ref cells[current.x, current.y], ref cells[next.x, next.y], next - current);
                cells[next.x, next.y].Visited = true;
                visitedOrder.Add(next);

                // Wir wollen den Pfad von current → next auch im Stack abbilden.
                // Falls current nicht oben liegt (Jitter-Fall), pushen wir current nur, wenn er nicht bereits oben ist.
                if (stack.Count == 0 || stack.Peek() != current)
                    stack.Push(current);

                stack.Push(next);

                lastDir = next - current;
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

        // Wählt den nächsten Nachbarn mit leichter Bevorzugung für Kurven (Richtungswechsel)
        private static Vector2Int PickNextWithTurnBias(List<Vector2Int> candidates, Vector2Int current, Vector2Int lastDir)
        {
            if (candidates.Count == 1 || lastDir == Vector2Int.zero)
                return candidates[Random.Range(0, candidates.Count)];

            // Gewichte berechnen: gleiche Richtung etwas „bestrafen“, Abbiegen etwas „belohnen“
            float total = 0f;
            var weights = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                var dir = candidates[i] - current;
                // Wenn gleiche Richtung wie zuletzt, geringere Gewichtung; sonst Bonus
                float w = (dir == lastDir) ? 1f : 1f + TurnBias;
                weights[i] = w;
                total += w;
            }

            float r = Random.value * total;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (r < weights[i]) return candidates[i];
                r -= weights[i];
            }
            return candidates[candidates.Count - 1];
        }

        // Versucht, eine bereits besuchte Zelle mit noch unbesuchten Nachbarn zu finden
        private static bool TryPickVisitedWithUnvisitedNeighbor(MazeCell[,] cells, int width, int height, List<Vector2Int> visitedOrder, out Vector2Int picked)
        {
            // Einige zufällige Versuche, statt linear alles zu durchsuchen (performancefreundlich)
            const int attempts = 12;
            for (int i = 0; i < attempts; i++)
            {
                var idx = Random.Range(0, visitedOrder.Count);
                var v = visitedOrder[idx];
                var neigh = GetUnvisitedNeighbors(cells, v, width, height);
                if (neigh.Count > 0)
                {
                    picked = v;
                    return true;
                }
            }

            // Fallback: vollständige Suche
            for (int i = 0; i < visitedOrder.Count; i++)
            {
                var v = visitedOrder[i];
                var neigh = GetUnvisitedNeighbors(cells, v, width, height);
                if (neigh.Count > 0)
                {
                    picked = v;
                    return true;
                }
            }

            picked = default;
            return false;
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
