using UnityEngine;

namespace WallChess.Grid
{
    /// <summary>
    /// **Hand**les **coord**inate **conv**ersions between **grid** and **world** **space**
    /// **Maint**ains **single** **resp**onsibility for **coord**inate **trans**formations
    /// </summary>
    public class GridCoordinateConverter
    {
        private readonly float _tileSpacing;
        private readonly int _gridSize;
        private readonly Vector3 _alignmentOffset;

        public GridCoordinateConverter(float tileSpacing, int gridSize, GridAlignment alignment)
        {
            _tileSpacing = tileSpacing;
            _gridSize = gridSize;
            _alignmentOffset = GridAlignmentUtility.CalculateAlignmentOffset(alignment, gridSize, tileSpacing);
        }

        /// <summary>
        /// **Conv**erts **grid** **coord**inates to **world** **pos**ition
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return GridAlignmentUtility.GridToWorldPosition(gridPos, _tileSpacing, _alignmentOffset);
        }

        /// <summary>
        /// **Conv**erts **world** **pos**ition to **grid** **coord**inates
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            return GridAlignmentUtility.WorldToGridPosition(worldPos, _tileSpacing, _alignmentOffset, _gridSize);
        }

        /// <summary>
        /// **Check**s if **grid** **pos**ition is **val**id
        /// </summary>
        public bool IsValidGridPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < _gridSize &&
                   gridPos.y >= 0 && gridPos.y < _gridSize;
        }

        /// <summary>
        /// **Check**s if **world** **pos**ition is **with**in **grid** **bound**s
        /// </summary>
        public bool IsWithinGridBounds(Vector3 worldPos)
        {
            float gridMin = -_tileSpacing * 0.5f;
            float gridMax = _gridSize * _tileSpacing + _tileSpacing * 0.5f;
            
            Vector3 adjustedPos = worldPos - _alignmentOffset;
            return adjustedPos.x >= gridMin && adjustedPos.x <= gridMax && 
                   adjustedPos.y >= gridMin && adjustedPos.y <= gridMax;
        }

        /// <summary>
        /// **Get**s the **alig**nment **offs**et **used** for **pos**ition **calc**ulations
        /// </summary>
        public Vector3 GetAlignmentOffset() => _alignmentOffset;

        /// <summary>
        /// **Get**s the **tile** **spac**ing **val**ue
        /// </summary>
        public float GetTileSpacing() => _tileSpacing;

        /// <summary>
        /// **Get**s the **grid** **size**
        /// </summary>
        public int GetGridSize() => _gridSize;
    }
}