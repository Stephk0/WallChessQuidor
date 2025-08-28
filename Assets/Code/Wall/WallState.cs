using UnityEngine;
using System;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Holds occupancy arrays and exposes helpers for gaps & walls.
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

        private bool[,] h; // [HCols,HRows]
        private bool[,] v; // [VCols,VRows]

        private readonly List<GameObject> managed = new List<GameObject>(32);
        public IReadOnlyList<GameObject> ManagedWalls => managed;

        public WallState(Func<int> getCells, float spacing)
        {
            this.getCells = getCells;
            this.spacing = spacing;
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
            if (o == Orientation.Horizontal)
                return new Vector3((x + hOffX) * spacing, (y + hOffY) * spacing, 0f);
            else
                return new Vector3((x + vOffX) * spacing, (y + vOffY) * spacing, 0f);
        }

        public (int maxHX, int maxVY) MaxExtentsWithinBoard()
        {
            // ensure two-cell span stays within board
            int maxHX = Math.Min(HCols - 2, Cells - 2);
            int maxVY = Math.Min(VRows - 2, Cells - 2);
            return (maxHX, maxVY);
        }

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
