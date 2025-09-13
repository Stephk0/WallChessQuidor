using UnityEngine;
using WallChess.Grid;

namespace WallChess
{
    /// <summary>
    /// Debug script to test pathfinding visualization functionality
    /// Can be added to any GameObject to test the visualization system
    /// </summary>
    public class PathfindingVisualizationTester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WallManager wallManager;
        [SerializeField] private GridSystem gridSystem;
        [SerializeField] private WallChessGameManager gameManager;
        
        [Header("Test Settings")]
        [SerializeField] private bool autoFindComponents = true;
        [SerializeField] private bool showDebugInfo = true;
        
        void Start()
        {
            if (autoFindComponents)
            {
                FindComponents();
            }
            
            if (showDebugInfo)
            {
                ShowSystemInfo();
            }
        }
        
        void Update()
        {
            HandleDebugInput();
        }
        
        private void FindComponents()
        {
            if (wallManager == null)
            {
                wallManager = FindObjectOfType<WallManager>();
                if (wallManager != null)
                    Debug.Log("PathfindingVisualizationTester: Found WallManager");
            }
            
            if (gridSystem == null)
            {
                gridSystem = FindObjectOfType<GridSystem>();
                if (gridSystem != null)
                    Debug.Log("PathfindingVisualizationTester: Found GridSystem");
            }
            
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<WallChessGameManager>();
                if (gameManager != null)
                    Debug.Log("PathfindingVisualizationTester: Found WallChessGameManager");
            }
        }
        
        private void ShowSystemInfo()
        {
            Debug.Log("=== PATHFINDING VISUALIZATION TEST ===");
            Debug.Log($"WallManager: {(wallManager != null ? "Found" : "Missing")}");
            Debug.Log($"GridSystem: {(gridSystem != null ? "Found" : "Missing")}");
            Debug.Log($"GameManager: {(gameManager != null ? "Found" : "Missing")}");
            
            if (wallManager != null)
            {
                var visualizer = wallManager.GetPathfindingVisualizer();
                Debug.Log($"PathfindingVisualizer: {(visualizer != null ? "Active" : "Not initialized")}");
                
                if (visualizer != null)
                {
                    Debug.Log($"Current Debug Mode: {visualizer.CurrentDebugMode}");
                    Debug.Log($"Visualization Active: {visualizer.IsVisualizationActive}");
                }
            }
            
            Debug.Log("=== CONTROLS ===");
            Debug.Log("P - Toggle pathfinding visualization on/off");
            Debug.Log("O - Cycle through debug modes (Off, Pawn1Only, Pawn2Only, BothPawns)");
            Debug.Log("R - Refresh visualization");
            Debug.Log("I - Show system info");
            Debug.Log("==============================");
        }
        
        private void HandleDebugInput()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                ShowSystemInfo();
            }
            
            if (wallManager != null)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    Debug.Log("Manual toggle pathfinding visualization...");
                    wallManager.TogglePathfindingVisualization();
                }
                
                if (Input.GetKeyDown(KeyCode.O))
                {
                    Debug.Log("Manual cycle debug mode...");
                    wallManager.CyclePathfindingDebugMode();
                }
                
                if (Input.GetKeyDown(KeyCode.R))
                {
                    Debug.Log("Manual refresh visualization...");
                    wallManager.RefreshPathfindingVisualization();
                }
                
                // Additional test controls
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    Debug.Log("Setting debug mode to Pawn1Only...");
                    wallManager.SetPathfindingDebugMode(GridPathfindingVisualizer.DebugMode.Pawn1Only);
                }
                
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    Debug.Log("Setting debug mode to Pawn2Only...");
                    wallManager.SetPathfindingDebugMode(GridPathfindingVisualizer.DebugMode.Pawn2Only);
                }
                
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    Debug.Log("Setting debug mode to BothPawns...");
                    wallManager.SetPathfindingDebugMode(GridPathfindingVisualizer.DebugMode.BothPawns);
                }
                
                if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    Debug.Log("Setting debug mode to Off...");
                    wallManager.SetPathfindingDebugMode(GridPathfindingVisualizer.DebugMode.Off);
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.R))
                {
                    Debug.LogWarning("WallManager not found! Make sure the WallManager component exists in the scene.");
                }
            }
        }
        
        [ContextMenu("Test Pathfinding Visualization")]
        public void TestPathfindingVisualization()
        {
            if (wallManager == null)
            {
                Debug.LogError("Cannot test: WallManager not found!");
                return;
            }
            
            Debug.Log("Testing pathfinding visualization...");
            
            // Enable visualization
            wallManager.TogglePathfindingVisualization();
            
            // Cycle through all modes
            wallManager.SetPathfindingDebugMode(GridPathfindingVisualizer.DebugMode.Pawn1Only);
            wallManager.RefreshPathfindingVisualization();
            
            Debug.Log("Pathfinding visualization test completed. Check the scene view for colored spheres.");
        }
        
        [ContextMenu("Show Available Paths")]
        public void ShowAvailablePaths()
        {
            if (gridSystem == null || gameManager == null)
            {
                Debug.LogError("Cannot show paths: GridSystem or GameManager not found!");
                return;
            }
            
            Debug.Log("=== AVAILABLE PATHS ANALYSIS ===");
            
            if (gameManager.pawns != null && gameManager.pawns.Count >= 2)
            {
                var pawn1 = gameManager.pawns[0];
                var pawn2 = gameManager.pawns[1];
                
                Debug.Log($"Pawn 1 Position: {pawn1.position}");
                Debug.Log($"Pawn 2 Position: {pawn2.position}");
                
                // Test pathfinding for pawn 1 to top row
                int gridSize = gridSystem.GetGridSize();
                bool pawn1HasPath = false;
                bool pawn2HasPath = false;
                
                for (int x = 0; x < gridSize; x++)
                {
                    Vector2Int goal1 = new Vector2Int(x, gridSize - 1); // Top row
                    Vector2Int goal2 = new Vector2Int(x, 0); // Bottom row
                    
                    if (GridPathfinder.PathExists(gridSystem, pawn1.position, goal1))
                    {
                        pawn1HasPath = true;
                        var path = GridPathfinder.FindPath(gridSystem, pawn1.position, goal1);
                        Debug.Log($"Pawn 1 can reach goal {goal1} in {path.Count} moves");
                        break;
                    }
                    
                    if (GridPathfinder.PathExists(gridSystem, pawn2.position, goal2))
                    {
                        pawn2HasPath = true;
                        var path = GridPathfinder.FindPath(gridSystem, pawn2.position, goal2);
                        Debug.Log($"Pawn 2 can reach goal {goal2} in {path.Count} moves");
                        break;
                    }
                }
                
                Debug.Log($"Pawn 1 has valid path to win: {pawn1HasPath}");
                Debug.Log($"Pawn 2 has valid path to win: {pawn2HasPath}");
            }
            else
            {
                Debug.LogWarning("Not enough pawns available for path analysis");
            }
            
            Debug.Log("=============================");
        }
    }
}
