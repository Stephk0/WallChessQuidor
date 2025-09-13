using UnityEngine;

namespace WallChess
{
    /// <summary>
    /// Immutable value struct describing a candidate wall placement.
    /// </summary>
    public readonly struct WallInfo
    {
        public readonly WallState.Orientation orientation;
        public readonly int x;        // grid index
        public readonly int y;        // grid index
        public readonly Vector3 position;
        public readonly Vector3 scale;

        public WallInfo(WallState.Orientation o, int x, int y, Vector3 pos, Vector3 scale)
        {
            this.orientation = o;
            this.x = x;
            this.y = y;
            this.position = pos;
            this.scale = scale;
        }
    }
}
