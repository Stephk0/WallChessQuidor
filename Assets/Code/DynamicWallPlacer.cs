using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    public class DynamicWallPlacer : MonoBehaviour
    {
        #region Dependencies
        [Header("Dependencies")]
        [SerializeField] private WallChessGameManager gameManager;
        [SerializeField] private GridSystem gridSystem;
        
        [Header("Auto-Connect")]
        [SerializeField] private bool autoConnectToGameManager = true;
        #endregion

        #region State
        private GameObject wallPreview;
        private Renderer wallPreviewRenderer;
        private bool isPlacing = false;
        private GridSystem.Orientation? orientationLock = null;
        
        [Header("Wall Settings")]
        public int wallsLeft = 9;
        
        // Track placed wall GameObjects for cleanup
        private List<GameObject> placedWallObjects = new List<GameObject>(32);
        #endregion

        #region Inspector Settings
        [Header("Placement Behavior")]
        public float gapSnapMargin = 0.25f;
        public float laneSnapMargin = 0.3f;
        public float unlockMultiplier = 1.5f;

        [Header("Visual Settings")]
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material previewMaterial;

        [Header("Input Settings")]
        public LayerMask placementMask = ~0;
        public float placementPlaneZ = 0f;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            if (autoConnectToGameManager)
            {
                ConnectToManagers();
            }
        }

        void Start()
        {
            if (gridSystem != null)
            {
                SyncWithGridSystem();
            }
        }

        void Update()
        {
            if (!CanAcceptInput()) return;

            if (Input.GetMouseButtonDown(0) && wallsLeft > 0)
            {
                TryStartPlacing();
            }
            else if (Input.GetMouseButton(0) && isPlacing)
            {
                UpdatePreview();
            }
            else if (Input.GetMouseButtonUp(0) && isPlacing)
            {
                PlaceWall();
            }
        }
        #endregion

        #region Initialization and Dependencies
        private void ConnectToManagers()
        {
            if (gameManager == null)
                gameManager = FindObjectOfType<WallChessGameManager>();
                
            if (gridSystem == null && gameManager != null)
                gridSystem = gameManager.GetGridSystem();
        }

        public void Initialize(WallChessGameManager gm, GridSystem gs)
        {
            gameManager = gm;
            gridSystem = gs;
            SyncWithGridSystem();
        }

        public void SyncWithGridSystem()
        {
            if (gridSystem == null)
            {
                Debug.LogWarning("DynamicWallPlacer: No GridSystem reference found!");
                return;
            }

            if (gameManager != null)
            {
                wallsLeft = gameManager.wallsPerPlayer;
            }

            // Subscribe to grid system events
            gridSystem.OnWallPlaced += OnWallPlacedInGrid;
            gridSystem.OnGridCleared += OnGridCleared;
            
            Debug.Log($"DynamicWallPlacer synced with GridSystem: {gridSystem.GetGridSize()}x{gridSystem.GetGridSize()}");
        }
        #endregion

        #region Public API
        public int GetWallsLeft() => wallsLeft;
        public void SetWallsLeft(int count) => wallsLeft = Mathf.Max(0, count);
        
        public void ClearWalls()
        {
            // Destroy visual wall objects
            for (int i = placedWallObjects.Count - 1; i >= 0; i--)
            {
                var wallObj = placedWallObjects[i];
                if (wallObj != null) 
                    SafeDestroy(wallObj);
                placedWallObjects.RemoveAt(i);
            }

            // Clear grid system walls
            if (gridSystem != null)
                gridSystem.ClearGrid();

            if (gameManager != null)
                wallsLeft = gameManager.wallsPerPlayer;
        }

        public GridSystem.WallInfo? GetNearestWallInfo(Vector3 worldPosition)
        {
            if (gridSystem == null) return null;
            return gridSystem.FindNearestValidWall(worldPosition, gapSnapMargin);
        }
        #endregion

        #region Input Validation
        private bool CanAcceptInput()
        {
            if (gridSystem == null) return false;
            if (gameManager != null && !gameManager.IsPlayerTurn()) return false;
            return true;
        }
        #endregion

        #region Placement Flow
        void TryStartPlacing()
        {
            Vector3 mousePos = GetMouseWorld();
            if (gridSystem.IsWithinGridBounds(mousePos))
            {
                isPlacing = true;
                CreatePreview();
            }
        }

        void CreatePreview()
        {
            if (wallPreview == null)
            {
                wallPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallPreview.name = "WallPreview";
                wallPreviewRenderer = wallPreview.GetComponent<Renderer>();
                
                if (previewMaterial != null)
                    wallPreviewRenderer.material = previewMaterial;
                else
                    wallPreviewRenderer.material.color = new Color(1, 1, 0, 0.5f);
                
                var collider = wallPreview.GetComponent<Collider>();
                SafeDestroy(collider);
            }
        }

        void UpdatePreview()
        {
            if (wallPreview == null) return;

            Vector3 mousePos = GetMouseWorld();
            if (!gridSystem.IsWithinGridBounds(mousePos)) 
            { 
                wallPreview.SetActive(false); 
                return; 
            }

            var nearestWall = gridSystem.FindNearestValidWall(mousePos, gapSnapMargin);
            if (!nearestWall.HasValue)
            {
                wallPreview.SetActive(false);
                return;
            }

            var wallInfo = nearestWall.Value;
            wallPreview.SetActive(true);
            wallPreview.transform.position = wallInfo.worldPosition;
            wallPreview.transform.localScale = wallInfo.scale;

            bool canPlace = gridSystem.CanPlaceWall(wallInfo);
            wallPreviewRenderer.material.color = canPlace 
                ? new Color(0, 1, 0, 0.7f)
                : new Color(1, 0, 0, 0.7f);
        }

        void PlaceWall()
        {
            if (wallPreview != null && wallPreview.activeInHierarchy)
            {
                Vector3 mousePos = GetMouseWorld();
                var nearestWall = gridSystem.FindNearestValidWall(mousePos, gapSnapMargin);
                
                if (nearestWall.HasValue && gridSystem.CanPlaceWall(nearestWall.Value))
                {
                    var wallInfo = nearestWall.Value;
                    
                    // Place wall in grid system
                    if (gridSystem.PlaceWall(wallInfo))
                    {
                        // Convert preview to actual wall
                        ConvertPreviewToWall(wallInfo);
                        wallsLeft--;
                        
                        Debug.Log($"Wall placed at ({wallInfo.x},{wallInfo.y}) {wallInfo.orientation}! Remaining: {wallsLeft}");
                        
                        // End turn if this was the game manager
                        if (gameManager != null)
                        {
                            gameManager.EndTurn();
                        }
                    }
                }
                else
                {
                    SafeDestroy(wallPreview);
                }
            }
            else if (wallPreview != null)
            {
                SafeDestroy(wallPreview);
            }
            
            wallPreview = null;
            wallPreviewRenderer = null;
            orientationLock = null;
            isPlacing = false;
        }

        private void ConvertPreviewToWall(GridSystem.WallInfo wallInfo)
        {
            if (wallPreview != null)
            {
                // Update preview to look like a real wall
                wallPreview.name = "Wall";
                wallPreview.tag = "Wall";
                
                if (wallMaterial != null)
                    wallPreviewRenderer.material = wallMaterial;
                else
                    wallPreviewRenderer.material.color = Color.yellow;
                
                // Track this wall object
                placedWallObjects.Add(wallPreview);
            }
        }
        #endregion

        #region Event Handlers
        private void OnWallPlacedInGrid(GridSystem.WallInfo wallInfo)
        {
            // This is called when a wall is placed via the GridSystem
            // We may not need to do anything here if we're the one placing it
        }

        private void OnGridCleared()
        {
            // Grid was cleared, reset our wall count
            if (gameManager != null)
                wallsLeft = gameManager.wallsPerPlayer;
        }
        #endregion

        #region Input & Camera
        Vector3 GetMouseWorld()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, placementPlaneZ));
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            Vector3 mouse = Input.mousePosition;
            mouse.z = Mathf.Abs(cam.transform.position.z - placementPlaneZ);
            return cam.ScreenToWorldPoint(mouse);
        }
        #endregion

        #region Utilities
        void SafeDestroy(Object obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) 
                DestroyImmediate(obj);
            else 
                Destroy(obj);
#else
            Destroy(obj);
#endif
        }
        #endregion

        #region Debug and Editor
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (gridSystem == null) return;

            var gridSettings = gridSystem.GetGridSettings();
            float spacing = gridSettings.TileSpacing;
            int gridSize = gridSettings.gridSize;

            // Draw grid bounds
            Gizmos.color = Color.cyan;
            Vector3 min = new Vector3(-spacing * 0.5f, -spacing * 0.5f, 0f);
            Vector3 max = new Vector3(gridSize * spacing + spacing * 0.5f,
                                      gridSize * spacing + spacing * 0.5f, 0f);
            Vector3 size = max - min;
            Gizmos.DrawWireCube(min + size * 0.5f, size);

            // Draw current mouse position if placing
            if (isPlacing)
            {
                Vector3 mousePos = GetMouseWorld();
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(mousePos, 0.1f);
            }
        }
#endif
        #endregion

        void OnDestroy()
        {
            // Unsubscribe from events
            if (gridSystem != null)
            {
                gridSystem.OnWallPlaced -= OnWallPlacedInGrid;
                gridSystem.OnGridCleared -= OnGridCleared;
            }
        }
    }
}
