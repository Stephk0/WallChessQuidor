using UnityEngine;
using System.Collections.Generic;

namespace WallChess.Grid
{
    public class GridWallManager
    {
        private readonly GridCoordinateConverter _coordinateConverter;
        private readonly GridSystem.GridSettings _gridSettings;
        private readonly GridSystem.GapConfiguration _gapConfig;
        
        private bool[,] _horizontalGaps;
        private bool[,] _verticalGaps;
        private List<GridSystem.WallInfo> _placedWalls;
        private HashSet<Vector2Int> _wallIntersections;
        
        private int HorizontalGapCols => _coordinateConverter.GetGridSize() + 1;
        private int HorizontalGapRows => _coordinateConverter.GetGridSize();
        private int VerticalGapCols => _coordinateConverter.GetGridSize();
        private int VerticalGapRows => _coordinateConverter.GetGridSize() + 1;

        public System.Action<GridSystem.WallInfo> OnWallPlaced;

        public GridWallManager(GridCoordinateConverter coordinateConverter, 
            GridSystem.GridSettings gridSettings, GridSystem.GapConfiguration gapConfig)
        {
            _coordinateConverter = coordinateConverter;
            _gridSettings = gridSettings;
            _gapConfig = gapConfig;
            
            InitializeArrays();
        }

        private void InitializeArrays()
        {
            _horizontalGaps = new bool[HorizontalGapCols, HorizontalGapRows];
            _verticalGaps = new bool[VerticalGapCols, VerticalGapRows];
            _placedWalls = new List<GridSystem.WallInfo>();
            _wallIntersections = new HashSet<Vector2Int>();
            
            ClearArrays();
        }

        private void ClearArrays()
        {
            for (int x = 0; x < HorizontalGapCols; x++)
                for (int y = 0; y < HorizontalGapRows; y++)
                    _horizontalGaps[x, y] = false;

            for (int x = 0; x < VerticalGapCols; x++)
                for (int y = 0; y < VerticalGapRows; y++)
                    _verticalGaps[x, y] = false;

            _placedWalls.Clear();
            _wallIntersections.Clear();
        }

        public bool IsGapOccupied(GridSystem.Orientation orientation, int x, int y)
        {
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                if (x < 0 || x >= HorizontalGapCols || y < 0 || y >= HorizontalGapRows) return true;
                return _horizontalGaps[x, y];
            }
            else
            {
                if (x < 0 || x >= VerticalGapCols || y < 0 || y >= VerticalGapRows) return true;
                return _verticalGaps[x, y];
            }
        }

        public void SetGapOccupied(GridSystem.Orientation orientation, int x, int y, bool occupied)
        {
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                if (x >= 0 && x < HorizontalGapCols && y >= 0 && y < HorizontalGapRows)
                    _horizontalGaps[x, y] = occupied;
            }
            else
            {
                if (x >= 0 && x < VerticalGapCols && y >= 0 && y < VerticalGapRows)
                    _verticalGaps[x, y] = occupied;
            }
        }

        public bool CanPlaceWall(GridSystem.Orientation orientation, int x, int y)
        {
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                if (x < 0 || x + 1 >= HorizontalGapCols || y < 0 || y >= HorizontalGapRows) return false;
                if (IsGapOccupied(GridSystem.Orientation.Horizontal, x, y)) return false;
                if (IsGapOccupied(GridSystem.Orientation.Horizontal, x + 1, y)) return false;
            }
            else
            {
                if (x < 0 || x >= VerticalGapCols || y < 0 || y + 1 >= VerticalGapRows) return false;
                if (IsGapOccupied(GridSystem.Orientation.Vertical, x, y)) return false;
                if (IsGapOccupied(GridSystem.Orientation.Vertical, x, y + 1)) return false;
            }

            return true;
        }

        public bool PlaceWall(GridSystem.WallInfo wallInfo)
        {
            if (!CanPlaceWall(wallInfo.orientation, wallInfo.x, wallInfo.y)) return false;

            if (wallInfo.orientation == GridSystem.Orientation.Horizontal)
            {
                SetGapOccupied(GridSystem.Orientation.Horizontal, wallInfo.x, wallInfo.y, true);
                SetGapOccupied(GridSystem.Orientation.Horizontal, wallInfo.x + 1, wallInfo.y, true);
            }
            else
            {
                SetGapOccupied(GridSystem.Orientation.Vertical, wallInfo.x, wallInfo.y, true);
                SetGapOccupied(GridSystem.Orientation.Vertical, wallInfo.x, wallInfo.y + 1, true);
            }

            _placedWalls.Add(wallInfo);
            OnWallPlaced?.Invoke(wallInfo);
            return true;
        }

        public bool IsMovementBlockedByWalls(Vector2Int from, Vector2Int to)
        {
            Vector2Int diff = to - from;
            
            if (diff.y == 1)
            {
                int gapX = from.x;
                int gapY = from.y;
                return IsHorizontalWallBlocking(gapX, gapY);
            }
            else if (diff.y == -1)
            {
                int gapX = from.x;
                int gapY = to.y;
                return IsHorizontalWallBlocking(gapX, gapY);
            }
            else if (diff.x == 1)
            {
                int gapX = from.x;
                int gapY = from.y;
                return IsVerticalWallBlocking(gapX, gapY);
            }
            else if (diff.x == -1)
            {
                int gapX = to.x;
                int gapY = from.y;
                return IsVerticalWallBlocking(gapX, gapY);
            }
            
            return false;
        }

        private bool IsHorizontalWallBlocking(int gapX, int gapY)
        {
            if (gapX < 0 || gapY < 0) return false;
            
            bool wall1Left = IsGapOccupied(GridSystem.Orientation.Horizontal, gapX, gapY);
            bool wall1Right = IsGapOccupied(GridSystem.Orientation.Horizontal, gapX + 1, gapY);
            if (wall1Left && wall1Right)
                return true;
            
            if (gapX - 1 >= 0)
            {
                bool wall2Left = IsGapOccupied(GridSystem.Orientation.Horizontal, gapX - 1, gapY);
                bool wall2Right = IsGapOccupied(GridSystem.Orientation.Horizontal, gapX, gapY);
                if (wall2Left && wall2Right)
                    return true;
            }
            
            return false;
        }

        private bool IsVerticalWallBlocking(int gapX, int gapY)
        {
            if (gapX < 0 || gapY < 0) return false;
            
            bool wall1Bottom = IsGapOccupied(GridSystem.Orientation.Vertical, gapX, gapY);
            bool wall1Top = IsGapOccupied(GridSystem.Orientation.Vertical, gapX, gapY + 1);
            if (wall1Bottom && wall1Top)
                return true;
            
            if (gapY - 1 >= 0)
            {
                bool wall2Bottom = IsGapOccupied(GridSystem.Orientation.Vertical, gapX, gapY - 1);
                bool wall2Top = IsGapOccupied(GridSystem.Orientation.Vertical, gapX, gapY);
                if (wall2Bottom && wall2Top)
                    return true;
            }
            
            return false;
        }

        public void ClearWalls()
        {
            ClearArrays();
        }
    }
}