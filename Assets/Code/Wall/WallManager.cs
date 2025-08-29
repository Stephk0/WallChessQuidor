using UnityEngine;
using System.Collections.Generic;
using WallChess.Grid;

namespace WallChess
{
    /// <summary>
    /// Unified WallManager that uses GridSystem as single source of truth for occupancy.
    /// No more dual tracking systems - everything goes through GridSystem.
    /// </summary>
    public class WallManager : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private Material wallMaterial;
        [SerializeField] private GameObject wallPrefab;

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

            // Initialize unified systems that use GridSystem as source of truth
            validator = new WallValidator(gridSystem, gameManager);
            visuals = new WallVisuals(wallPrefab, wallMaterial, validPreviewColor, invalidPreviewColor, placingPreviewColor);
            placement = new WallPlacementController(this, gameManager, gridSystem, validator, visuals, placementPlaneZ);

            Debug.Log("WallManager initialized with unified GridSystem integration");
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
        
        public bool PlaceWall(GridSystem.Orientation orientation, int x, int y, Vector3 worldPos, Vector3 scale)
        {
            // Apply boundary constraints first
            Vector2Int constrainedPos = ApplyBoundaryConstraints(orientation, x, y);
            x = constrainedPos.x;
            y = constrainedPos.y;
            
            // Recalculate world position with constrained coordinates
            worldPos = GetWallWorldPosition(orientation, x, y);
            
            var wallInfo = new GridSystem.WallInfo(orientation, x, y, worldPos, scale);
            return gridSystem.PlaceWall(wallInfo);
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
        
        public Vector3 GetWallScale(GridSystem.Orientation orientation)
        {
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