using UnityEngine;

namespace WallChess
{
    public partial class WallManager
    {
        public readonly struct WallInfo
        {
            public readonly Orientation orientation;
            public readonly int x;        // grid index
            public readonly int y;        // grid index
            public readonly Vector3 position;
            public readonly Vector3 scale;

            public WallInfo(Orientation o, int x, int y, Vector3 pos, Vector3 scale)
            {
                this.orientation = o;
                this.x = x;
                this.y = y;
                this.position = pos;
                this.scale = scale;
            }
            
            public string GetGapKey()
            {
                return $"{(orientation == Orientation.Horizontal ? "H" : "V")}_{x}_{y}";
            }
        }
    }
}