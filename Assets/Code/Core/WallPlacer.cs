using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace WallChess.Core
{
    /// <summary>
    /// Consolidated WallPlacer implementation combining best features from all variants.
    /// Part of Phase 1: Emergency Codebase Cleanup (MVP Implementation Guide)
    /// Merges: ImprovedWallPlacer, WallPlacementController, FixedWallPlacer
    /// </summary>
    public class WallPlacer : MonoBehaviour
    {
        #region Constants
        // Grid configuration (single source of truth)
        public const int GRID_SIZE = 9;  // 9x9 Quoridor board
        public const int CELLS = 8;      // 8x8 cells between 9x9 tiles
        
        // Gap dimensions for wall placement
        private const int H_COLS = CELLS + 1;  // Horizontal gap columns
        private const int H_ROWS = CELLS;      // Horizontal gap rows
        private const int V_COLS = CELLS;      // Vertical gap columns
        private const int V_ROWS = CELLS + 1;  // Vertical gap rows
        #endregion

        #region Types
        public enum WallOrientation 
        { 
            Horizontal, 
            Vertical 
        }

        [System.Serializable]
        public struct WallInfo
        {
            public WallOrientation orientation;
            public Vector2Int gridPosition;
            public Vector3 worldPosition;
            public int playerId;
            public GameObject wallObject;

            public WallInfo(WallOrientation orient, Vector2Int gridPos, Vector3 worldPos, int player)
            {
                orientation = orient;
                gridPosition = gridPos;
                worldPosition = worldPos;
                playerId = player;
                wallObject = null;
            }
        }
        #endregion

        #region Configuration
        [Header("Wall Configuration")]
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private int maxWallsPerPlayer = 10;
        [SerializeField] private float wallThickness = 0.2f;
        [SerializeField] private float wallHeight = 0.5f;
        [SerializeField] private float wallLength = 2.0f;
        
        [Header("Visual Settings")]
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;
        [SerializeField] private Material[] playerWallMaterials;
        
        [Header("Placement Settings")]
        [SerializeField] private float snapDistance = 0.3f;
        [SerializeField] private LayerMask wallPlacementLayer;
        [SerializeField] private LayerMask blockingLayer;
        #endregion

        #region State
        private readonly List<WallInfo> placedWalls = new List<WallInfo>();
        private readonly Dictionary<int, int> wallsPerPlayer = new Dictionary<int, int>();
        
        // Efficient gap tracking with boolean arrays
        private readonly bool[,] horizontalGaps = new bool[H_COLS, H_ROWS];
        private readonly bool[,] verticalGaps = new bool[V_COLS, V_ROWS];
        
        // Preview state
        private GameObject wallPreview;
        private Renderer wallPreviewRenderer;
        private bool isPlacingWall = false;
        private WallOrientation currentOrientation = WallOrientation.Horizontal;
        
        // References
        private GridSystem gridSystem;
        private PathValidator pathValidator;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            gridSystem = FindObjectOfType<GridSystem>();
            pathValidator = GetComponent<PathValidator>() ?? gameObject.AddComponent<PathValidator>();
            InitializeWallCounts();
        }

        void Update()
        {
            if (isPlacingWall)
            {
                UpdateWallPreview();
                HandlePlacementInput();
            }
        }

        void OnDestroy()
        {
            if (wallPreview != null)
            {
                Destroy(wallPreview);
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Starts wall placement mode for the specified player
        /// </summary>
        public bool StartWallPlacement(int playerId)
        {
            if (!CanPlayerPlaceWall(playerId))
            {
                Debug.LogWarning($"Player {playerId} cannot place wall (no walls left)");
                return false;
            }

            isPlacingWall = true;
            CreateWallPreview();
            return true;
        }

        /// <summary>
        /// Cancels the current wall placement
        /// </summary>
        public void CancelWallPlacement()
        {
            isPlacingWall = false;
            if (wallPreview != null)
            {
                wallPreview.SetActive(false);
            }
        }

        /// <summary>
        /// Places a wall at the specified position
        /// </summary>
        public bool PlaceWall(Vector2Int gridPosition, WallOrientation orientation, int playerId)
        {
            if (!CanPlaceWall(gridPosition, orientation))
            {
                Debug.LogWarning($"Cannot place wall at {gridPosition} ({orientation})");
                return false;
            }

            // Create the wall
            Vector3 worldPos = CalculateWallWorldPosition(gridPosition, orientation);
            GameObject wall = CreateWall(worldPos, orientation, playerId);
            
            // Record the placement
            WallInfo info = new WallInfo(orientation, gridPosition, worldPos, playerId);
            info.wallObject = wall;
            placedWalls.Add(info);
            
            // Update gap tracking
            MarkGapAsOccupied(gridPosition, orientation);
            
            // Update player wall count
            if (!wallsPerPlayer.ContainsKey(playerId))
                wallsPerPlayer[playerId] = 0;
            wallsPerPlayer[playerId]++;
            
            // Hide preview
            CancelWallPlacement();
            
            Debug.Log($"Wall placed by Player {playerId} at {gridPosition} ({orientation})");
            return true;
        }

        /// <summary>
        /// Removes a wall (for undo functionality)
        /// </summary>
        public bool RemoveWall(WallInfo wallInfo)
        {
            if (!placedWalls.Contains(wallInfo))
                return false;

            // Clear gap tracking
            ClearGapOccupation(wallInfo.gridPosition, wallInfo.orientation);
            
            // Destroy the wall object
            if (wallInfo.wallObject != null)
            {
                Destroy(wallInfo.wallObject);
            }
            
            // Update player wall count
            if (wallsPerPlayer.ContainsKey(wallInfo.playerId))
            {
                wallsPerPlayer[wallInfo.playerId]--;
            }
            
            // Remove from list
            placedWalls.Remove(wallInfo);
            
            return true;
        }

        /// <summary>
        /// Checks if a wall can be placed at the given position
        /// </summary>
        public bool CanPlaceWall(Vector2Int gridPosition, WallOrientation orientation)
        {
            // Check bounds
            if (!IsValidGapPosition(gridPosition, orientation))
                return false;

            // Check if gap is already occupied
            if (IsGapOccupied(gridPosition, orientation))
                return false;

            // Check for crossing walls
            if (WouldCrossExistingWall(gridPosition, orientation))
                return false;

            // Check if placement would block all paths to goal
            if (!ValidatePathsAfterPlacement(gridPosition, orientation))
                return false;

            return true;
        }

        /// <summary>
        /// Gets the number of walls remaining for a player
        /// </summary>
        public int GetWallsRemaining(int playerId)
        {
            int used = wallsPerPlayer.ContainsKey(playerId) ? wallsPerPlayer[playerId] : 0;
            return maxWallsPerPlayer - used;
        }

        /// <summary>
        /// Checks if a player can place a wall
        /// </summary>
        public bool CanPlayerPlaceWall(int playerId)
        {
            return GetWallsRemaining(playerId) > 0;
        }

        /// <summary>
        /// Gets all placed walls
        /// </summary>
        public List<WallInfo> GetPlacedWalls()
        {
            return new List<WallInfo>(placedWalls);
        }

        /// <summary>
        /// Resets all wall placements
        /// </summary>
        public void ResetWalls()
        {
            // Destroy all wall objects
            foreach (var wall in placedWalls)
            {
                if (wall.wallObject != null)
                {
                    Destroy(wall.wallObject);
                }
            }
            
            // Clear tracking
            placedWalls.Clear();
            wallsPerPlayer.Clear();
            
            // Reset gap arrays
            System.Array.Clear(horizontalGaps, 0, horizontalGaps.Length);
            System.Array.Clear(verticalGaps, 0, verticalGaps.Length);
            
            InitializeWallCounts();
        }
        #endregion

        #region Private Methods - Validation
        private bool IsValidGapPosition(Vector2Int position, WallOrientation orientation)
        {
            if (orientation == WallOrientation.Horizontal)
            {
                return position.x >= 0 && position.x < H_COLS &&
                       position.y >= 0 && position.y < H_ROWS;
            }
            else
            {
                return position.x >= 0 && position.x < V_COLS &&
                       position.y >= 0 && position.y < V_ROWS;
            }
        }

        private bool IsGapOccupied(Vector2Int position, WallOrientation orientation)
        {
            if (orientation == WallOrientation.Horizontal)
            {
                // Check both segments of the horizontal wall
                if (position.x >= 0 && position.x < H_COLS - 1)
                {
                    return horizontalGaps[position.x, position.y] || 
                           horizontalGaps[position.x + 1, position.y];
                }
                return horizontalGaps[position.x, position.y];
            }
            else
            {
                // Check both segments of the vertical wall
                if (position.y >= 0 && position.y < V_ROWS - 1)
                {
                    return verticalGaps[position.x, position.y] || 
                           verticalGaps[position.x, position.y + 1];
                }
                return verticalGaps[position.x, position.y];
            }
        }

        private bool WouldCrossExistingWall(Vector2Int position, WallOrientation orientation)
        {
            // Check if placing this wall would cross an existing perpendicular wall
            if (orientation == WallOrientation.Horizontal)
            {
                // Check for vertical wall at the intersection point
                if (position.x > 0 && position.x < CELLS && 
                    position.y >= 0 && position.y < CELLS)
                {
                    return verticalGaps[position.x - 1, position.y] && 
                           verticalGaps[position.x - 1, position.y + 1];
                }
            }
            else
            {
                // Check for horizontal wall at the intersection point
                if (position.y > 0 && position.y < CELLS && 
                    position.x >= 0 && position.x < CELLS)
                {
                    return horizontalGaps[position.x, position.y - 1] && 
                           horizontalGaps[position.x + 1, position.y - 1];
                }
            }
            return false;
        }

        private bool ValidatePathsAfterPlacement(Vector2Int position, WallOrientation orientation)
        {
            // Temporarily mark the gap as occupied
            MarkGapAsOccupied(position, orientation);
            
            // Check if all players can still reach their goals
            bool allPathsValid = pathValidator.ValidateAllPlayerPaths(this);
            
            // Restore the gap state
            ClearGapOccupation(position, orientation);
            
            return allPathsValid;
        }

        private void MarkGapAsOccupied(Vector2Int position, WallOrientation orientation)
        {
            if (orientation == WallOrientation.Horizontal)
            {
                horizontalGaps[position.x, position.y] = true;
                if (position.x < H_COLS - 1)
                {
                    horizontalGaps[position.x + 1, position.y] = true;
                }
            }
            else
            {
                verticalGaps[position.x, position.y] = true;
                if (position.y < V_ROWS - 1)
                {
                    verticalGaps[position.x, position.y + 1] = true;
                }
            }
        }

        private void ClearGapOccupation(Vector2Int position, WallOrientation orientation)
        {
            if (orientation == WallOrientation.Horizontal)
            {
                horizontalGaps[position.x, position.y] = false;
                if (position.x < H_COLS - 1)
                {
                    horizontalGaps[position.x + 1, position.y] = false;
                }
            }
            else
            {
                verticalGaps[position.x, position.y] = false;
                if (position.y < V_ROWS - 1)
                {
                    verticalGaps[position.x, position.y + 1] = false;
                }
            }
        }
        #endregion

        #region Private Methods - Wall Creation
        private GameObject CreateWall(Vector3 position, WallOrientation orientation, int playerId)
        {
            GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity);
            wall.name = $"Wall_P{playerId}_{orientation}_{placedWalls.Count}";
            
            // Set rotation
            if (orientation == WallOrientation.Vertical)
            {
                wall.transform.rotation = Quaternion.Euler(0, 90, 0);
            }
            
            // Set scale
            wall.transform.localScale = new Vector3(wallLength, wallHeight, wallThickness);
            
            // Set material
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null && playerWallMaterials != null && playerWallMaterials.Length > playerId)
            {
                renderer.material = playerWallMaterials[playerId];
            }
            
            // Set layer
            wall.layer = LayerMask.NameToLayer("Wall");
            
            return wall;
        }

        private void CreateWallPreview()
        {
            if (wallPreview == null)
            {
                wallPreview = Instantiate(wallPrefab);
                wallPreview.name = "WallPreview";
                wallPreviewRenderer = wallPreview.GetComponent<Renderer>();
                
                // Make preview semi-transparent
                if (wallPreviewRenderer != null && validPlacementMaterial != null)
                {
                    wallPreviewRenderer.material = validPlacementMaterial;
                }
                
                // Disable colliders on preview
                Collider[] colliders = wallPreview.GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    col.enabled = false;
                }
            }
            
            wallPreview.SetActive(true);
        }

        private void UpdateWallPreview()
        {
            if (wallPreview == null) return;

            // Get mouse position in world space
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, wallPlacementLayer))
            {
                // Snap to nearest valid gap
                Vector2Int nearestGap = FindNearestGap(hit.point);
                Vector3 snapPosition = CalculateWallWorldPosition(nearestGap, currentOrientation);
                
                wallPreview.transform.position = snapPosition;
                wallPreview.transform.rotation = currentOrientation == WallOrientation.Vertical ? 
                    Quaternion.Euler(0, 90, 0) : Quaternion.identity;
                
                // Update preview material based on validity
                bool canPlace = CanPlaceWall(nearestGap, currentOrientation);
                if (wallPreviewRenderer != null)
                {
                    wallPreviewRenderer.material = canPlace ? validPlacementMaterial : invalidPlacementMaterial;
                }
            }
        }

        private Vector2Int FindNearestGap(Vector3 worldPosition)
        {
            // Convert world position to grid coordinates
            if (gridSystem != null)
            {
                Vector2Int gridPos = gridSystem.WorldToGrid(worldPosition);
                
                // Adjust for gap positioning based on orientation
                if (currentOrientation == WallOrientation.Horizontal)
                {
                    // Horizontal gaps are between rows
                    gridPos.y = Mathf.Clamp(gridPos.y, 0, H_ROWS - 1);
                    gridPos.x = Mathf.Clamp(gridPos.x, 0, H_COLS - 1);
                }
                else
                {
                    // Vertical gaps are between columns
                    gridPos.x = Mathf.Clamp(gridPos.x, 0, V_COLS - 1);
                    gridPos.y = Mathf.Clamp(gridPos.y, 0, V_ROWS - 1);
                }
                
                return gridPos;
            }
            
            // Fallback if no grid system
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x),
                Mathf.RoundToInt(worldPosition.z)
            );
        }

        private Vector3 CalculateWallWorldPosition(Vector2Int gridPosition, WallOrientation orientation)
        {
            Vector3 basePosition = new Vector3(gridPosition.x, wallHeight / 2f, gridPosition.y);
            
            if (orientation == WallOrientation.Horizontal)
            {
                // Horizontal wall spans between columns
                basePosition.x += 0.5f;
            }
            else
            {
                // Vertical wall spans between rows
                basePosition.z += 0.5f;
            }
            
            return basePosition;
        }
        #endregion

        #region Private Methods - Input Handling
        private void HandlePlacementInput()
        {
            // Rotate wall with R key or right mouse button
            if (UnityEngine.Input.GetKeyDown(KeyCode.R) || UnityEngine.Input.GetMouseButtonDown(1))
            {
                currentOrientation = currentOrientation == WallOrientation.Horizontal ? 
                    WallOrientation.Vertical : WallOrientation.Horizontal;
            }
            
            // Cancel placement with Escape
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                CancelWallPlacement();
            }
            
            // Confirm placement with left mouse button
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                TryPlaceWallAtMouse();
            }
        }

        private void TryPlaceWallAtMouse()
        {
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, wallPlacementLayer))
            {
                Vector2Int nearestGap = FindNearestGap(hit.point);
                
                // Get current player ID (would come from game manager in real implementation)
                int currentPlayerId = GetCurrentPlayerId();
                
                if (PlaceWall(nearestGap, currentOrientation, currentPlayerId))
                {
                    // Success - placement handled by PlaceWall method
                }
                else
                {
                    // Failed - could play error sound here
                    Debug.LogWarning("Invalid wall placement");
                }
            }
        }

        private int GetCurrentPlayerId()
        {
            // This would normally come from the game manager
            // For now, return 0 as placeholder
            var gameManager = FindObjectOfType<WallChessGameManager>();
            if (gameManager != null)
            {
                return gameManager.CurrentPlayerIndex;
            }
            return 0;
        }
        #endregion

        #region Private Methods - Initialization
        private void InitializeWallCounts()
        {
            // Initialize wall counts for 2-4 players
            for (int i = 0; i < 4; i++)
            {
                wallsPerPlayer[i] = 0;
            }
        }
        #endregion
    }

    /// <summary>
    /// Helper class for validating paths after wall placement
    /// </summary>
    public class PathValidator : MonoBehaviour
    {
        /// <summary>
        /// Validates that all players can still reach their goal after a wall placement
        /// </summary>
        public bool ValidateAllPlayerPaths(WallPlacer wallPlacer)
        {
            // This would implement A* or BFS pathfinding
            // For now, return true as placeholder
            // In production, this would check each player's path to their goal row
            return true;
        }
    }

    /// <summary>
    /// Extension to GridSystem for wall placement
    /// </summary>
    public static class GridSystemExtensions
    {
        public static Vector2Int WorldToGrid(this GridSystem grid, Vector3 worldPosition)
        {
            if (grid == null) return Vector2Int.zero;
            
            // Convert world position to grid coordinates
            float cellSize = 1.0f; // Would come from grid configuration
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / cellSize),
                Mathf.RoundToInt(worldPosition.z / cellSize)
            );
        }
    }
}