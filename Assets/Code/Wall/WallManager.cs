using UnityEngine;
using System.Collections.Generic;
using WallChess.Grid;

namespace WallChess
{
    /// <summary>
    /// Unified WallManager that uses GridSystem as single source of truth for occupancy.
    /// No more dual tracking systems - everything goes through GridSystem.
    /// Enhanced with prefab rotation system and debug mode support.
    /// </summary>
    public class WallManager : MonoBehaviour
    {
        [Header("Debug & Development")]
        [SerializeField] private bool boxForPrefabDebugMode = false;
        [Tooltip("When enabled, uses primitive boxes scaled to fit gaps instead of prefabs")]
        
        [Header("Wall Prefabs")]
        [SerializeField] private List<GameObject> wallPrefabs = new List<GameObject>();
        [Tooltip("List of wall prefabs to randomly choose from for placement")]
        
        [Header("Prefab Orientation")]
        [SerializeField] private Vector3 horizontalRotation = Vector3.zero;
        [Tooltip("Rotation applied to prefabs when placing horizontally")]
        [SerializeField] private Vector3 verticalRotation = new Vector3(0, 0, 90);
        [Tooltip("Rotation applied to prefabs when placing vertically")]
        [SerializeField] private Vector3 rotationAxis = Vector3.forward;
        [Tooltip("Axis around which to rotate the prefab for orientation changes")]

        [Header("Legacy Assets (Debug Mode Only)")]
        [SerializeField] private Material wallMaterial;
        [SerializeField] private GameObject wallPrefab;
        [Tooltip("Used only when Box For Prefab Debug Mode is enabled")]

        [Header("Placement Visuals")]
        [SerializeField] private Color validPreviewColor = new Color(0, 1, 0, 0.7f);
        [SerializeField] private Color invalidPreviewColor = new Color(1, 0, 0, 0.7f);
        [SerializeField] private Color placingPreviewColor = new Color(1, 1, 0, 0.5f);
        [SerializeField] private float placementPlaneZ = 0f;

        [Header("Snap & Lanes")]
        [SerializeField] private float gapSnapMargin = 0.25f;
        [SerializeField] private float laneSnapMargin = 0.5f;  // Increased for new intersection approach
        [SerializeField] private float unlockMultiplier = 1.8f; // Balanced for smooth orientation switching

        private WallChessGameManager gameManager;
        private GridSystem gridSystem;
        private GridCoordinateConverter coordinateConverter;
        private WallValidator validator;
        private WallVisuals visuals;
        private WallPlacementController placement;
        private List<GameObject> managedWalls = new List<GameObject>();

        public void Initialize(WallChessGameManager gm)
        {
            gameManager = gm;
            gridSystem = gameManager != null ? gameManager.GetGridSystem() : null;

            if (gridSystem == null)
            {
                Debug.LogError("WallManager: GridSystem not found! WallManager requires GridSystem to be initialized first.");
                return;
            }

            // Get the coordinate converter from the grid system to respect alignment
            coordinateConverter = GetCoordinateConverterFromGrid();
            if (coordinateConverter == null)
            {
                Debug.LogError("WallManager: Could not get coordinate converter from GridSystem!");
                return;
            }

            // Validate prefab setup
            ValidatePrefabSetup();

            // Initialize unified systems that use GridSystem as source of truth
            validator = new WallValidator(gridSystem, gameManager);
            visuals = new WallVisuals(GetActivePrefab(), GetActiveMaterial(), validPreviewColor, invalidPreviewColor, placingPreviewColor);
            visuals.SetWallManager(this); // Set reference for advanced preview features
            placement = new WallPlacementController(this, gameManager, gridSystem, validator, visuals, placementPlaneZ);

            Debug.Log($"WallManager initialized with unified GridSystem integration. Debug mode: {boxForPrefabDebugMode}");
        }

        /// <summary>
        /// Gets the coordinate converter from the grid system using reflection
        /// since GridCoordinateConverter is private in GridSystem
        /// </summary>
        private GridCoordinateConverter GetCoordinateConverterFromGrid()
        {
            var field = typeof(GridSystem).GetField("coordinateConverter", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                return (GridCoordinateConverter)field.GetValue(gridSystem);
            }

            // Fallback: create our own converter using grid settings
            var settings = gridSystem.GetGridSettings();
            var alignment = gridSystem.GetGridAlignment();
            return new GridCoordinateConverter(settings.TileSpacing, settings.gridSize, alignment);
        }

        /// <summary>
        /// Validates prefab setup and provides warnings if needed
        /// </summary>
        private void ValidatePrefabSetup()
        {
            if (boxForPrefabDebugMode)
            {
                if (wallPrefab == null)
                {
                    Debug.LogWarning("WallManager: Box for Prefab Debug Mode is enabled but no wallPrefab is assigned!");
                }
                if (wallMaterial == null)
                {
                    Debug.LogWarning("WallManager: Box for Prefab Debug Mode is enabled but no wallMaterial is assigned!");
                }
            }
            else
            {
                if (wallPrefabs == null || wallPrefabs.Count == 0)
                {
                    Debug.LogError("WallManager: No wall prefabs assigned! Either enable Debug Mode or assign wall prefabs.");
                }
                else
                {
                    // Check for null prefabs in the list
                    for (int i = wallPrefabs.Count - 1; i >= 0; i--)
                    {
                        if (wallPrefabs[i] == null)
                        {
                            Debug.LogWarning($"WallManager: Null prefab found at index {i}, removing from list.");
                            wallPrefabs.RemoveAt(i);
                        }
                    }
                    
                    if (wallPrefabs.Count == 0)
                    {
                        Debug.LogError("WallManager: All wall prefabs were null! Either enable Debug Mode or assign valid wall prefabs.");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the active prefab based on current mode
        /// </summary>
        private GameObject GetActivePrefab()
        {
            if (boxForPrefabDebugMode)
            {
                return wallPrefab;
            }
            
            if (wallPrefabs != null && wallPrefabs.Count > 0)
            {
                // For now, return the first valid prefab. Could be randomized later if desired.
                return wallPrefabs[0];
            }
            
            // Fallback to debug prefab
            Debug.LogWarning("WallManager: No valid prefabs available, falling back to debug prefab.");
            return wallPrefab;
        }

        /// <summary>
        /// Gets the active material based on current mode
        /// </summary>
        private Material GetActiveMaterial()
        {
            if (boxForPrefabDebugMode)
            {
                return wallMaterial;
            }
            
            // In prefab mode, materials should come from the prefab itself
            return null;
        }

        /// <summary>
        /// Gets a random wall prefab from the list (for variety in wall placement)
        /// </summary>
        public GameObject GetRandomWallPrefab()
        {
            if (boxForPrefabDebugMode || wallPrefabs == null || wallPrefabs.Count == 0)
            {
                return GetActivePrefab();
            }
            
            int randomIndex = Random.Range(0, wallPrefabs.Count);
            return wallPrefabs[randomIndex];
        }

        /// <summary>
        /// Gets the appropriate rotation for a wall based on its orientation
        /// </summary>
        public Quaternion GetWallRotation(GridSystem.Orientation orientation)
        {
            if (boxForPrefabDebugMode)
            {
                // In debug mode, we still use scaling, so no rotation needed
                return Quaternion.identity;
            }

            Vector3 targetRotation = orientation == GridSystem.Orientation.Horizontal 
                                   ? horizontalRotation 
                                   : verticalRotation;
            
            return Quaternion.Euler(targetRotation);
        }

        /// <summary>
        /// Gets the scale for walls (used mainly in debug mode)
        /// </summary>
        public Vector3 GetWallScale(GridSystem.Orientation orientation)
        {
            if (!boxForPrefabDebugMode)
            {
                // In prefab mode, use the prefab's natural scale
                return Vector3.one;
            }

            // Debug mode: use the original scaling logic
            var settings = gridSystem.GetGridSettings();
            
            // Wall spans exactly 2 tiles plus the gap between them
            float wallLength = (settings.tileSize * 2f) + settings.tileGap;
            
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                // Horizontal wall: length in X direction, thickness in Y direction
                return new Vector3(wallLength, settings.wallThickness, settings.wallHeight);
            }
            else
            {
                // Vertical wall: thickness in X direction, length in Y direction
                return new Vector3(settings.wallThickness, wallLength, settings.wallHeight);
            }
        }

        void Update()
        {
            if (coordinateConverter == null) return;

            // debug hooks kept from original
            if (Input.GetKeyDown(KeyCode.Y)) placement.RunAutomaticWallTest();
            if (Input.GetKeyDown(KeyCode.T)) placement.TestWallBlocking();
            if (Input.GetKeyDown(KeyCode.G)) TestGapDetection(); // New gap detection test

            placement.Tick();
        }
        
        /// <summary>
        /// Test gap detection and lane system
        /// </summary>
        private void TestGapDetection()
        {
            Debug.Log("=== GAP DETECTION TEST ===");
            
            Vector3 mouseWorld = GetMouseWorld();
            Debug.Log($"Mouse world position: {mouseWorld}");
            
            // Test lane detection with current settings
            Debug.Log($"Current lane settings: laneSnapMargin={laneSnapMargin}, unlockMultiplier={unlockMultiplier}");
            
            // Simulate gap detection at mouse position
            var testWallInfo = placement.FindNearestWallGap(mouseWorld);
            if (testWallInfo.HasValue)
            {
                Debug.Log($"Nearest wall gap: {testWallInfo.Value.orientation} at ({testWallInfo.Value.x},{testWallInfo.Value.y})");
                Debug.Log($"Gap world position: {testWallInfo.Value.worldPosition}");
            }
            else
            {
                Debug.Log("No wall gap found at mouse position");
            }
        }
        
        private Vector3 GetMouseWorld()
        {
            var cam = Camera.main;
            if (!cam) return Vector3.zero;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.forward, new Vector3(0, 0, placementPlaneZ));
            return plane.Raycast(ray, out float enter) ? ray.GetPoint(enter)
                 : cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(cam.transform.position.z - placementPlaneZ)));
        }

        public bool TryPlaceWall(Vector3 worldPosition) => placement.TryPlaceWall(worldPosition);

        public void ClearAllWalls()
        {
            visuals.DestroyAll();
            
            // Clear managed walls
            for (int i = managedWalls.Count - 1; i >= 0; i--)
            {
                var wall = managedWalls[i];
                if (wall != null)
                {
                    WallState.SafeDestroy(wall);
                }
            }
            managedWalls.Clear();
            
            // Reset player walls
            if (gameManager != null)
            {
                foreach (var pawn in gameManager.pawns)
                {
                    pawn.wallsRemaining = gameManager.wallsPerPlayer;
                }
            }
            
            // Clear grid occupancy
            gridSystem?.ClearGrid();
        }

        void OnDestroy()
        {
            visuals.CleanupPreview();
            ClearAllWalls();
        }

        // Public API for wall management
        public void AddManagedWall(GameObject wall)
        {
            managedWalls.Add(wall);
        }
        
        public bool CanPlaceWall(GridSystem.Orientation orientation, int x, int y)
        {
            // Apply boundary constraints first
            Vector2Int constrainedPos = ApplyBoundaryConstraints(orientation, x, y);
            return gridSystem.CanPlaceWall(orientation, constrainedPos.x, constrainedPos.y);
        }
        
        /// <summary>
        /// CLEAN EVENT-DRIVEN: Wall placement with no direct game state management
        /// Only handles wall placement logic - event system handles game state changes
        /// </summary>
        public bool PlaceWall(GridSystem.Orientation orientation, int x, int y, Vector3 worldPos, Vector3 scale)
        {
            // Apply boundary constraints first
            Vector2Int constrainedPos = ApplyBoundaryConstraints(orientation, x, y);
            x = constrainedPos.x;
            y = constrainedPos.y;
            
            // Recalculate world position with constrained coordinates
            worldPos = GetWallWorldPosition(orientation, x, y);
            
            var wallInfo = new GridSystem.WallInfo(orientation, x, y, worldPos, scale);
            bool placed = gridSystem.PlaceWall(wallInfo);
            
            if (placed)
            {
                Debug.Log($"WallManager.PlaceWall: Wall placed successfully at {orientation} ({x},{y}) - OnWallPlaced event will handle game state");
            }
            else
            {
                Debug.LogWarning($"WallManager.PlaceWall: Failed to place wall at {orientation} ({x},{y})");
            }
            
            return placed;
        }
        
        /// <summary>
        /// Apply boundary constraints to ensure walls are always 2 units long and within bounds
        /// </summary>
        private Vector2Int ApplyBoundaryConstraints(GridSystem.Orientation orientation, int x, int y)
        {
            int gridSize = gridSystem.GetGridSize();
            
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                // Horizontal walls need 2 tiles horizontally (x spans from x to x+1)
                // Max x position is gridSize-2 to ensure x+1 is still within bounds
                int maxX = gridSize - 2;
                x = Mathf.Clamp(x, 0, maxX);
                
                // Horizontal walls can be placed at any y position from 0 to gridSize-2
                // (they separate row y from row y+1)
                int maxY = gridSize - 2;
                y = Mathf.Clamp(y, 0, maxY);
            }
            else // Vertical
            {
                // Vertical walls can be placed at any x position from 0 to gridSize-2  
                // (they separate column x from column x+1)
                int maxX = gridSize - 2;
                x = Mathf.Clamp(x, 0, maxX);
                
                // Vertical walls need 2 tiles vertically (y spans from y to y+1)
                // Max y position is gridSize-2 to ensure y+1 is still within bounds
                int maxY = gridSize - 2;
                y = Mathf.Clamp(y, 0, maxY);
            }
            
            return new Vector2Int(x, y);
        }
        
        public Vector3 GetWallWorldPosition(GridSystem.Orientation orientation, int x, int y)
        {
            // Apply boundary constraints first to ensure walls stay within bounds
            Vector2Int constrainedPos = ApplyBoundaryConstraints(orientation, x, y);
            x = constrainedPos.x;
            y = constrainedPos.y;
            
            // Calculate intersection point between tiles using proper grid coordinates
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                // Horizontal wall spans between tiles (x,y)-(x+1,y) and tiles (x,y+1)-(x+1,y+1)
                // It should be centered at the intersection between these tile pairs
                Vector2Int tile1 = new Vector2Int(x, y);
                Vector2Int tile2 = new Vector2Int(x + 1, y + 1);
                
                Vector3 pos1 = gridSystem.GridToWorldPosition(tile1);
                Vector3 pos2 = gridSystem.GridToWorldPosition(tile2);
                
                // Center point between the two diagonal tiles
                return new Vector3(
                    (pos1.x + pos2.x) / 2f,
                    (pos1.y + pos2.y) / 2f,
                    0f
                );
            }
            else
            {
                // Vertical wall spans between tiles (x,y)-(x,y+1) and tiles (x+1,y)-(x+1,y+1) 
                // It should be centered at the intersection between these tile pairs
                Vector2Int tile1 = new Vector2Int(x, y);
                Vector2Int tile2 = new Vector2Int(x + 1, y + 1);
                
                Vector3 pos1 = gridSystem.GridToWorldPosition(tile1);
                Vector3 pos2 = gridSystem.GridToWorldPosition(tile2);
                
                // Center point between the two diagonal tiles
                return new Vector3(
                    (pos1.x + pos2.x) / 2f,
                    (pos1.y + pos2.y) / 2f,
                    0f
                );
            }
        }
        
        // Public accessors for AI integration
        public WallValidator GetWallValidator() => validator;
        public WallVisuals GetWallVisuals() => visuals;
        public WallPlacementController GetPlacementController() => placement;
        public GridSystem GetGridSystem() => gridSystem;
        public List<GameObject> GetManagedWalls() => managedWalls;
        
        // Public accessors for gap detection settings (for WallPlacementController)
        public float GetGapSnapMargin() => gapSnapMargin;
        public float GetLaneSnapMargin() => laneSnapMargin;
        public float GetUnlockMultiplier() => unlockMultiplier;

        // Public accessors for prefab system
        public bool IsDebugMode() => boxForPrefabDebugMode;
        public Vector3 GetRotationAxis() => rotationAxis;
        public Vector3 GetHorizontalRotation() => horizontalRotation;
        public Vector3 GetVerticalRotation() => verticalRotation;
        
        // Legacy compatibility methods for AIOpponent
        [System.Obsolete("Use GetGridSystem().CanPlaceWall() instead")]
        public GapDetector GetGapDetector() 
        {
            Debug.LogWarning("GetGapDetector() is obsolete. Update AIOpponent to use unified GridSystem API.");
            return null; // Return null to force update to new API
        }
        
        [System.Obsolete("Use GetGridSystem() instead")]
        public WallState GetWallState()
        {
            Debug.LogWarning("GetWallState() is obsolete. Update AIOpponent to use unified GridSystem API.");
            return null; // Return null to force update to new API
        }
    }
}