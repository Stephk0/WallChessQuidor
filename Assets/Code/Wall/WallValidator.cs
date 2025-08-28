using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Validates wall placements and path constraints.
    /// </summary>
    public class WallValidator
    {
        private readonly WallState s;
        private readonly WallChessGameManager gm;

        public WallValidator(WallState state, WallChessGameManager gameManager)
        {
            s = state; gm = gameManager;
        }

        public bool CanPlace(GapDetector.WallInfo w)
        {
            if (gm == null) return false;
            if (!gm.CanPlaceWalls()) return false;
            if (!gm.CurrentPlayerHasWalls()) return false;

            if (w.orientation == WallState.Orientation.Horizontal)
            {
                if (w.x < 0 || w.x + 1 >= s.HCols || w.y < 0 || w.y >= s.HRows) return false;
                if (s.IsOccupied(WallState.Orientation.Horizontal, w.x, w.y)) return false;
                if (s.IsOccupied(WallState.Orientation.Horizontal, w.x + 1, w.y)) return false;
            }
            else
            {
                if (w.x < 0 || w.x >= s.VCols || w.y < 0 || w.y + 1 >= s.VRows) return false;
                if (s.IsOccupied(WallState.Orientation.Vertical, w.x, w.y)) return false;
                if (s.IsOccupied(WallState.Orientation.Vertical, w.x, w.y + 1)) return false;
            }

            if (WouldCross(w)) return false;
            if (WouldBlockPaths(w)) return false;

            return true;
        }

        bool WouldCross(GapDetector.WallInfo w)
        {
            int x = w.x; int y = w.y;
            if (w.orientation == WallState.Orientation.Horizontal)
            {
                if (y + 1 >= s.VRows) return false;
                return s.IsOccupied(WallState.Orientation.Vertical, x, y) &&
                       s.IsOccupied(WallState.Orientation.Vertical, x, y + 1);
            }
            else
            {
                if (x + 1 >= s.HCols) return false;
                return s.IsOccupied(WallState.Orientation.Horizontal, x, y) &&
                       s.IsOccupied(WallState.Orientation.Horizontal, x + 1, y);
            }
        }

        bool WouldBlockPaths(GapDetector.WallInfo w)
        {
            // temp occupy
            Occupy(w, true);
            bool a = HasPathToGoal(gm.playerPosition, true);
            bool b = HasPathToGoal(gm.opponentPosition, false);
            Occupy(w, false);
            return !(a && b);
        }

        void Occupy(GapDetector.WallInfo w, bool on)
        {
            if (w.orientation == WallState.Orientation.Horizontal)
            {
                s.SetOccupied(WallState.Orientation.Horizontal, w.x, w.y, on);
                s.SetOccupied(WallState.Orientation.Horizontal, w.x + 1, w.y, on);
            }
            else
            {
                s.SetOccupied(WallState.Orientation.Vertical, w.x, w.y, on);
                s.SetOccupied(WallState.Orientation.Vertical, w.x, w.y + 1, on);
            }
        }

        bool HasPathToGoal(Vector2Int start, bool isPlayer)
        {
            int goalY = isPlayer ? gm.gridSize - 1 : 0;
            var q = new Queue<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            q.Enqueue(start); seen.Add(start);
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur.y == goalY) return true;
                foreach (var d in dirs)
                {
                    var next = cur + d;
                    if (next.x < 0 || next.x >= gm.gridSize || next.y < 0 || next.y >= gm.gridSize) continue;
                    if (seen.Contains(next)) continue;
                    if (IsBlocked(cur, next)) continue;
                    q.Enqueue(next); seen.Add(next);
                }
            }
            return false;
        }

        bool IsBlocked(Vector2Int from, Vector2Int to)
        {
            var diff = to - from;
            if (diff.y == 1) return IsHBlocking(from.x, from.y);        // up
            if (diff.y == -1) return IsHBlocking(from.x, to.y);          // down
            if (diff.x == 1) return IsVBlocking(from.x, from.y);         // right
            if (diff.x == -1) return IsVBlocking(to.x, from.y);          // left
            return false;
        }

        bool IsHBlocking(int gapX, int gapY)
        {
            if (gapX < 0 || gapY < 0 || gapY >= s.HRows) return false;
            if (gapX + 1 < s.HCols &&
                s.IsOccupied(WallState.Orientation.Horizontal, gapX, gapY) &&
                s.IsOccupied(WallState.Orientation.Horizontal, gapX + 1, gapY)) return true;
            if (gapX - 1 >= 0 &&
                s.IsOccupied(WallState.Orientation.Horizontal, gapX - 1, gapY) &&
                s.IsOccupied(WallState.Orientation.Horizontal, gapX, gapY)) return true;
            return false;
        }

        bool IsVBlocking(int gapX, int gapY)
        {
            if (gapX < 0 || gapY < 0 || gapX >= s.VCols) return false;
            if (gapY + 1 < s.VRows &&
                s.IsOccupied(WallState.Orientation.Vertical, gapX, gapY) &&
                s.IsOccupied(WallState.Orientation.Vertical, gapX, gapY + 1)) return true;
            if (gapY - 1 >= 0 &&
                s.IsOccupied(WallState.Orientation.Vertical, gapX, gapY - 1) &&
                s.IsOccupied(WallState.Orientation.Vertical, gapX, gapY)) return true;
            return false;
        }
    }
}
