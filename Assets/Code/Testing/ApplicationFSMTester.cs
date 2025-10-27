using UnityEngine;
using WallChess.Core;
using WallChess.Core.ApplicationStates;

namespace WallChess.Testing
{
    /// <summary>
    /// Test script to demonstrate and test the new Application FSM flow
    /// Attach this to the same GameObject as MainApplicationController for testing
    /// 
    /// Controls:
    /// - P: Start new game
    /// - S: Open settings
    /// - M: Return to menu
    /// - Escape: Context-sensitive back/quit
    /// - R: Restart game (when in game over)
    /// - V: Simulate victory (for testing)
    /// </summary>
    public class ApplicationFSMTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool enableTestUI = true;
        [SerializeField] private bool showDebugInfo = true;
        
        private MainApplicationController appController;
        private WallChessGameManager gameManager;
        
        void Awake()
        {
            appController = GetComponent<MainApplicationController>();
            gameManager = GetComponent<WallChessGameManager>();
        }
        
        void Start()
        {
            if (enableTestUI)
            {
                Debug.Log("=== Application FSM Tester Started ===");
                Debug.Log("Controls:");
                Debug.Log("P - Start new game");
                Debug.Log("S - Open settings");
                Debug.Log("M - Return to menu");
                Debug.Log("Escape - Context-sensitive back/quit");
                Debug.Log("R - Restart game (when in game over)");
                Debug.Log("V - Simulate victory (for testing)");
                Debug.Log("=====================================");
            }
        }
        
        void Update()
        {
            if (appController == null) return;
            
            HandleTestInput();
            
            if (showDebugInfo && Input.GetKeyDown(KeyCode.Tab))
            {
                PrintDebugInfo();
            }
        }
        
        void HandleTestInput()
        {
            // Global controls
            if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("[TEST] Starting new game...");
                appController.StartNewGame();
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("[TEST] Opening settings...");
                appController.OpenSettings();
            }
            
            if (Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("[TEST] Returning to main menu...");
                appController.ReturnToMainMenu();
            }
            
            if (Input.GetKeyDown(KeyCode.V))
            {
                Debug.Log("[TEST] Simulating victory...");
                SimulateVictory();
            }
            
            // Context-sensitive Escape handling is handled by individual states
        }
        
        void SimulateVictory()
        {
            if (gameManager != null && appController.IsInGameState())
            {
                // Trigger the victory event manually for testing
                WallChessGameManager.OnPlayerVictory?.Invoke(0);
            }
            else
            {
                Debug.Log("[TEST] Cannot simulate victory - not in game state");
            }
        }
        
        void PrintDebugInfo()
        {
            Debug.Log("=== FSM Debug Info ===");
            Debug.Log($"Application State: {appController.GetCurrentApplicationState()}");
            
            if (gameManager != null)
            {
                Debug.Log($"Game Manager State: {gameManager.GetCurrentState()}");
                Debug.Log($"Active Player: {gameManager.GetActivePawnIndex()}");
                Debug.Log($"Number of Players: {gameManager.numberOfPlayers}");
                Debug.Log($"Grid Size: {gameManager.gridSize}x{gameManager.gridSize}");
            }
            
            Debug.Log($"In Game State: {appController.IsInGameState()}");
            Debug.Log($"In Menu State: {appController.IsInMenuState()}");
            Debug.Log("====================");
        }
        
        void OnGUI()
        {
            if (!enableTestUI) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Application FSM Tester");
            
            if (appController != null)
            {
                GUILayout.Label($"App State: {appController.GetCurrentApplicationState()}");
                
                if (gameManager != null)
                {
                    GUILayout.Label($"Game State: {gameManager.GetCurrentState()}");
                    GUILayout.Label($"Active Player: {gameManager.GetActivePawnIndex()}");
                }
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("Start New Game (P)"))
                {
                    appController.StartNewGame();
                }
                
                if (GUILayout.Button("Open Settings (S)"))
                {
                    appController.OpenSettings();
                }
                
                if (GUILayout.Button("Return to Menu (M)"))
                {
                    appController.ReturnToMainMenu();
                }
                
                if (appController.IsInGameState() && GUILayout.Button("Simulate Victory (V)"))
                {
                    SimulateVictory();
                }
            }
            else
            {
                GUILayout.Label("MainApplicationController not found!");
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        #region Context Menu Debug Methods
        
        [ContextMenu("Test/Print FSM State")]
        private void TestPrintFSMState()
        {
            PrintDebugInfo();
        }
        
        [ContextMenu("Test/Force Start Game")]
        private void TestForceStartGame()
        {
            if (appController != null)
                appController.StartNewGame();
        }
        
        [ContextMenu("Test/Force Return to Menu")]
        private void TestForceReturnToMenu()
        {
            if (appController != null)
                appController.ReturnToMainMenu();
        }
        
        [ContextMenu("Test/Simulate Victory")]
        private void TestSimulateVictory()
        {
            SimulateVictory();
        }
        
        #endregion
    }
}