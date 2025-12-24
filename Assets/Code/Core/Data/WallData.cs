using System;
using System.Collections.Generic;

namespace WallChess.Core.Data
{
    /// <summary>
    /// Wall orientation - horizontal blocks vertical movement, vertical blocks horizontal.
    /// </summary>
    public enum WallOrientation : byte
    {
        Horizontal = 0, // Blocks North/South movement
        Vertical = 1    // Blocks East/West movement
    }

    /// <summary>
    /// Immutable wall placement. Position is the bottom-left corner of the 2-cell wall.
    /// </summary>
    [Serializable]
    public readonly struct WallPlacement : IEquatable<WallPlacement>
    {
        public readonly BoardPosition Position;
        public readonly WallOrientation Orientation;
        public readonly int PlacedByPlayerIndex;
        public readonly int TurnPlaced;

        public WallPlacement(BoardPosition position, WallOrientation orientation, int playerIndex = -1, int turn = -1)
        {
            Position = position;
            Orientation = orientation;
            PlacedByPlayerIndex = playerIndex;
            TurnPlaced = turn;
        }

        /// <summary>
        /// Returns the two gap positions this wall occupies.
        /// Quoridor walls span 2 cells.
        /// </summary>
        public (BoardPosition first, BoardPosition second) GetOccupiedGaps()
        {
            if (Orientation == WallOrientation.Horizontal)
                return (Position, Position.Offset(1, 0));
            else
                return (Position, Position.Offset(0, 1));
        }

        /// <summary>
        /// Returns the center intersection point of the wall.
        /// </summary>
        public BoardPosition GetCenterIntersection() => Position;

        public bool Equals(WallPlacement other) =>
            Position == other.Position && Orientation == other.Orientation;
        public override bool Equals(object obj) => obj is WallPlacement other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Position, Orientation);
        public static bool operator ==(WallPlacement a, WallPlacement b) => a.Equals(b);
        public static bool operator !=(WallPlacement a, WallPlacement b) => !a.Equals(b);
        public override string ToString() => $"Wall({Orientation}@{Position})";
    }

    /// <summary>
    /// Complete wall state for the board. Tracks all placed walls and occupied gaps.
    /// </summary>
    [Serializable]
    public class WallState
    {
        private readonly List<WallPlacement> _placedWalls = new(20);
        private readonly HashSet<(BoardPosition, WallOrientation)> _occupiedGaps = new();
        private readonly HashSet<BoardPosition> _occupiedIntersections = new();

        public IReadOnlyList<WallPlacement> PlacedWalls => _placedWalls;
        public int WallCount => _placedWalls.Count;

        public bool IsGapOccupied(BoardPosition pos, WallOrientation orientation) =>
            _occupiedGaps.Contains((pos, orientation));

        public bool IsIntersectionOccupied(BoardPosition pos) =>
            _occupiedIntersections.Contains(pos);

        /// <summary>
        /// Checks if movement between two adjacent positions is blocked by a wall.
        /// </summary>
        public bool IsMovementBlocked(BoardPosition from, BoardPosition to)
        {
            if (!from.IsAdjacent(to)) return true;

            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            if (dy != 0) // Vertical movement, check horizontal walls
            {
                int wallY = dy > 0 ? from.Y : to.Y;
                // Check both possible wall positions that could block this movement
                var gap1 = new BoardPosition(from.X, wallY);
                var gap2 = new BoardPosition(from.X - 1, wallY);

                if (_occupiedGaps.Contains((gap1, WallOrientation.Horizontal)) ||
                    _occupiedGaps.Contains((gap2, WallOrientation.Horizontal)))
                    return true;
            }
            else // Horizontal movement, check vertical walls
            {
                int wallX = dx > 0 ? from.X : to.X;
                var gap1 = new BoardPosition(wallX, from.Y);
                var gap2 = new BoardPosition(wallX, from.Y - 1);

                if (_occupiedGaps.Contains((gap1, WallOrientation.Vertical)) ||
                    _occupiedGaps.Contains((gap2, WallOrientation.Vertical)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Places a wall. Returns false if placement conflicts with existing walls.
        /// </summary>
        public bool TryPlaceWall(WallPlacement wall)
        {
            var (gap1, gap2) = wall.GetOccupiedGaps();
            var intersection = wall.GetCenterIntersection();

            // Check if gaps or intersection already occupied
            if (IsGapOccupied(gap1, wall.Orientation) ||
                IsGapOccupied(gap2, wall.Orientation) ||
                IsIntersectionOccupied(intersection))
                return false;

            _placedWalls.Add(wall);
            _occupiedGaps.Add((gap1, wall.Orientation));
            _occupiedGaps.Add((gap2, wall.Orientation));
            _occupiedIntersections.Add(intersection);
            return true;
        }

        /// <summary>
        /// Removes the last placed wall. For undo functionality.
        /// </summary>
        public bool TryRemoveLastWall()
        {
            if (_placedWalls.Count == 0) return false;

            var wall = _placedWalls[^1];
            var (gap1, gap2) = wall.GetOccupiedGaps();
            var intersection = wall.GetCenterIntersection();

            _placedWalls.RemoveAt(_placedWalls.Count - 1);
            _occupiedGaps.Remove((gap1, wall.Orientation));
            _occupiedGaps.Remove((gap2, wall.Orientation));
            _occupiedIntersections.Remove(intersection);
            return true;
        }

        public void Clear()
        {
            _placedWalls.Clear();
            _occupiedGaps.Clear();
            _occupiedIntersections.Clear();
        }

        /// <summary>
        /// Creates a deep copy for AI simulation / undo.
        /// </summary>
        public WallState Clone()
        {
            var clone = new WallState();
            foreach (var wall in _placedWalls)
                clone.TryPlaceWall(wall);
            return clone;
        }
    }
}
