using UnityEngine;
using WallChess.Core.Data;

namespace WallChess.Input
{
    /// <summary>
    /// Phase of input gesture.
    /// </summary>
    public enum InputPhase
    {
        Begin,
        Move,
        End,
        Cancel
    }

    /// <summary>
    /// Type of object hit by input raycast.
    /// </summary>
    public enum HitType
    {
        None,
        Tile,
        Pawn,
        WallGap,
        UI
    }

    /// <summary>
    /// Normalized input data for touch/mouse handling.
    /// </summary>
    public struct InputData
    {
        public Vector2 ScreenPosition;
        public Vector3 WorldPosition;
        public BoardPosition GridPosition;
        public InputPhase Phase;
        public HitType HitType;
        public int HitPlayerIndex;  // If hit a pawn
        public bool IsValid;

        public static InputData Invalid => new InputData { IsValid = false };

        public override string ToString()
        {
            return $"Input({Phase}, {HitType}, Grid:{GridPosition}, Valid:{IsValid})";
        }
    }
}
