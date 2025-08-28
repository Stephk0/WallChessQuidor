using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using WallChess.Grid;

namespace WallChess
{
    public class GridSystem : MonoBehaviour
    {
        [System.Serializable]
        public struct GridSettings
        {
            public int gridSize;
            public float tileSize;
            public float tileGap;
            public float wallThickness;
            public float wallHeight;
            
            public float TileSpacing => tileSize + tileGap;
        }

        [System.Serializable]
        public struct GapConfiguration
        {
            public float horizontalGapOffsetX;
            public float horizontalGapOffsetY;
            public float verticalGapOffsetX;
            public float verticalGapOffsetY;
        }

        public enum Orientation { Horizontal, Vertical }

        [System.Serializable]
        public struct WallInfo
        {
            public readonly Orientation orientation;
            public readonly int x;
            public readonly int y;
            public readonly Vector3 worldPosition;
            public readonly Vector3 scale;

            public WallInfo(Orientation orientation, int x, int y, Vector3 worldPosition, Vector3 scale)
            {
                this.orientation = orientation;
                this.x = x;
                this.y = y;
                this.worldPosition = worldPosition;
                this.scale = scale;
            }

        }

        #region Settings
        [Header("Grid Configuration")]
        [SerializeField] private GridSettings gridSettings;
        [SerializeField] private GapConfiguration gapConfig;
        
        [Header("Grid Alignment")]
        [SerializeField] private GridAlignment gridAlignment = GridAlignment.Default;
        
        [Header("Tile Visual Settings")]
        [SerializeField] private Material tileMaterial;
        [SerializeField] private Color lightTileColor = Color.white;
        [SerializeField] private Color darkTileColor = Color.gray;
        
        [Header("UI Text Settings")]
        [SerializeField] private bool showTileLabels = true;
        [SerializeField] private Font labelFont;
        [SerializeField] private int labelFontSize = 12;
        [SerializeField] private Color labelColor = Color.black;
        #endregion

        #region Components
        private GridCoordinateConverter coordinateConverter;
        private GridTileManager tileManager;
        private GridWallManager wallManager;
        private GridUIManager uiManager;
        #endregion

        #region Events
        public System.Action<Vector2Int, bool> OnTileOccupancyChanged;
        public System.Action<WallInfo> OnWallPlaced;
        public System.Action OnGridCleared;
        #endregion

        #region Initialization
        public void Initialize(GridSettings settings, GapConfiguration gaps = default)
        {
            gridSettings = settings;
            gapConfig = gaps;
            
            if (gaps.Equals(default(GapConfiguration)))
            {
                gapConfig = new GapConfiguration
                {
                    horizontalGapOffsetX = 0.5f,
                    horizontalGapOffsetY = 0.5f,
                    verticalGapOffsetX = 0.5f,
                    verticalGapOffsetY = 0.5f
                };
            }

            InitializeComponents();
            CreateGrid();
            
            Debug.Log($"GridSystem initialized: {gridSettings.gridSize}x{gridSettings.gridSize}, " +
                     $"spacing={gridSettings.TileSpacing}, alignment={gridAlignment.horizontal}");
        }

        private void InitializeComponents()
        {
            coordinateConverter = new GridCoordinateConverter(
                gridSettings.TileSpacing, 
                gridSettings.gridSize, 
                gridAlignment
            );

            tileManager = new GridTileManager(
                coordinateConverter,
                this.transform,
                gridSettings.tileSize,
                tileMaterial,
                lightTileColor,
                darkTileColor
            );

            wallManager = new GridWallManager(
                coordinateConverter,
                gridSettings,
                gapConfig
            );

            uiManager = new GridUIManager(
                coordinateConverter,
                this.transform,
                showTileLabels,
                labelFont,
                labelFontSize,
                labelColor
            );

            // Subscribe to events
            tileManager.OnTileOccupancyChanged += (pos, occupied) => OnTileOccupancyChanged?.Invoke(pos, occupied);
            wallManager.OnWallPlaced += (wallInfo) => OnWallPlaced?.Invoke(wallInfo);
        }

        private void CreateGrid()
        {
            tileManager.CreateAllTiles();
            uiManager.CreateAllTileLabels();
        }
        #endregion

        #region Public API - Grid Properties
        public GridSettings GetGridSettings() => gridSettings;
        public Vector2Int GetGridDimensions() => new Vector2Int(gridSettings.gridSize, gridSettings.gridSize);
        public float GetTileSpacing() => gridSettings.TileSpacing;
        public int GetGridSize() => gridSettings.gridSize;
        public GridAlignment GetGridAlignment() => gridAlignment;
        #endregion

        #region Public API - Coordinate Conversion
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return coordinateConverter.GridToWorldPosition(gridPos);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            return coordinateConverter.WorldToGridPosition(worldPos);
        }

        public bool IsValidGridPosition(Vector2Int gridPos)
        {
            return coordinateConverter.IsValidGridPosition(gridPos);
        }

        public bool IsWithinGridBounds(Vector3 worldPos)
        {
            return coordinateConverter.IsWithinGridBounds(worldPos);
        }
        #endregion

        #region Public API - Tile Management
        public bool IsTileOccupied(Vector2Int gridPos)
        {
            return tileManager.IsTileOccupied(gridPos);
        }

        public void SetTileOccupied(Vector2Int gridPos, bool occupied)
        {
            tileManager.SetTileOccupied(gridPos, occupied);
            
            if (showTileLabels)
            {
                string labelText = occupied ? $"{gridPos.x},{gridPos.y}\n[OCCUPIED]" : $"{gridPos.x},{gridPos.y}";
                uiManager.UpdateTileLabel(gridPos.x, gridPos.y, labelText);
            }
        }

        public GameObject GetTile(Vector2Int gridPos)
        {
            return tileManager.GetTile(gridPos);
        }

        public Vector3 GetTileCenter(Vector2Int gridPos)
        {
            return tileManager.GetTileCenter(gridPos);
        }

        public List<Vector2Int> GetValidMoves(Vector2Int currentPos)
        {
            List<Vector2Int> adjacentPositions = tileManager.GetValidAdjacentPositions(currentPos);
            List<Vector2Int> validMoves = new List<Vector2Int>();

            foreach (Vector2Int pos in adjacentPositions)
            {
                if (!wallManager.IsMovementBlockedByWalls(currentPos, pos))
                {
                    validMoves.Add(pos);
                }
            }

            return validMoves;
        }
        #endregion

        #region Public API - Wall Management
        public bool IsGapOccupied(Orientation orientation, int x, int y)
        {
            return wallManager.IsGapOccupied(orientation, x, y);
        }

        public bool CanPlaceWall(Orientation orientation, int x, int y)
        {
            return wallManager.CanPlaceWall(orientation, x, y);
        }

        public bool PlaceWall(WallInfo wallInfo)
        {
            return wallManager.PlaceWall(wallInfo);
        }
        #endregion

        #region Public API - UI Management
        public void ToggleTileLabels(bool show)
        {
            showTileLabels = show;
            uiManager.ToggleTileLabels(show);
        }

        public void UpdateTileLabel(int x, int y, string newText)
        {
            uiManager.UpdateTileLabel(x, y, newText);
        }
        #endregion

        #region Public API - Utility
        public void ClearGrid()
        {
            tileManager.ClearOccupancy();
            wallManager.ClearWalls();
            OnGridCleared?.Invoke();
        }

        public void ReconfigureGrid(GridSettings newSettings, GapConfiguration newGaps = default, 
            GridAlignment newAlignment = default)
        {
            // Update alignment if provided
            if (!newAlignment.Equals(default(GridAlignment)))
            {
                gridAlignment = newAlignment;
            }

            // Destroy existing components
            DestroyExistingGrid();

            // Reinitialize with new settings
            Initialize(newSettings, newGaps);
        }

        private void DestroyExistingGrid()
        {
            tileManager?.DestroyAllTiles();
            uiManager?.DestroyAllLabels();
        }
        #endregion

        #region Gizmos
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (gridSettings.gridSize <= 0 || coordinateConverter == null) return;

            float spacing = gridSettings.TileSpacing;
            Vector3 alignmentOffset = coordinateConverter.GetAlignmentOffset();

            // Draw grid bounds with alignment
            Gizmos.color = Color.white;
            Vector3 min = new Vector3(-spacing * 0.5f, -spacing * 0.5f, 0f) + alignmentOffset;
            Vector3 max = new Vector3(gridSettings.gridSize * spacing + spacing * 0.5f,
                                      gridSettings.gridSize * spacing + spacing * 0.5f, 0f) + alignmentOffset;
            Vector3 size = max - min;
            Gizmos.DrawWireCube(min + size * 0.5f, size);

            // Draw alignment indicator
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(alignmentOffset, 0.1f);
        }
#endif
        #endregion
    }
}
