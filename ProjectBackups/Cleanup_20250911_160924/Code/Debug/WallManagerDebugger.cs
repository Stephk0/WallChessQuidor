using UnityEngine;
using WallChess;
using WallChess.Core;

namespace WallChess.Diagnostics
{
    /// <summary>
    /// Debug script to diagnose WallManager issues after FSM integration
    /// </summary>
    public class WallManagerDebugger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WallChessGameManager gameManager;
        [SerializeField] private WallManager wallManager;
        [SerializeField] private GameStateController stateController;
        [SerializeField] private StateMachine stateMachine;

        [Header("Debug Options")]
        [SerializeField] private bool autoFindReferences = true;
        [SerializeField] private bool debugOnStart = true;

        void Start()
        {
            if (autoFindReferences)
            {
                FindReferences();
            }

            if (debugOnStart)
            {
                RunDiagnostics();
            }
        }

        void FindReferences()
        {
            if (gameManager == null) gameManager = FindObjectOfType<WallChessGameManager>();
            if (wallManager == null) wallManager = FindObjectOfType<WallManager>();
            if (stateController == null) stateController = FindObjectOfType<GameStateController>();
            if (stateMachine == null) stateMachine = FindObjectOfType<StateMachine>();

            Debug.Log("WallManagerDebugger: References found automatically");
        }

        [ContextMenu("Run Diagnostics")]
        void RunDiagnostics()
        {
            Debug.Log("=== WALL MANAGER FSM DIAGNOSTICS ===");
            
            // Check basic references
            Debug.Log($"GameManager found: {gameManager != null}");
            Debug.Log($"WallManager found: {wallManager != null}");
            Debug.Log($"StateController found: {stateController != null}");
            Debug.Log($"StateMachine found: {stateMachine != null}");

            if (gameManager == null)
            {
                Debug.LogError("CRITICAL: WallChessGameManager not found!");
                return;
            }

            if (wallManager == null)
            {
                Debug.LogError("CRITICAL: WallManager not found!");
                return;
            }

            // Check initialization status
            Debug.Log($"GameManager Current State: {gameManager.GetCurrentState()}");
            Debug.Log($"GameManager Active Player: {gameManager.GetActivePawnIndex()}");
            
            if (stateController != null)
            {
                Debug.Log($"FSM Current State: {stateController.GetCurrentFSMState()}");
            }

            // Check WallManager components
            var gridSystem = gameManager.GetGridSystem();
            Debug.Log($"GridSystem found: {gridSystem != null}");
            
            var validator = wallManager.GetWallValidator();
            Debug.Log($"WallValidator found: {validator != null}");
            
            var placementController = wallManager.GetPlacementController();
            Debug.Log($"PlacementController found: {placementController != null}");

            // Test wall placement capability
            TestWallPlacementCapability();
        }

        void TestWallPlacementCapability()
        {
            Debug.Log("--- Testing Wall Placement Capability ---");
            
            if (gameManager == null || wallManager == null)
            {
                Debug.LogError("Cannot test - missing core components");
                return;
            }

            var activePawn = gameManager.GetActivePawn();
            if (activePawn == null)
            {
                Debug.LogError("No active pawn found");
                return;
            }

            Debug.Log($"Active player has {activePawn.wallsRemaining} walls remaining");
            Debug.Log($"Can place walls: {gameManager.CurrentPlayerHasWalls()}");

            // Test a simple wall placement validation
            var gridSystem = gameManager.GetGridSystem();
            if (gridSystem != null)
            {
                bool canPlaceHorizontal = wallManager.CanPlaceWall(GridSystem.Orientation.Horizontal, 1, 1);
                bool canPlaceVertical = wallManager.CanPlaceWall(GridSystem.Orientation.Vertical, 1, 1);
                
                Debug.Log($"Can place horizontal wall at (1,1): {canPlaceHorizontal}");
                Debug.Log($"Can place vertical wall at (1,1): {canPlaceVertical}");
            }
        }

        [ContextMenu("Test Wall Placement")]
        void TestWallPlacement()
        {
            if (wallManager == null)
            {
                Debug.LogError("WallManager not found");
                return;
            }

            // Try to place a wall at a specific position
            Vector3 testPosition = new Vector3(1f, 1f, 0f);
            bool success = wallManager.TryPlaceWall(testPosition);
            Debug.Log($"Wall placement test at {testPosition}: {(success ? "SUCCESS" : "FAILED")}");
        }

        [ContextMenu("Check FSM Integration")]
        void CheckFSMIntegration()
        {
            Debug.Log("--- FSM Integration Check ---");
            
            if (stateController == null)
            {
                Debug.LogWarning("No GameStateController found - FSM not integrated");
                return;
            }

            Debug.Log($"FSM State: {stateController.GetCurrentFSMState()}");
            Debug.Log($"Legacy State: {gameManager?.GetCurrentState()}");

            // Test state transition
            Debug.Log("Testing wall placement state transition...");
            stateController.OnWallPlacementInitiated();
            
            Debug.Log($"After wall placement initiation - FSM State: {stateController.GetCurrentFSMState()}");
        }
    }
}