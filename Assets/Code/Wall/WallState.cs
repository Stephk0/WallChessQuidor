using UnityEngine;
using System;
using System.Collections.Generic;
using WallChess.Grid;

namespace WallChess
{
    /// <summary>
    /// Holds occupancy arrays and exposes helpers for gaps & walls.
    /// Now uses GridCoordinateConverter for proper alignment handling.
    /// </summary>
    public class WallState
    {
        public enum Orientation { Horizontal, Vertical }

        private readonly Func<int> getCells;
        public int Cells => getCells();
        public int HCols => Cells + 1;
        public int HRows => Cells - 1;
        public int VCols => Cells - 1;
        public int VRows => Cells + 1;

        public readonly float spacing;
        public readonly float hOffX = 0.5f, hOffY = 0.5f, vOffX = 0.5f, vOffY = 0.5f;
        
        private readonly GridCoordinateConverter coordinateConverter;

        private bool[,] h; // [HCols,HRows]
        private bool[,] v; // [VCols,VRows]

        private readonly List<GameObject> managed = new List<GameObject>(32);
        public IReadOnlyList<GameObject> ManagedWalls => managed;

        public WallState(Func<int> getCells, float spacing, GridCoordinateConverter coordinateConverter = null)
        {
            this.getCells = getCells;
            this.spacing = spacing;
            this.coordinateConverter = coordinateConverter;
            Allocate();
        }

        public void Allocate()
        {
            h = new bool[HCols, HRows];
            v = new bool[VCols, VRows];
            ClearGaps();
        }

        public void ClearGaps()
        {
            for (int x = 0; x < HCols; x++) for (int y = 0; y < HRows; y++) h[x, y] = false;
            for (int x = 0; x < VCols; x++) for (int y = 0; y < VRows; y++) v[x, y] = false;
        }

        public void ClearAll(GridSystem grid, WallChessGameManager gm)
        {
            // remove visuals
            for (int i = managed.Count - 1; i >= 0; i--)
            {
                var go = managed[i];
                if (go != null) SafeDestroy(go);
            }
            managed.Clear();
            // reset data
            ClearGaps();
            // reset pawns
            if (gm != null)
            {
                foreach (var pawn in gm.pawns) pawn.wallsRemaining = gm.wallsPerPlayer;
            }
            // sync grid
            grid?.ClearGrid();
        }

        public void AddManaged(GameObject go) => managed.Add(go);

        public bool IsOccupied(Orientation o, int x, int y)
        {
            if (o == Orientation.Horizontal)
            {
                if (x < 0 || x >= HCols || y < 0 || y >= HRows) return true;
                return h[x, y];
            }
            else
            {
                if (x < 0 || x >= VCols || y < 0 || y >= VRows) return true;
                return v[x, y];
            }
        }

        public void SetOccupied(Orientation o, int x, int y, bool value)
        {
            if (o == Orientation.Horizontal)
            {
                if (x >= 0 && x < HCols && y >= 0 && y < HRows) h[x, y] = value;
            }
            else
            {
                if (x >= 0 && x < VCols && y >= 0 && y < VRows) v[x, y] = value;
            }
        }

        public Vector3 GapCenter(Orientation o, int x, int y)
        {
            Vector3 basePos;
            
            if (o == Orientation.Horizontal)
                basePos = new Vector3((x + hOffX) * spacing, (y + hOffY) * spacing, 0f);
            else
                basePos = new Vector3((x + vOffX) * spacing, (y + vOffY) * spacing, 0f);

            // Apply alignment offset if coordinate converter is available
            if (coordinateConverter != null)
            {
                Vector3 alignmentOffset = coordinateConverter.GetAlignmentOffset();
                return basePos + alignmentOffset;
            }

            return basePos;
        }

        public (int maxHX, int maxVY) MaxExtentsWithinBoard()
        {
            // ensure two-cell span stays within board
            int maxHX = Math.Min(HCols - 2, Cells - 2);
            int maxVY = Math.Min(VRows - 2, Cells - 2);
            return (maxHX, maxVY);
        }

        /// <summary>
        /// Converts world position to gap coordinates, accounting for grid alignment
        /// </summary>
        public Vector2Int WorldToGapPosition(Vector3 worldPos, Orientation orientation)
        {
            if (coordinateConverter != null)
            {
                // Remove alignment offset first
                Vector3 alignmentOffset = coordinateConverter.GetAlignmentOffset();
                worldPos -= alignmentOffset;
            }

            if (orientation == Orientation.Horizontal)
            {
                float fx = worldPos.x / spacing - hOffX;
                float fy = worldPos.y / spacing - hOffY;
                return new Vector2Int(Mathf.RoundToInt(fx), Mathf.RoundToInt(fy));
            }
            else
            {
                float fx = worldPos.x / spacing - vOffX;
                float fy = worldPos.y / spacing - vOffY;
                return new Vector2Int(Mathf.RoundToInt(fx), Mathf.RoundToInt(fy));
            }
        }

        /// <summary>
        /// Gets the coordinate converter for grid alignment calculations
        /// </summary>
        public GridCoordinateConverter GetCoordinateConverter() => coordinateConverter;

        public static void SafeDestroy(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEngine.Object.DestroyImmediate(obj);
            else UnityEngine.Object.Destroy(obj);
#else
            UnityEngine.Object.Destroy(obj);
#endif
        }
    }
}