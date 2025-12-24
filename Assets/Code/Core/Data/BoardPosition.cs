using System;
using UnityEngine;

namespace WallChess.Core.Data
{
    /// <summary>
    /// Immutable board position. Single representation for all grid coordinates.
    /// </summary>
    [Serializable]
    public readonly struct BoardPosition : IEquatable<BoardPosition>
    {
        public readonly int X;
        public readonly int Y;

        public BoardPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        // Conversion for Unity compatibility
        public static implicit operator Vector2Int(BoardPosition pos) => new(pos.X, pos.Y);
        public static implicit operator BoardPosition(Vector2Int v) => new(v.x, v.y);

        // Directional helpers
        public BoardPosition North => new(X, Y + 1);
        public BoardPosition South => new(X, Y - 1);
        public BoardPosition East => new(X + 1, Y);
        public BoardPosition West => new(X - 1, Y);

        public BoardPosition Offset(int dx, int dy) => new(X + dx, Y + dy);

        public int ManhattanDistance(BoardPosition other) =>
            Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        public bool IsAdjacent(BoardPosition other) => ManhattanDistance(other) == 1;

        public bool IsWithinBounds(int gridSize) =>
            X >= 0 && X < gridSize && Y >= 0 && Y < gridSize;

        public bool IsWithinBounds(int boardWidth, int boardHeight) =>
            X >= 0 && X < boardWidth && Y >= 0 && Y < boardHeight;

        // Equality
        public bool Equals(BoardPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is BoardPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(BoardPosition a, BoardPosition b) => a.Equals(b);
        public static bool operator !=(BoardPosition a, BoardPosition b) => !a.Equals(b);
        public override string ToString() => $"({X},{Y})";
    }
}
