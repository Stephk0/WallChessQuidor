using UnityEngine;
using WallChess.Core;
using WallChess.Core.States;

namespace WallChess.Testing
{
    /// <summary>
    /// Demo script to test FSM integration with WallChessGameManager
    /// Add this to the same GameObject as WallChessGameManager and GameStateController
    /// </summary>
    public class FSMIntegrationDemo : MonoBehaviour
    {
        [Header("References")]
        public WallChessGameManager gameManager;
        public GameStateController gameStateController;
        public StateMachine stateMachine;
        
        [Header("Demo Settings")]
        public bool autoTest = false;
        public float testInterval = 3f;
        
        private float testTimer;
        private int testStep = 0;
        
        void Start()
        {
            // Auto-find components if not assigned
            if (gameManager == null)
                gameManager = GetComponent<WallChessGameManager>();
                
            if (gameStateController == null)
                gameStateController = GetComponent<GameStateController>();
                
            if (stateMachine == null)
                stateMachine = GetComponent<StateMachine>();
            
            // Subscribe to game events to show FSM coordination
            WallChessGameManager.OnPlayerTurnChanged += OnPlayerTurnChanged;
            WallChessGameManager.OnPlayerVictory += OnPlayerVictory;
            
            Debug.Log("FSMIntegrationDemo: Initialized - monitoring FSM integration");
        }
        
        void Update()
        {
            if (autoTest)
            {
                testTimer += Time.deltaTime;
                if (testTimer >= testInterval)
                {
                    RunAutoTest();
                    testTimer = 0f;
                }
            }
        }
        
        void RunAutoTest()
        {
            if (gameManager == null || gameStateController == null) return;
            
            Debug.Log("FSMIntegrationDemo: Auto-test step " + testStep);
            Debug.Log("Current Legacy State: " + gameManager.GetCurrentState());
            Debug.Log("Current FSM State: " + gameStateController.GetCurrentFSMState());
            
            testStep++;
            if (testStep > 3) testStep = 0;
        }
        
        #region Event Handlers
        private void OnPlayerTurnChanged(int playerIndex)
        {
            Debug.Log("FSMIntegrationDemo: Player turn changed to " + playerIndex);
            string legacyState = gameManager?.GetCurrentState().ToString() ?? "N/A";
            string fsmState = gameStateController?.GetCurrentFSMState() ?? "N/A";
            Debug.Log("Legacy State: " + legacyState + ", FSM State: " + fsmState);
        }
        
        private void OnPlayerVictory(int winningPlayer)
        {
            Debug.Log("FSMIntegrationDemo: Player " + winningPlayer + " won!");
            string legacyState = gameManager?.GetCurrentState().ToString() ?? "N/A";
            string fsmState = gameStateController?.GetCurrentFSMState() ?? "N/A";
            Debug.Log("Legacy State: " + legacyState + ", FSM State: " + fsmState);
        }
        #endregion
        
        #region Context Menu Tests
        [ContextMenu("Demo/Test FSM State Sync")]
        private void TestFSMStateSync()
        {
            if (gameManager == null || gameStateController == null)
            {
                Debug.LogError("Required components not found");
                return;
            }
            
            Debug.Log("=== FSM State Sync Test ===");
            Debug.Log("Legacy State: " + gameManager.GetCurrentState());
            Debug.Log("FSM State: " + gameStateController.GetCurrentFSMState());
            Debug.Log("Active Player: " + gameManager.GetActivePawnIndex());
            
            var activePawn = gameManager.GetActivePawn();
            if (activePawn != null)
            {
                Debug.Log("Active Pawn Position: " + activePawn.position + ", Walls: " + activePawn.wallsRemaining);
            }
        }
        
        [ContextMenu("Demo/Simulate Pawn Move")]
        private void SimulatePawnMove()
        {
            if (gameManager == null) return;
            
            Debug.Log("=== Simulating Pawn Move ===");
            Debug.Log("Before: Legacy=" + gameManager.GetCurrentState() + ", FSM=" + (gameStateController?.GetCurrentFSMState() ?? "N/A"));
            
            // This would normally be called by PlayerControllerV2
            gameStateController?.OnPawnMoveInitiated();
            
            Debug.Log("During Move: Legacy=" + gameManager.GetCurrentState() + ", FSM=" + (gameStateController?.GetCurrentFSMState() ?? "N/A"));
            
            // Simulate move completion after a short delay
            Invoke(nameof(CompletePawnMoveTest), 0.5f);
        }
        
        private void CompletePawnMoveTest()
        {
            gameStateController?.OnPawnMoveCompleted();
            Debug.Log("After Move: Legacy=" + gameManager.GetCurrentState() + ", FSM=" + (gameStateController?.GetCurrentFSMState() ?? "N/A"));
        }
        
        [ContextMenu("Demo/Simulate Wall Placement")]
        private void SimulateWallPlacement()
        {
            if (gameManager == null) return;
            
            Debug.Log("=== Simulating Wall Placement ===");
            Debug.Log("Before: Legacy=" + gameManager.GetCurrentState() + ", FSM=" + (gameStateController?.GetCurrentFSMState() ?? "N/A"));
            
            // This would normally be called by WallManager
            gameStateController?.OnWallPlacementInitiated();
            
            Debug.Log("During Placement: Legacy=" + gameManager.GetCurrentState() + ", FSM=" + (gameStateController?.GetCurrentFSMState() ?? "N/A"));
            
            // Simulate placement completion after a short delay
            Invoke(nameof(CompleteWallPlacementTest), 0.5f);
        }
        
        private void CompleteWallPlacementTest()
        {
            gameStateController?.OnWallPlacementCompleted();
            Debug.Log("After Placement: Legacy=" + gameManager.GetCurrentState() + ", FSM=" + (gameStateController?.GetCurrentFSMState() ?? "N/A"));
        }
        
        [ContextMenu("Demo/Print Current Game Info")]
        private void PrintCurrentGameInfo()
        {
            if (gameManager == null)
            {
                Debug.LogError("GameManager not found");
                return;
            }
            
            Debug.Log("=== Current Game State ===");
            Debug.Log("Legacy State: " + gameManager.GetCurrentState());
            Debug.Log("Legacy Action: " + gameManager.GetCurrentAction());
            Debug.Log("FSM State: " + (gameStateController?.GetCurrentFSMState() ?? "N/A"));
            Debug.Log("Active Player: " + gameManager.GetActivePawnIndex());
            Debug.Log("Total Players: " + (gameManager.pawns?.Count ?? 0));
            Debug.Log("Debug Mode: " + gameManager.debugMode);
            
            var activePawn = gameManager.GetActivePawn();
            if (activePawn != null)
            {
                Debug.Log("Active Pawn - Position: " + activePawn.position + ", Walls: " + activePawn.wallsRemaining);
            }
        }
        #endregion
        
        void OnDestroy()
        {
            // Unsubscribe from events
            WallChessGameManager.OnPlayerTurnChanged -= OnPlayerTurnChanged;
            WallChessGameManager.OnPlayerVictory -= OnPlayerVictory;
        }
    }
}