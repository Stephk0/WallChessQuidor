using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WallChess.Grid;

namespace WallChess
{
    /// <summary>
    /// Unified WallManager that uses GridSystem as single source of truth for occupancy.
    /// REFACTORED: Now uses on-demand pathfinding validation when walls are placed.
    /// No more pre-calculation - validates paths immediately using temporary wall placement.
    /// 
    /// Pathfinding Visualization Controls:
    /// - P: Toggle pathfinding visualization on/off
    /// - O: Cycle through debug modes (Off, Pawn1Only, Pawn2Only, BothPawns)
    /// - R: Refresh visualization
    /// - M: Test smooth rotation on existing walls
    /// 
    /// Visualization Colors:
    /// - Green: Valid path tiles
    /// - Red: Inaccessible tiles
    /// - Magenta: Last known accessible tiles
    /// - Blue: Pawn2 specific paths (in BothPawns mode)
    /// - Cyan blend: Tiles accessible by both pawns
    /// </summary>
    public class WallManager : MonoBehaviour
    {
        [Header("Debug & Development")]
        [SerializeField] private bool boxForPrefabDebugMode = false;
        [Tooltip("When enabled, uses primitive boxes scaled to fit gaps instead of prefabs")]
        
        [Header("Wall Prefabs")]
        [SerializeField] private List<GameObject> wallPrefabs = new List<GameObject>();
        [Tooltip("List of wall prefabs to randomly choose from for placement")]
        
        [Header("Rotation Animation")]
        [SerializeField] private float rotationLerpDuration = 0.3f;
        [Tooltip("Duration in seconds for smooth rotation transitions")]
        [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Animation curve for rotation interpolation")]
        [SerializeField] private bool enableRotationLerp = true;
        [Tooltip("Enable smooth rotation animation for wall prefabs")]
        
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
        [SerializeField] private float laneSnapMargin = 0.5f;
        [SerializeField] private float unlockMultiplier = 1.8f;
        
        [Header("Pathfinding Debug Visualization")]
        [SerializeField] private bool enablePathfindingVisualization = false;
        [Tooltip("Enable pathfinding visualization during wall placement")]
        [SerializeField] private GridPathfindingVisualizer.DebugMode pathfindingDebugMode = GridPathfindingVisualizer.DebugMode.BothPawns;
        [Tooltip("Which pawns to show pathfinding visualization for")]
        [SerializeField] private bool updateVisualizationOnWallPlacement = true;
        [Tooltip("Automatically update pathfinding visualization when walls are placed")]

        private WallChessGameManager gameManager;
        private GridSystem gridSystem;
        private GridCoordinateConverter coordinateConverter;
        private WallValidator validator;
        private WallVisuals visuals;
        private WallPlacementController placement;
        private GridPathfindingVisualizer pathfindingVisualizer;
        private List<GameObject> managedWalls = new List<GameObject>();
        private Dictionary<GameObject, Coroutine> rotationCoroutines = new Dictionary<GameObject, Coroutine>();

        public void Initialize(WallChessGameManager gm)
        {
            gameManager = gm;
            gridSystem = gameManager != null ? gameManager.GetGridSystem() : null;

            if (gridSystem == null)
            {
                Debug.LogError("WallManager: GridSystem not found! WallManager requires GridSystem to be initialized first.");
                return;
            }

            coordinateConverter = GetCoordinateConverterFromGrid();
            if (coordinateConverter == null)
            {
                Debug.LogError("WallManager: Could not get coordinate converter from GridSystem!");
                return;
            }

            ValidatePrefabSetup();

            validator = new WallValidator(gridSystem, gameManager);
            visuals = new WallVisuals(GetActivePrefab(), GetActiveMaterial(), validPreviewColor, invalidPreviewColor, placingPreviewColor);
            visuals.SetWallManager(this);
            placement = new WallPlacementController(this, gameManager, gridSystem, validator, visuals, placementPlaneZ);
            
            InitializePathfindingVisualizer();

            Debug.Log($"WallManager initialized with on-demand pathfinding validation. Debug mode: {boxForPrefabDebugMode}, Pathfinding visualization: {enablePathfindingVisualization}");
        }

        private GridCoordinateConverter GetCoordinateConverterFromGrid()
        {
            var field = typeof(GridSystem).GetField("coordinateConverter", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                return (GridCoordinateConverter)field.GetValue(gridSystem);
            }

            var settings = gridSystem.GetGridSettings();
            var alignment = gridSystem.GetGridAlignment();
            return new GridCoordinateConverter(settings.TileSpacing, settings.gridSize, alignment);
        }

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
        
        private void InitializePathfindingVisualizer()
        {
            if (!enablePathfindingVisualization) return;
            
            GameObject visualizerGO = new GameObject("PathfindingVisualizer");
            visualizerGO.transform.parent = transform;
            
            pathfindingVisualizer = visualizerGO.AddComponent<GridPathfindingVisualizer>();
            pathfindingVisualizer.Initialize(gridSystem, gameManager);
            pathfindingVisualizer.SetDebugMode(pathfindingDebugMode);
            
            Debug.Log($"PathfindingVisualizer initialized with mode: {pathfindingDebugMode}");
        }

        private GameObject GetActivePrefab()
        {
            if (boxForPrefabDebugMode)
            {
                return wallPrefab;
            }
            
            if (wallPrefabs != null && wallPrefabs.Count > 0)
            {
                return wallPrefabs[0];
            }
            
            Debug.LogWarning("WallManager: No valid prefabs available, falling back to debug prefab.");
            return wallPrefab;
        }

        private Material GetActiveMaterial()
        {
            if (boxForPrefabDebugMode)
            {
                return wallMaterial;
            }
            return null;
        }

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
                return Quaternion.identity;
            }

            Vector3 targetRotation = orientation == GridSystem.Orientation.Horizontal 
                                   ? horizontalRotation 
                                   : verticalRotation;
            
            return Quaternion.Euler(targetRotation);
        }
        
        /// <summary>
        /// Applies smooth rotation to a wall GameObject with lerping
        /// </summary>
        public void ApplySmoothRotation(GameObject wallObject, GridSystem.Orientation newOrientation)
        {
            Debug.Log($"ApplySmoothRotation called for {wallObject?.name} to {newOrientation}. Lerp enabled: {enableRotationLerp}, Debug mode: {boxForPrefabDebugMode}");
            
            if (wallObject == null || !enableRotationLerp || boxForPrefabDebugMode)
            {
                if (wallObject != null)
                    wallObject.transform.rotation = GetWallRotation(newOrientation);
                return;
            }
            
            if (rotationCoroutines.ContainsKey(wallObject))
            {
                if (rotationCoroutines[wallObject] != null)
                    StopCoroutine(rotationCoroutines[wallObject]);
                rotationCoroutines.Remove(wallObject);
            }
            
            Coroutine rotationCoroutine = StartCoroutine(LerpWallRotation(wallObject, newOrientation));
            rotationCoroutines[wallObject] = rotationCoroutine;
        }
        
        /// <summary>
        /// Coroutine for smoothly rotating a wall to its target orientation
        /// </summary>
        private System.Collections.IEnumerator LerpWallRotation(GameObject wallObject, GridSystem.Orientation targetOrientation)
        {
            if (wallObject == null) yield break;
            
            Quaternion startRotation = wallObject.transform.rotation;
            Quaternion targetRotation = GetWallRotation(targetOrientation);
            
            Debug.Log($"LerpWallRotation started for {wallObject?.name} from {startRotation.eulerAngles} to {targetRotation.eulerAngles}");
            
            if (Quaternion.Angle(startRotation, targetRotation) < 0.1f)
            {
                rotationCoroutines.Remove(wallObject);
                yield break;
            }
            
            float elapsed = 0f;
            
            while (elapsed < rotationLerpDuration)
            {
                if (wallObject == null)
                {
                    rotationCoroutines.Remove(wallObject);
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                float t = elapsed / rotationLerpDuration;
                float curveValue = rotationCurve.Evaluate(t);
                
                wallObject.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);
                
                yield return null;
            }
            
            if (wallObject != null)
            {
                wallObject.transform.rotation = targetRotation;
            }
            
            rotationCoroutines.Remove(wallObject);
        }
        
        /// <summary>
        /// Stops all rotation animations and cleans up coroutines
        /// </summary>
        public void StopAllRotationAnimations()
        {
            foreach (var kvp in rotationCoroutines)
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
            }
            rotationCoroutines.Clear();
        }

        public Vector3 GetWallScale(GridSystem.Orientation orientation)
        {
            if (!boxForPrefabDebugMode)
            {
                return Vector3.one;
            }

            var settings = gridSystem.GetGridSettings();
            float wallLength = (settings.tileSize * 2f) + settings.tileGap;
            
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                return new Vector3(wallLength, settings.wallThickness, settings.wallHeight);
            }
            else
            {
                return new Vector3(settings.wallThickness, wallLength, settings.wallHeight);
            }
        }

        void Update()
        {
            if (coordinateConverter == null) return;

            if (Input.GetKeyDown(KeyCode.Y)) placement.RunAutomaticWallTest();
            if (Input.GetKeyDown(KeyCode.T)) placement.TestWallBlocking();
            if (Input.GetKeyDown(KeyCode.G)) TestGapDetection();
            if (Input.GetKeyDown(KeyCode.V)) validator.DebugValidateGameState();
            if (Input.GetKeyDown(KeyCode.B)) validator.DebugPrintAllPawnPaths();
            
            if (Input.GetKeyDown(KeyCode.P)) TogglePathfindingVisualization();
            if (Input.GetKeyDown(KeyCode.O)) CyclePathfindingDebugMode();
            if (Input.GetKeyDown(KeyCode.R)) RefreshPathfindingVisualization();
            if (Input.GetKeyDown(KeyCode.M)) TestSmoothRotation();

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
            Debug.Log($"Current lane settings: laneSnapMargin={laneSnapMargin}, unlockMultiplier={unlockMultiplier}");
            
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
        
        /// <summary>
        /// Test method to manually trigger smooth rotation on a wall (for debugging)
        /// Press 'M' key to test rotation
        /// </summary>
        private void TestSmoothRotation()
        {
            Debug.Log("=== TESTING SMOOTH ROTATION ===");
            
            // Find any existing wall in the scene to test rotation
            if (managedWalls != null && managedWalls.Count > 0)
            {
                GameObject testWall = managedWalls[0];
                if (testWall != null)
                {
                    Debug.Log($"Testing smooth rotation on wall: {testWall.name}");
                    Debug.Log($"Rotation settings - Duration: {rotationLerpDuration}s, Enabled: {enableRotationLerp}, Debug Mode: {boxForPrefabDebugMode}");
                    
                    // Toggle between horizontal and vertical for testing
                    bool useVertical = Time.frameCount % 2 == 0; // Simple toggle based on frame count
                    GridSystem.Orientation newOrientation = useVertical ? GridSystem.Orientation.Vertical : GridSystem.Orientation.Horizontal;
                    
                    Debug.Log($"Applying smooth rotation to {newOrientation}");
                    
                    // Test the smooth rotation
                    ApplySmoothRotation(testWall, newOrientation);
                }
                else
                {
                    Debug.LogWarning("TestSmoothRotation: Found wall reference but GameObject is null");
                }
            }
            else
            {
                Debug.LogWarning("TestSmoothRotation: No walls found to test rotation on. Place a wall first.");
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
            
            for (int i = managedWalls.Count - 1; i >= 0; i--)
            {
                var wall = managedWalls[i];
                if (wall != null)
                {
                    WallState.SafeDestroy(wall);
                }
            }
            managedWalls.Clear();
            
            if (gameManager != null)
            {
                foreach (var pawn in gameManager.pawns)
                {
                    pawn.wallsRemaining = gameManager.wallsPerPlayer;
                }
            }
            
            gridSystem?.ClearGrid();
            
            if (pathfindingVisualizer != null && enablePathfindingVisualization)
            {
                pathfindingVisualizer.RefreshVisualization();
            }
        }

        void OnDestroy()
        {
            visuals.CleanupPreview();
            StopAllRotationAnimations();
            ClearAllWalls();
            
            if (pathfindingVisualizer != null)
            {
                if (pathfindingVisualizer.gameObject != null)
                {
                    DestroyImmediate(pathfindingVisualizer.gameObject);
                }
                pathfindingVisualizer = null;
            }
        }

        // Public API
        public void AddManagedWall(GameObject wall)
        {
            managedWalls.Add(wall);
        }
        
        public bool CanPlaceWall(GridSystem.Orientation orientation, int x, int y)
        {
            Vector2Int constrainedPos = ApplyBoundaryConstraints(orientation, x, y);
            return gridSystem.CanPlaceWall(orientation, constrainedPos.x, constrainedPos.y);
        }
        
        public bool PlaceWall(GridSystem.Orientation orientation, int x, int y, Vector3 worldPos, Vector3 scale)
        {
            Vector2Int constrainedPos = ApplyBoundaryConstraints(orientation, x, y);
            x = constrainedPos.x;
            y = constrainedPos.y;
            
            worldPos = GetWallWorldPosition(orientation, x, y);
            
            var wallInfo = new GridSystem.WallInfo(orientation, x, y, worldPos, scale);
            bool placed = gridSystem.PlaceWall(wallInfo);
            
            if (placed)
            {
                Debug.Log($"WallManager.PlaceWall: Wall placed successfully at {orientation} ({x},{y}) - OnWallPlaced event will handle game state");
                
                if (!validator.ValidateAllPawnPaths())
                {
                    Debug.LogError($"CRITICAL: Wall placement at {orientation} ({x},{y}) resulted in invalid game state!");
                    validator.DebugPrintAllPawnPaths();
                }
                
                if (updateVisualizationOnWallPlacement && pathfindingVisualizer != null)
                {
                    pathfindingVisualizer.RefreshVisualization();
                }
            }
            else
            {
                Debug.LogWarning($"WallManager.PlaceWall: Failed to place wall at {orientation} ({x},{y})");
            }
            
            return placed;
        }
        
        private Vector2Int ApplyBoundaryConstraints(GridSystem.Orientation orientation, int x, int y)
        {
            int gridSize = gridSystem.GetGridSize();
            
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                int maxX = gridSize - 2;
                x = Mathf.Clamp(x, 0, maxX);
                int maxY = gridSize - 2;
                y = Mathf.Clamp(y, 0, maxY);
            }
            else
            {
                int maxX = gridSize - 2;
                x = Mathf.Clamp(x, 0, maxX);
                int maxY = gridSize - 2;
                y = Mathf.Clamp(y, 0, maxY);
            }
            
            return new Vector2Int(x, y);
        }
        
        public Vector3 GetWallWorldPosition(GridSystem.Orientation orientation, int x, int y)
        {
            Vector2Int constrainedPos = ApplyBoundaryConstraints(orientation, x, y);
            x = constrainedPos.x;
            y = constrainedPos.y;
            
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                Vector2Int tile1 = new Vector2Int(x, y);
                Vector2Int tile2 = new Vector2Int(x + 1, y + 1);
                
                Vector3 pos1 = gridSystem.GridToWorldPosition(tile1);
                Vector3 pos2 = gridSystem.GridToWorldPosition(tile2);
                
                return new Vector3(
                    (pos1.x + pos2.x) / 2f,
                    (pos1.y + pos2.y) / 2f,
                    0f
                );
            }
            else
            {
                Vector2Int tile1 = new Vector2Int(x, y);
                Vector2Int tile2 = new Vector2Int(x + 1, y + 1);
                
                Vector3 pos1 = gridSystem.GridToWorldPosition(tile1);
                Vector3 pos2 = gridSystem.GridToWorldPosition(tile2);
                
                return new Vector3(
                    (pos1.x + pos2.x) / 2f,
                    (pos1.y + pos2.y) / 2f,
                    0f
                );
            }
        }
        
        // Pathfinding Visualization Controls
        public void TogglePathfindingVisualization()
        {
            enablePathfindingVisualization = !enablePathfindingVisualization;
            
            if (enablePathfindingVisualization)
            {
                if (pathfindingVisualizer == null)
                {
                    InitializePathfindingVisualizer();
                }
                else
                {
                    pathfindingVisualizer.SetDebugMode(pathfindingDebugMode);
                }
                Debug.Log("Pathfinding visualization enabled");
            }
            else
            {
                if (pathfindingVisualizer != null)
                {
                    pathfindingVisualizer.SetDebugMode(GridPathfindingVisualizer.DebugMode.Off);
                }
                Debug.Log("Pathfinding visualization disabled");
            }
        }
        
        public void CyclePathfindingDebugMode()
        {
            if (pathfindingVisualizer != null)
            {
                pathfindingVisualizer.ToggleDebugMode();
                pathfindingDebugMode = pathfindingVisualizer.CurrentDebugMode;
                Debug.Log($"Pathfinding debug mode: {pathfindingDebugMode}");
            }
            else if (enablePathfindingVisualization)
            {
                InitializePathfindingVisualizer();
                CyclePathfindingDebugMode();
            }
            else
            {
                Debug.Log("Enable pathfinding visualization first (Press P)");
            }
        }
        
        public void RefreshPathfindingVisualization()
        {
            if (pathfindingVisualizer != null && enablePathfindingVisualization)
            {
                pathfindingVisualizer.RefreshVisualization();
                Debug.Log("Pathfinding visualization refreshed");
            }
            else
            {
                Debug.Log("Pathfinding visualization not active");
            }
        }
        
        public void SetPathfindingDebugMode(GridPathfindingVisualizer.DebugMode mode)
        {
            pathfindingDebugMode = mode;
            if (pathfindingVisualizer != null)
            {
                pathfindingVisualizer.SetDebugMode(mode);
            }
        }
        
        // Public accessors
        public WallValidator GetWallValidator() => validator;
        public WallVisuals GetWallVisuals() => visuals;
        public WallPlacementController GetPlacementController() => placement;
        public GridSystem GetGridSystem() => gridSystem;
        public List<GameObject> GetManagedWalls() => managedWalls;
        public GridPathfindingVisualizer GetPathfindingVisualizer() => pathfindingVisualizer;
        
        public float GetGapSnapMargin() => gapSnapMargin;
        public float GetLaneSnapMargin() => laneSnapMargin;
        public float GetUnlockMultiplier() => unlockMultiplier;

        // Public accessors for rotation animation settings
        public float GetRotationLerpDuration() => rotationLerpDuration;
        public AnimationCurve GetRotationCurve() => rotationCurve;
        public bool IsRotationLerpEnabled() => enableRotationLerp;

        public bool IsDebugMode() => boxForPrefabDebugMode;
        public Vector3 GetRotationAxis() => rotationAxis;
        public Vector3 GetHorizontalRotation() => horizontalRotation;
        public Vector3 GetVerticalRotation() => verticalRotation;
        
        // Legacy compatibility methods
        [System.Obsolete("Use GetGridSystem().CanPlaceWall() instead")]
        public GapDetector GetGapDetector() 
        {
            Debug.LogWarning("GetGapDetector() is obsolete. Update AIOpponent to use unified GridSystem API.");
            return null;
        }
        
        [System.Obsolete("Use GetGridSystem() instead")]
        public WallState GetWallState()
        {
            Debug.LogWarning("GetWallState() is obsolete. Update AIOpponent to use unified GridSystem API.");
            return null;
        }

        public int GetCurrentPlayerWallsRemaining()
        {
            if (gameManager == null) return 0;
            
            var activePawn = gameManager.GetActivePawn();
            return activePawn?.wallsRemaining ?? 0;
        }
        
        public int GetWallsPerPlayer()
        {
            return gameManager?.wallsPerPlayer ?? 9;
        }
        
        public int GetPlayerWallsRemaining(int playerIndex)
        {
            if (gameManager == null || playerIndex < 0 || playerIndex >= gameManager.pawns.Count)
                return 0;
                
            return gameManager.pawns[playerIndex].wallsRemaining;
        }
        
        public bool CanCurrentPlayerPlaceWalls()
        {
            if (gameManager == null) return false;
            
            var activePawn = gameManager.GetActivePawn();
            bool hasWalls = activePawn != null && activePawn.wallsRemaining > 0;
            bool gameAllows = gameManager.CanPlaceWalls();
            
            return hasWalls && gameAllows;
        }
    }
}