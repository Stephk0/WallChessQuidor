using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

            public string GetGapKey()
            {
                return $"{(orientation == Orientation.Horizontal ? "H" : "V")}_{x}_{y}";
            }
        }

        #region Settings
        [Header("Grid Configuration")]
        [SerializeField] private GridSettings gridSettings;
        [SerializeField] private GapConfiguration gapConfig;
        
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

        #region Grid Data
        private GameObject[,] gridTiles;
        private GameObject[,] tileLabels;
        private bool[,] tileOccupied;
        
        // Wall gap tracking
        private bool[,] horizontalGaps; // [HCols, HRows]
        private bool[,] verticalGaps;   // [VCols, VRows]
        private List<WallInfo> placedWalls;
        private HashSet<Vector2Int> wallIntersections;
        
        // UI Canvas
        private Canvas uiCanvas;
        private GameObject canvasObject;
        
        // Computed dimensions
        private int HorizontalGapCols => gridSettings.gridSize + 1;
        private int HorizontalGapRows => gridSettings.gridSize;
        private int VerticalGapCols => gridSettings.gridSize;
        private int VerticalGapRows => gridSettings.gridSize + 1;
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
            
            // Set default gap offsets if not provided
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

            InitializeArrays();
            CreateUICanvas();
            CreateGridTiles();
            
            Debug.Log($"GridSystem initialized: {gridSettings.gridSize}x{gridSettings.gridSize}, spacing={gridSettings.TileSpacing}");
        }

        private void InitializeArrays()
        {
            // Initialize tile arrays
            gridTiles = new GameObject[gridSettings.gridSize, gridSettings.gridSize];
            tileLabels = new GameObject[gridSettings.gridSize, gridSettings.gridSize];
            tileOccupied = new bool[gridSettings.gridSize, gridSettings.gridSize];
            
            // Initialize gap arrays
            horizontalGaps = new bool[HorizontalGapCols, HorizontalGapRows];
            verticalGaps = new bool[VerticalGapCols, VerticalGapRows];
            
            // Initialize collections
            placedWalls = new List<WallInfo>();
            wallIntersections = new HashSet<Vector2Int>();
            
            ClearArrays();
        }

        private void ClearArrays()
        {
            // Clear tile occupancy
            for (int x = 0; x < gridSettings.gridSize; x++)
                for (int y = 0; y < gridSettings.gridSize; y++)
                    tileOccupied[x, y] = false;

            // Clear gap occupancy
            for (int x = 0; x < HorizontalGapCols; x++)
                for (int y = 0; y < HorizontalGapRows; y++)
                    horizontalGaps[x, y] = false;

            for (int x = 0; x < VerticalGapCols; x++)
                for (int y = 0; y < VerticalGapRows; y++)
                    verticalGaps[x, y] = false;

            placedWalls.Clear();
            wallIntersections.Clear();
        }

        private void CreateUICanvas()
        {
            // Create or find existing canvas
            uiCanvas = FindObjectOfType<Canvas>();
            
            if (uiCanvas == null)
            {
                canvasObject = new GameObject("GridCanvas");
                canvasObject.transform.parent = this.transform;
                
                uiCanvas = canvasObject.AddComponent<Canvas>();
                uiCanvas.renderMode = RenderMode.WorldSpace;
                uiCanvas.worldCamera = Camera.main;
                
                // Add CanvasScaler for better text scaling
                var canvasScaler = canvasObject.AddComponent<CanvasScaler>();
                canvasScaler.dynamicPixelsPerUnit = 100f;
                
                // Add GraphicRaycaster for UI interactions
                canvasObject.AddComponent<GraphicRaycaster>();
                
                // Position the canvas slightly forward
                canvasObject.transform.position = new Vector3(0, 0, -0.1f);
                canvasObject.transform.localScale = Vector3.one * 0.01f; // Scale down for world space
            }
        }

        private void CreateGridTiles()
        {
            for (int x = 0; x < gridSettings.gridSize; x++)
            {
                for (int y = 0; y < gridSettings.gridSize; y++)
                {
                    Vector3 position = GridToWorldPosition(new Vector2Int(x, y));
                    
                    // Create tile
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tile.name = $"Tile_{x}_{y}";
                    tile.transform.position = position;
                    tile.transform.localScale = Vector3.one * gridSettings.tileSize;
                    tile.transform.parent = this.transform;
                    
                    // Set up tile appearance
                    Renderer tileRenderer = tile.GetComponent<Renderer>();
                    if (tileMaterial != null)
                        tileRenderer.material = tileMaterial;
                    else
                        tileRenderer.material = new Material(Shader.Find("Sprites/Default"));
                    
                    tileRenderer.material.color = (x + y) % 2 == 0 ? lightTileColor : darkTileColor;
                    
                    // Remove collider
                    if (tile.GetComponent<Collider>() != null)
                        DestroyImmediate(tile.GetComponent<Collider>());
                    
                    gridTiles[x, y] = tile;

                    // Create UI text label for the tile
                    if (showTileLabels && uiCanvas != null)
                    {
                        CreateTileLabel(x, y, position);
                    }
                }
            }
        }

        private void CreateTileLabel(int x, int y, Vector3 tilePosition)
        {
            // Create text GameObject
            GameObject textObj = new GameObject($"Label_{x}_{y}");
            textObj.transform.SetParent(uiCanvas.transform);
            
            // Add Text component
            Text textComponent = textObj.AddComponent<Text>();
            textComponent.text = $"{x},{y}";
            textComponent.font = labelFont != null ? labelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = labelFontSize;
            textComponent.color = labelColor;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            // Set RectTransform properties
            RectTransform rectTransform = textComponent.GetComponent<RectTransform>();
            
            // Convert world position to canvas position
            Vector3 canvasPos = tilePosition;
            canvasPos.z = -0.05f; // Slightly in front of tiles
            
            // Apply canvas scaling
            Vector3 localPos = uiCanvas.transform.InverseTransformPoint(canvasPos);
            rectTransform.localPosition = localPos;
            
            // Set size
            rectTransform.sizeDelta = new Vector2(100, 50);
            
            // Store reference
            tileLabels[x, y] = textObj;
        }

        public void ToggleTileLabels(bool show)
        {
            showTileLabels = show;
            
            if (tileLabels != null)
            {
                for (int x = 0; x < gridSettings.gridSize; x++)
                {
                    for (int y = 0; y < gridSettings.gridSize; y++)
                    {
                        if (tileLabels[x, y] != null)
                        {
                            tileLabels[x, y].SetActive(show);
                        }
                    }
                }
            }
        }

        public void UpdateTileLabel(int x, int y, string newText)
        {
            if (tileLabels != null && x >= 0 && x < gridSettings.gridSize && y >= 0 && y < gridSettings.gridSize)
            {
                if (tileLabels[x, y] != null)
                {
                    var textComponent = tileLabels[x, y].GetComponent<Text>();
                    if (textComponent != null)
                    {
                        textComponent.text = newText;
                    }
                }
            }
        }
        #endregion

        #region Public API - Grid Properties
        public GridSettings GetGridSettings() => gridSettings;
        public Vector2Int GetGridDimensions() => new Vector2Int(gridSettings.gridSize, gridSettings.gridSize);
        public Vector2Int GetHorizontalGapDimensions() => new Vector2Int(HorizontalGapCols, HorizontalGapRows);
        public Vector2Int GetVerticalGapDimensions() => new Vector2Int(VerticalGapCols, VerticalGapRows);
        public float GetTileSpacing() => gridSettings.TileSpacing;
        public int GetGridSize() => gridSettings.gridSize;
        #endregion

        #region Public API - Coordinate Conversion
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            float spacing = gridSettings.TileSpacing;
            return new Vector3(gridPos.x * spacing, gridPos.y * spacing, 0);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            float spacing = gridSettings.TileSpacing;
            int x = Mathf.RoundToInt(worldPos.x / spacing);
            int y = Mathf.RoundToInt(worldPos.y / spacing);
            
            x = Mathf.Clamp(x, 0, gridSettings.gridSize - 1);
            y = Mathf.Clamp(y, 0, gridSettings.gridSize - 1);
            
            return new Vector2Int(x, y);
        }

        public bool IsValidGridPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < gridSettings.gridSize &&
                   gridPos.y >= 0 && gridPos.y < gridSettings.gridSize;
        }

        public bool IsWithinGridBounds(Vector3 worldPos)
        {
            float spacing = gridSettings.TileSpacing;
            float gridMin = 0f - spacing * 0.5f;
            float gridMax = gridSettings.gridSize * spacing + spacing * 0.5f;
            return worldPos.x >= gridMin && worldPos.x <= gridMax && 
                   worldPos.y >= gridMin && worldPos.y <= gridMax;
        }
        #endregion

        #region Public API - Tile Management
        public bool IsTileOccupied(Vector2Int gridPos)
        {
            if (!IsValidGridPosition(gridPos)) return true;
            return tileOccupied[gridPos.x, gridPos.y];
        }

        public void SetTileOccupied(Vector2Int gridPos, bool occupied)
        {
            if (!IsValidGridPosition(gridPos)) return;
            
            tileOccupied[gridPos.x, gridPos.y] = occupied;
            OnTileOccupancyChanged?.Invoke(gridPos, occupied);
            
            // Update tile label to show occupancy
            if (showTileLabels && occupied)
            {
                UpdateTileLabel(gridPos.x, gridPos.y, $"{gridPos.x},{gridPos.y}\n[OCCUPIED]");
            }
            else if (showTileLabels)
            {
                UpdateTileLabel(gridPos.x, gridPos.y, $"{gridPos.x},{gridPos.y}");
            }
        }

        public GameObject GetTile(Vector2Int gridPos)
        {
            if (!IsValidGridPosition(gridPos)) return null;
            return gridTiles[gridPos.x, gridPos.y];
        }

        public Vector3 GetTileCenter(Vector2Int gridPos)
        {
            if (!IsValidGridPosition(gridPos)) return Vector3.zero;
            return gridTiles[gridPos.x, gridPos.y].transform.position;
        }

        public List<Vector2Int> GetValidMoves(Vector2Int currentPos)
        {
            List<Vector2Int> validMoves = new List<Vector2Int>();
            
            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int newPos = currentPos + direction;
                
                if (IsValidGridPosition(newPos) && !IsTileOccupied(newPos))
                {
                    // Check if path is blocked by walls using corrected logic
                    if (!IsMovementBlockedByWalls(currentPos, newPos))
                    {
                        validMoves.Add(newPos);
                    }
                }
            }

            return validMoves;
        }
        
        /// <summary>
        /// Check if movement between two adjacent positions is blocked by walls
        /// Uses corrected logic that properly validates both parts of a wall
        /// </summary>
        private bool IsMovementBlockedByWalls(Vector2Int from, Vector2Int to)
        {
            Vector2Int diff = to - from;
            
            // Determine which gap to check based on movement direction
            if (diff.y == 1) // Moving up
            {
                // Check horizontal wall above 'from' position
                int gapX = from.x;
                int gapY = from.y; // Gap between row 'from.y' and row 'to.y'
                
                return IsHorizontalWallBlocking(gapX, gapY);
            }
            else if (diff.y == -1) // Moving down
            {
                // Check horizontal wall below 'from' position
                int gapX = from.x;
                int gapY = to.y; // Gap between row 'to.y' and row 'from.y' (use target row)
                
                return IsHorizontalWallBlocking(gapX, gapY);
            }
            else if (diff.x == 1) // Moving right
            {
                // Check vertical wall to the right of 'from' position
                int gapX = from.x; // Gap is at the 'from' position for rightward movement
                int gapY = from.y;
                
                return IsVerticalWallBlocking(gapX, gapY);
            }
            else if (diff.x == -1) // Moving left
            {
                // Check vertical wall to the left of 'from' position  
                // The gap between tile column 'to.x' and 'from.x' is at gap position 'to.x'
                int gapX = to.x; // FIXED: Use target column instead of from.x - 1
                int gapY = from.y;
                
                return IsVerticalWallBlocking(gapX, gapY);
            }
            
            return false; // Invalid movement direction
        }
        
        /// <summary>
        /// Check if a horizontal wall is blocking movement
        /// A horizontal wall blocks if BOTH of its gap positions are occupied
        /// FIXED: Check both possible wall positions that could block this gap
        /// </summary>
        private bool IsHorizontalWallBlocking(int gapX, int gapY)
        {
            // Check bounds first
            if (gapX < 0 || gapY < 0) return false;
            
            // A horizontal wall can block movement at gapX in two ways:
            // 1. Wall starts at gapX: occupies (gapX, gapY) and (gapX+1, gapY)
            // 2. Wall starts at gapX-1: occupies (gapX-1, gapY) and (gapX, gapY)
            
            // Check possibility 1: Wall starts at gapX
            bool wall1Left = IsGapOccupied(Orientation.Horizontal, gapX, gapY);
            bool wall1Right = IsGapOccupied(Orientation.Horizontal, gapX + 1, gapY);
            if (wall1Left && wall1Right)
            {
                return true;
            }
            
            // Check possibility 2: Wall starts at gapX-1
            if (gapX - 1 >= 0)
            {
                bool wall2Left = IsGapOccupied(Orientation.Horizontal, gapX - 1, gapY);
                bool wall2Right = IsGapOccupied(Orientation.Horizontal, gapX, gapY);
                if (wall2Left && wall2Right)
                {
                    return true;
                }
            }
            
            return false; // No wall blocks this position
        }
        
        /// <summary>
        /// Check if a vertical wall is blocking movement
        /// A vertical wall blocks if BOTH of its gap positions are occupied
        /// FIXED: Check both possible wall positions that could block this gap
        /// </summary>
        private bool IsVerticalWallBlocking(int gapX, int gapY)
        {
            // Check bounds first
            if (gapX < 0 || gapY < 0) return false;
            
            // A vertical wall can block movement at gapY in two ways:
            // 1. Wall starts at gapY: occupies (gapX, gapY) and (gapX, gapY+1)
            // 2. Wall starts at gapY-1: occupies (gapX, gapY-1) and (gapX, gapY)
            
            // Check possibility 1: Wall starts at gapY
            bool wall1Bottom = IsGapOccupied(Orientation.Vertical, gapX, gapY);
            bool wall1Top = IsGapOccupied(Orientation.Vertical, gapX, gapY + 1);
            if (wall1Bottom && wall1Top)
            {
                return true;
            }
            
            // Check possibility 2: Wall starts at gapY-1
            if (gapY - 1 >= 0)
            {
                bool wall2Bottom = IsGapOccupied(Orientation.Vertical, gapX, gapY - 1);
                bool wall2Top = IsGapOccupied(Orientation.Vertical, gapX, gapY);
                if (wall2Bottom && wall2Top)
                {
                    return true;
                }
            }
            
            return false; // No wall blocks this position
        }
        #endregion

        #region Public API - Wall Management
        public bool IsGapOccupied(Orientation orientation, int x, int y)
        {
            if (orientation == Orientation.Horizontal)
            {
                if (x < 0 || x >= HorizontalGapCols || y < 0 || y >= HorizontalGapRows) return true;
                return horizontalGaps[x, y];
            }
            else
            {
                if (x < 0 || x >= VerticalGapCols || y < 0 || y >= VerticalGapRows) return true;
                return verticalGaps[x, y];
            }
        }

        public void SetGapOccupied(Orientation orientation, int x, int y, bool occupied)
        {
            if (orientation == Orientation.Horizontal)
            {
                if (x >= 0 && x < HorizontalGapCols && y >= 0 && y < HorizontalGapRows)
                    horizontalGaps[x, y] = occupied;
            }
            else
            {
                if (x >= 0 && x < VerticalGapCols && y >= 0 && y < VerticalGapRows)
                    verticalGaps[x, y] = occupied;
            }
        }

        public Vector3 GetGapWorldPosition(Orientation orientation, int x, int y)
        {
            float spacing = gridSettings.TileSpacing;
            
            if (orientation == Orientation.Horizontal)
            {
                return new Vector3(
                    (x + gapConfig.horizontalGapOffsetX) * spacing,
                    (y + gapConfig.horizontalGapOffsetY) * spacing, 
                    0f
                );
            }
            else
            {
                return new Vector3(
                    (x + gapConfig.verticalGapOffsetX) * spacing,
                    (y + gapConfig.verticalGapOffsetY) * spacing, 
                    0f
                );
            }
        }

        public WallInfo CreateWallInfo(Orientation orientation, int x, int y)
        {
            Vector3 center = GetGapWorldPosition(orientation, x, y);
            
            Vector3 scale = (orientation == Orientation.Horizontal)
                ? new Vector3(gridSettings.TileSpacing * 2f, gridSettings.wallThickness, gridSettings.wallHeight)
                : new Vector3(gridSettings.wallThickness, gridSettings.TileSpacing * 2f, gridSettings.wallHeight);

            return new WallInfo(orientation, x, y, new Vector3(center.x, center.y, -0.1f), scale);
        }

        public bool CanPlaceWall(WallInfo wallInfo)
        {
            return CanPlaceWall(wallInfo.orientation, wallInfo.x, wallInfo.y);
        }

        public bool CanPlaceWall(Orientation orientation, int x, int y)
        {
            if (orientation == Orientation.Horizontal)
            {
                // Check bounds and occupancy for horizontal wall (spans 2 gaps)
                if (x < 0 || x + 1 >= HorizontalGapCols || y < 0 || y >= HorizontalGapRows) return false;
                if (IsGapOccupied(Orientation.Horizontal, x, y)) return false;
                if (IsGapOccupied(Orientation.Horizontal, x + 1, y)) return false;
                
                // Check for crossing walls
                if (y + 1 < VerticalGapRows && 
                    IsGapOccupied(Orientation.Vertical, x, y) && 
                    IsGapOccupied(Orientation.Vertical, x, y + 1)) 
                    return false;
            }
            else
            {
                // Check bounds and occupancy for vertical wall (spans 2 gaps)
                if (x < 0 || x >= VerticalGapCols || y < 0 || y + 1 >= VerticalGapRows) return false;
                if (IsGapOccupied(Orientation.Vertical, x, y)) return false;
                if (IsGapOccupied(Orientation.Vertical, x, y + 1)) return false;
                
                // Check for crossing walls
                if (x + 1 < HorizontalGapCols && 
                    IsGapOccupied(Orientation.Horizontal, x, y) && 
                    IsGapOccupied(Orientation.Horizontal, x + 1, y)) 
                    return false;
            }

            return true;
        }

        public bool PlaceWall(WallInfo wallInfo)
        {
            if (!CanPlaceWall(wallInfo)) return false;

            // Mark gaps as occupied
            if (wallInfo.orientation == Orientation.Horizontal)
            {
                SetGapOccupied(Orientation.Horizontal, wallInfo.x, wallInfo.y, true);
                SetGapOccupied(Orientation.Horizontal, wallInfo.x + 1, wallInfo.y, true);
            }
            else
            {
                SetGapOccupied(Orientation.Vertical, wallInfo.x, wallInfo.y, true);
                SetGapOccupied(Orientation.Vertical, wallInfo.x, wallInfo.y + 1, true);
            }

            // Track wall and intersections
            placedWalls.Add(wallInfo);
            UpdateWallIntersections(wallInfo);

            OnWallPlaced?.Invoke(wallInfo);
            return true;
        }

        private void UpdateWallIntersections(WallInfo wallInfo)
        {
            // Calculate potential intersection points for this wall
            Vector2Int intersection = new Vector2Int(wallInfo.x, wallInfo.y);
            
            if (wallInfo.orientation == Orientation.Horizontal)
            {
                wallIntersections.Add(intersection);
                wallIntersections.Add(new Vector2Int(wallInfo.x + 1, wallInfo.y));
            }
            else
            {
                wallIntersections.Add(intersection);
                wallIntersections.Add(new Vector2Int(wallInfo.x, wallInfo.y + 1));
            }
        }

        public List<WallInfo> GetPlacedWalls() => new List<WallInfo>(placedWalls);
        public HashSet<Vector2Int> GetWallIntersections() => new HashSet<Vector2Int>(wallIntersections);
        #endregion

        #region Public API - Gap Detection
        public WallInfo? FindNearestValidWall(Vector3 worldPosition, float snapMargin = 0.25f)
        {
            float bestDistSq = float.PositiveInfinity;
            WallInfo? bestWall = null;

            // Check horizontal gaps
            for (int y = 0; y < HorizontalGapRows; y++)
            {
                for (int x = 0; x <= HorizontalGapCols - 2; x++) // Wall spans 2 gaps
                {
                    if (CanPlaceWall(Orientation.Horizontal, x, y))
                    {
                        Vector3 gapCenter = GetGapWorldPosition(Orientation.Horizontal, x, y);
                        float distSq = (worldPosition - gapCenter).sqrMagnitude;
                        
                        if (distSq < bestDistSq && distSq <= snapMargin * snapMargin)
                        {
                            bestDistSq = distSq;
                            bestWall = CreateWallInfo(Orientation.Horizontal, x, y);
                        }
                    }
                }
            }

            // Check vertical gaps
            for (int x = 0; x < VerticalGapCols; x++)
            {
                for (int y = 0; y <= VerticalGapRows - 2; y++) // Wall spans 2 gaps
                {
                    if (CanPlaceWall(Orientation.Vertical, x, y))
                    {
                        Vector3 gapCenter = GetGapWorldPosition(Orientation.Vertical, x, y);
                        float distSq = (worldPosition - gapCenter).sqrMagnitude;
                        
                        if (distSq < bestDistSq && distSq <= snapMargin * snapMargin)
                        {
                            bestDistSq = distSq;
                            bestWall = CreateWallInfo(Orientation.Vertical, x, y);
                        }
                    }
                }
            }

            return bestWall;
        }
        #endregion

        #region Public API - Utility
        public void ClearGrid()
        {
            ClearArrays();
            OnGridCleared?.Invoke();
        }

        public void ReconfigureGrid(GridSettings newSettings, GapConfiguration newGaps = default)
        {
            // Destroy existing tiles and labels
            if (gridTiles != null)
            {
                for (int x = 0; x < gridTiles.GetLength(0); x++)
                {
                    for (int y = 0; y < gridTiles.GetLength(1); y++)
                    {
                        if (gridTiles[x, y] != null)
                            DestroyImmediate(gridTiles[x, y]);
                        if (tileLabels != null && tileLabels[x, y] != null)
                            DestroyImmediate(tileLabels[x, y]);
                    }
                }
            }

            // Destroy canvas if we created it
            if (canvasObject != null)
            {
                DestroyImmediate(canvasObject);
                canvasObject = null;
                uiCanvas = null;
            }

            // Reinitialize with new settings
            Initialize(newSettings, newGaps);
        }
        #endregion

        #region Gizmos
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (gridSettings.gridSize <= 0) return;

            float spacing = gridSettings.TileSpacing;

            // Draw grid bounds
            Gizmos.color = Color.white;
            Vector3 min = new Vector3(-spacing * 0.5f, -spacing * 0.5f, 0f);
            Vector3 max = new Vector3(gridSettings.gridSize * spacing + spacing * 0.5f,
                                      gridSettings.gridSize * spacing + spacing * 0.5f, 0f);
            Vector3 size = max - min;
            Gizmos.DrawWireCube(min + size * 0.5f, size);

            if (horizontalGaps == null || verticalGaps == null) return;

            // Draw gap centers and occupancy
            for (int y = 0; y < gridSettings.gridSize; y++)
            {
                for (int x = 0; x < gridSettings.gridSize; x++)
                {
                    // Draw horizontal gap indicators
                    if (x < HorizontalGapCols && y < HorizontalGapRows)
                    {
                        Vector3 hCenter = GetGapWorldPosition(Orientation.Horizontal, x, y);
                        bool hOccupied = (x + 1 < HorizontalGapCols) && 
                                         IsGapOccupied(Orientation.Horizontal, x, y) && 
                                         IsGapOccupied(Orientation.Horizontal, x + 1, y);
                        
                        Gizmos.color = hOccupied ? Color.red : Color.gray;
                        Gizmos.DrawCube(hCenter + Vector3.forward * -0.05f, 
                                       new Vector3(0.05f, 0.05f, 0.01f));
                    }

                    // Draw vertical gap indicators
                    if (x < VerticalGapCols && y < VerticalGapRows)
                    {
                        Vector3 vCenter = GetGapWorldPosition(Orientation.Vertical, x, y);
                        bool vOccupied = (y + 1 < VerticalGapRows) && 
                                         IsGapOccupied(Orientation.Vertical, x, y) && 
                                         IsGapOccupied(Orientation.Vertical, x, y + 1);
                        
                        Gizmos.color = vOccupied ? Color.red : Color.gray;
                        Gizmos.DrawSphere(vCenter, 0.03f);
                    }
                }
            }

            // Draw intersections
            Gizmos.color = Color.yellow;
            foreach (var intersection in wallIntersections)
            {
                Vector3 pos = new Vector3(intersection.x * spacing, intersection.y * spacing, 0f);
                Gizmos.DrawWireCube(pos, Vector3.one * 0.1f);
            }
        }
#endif
        #endregion
    }
}
