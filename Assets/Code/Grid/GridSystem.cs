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
        public enum CellType { Tile, HorizontalGap, VerticalGap, Intersection }

        [System.Serializable]
        public class GridCell
        {
            public CellType cellType;
            public bool isOccupied;
            public GameObject visualObject;
            public Vector2Int gridPos;
            
            public bool IsTile => cellType == CellType.Tile;
            public bool IsGap => cellType == CellType.HorizontalGap || cellType == CellType.VerticalGap;
            public bool IsIntersection => cellType == CellType.Intersection;
            
            public GridCell(CellType type, Vector2Int pos)
            {
                cellType = type;
                gridPos = pos;
                isOccupied = false;
                visualObject = null;
            }
        }

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
        private GridUIManager uiManager;
        public GridCell[,] unifiedGrid;
        private int fullGridSize; // Will be gridSize * 2 + 1 to accommodate tiles and gaps
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

            // Full grid includes tiles and gaps: tiles at even indices, gaps at odd
            fullGridSize = gridSettings.gridSize * 2 + 1;
            
            InitializeComponents();
            CreateGrid();
            
            Debug.Log($"GridSystem initialized: {gridSettings.gridSize}x{gridSettings.gridSize} tiles, " +
                     $"unified grid: {fullGridSize}x{fullGridSize}, spacing={gridSettings.TileSpacing}");
        }

        private void InitializeComponents()
        {
            coordinateConverter = new GridCoordinateConverter(
                gridSettings.TileSpacing, 
                gridSettings.gridSize, 
                gridAlignment
            );

            uiManager = new GridUIManager(
                coordinateConverter,
                this.transform,
                showTileLabels,
                labelFont,
                labelFontSize,
                labelColor
            );

            // Initialize unified grid
            InitializeUnifiedGrid();
        }
        
        private void InitializeUnifiedGrid()
        {
            unifiedGrid = new GridCell[fullGridSize, fullGridSize];
            
            for (int x = 0; x < fullGridSize; x++)
            {
                for (int y = 0; y < fullGridSize; y++)
                {
                    CellType cellType = DetermineCellType(x, y);
                    unifiedGrid[x, y] = new GridCell(cellType, new Vector2Int(x, y));
                }
            }
        }
        
        private CellType DetermineCellType(int x, int y)
        {
            bool xIsEven = x % 2 == 0;
            bool yIsEven = y % 2 == 0;
            
            if (xIsEven && yIsEven)
                return CellType.Tile;
            else if (!xIsEven && yIsEven)
                return CellType.VerticalGap;
            else if (xIsEven && !yIsEven)
                return CellType.HorizontalGap;
            else
                return CellType.Intersection;
        }

        private void CreateGrid()
        {
            CreateAllTiles();
            //uiManager.CreateAllTileLabels();
        }
        
        private void CreateAllTiles()
        {
            // Only create tiles up to gridSize count (not fullGridSize)
            // Tiles are at positions 0, 2, 4, 6, 8, 10, 12, 14, 16 for a 9x9 grid
            for (int x = 0; x < gridSettings.gridSize * 2; x += 2)
            {
                for (int y = 0; y < gridSettings.gridSize * 2; y += 2)
                {
                    CreateTileAt(x, y);
                }
            }
        }
        
        private void CreateTileAt(int unifiedX, int unifiedY)
        {
            if (unifiedGrid[unifiedX, unifiedY].cellType != CellType.Tile) return;
            
            // Convert unified grid position to actual tile position
            Vector2Int tilePos = UnifiedToTilePosition(new Vector2Int(unifiedX, unifiedY));
            Vector3 worldPosition = coordinateConverter.GridToWorldPosition(tilePos);
            
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.name = $"Tile_{tilePos.x}_{tilePos.y}";
            tile.transform.position = worldPosition;
            tile.transform.localScale = Vector3.one * gridSettings.tileSize;
            tile.transform.parent = transform;
            
            Renderer tileRenderer = tile.GetComponent<Renderer>();
            
            if (tileMaterial != null)
                tileRenderer.material = tileMaterial;
            else
                tileRenderer.material = new Material(Shader.Find("Sprites/Default"));
            
            tileRenderer.material.color = (tilePos.x + tilePos.y) % 2 == 0 ? 
                lightTileColor : darkTileColor;
            
            if (tile.GetComponent<Collider>() != null)
                DestroyImmediate(tile.GetComponent<Collider>());
            
            unifiedGrid[unifiedX, unifiedY].visualObject = tile;
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
        
        // Convert between tile coordinates (0-8) and unified grid coordinates (0-18)
        public Vector2Int TileToUnifiedPosition(Vector2Int tilePos)
        {
            return new Vector2Int(tilePos.x * 2, tilePos.y * 2);
        }
        
        public Vector2Int UnifiedToTilePosition(Vector2Int unifiedPos)
        {
            return new Vector2Int(unifiedPos.x / 2, unifiedPos.y / 2);
        }
        
        public bool IsValidUnifiedPosition(Vector2Int unifiedPos)
        {
            return unifiedPos.x >= 0 && unifiedPos.x < fullGridSize &&
                   unifiedPos.y >= 0 && unifiedPos.y < fullGridSize;
        }
        #endregion

        #region Public API - Tile Management
        public bool IsTileOccupied(Vector2Int tilePos)
        {
            Vector2Int unifiedPos = TileToUnifiedPosition(tilePos);
            if (!IsValidUnifiedPosition(unifiedPos)) return true;
            return unifiedGrid[unifiedPos.x, unifiedPos.y].isOccupied;
        }

        public void SetTileOccupied(Vector2Int tilePos, bool occupied)
        {
            Vector2Int unifiedPos = TileToUnifiedPosition(tilePos);
            if (!IsValidUnifiedPosition(unifiedPos)) return;
            
            unifiedGrid[unifiedPos.x, unifiedPos.y].isOccupied = occupied;
            OnTileOccupancyChanged?.Invoke(tilePos, occupied);
            
            if (showTileLabels)
            {
                string labelText = occupied ? $"{tilePos.x},{tilePos.y}\n[OCCUPIED]" : $"{tilePos.x},{tilePos.y}";
                uiManager.UpdateTileLabel(tilePos.x, tilePos.y, labelText);
            }
        }

        public GameObject GetTile(Vector2Int tilePos)
        {
            Vector2Int unifiedPos = TileToUnifiedPosition(tilePos);
            if (!IsValidUnifiedPosition(unifiedPos)) return null;
            return unifiedGrid[unifiedPos.x, unifiedPos.y].visualObject;
        }

        public Vector3 GetTileCenter(Vector2Int tilePos)
        {
            GameObject tile = GetTile(tilePos);
            return tile != null ? tile.transform.position : Vector3.zero;
        }

        public List<Vector2Int> GetValidMoves(Vector2Int currentTilePos)
        {
            List<Vector2Int> validMoves = new List<Vector2Int>();
            Vector2Int currentUnified = TileToUnifiedPosition(currentTilePos);
            
            // Check all 4 directions (pawns move 2 cells in unified grid)
            Vector2Int[] directions = {
                new Vector2Int(0, 2),  // Up
                new Vector2Int(0, -2), // Down
                new Vector2Int(2, 0),  // Right
                new Vector2Int(-2, 0)  // Left
            };
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int targetUnified = currentUnified + dir;
                Vector2Int gapUnified = currentUnified + (dir / 2); // Gap between current and target
                
                // FIXED: Check if target is within tile bounds, not just unified grid bounds
                Vector2Int targetTilePos = UnifiedToTilePosition(targetUnified);
                
                // Check tile bounds properly (0 to gridSize-1)
                if (targetTilePos.x < 0 || targetTilePos.x >= gridSettings.gridSize ||
                    targetTilePos.y < 0 || targetTilePos.y >= gridSettings.gridSize)
                    continue;
                
                // Check if target unified position is valid and is a tile
                if (IsValidUnifiedPosition(targetUnified) && 
                    unifiedGrid[targetUnified.x, targetUnified.y].IsTile &&
                    !unifiedGrid[targetUnified.x, targetUnified.y].isOccupied)
                {
                    // Check if gap is not blocked by wall
                    if (IsValidUnifiedPosition(gapUnified) && !unifiedGrid[gapUnified.x, gapUnified.y].isOccupied)
                    {
                        validMoves.Add(targetTilePos);
                    }
                }
            }
            
            return validMoves;
        }
        #endregion

        #region Public API - Wall Management
        public bool IsGapOccupied(Orientation orientation, int x, int y)
        {
            // Convert wall coordinates to unified grid position
            Vector2Int unifiedPos = WallToUnifiedPosition(orientation, x, y);
            if (!IsValidUnifiedPosition(unifiedPos)) return true;
            return unifiedGrid[unifiedPos.x, unifiedPos.y].isOccupied;
        }

        public bool CanPlaceWall(Orientation orientation, int x, int y)
        {
            // SIMPLIFIED: Check if any of the 3 points (2 gaps + middle intersection) are occupied
            if (orientation == Orientation.Horizontal)
            {
                Vector2Int leftGap = new Vector2Int(x * 2, y * 2 + 1);
                Vector2Int rightGap = new Vector2Int(x * 2 + 2, y * 2 + 1);
                Vector2Int middleIntersection = new Vector2Int(x * 2 + 1, y * 2 + 1);
                
                if (!IsValidUnifiedPosition(leftGap) || !IsValidUnifiedPosition(rightGap) || !IsValidUnifiedPosition(middleIntersection))
                    return false;
                    
                return !unifiedGrid[leftGap.x, leftGap.y].isOccupied && 
                       !unifiedGrid[rightGap.x, rightGap.y].isOccupied &&
                       !unifiedGrid[middleIntersection.x, middleIntersection.y].isOccupied;
            }
            else // Vertical
            {
                Vector2Int bottomGap = new Vector2Int(x * 2 + 1, y * 2);
                Vector2Int topGap = new Vector2Int(x * 2 + 1, y * 2 + 2);
                Vector2Int middleIntersection = new Vector2Int(x * 2 + 1, y * 2 + 1);
                
                if (!IsValidUnifiedPosition(bottomGap) || !IsValidUnifiedPosition(topGap) || !IsValidUnifiedPosition(middleIntersection))
                    return false;
                    
                return !unifiedGrid[bottomGap.x, bottomGap.y].isOccupied && 
                       !unifiedGrid[topGap.x, topGap.y].isOccupied &&
                       !unifiedGrid[middleIntersection.x, middleIntersection.y].isOccupied;
            }
        }

        public bool PlaceWall(WallInfo wallInfo)
        {
            return PlaceWall(wallInfo, true); // Default: trigger events
        }
        
        /// <summary>
        /// SIMPLIFIED FIX: Each wall occupies exactly 3 points (2 endpoint gaps + 1 middle intersection)
        /// This prevents wall crossings while still allowing T-crossings
        /// </summary>
        public bool PlaceWall(WallInfo wallInfo, bool triggerEvents)
        {
            if (!CanPlaceWall(wallInfo.orientation, wallInfo.x, wallInfo.y))
                return false;
            
            // Mark the 3 points as occupied: 2 endpoint gaps + 1 middle intersection
            if (wallInfo.orientation == Orientation.Horizontal)
            {
                Vector2Int leftGap = new Vector2Int(wallInfo.x * 2, wallInfo.y * 2 + 1);
                Vector2Int rightGap = new Vector2Int(wallInfo.x * 2 + 2, wallInfo.y * 2 + 1);
                Vector2Int middleIntersection = new Vector2Int(wallInfo.x * 2 + 1, wallInfo.y * 2 + 1);
                
                unifiedGrid[leftGap.x, leftGap.y].isOccupied = true;
                unifiedGrid[rightGap.x, rightGap.y].isOccupied = true;
                unifiedGrid[middleIntersection.x, middleIntersection.y].isOccupied = true;
            }
            else // Vertical
            {
                Vector2Int bottomGap = new Vector2Int(wallInfo.x * 2 + 1, wallInfo.y * 2);
                Vector2Int topGap = new Vector2Int(wallInfo.x * 2 + 1, wallInfo.y * 2 + 2);
                Vector2Int middleIntersection = new Vector2Int(wallInfo.x * 2 + 1, wallInfo.y * 2 + 1);
                
                unifiedGrid[bottomGap.x, bottomGap.y].isOccupied = true;
                unifiedGrid[topGap.x, topGap.y].isOccupied = true;
                unifiedGrid[middleIntersection.x, middleIntersection.y].isOccupied = true;
            }
            
            // Only trigger events if requested (not for temporary validation placements)
            if (triggerEvents)
            {
                OnWallPlaced?.Invoke(wallInfo);
            }
            return true;
        }
        
        private Vector2Int WallToUnifiedPosition(Orientation orientation, int x, int y)
        {
            if (orientation == Orientation.Horizontal)
                return new Vector2Int(x * 2, y * 2 + 1);
            else
                return new Vector2Int(x * 2 + 1, y * 2);
        }
        
        /// <summary>
        /// SIMPLIFIED: Remove wall occupancy (clear the 3 points: 2 endpoint gaps + middle intersection)
        /// </summary>
        public void RemoveWallOccupancy(Orientation orientation, int x, int y)
        {
            if (orientation == Orientation.Horizontal)
            {
                Vector2Int leftGap = new Vector2Int(x * 2, y * 2 + 1);
                Vector2Int rightGap = new Vector2Int(x * 2 + 2, y * 2 + 1);
                Vector2Int middleIntersection = new Vector2Int(x * 2 + 1, y * 2 + 1);
                
                if (IsValidUnifiedPosition(leftGap)) unifiedGrid[leftGap.x, leftGap.y].isOccupied = false;
                if (IsValidUnifiedPosition(rightGap)) unifiedGrid[rightGap.x, rightGap.y].isOccupied = false;
                if (IsValidUnifiedPosition(middleIntersection)) unifiedGrid[middleIntersection.x, middleIntersection.y].isOccupied = false;
            }
            else // Vertical
            {
                Vector2Int bottomGap = new Vector2Int(x * 2 + 1, y * 2);
                Vector2Int topGap = new Vector2Int(x * 2 + 1, y * 2 + 2);
                Vector2Int middleIntersection = new Vector2Int(x * 2 + 1, y * 2 + 1);
                
                if (IsValidUnifiedPosition(bottomGap)) unifiedGrid[bottomGap.x, bottomGap.y].isOccupied = false;
                if (IsValidUnifiedPosition(topGap)) unifiedGrid[topGap.x, topGap.y].isOccupied = false;
                if (IsValidUnifiedPosition(middleIntersection)) unifiedGrid[middleIntersection.x, middleIntersection.y].isOccupied = false;
            }
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
            // Clear all occupancy but keep visual objects
            for (int x = 0; x < fullGridSize; x++)
            {
                for (int y = 0; y < fullGridSize; y++)
                {
                    if (unifiedGrid[x, y] != null)
                        unifiedGrid[x, y].isOccupied = false;
                }
            }
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
            // Destroy all visual objects in unified grid
            if (unifiedGrid != null)
            {
                for (int x = 0; x < fullGridSize; x++)
                {
                    for (int y = 0; y < fullGridSize; y++)
                    {
                        if (unifiedGrid[x, y]?.visualObject != null)
                            DestroyImmediate(unifiedGrid[x, y].visualObject);
                    }
                }
            }
            
            uiManager?.DestroyAllLabels();
        }
        
        // Get cell from unified grid
        public GridCell GetCell(Vector2Int unifiedPos)
        {
            if (!IsValidUnifiedPosition(unifiedPos)) return null;
            return unifiedGrid[unifiedPos.x, unifiedPos.y];
        }
        
        // Check if path exists using A* on unified grid
        public bool PathExists(Vector2Int fromTile, Vector2Int toTile)
        {
            return GridPathfinder.PathExists(this, fromTile, toTile);
        }
        
        // Get shortest path between two tiles
        public List<Vector2Int> FindPath(Vector2Int fromTile, Vector2Int toTile)
        {
            return GridPathfinder.FindPath(this, fromTile, toTile);
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
