using UnityEngine;
using WallChess.Grid;

namespace WallChess
{
    /// <summary>
    /// Computes nearest valid gap and handles lane-based orientation locking.
    /// Now properly handles grid alignment through GridCoordinateConverter.
    /// </summary>
    public class GapDetector
    {
        private readonly WallState s;
        private readonly GridCoordinateConverter coordinateConverter;
        private readonly float laneSnap;
        private readonly float unlockMul;
        private readonly float margin;
        private WallState.Orientation? lockOrient = null;

        public GapDetector(WallState state, float laneSnapMargin, float unlockMultiplier, float tieMargin)
        {
            s = state;
            this.coordinateConverter = state.GetCoordinateConverter();
            laneSnap = laneSnapMargin;
            unlockMul = unlockMultiplier;
            margin = tieMargin;
        }

        float NearestHLane(float y)
        {
            Vector3 alignmentOffset = coordinateConverter?.GetAlignmentOffset() ?? Vector3.zero;
            float adjustedY = y - alignmentOffset.y;
            float laneY = (Mathf.Round(adjustedY / s.spacing - s.hOffY) + s.hOffY) * s.spacing;
            return laneY + alignmentOffset.y;
        }

        float NearestVLane(float x)
        {
            Vector3 alignmentOffset = coordinateConverter?.GetAlignmentOffset() ?? Vector3.zero;
            float adjustedX = x - alignmentOffset.x;
            float laneX = (Mathf.Round(adjustedX / s.spacing - s.vOffX) + s.vOffX) * s.spacing;
            return laneX + alignmentOffset.x;
        }

        public struct WallInfo
        {
            public WallState.Orientation orientation;
            public int x, y;
            public Vector3 pos, scale;
        }

        public bool TryFind(Vector3 world, WallChessGameManager gm, out WallInfo result)
        {
            // determine stripes
            float yLane = NearestHLane(world.y);
            float xLane = NearestVLane(world.x);
            float dY = Mathf.Abs(world.y - yLane);
            float dX = Mathf.Abs(world.x - xLane);

            bool inH = dY <= laneSnap;
            bool inV = dX <= laneSnap;

            if (lockOrient.HasValue)
            {
                if (lockOrient.Value == WallState.Orientation.Horizontal)
                {
                    if (dY <= laneSnap * unlockMul) { inH = true; inV = false; } else lockOrient = null;
                }
                else
                {
                    if (dX <= laneSnap * unlockMul) { inV = true; inH = false; } else lockOrient = null;
                }
            }
            else if (!inH && !inV) { inH = inV = true; }

            // candidates
            Candidate best = default;
            best.dist2 = float.PositiveInfinity;

            TryCandidates(world, WallState.Orientation.Horizontal, inH, ref best);
            TryCandidates(world, WallState.Orientation.Vertical, inV, ref best);

            if (best.found)
            {
                result = MakeInfo(best.o, best.x, best.y, gm);
                if (best.o == WallState.Orientation.Horizontal && dY <= laneSnap) lockOrient = best.o;
                if (best.o == WallState.Orientation.Vertical && dX <= laneSnap) lockOrient = best.o;
                return true;
            }

            result = default;
            return false;
        }

        struct Candidate { public bool found; public float dist2; public WallState.Orientation o; public int x, y; }
        void TryCandidates(Vector3 w, WallState.Orientation o, bool enabled, ref Candidate best)
        {
            if (!enabled) return;
            Index(w, o, true, out int aX, out int aY);
            Index(w, o, false, out int bX, out int bY);

            if (Clamp(ref aX, ref aY, o)) Eval(w, o, aX, aY, ref best);
            if (Clamp(ref bX, ref bY, o)) Eval(w, o, bX, bY, ref best);
        }

        void Eval(Vector3 w, WallState.Orientation o, int x, int y, ref Candidate best)
        {
            Vector3 c = s.GapCenter(o, x, y);
            float d2 = (w - c).sqrMagnitude;

            if (best.found && Mathf.Abs(d2 - best.dist2) < margin * margin)
            {
                // prefer not to flip orientation when tie
                if (best.o == WallState.Orientation.Vertical && o == WallState.Orientation.Horizontal) return;
            }
            if (d2 < best.dist2)
            {
                best.found = true; best.dist2 = d2; best.o = o; best.x = x; best.y = y;
            }
        }

        void Index(Vector3 p, WallState.Orientation o, bool floor, out int gx, out int gy)
        {
            // Account for alignment offset
            Vector3 alignmentOffset = coordinateConverter?.GetAlignmentOffset() ?? Vector3.zero;
            Vector3 adjustedPos = p - alignmentOffset;

            if (o == WallState.Orientation.Horizontal)
            {
                float fx = adjustedPos.x / s.spacing - s.hOffX;
                float fy = adjustedPos.y / s.spacing - s.hOffY;
                gx = floor ? Mathf.FloorToInt(fx) : Mathf.RoundToInt(fx);
                gy = floor ? Mathf.FloorToInt(fy) : Mathf.RoundToInt(fy);
            }
            else
            {
                float fx = adjustedPos.x / s.spacing - s.vOffX;
                float fy = adjustedPos.y / s.spacing - s.vOffY;
                gx = floor ? Mathf.FloorToInt(fx) : Mathf.RoundToInt(fx);
                gy = floor ? Mathf.FloorToInt(fy) : Mathf.RoundToInt(fy);
            }
        }

        bool Clamp(ref int x, ref int y, WallState.Orientation o)
        {
            if (o == WallState.Orientation.Horizontal)
            {
                int maxX = Mathf.Min(s.HCols - 2, s.Cells - 2);
                x = Mathf.Clamp(x, 0, maxX);
                y = Mathf.Clamp(y, 0, s.HRows - 1);
                return x >= 0 && x + 1 < s.HCols && y >= 0 && y < s.HRows && x + 1 <= s.Cells - 1;
            }
            else
            {
                int maxY = Mathf.Min(s.VRows - 2, s.Cells - 2);
                y = Mathf.Clamp(y, 0, maxY);
                x = Mathf.Clamp(x, 0, s.VCols - 1);
                return x >= 0 && x < s.VCols && y >= 0 && y + 1 < s.VRows && y + 1 <= s.Cells - 1;
            }
        }

        WallInfo MakeInfo(WallState.Orientation o, int x, int y, WallChessGameManager gm)
        {
            Vector3 c = s.GapCenter(o, x, y);
            Vector3 scale = (o == WallState.Orientation.Horizontal)
                ? new Vector3(gm.tileSize * 2f + gm.tileGap, gm.wallThickness, gm.wallHeight)
                : new Vector3(gm.wallThickness, gm.tileSize * 2f + gm.tileGap, gm.wallHeight);
            return new WallInfo { orientation = o, x = x, y = y, pos = new Vector3(c.x, c.y, -0.1f), scale = scale };
        }
    }
}
