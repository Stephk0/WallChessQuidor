using UnityEngine;
using WallChess.Core.States;

namespace WallChess.Core
{
    /// <summary>
    /// Coordinates the new FSM with the existing WallChessGameManager
    /// This allows gradual transition while maintaining compatibility
    /// Add this component to the same GameObject as WallChessGameManager
    /// </summary>
    public class GameStateController : MonoBehaviour
    {
        [Header("FSM Configuration")]
        [SerializeField] private bool enableFSM = true;
        [SerializeField] private bool debugFSM = true;
        
        [Header("References")]
        [SerializeField] private WallChessGameManager gameManager;
        [SerializeField] private StateMachine stateMachine;
        
        // State instances
        private GameplayState gameplayState;
        private PawnMovingState pawnMovingState;
        private WallPlacementState wallPlacementState;
        private GameOverState gameOverState;
        
        // Track legacy state to sync FSM
        private GameState lastLegacyState;
        
        void Awake()
        {
            // Auto-find components if not assigned
            if (gameManager == null)
                gameManager = GetComponent<WallChessGameManager>();
            
            if (stateMachine == null)
                stateMachine = GetComponent<StateMachine>();
            
            // Add StateMachine if it doesn't exist
            if (stateMachine == null)
            {
                stateMachine = gameObject.AddComponent<StateMachine>();
                Debug.Log("GameStateController: Added StateMachine component");
            }
        }
        
        void Start()
        {
            if (enableFSM)
            {
                InitializeFSM();
                SubscribeToEvents();
            }
            else
            {
                Debug.Log("GameStateController: FSM disabled, using legacy state management only");
            }
        }
        
        void InitializeFSM()
        {
            if (gameManager == null)
            {
                Debug.LogError("GameStateController: WallChessGameManager not found!");
                return;
            }
            
            // Create state instances
            gameplayState = new GameplayState(gameManager, stateMachine);
            pawnMovingState = new PawnMovingState(gameManager, stateMachine);
            wallPlacementState = new WallPlacementState(gameManager, stateMachine);
            gameOverState = new GameOverState(gameManager, stateMachine);
            
            // Register states with the state machine
            stateMachine.RegisterState(gameplayState);
            stateMachine.RegisterState(pawnMovingState);
            stateMachine.RegisterState(wallPlacementState);
            stateMachine.RegisterState(gameOverState);
            
            // Start with gameplay state
            stateMachine.ChangeState(gameplayState);
            lastLegacyState = gameManager.GetCurrentState();
            
            if (debugFSM)
                Debug.Log("GameStateController: FSM initialized and started in GameplayState");
        }
        
        void SubscribeToEvents()
        {
            // Subscribe to existing game manager events to coordinate state changes
            WallChessGameManager.OnPlayerTurnChanged += OnPlayerTurnChanged;
            WallChessGameManager.OnPlayerVictory += OnPlayerVictory;
        }
        
        void Update()
        {
            if (!enableFSM || gameManager == null) return;
            
            // Monitor legacy state changes and sync FSM if needed
            SyncWithLegacyState();
        }
        
        /// <summary>
        /// Sync FSM with legacy GameState enum changes
        /// </summary>
        void SyncWithLegacyState()
        {
            var currentLegacyState = gameManager.GetCurrentState();
            
            if (currentLegacyState != lastLegacyState)
            {
                if (debugFSM)
                    Debug.Log($"GameStateController: Legacy state changed from {lastLegacyState} to {currentLegacyState}");
                
                // Update FSM to match legacy state if needed
                SyncFSMToLegacyState(currentLegacyState);
                lastLegacyState = currentLegacyState;
            }
        }
        
        /// <summary>
        /// Ensure FSM state matches the legacy state
        /// </summary>
/// <summary>
        /// Ensure FSM state matches the legacy state - but don't interfere with turn management
        /// </summary>
        void SyncFSMToLegacyState(GameState legacyState)
        {
            if (debugFSM)
                Debug.Log($"GameStateController: Syncing FSM to legacy state: {legacyState}");
            
            switch (legacyState)
            {
                case GameState.PlayerTurn:
                    if (!stateMachine.IsInState<GameplayState>())
                    {
                        if (debugFSM) Debug.Log("GameStateController: Syncing FSM to GameplayState");
                        stateMachine.ChangeState<GameplayState>();
                    }
                    break;
                    
                case GameState.PawnMoving:
                    if (!stateMachine.IsInState<PawnMovingState>())
                    {
                        if (debugFSM) Debug.Log("GameStateController: Syncing FSM to PawnMovingState");
                        stateMachine.ChangeState<PawnMovingState>();
                    }
                    break;
                    
                case GameState.WallPlacement:
                    if (!stateMachine.IsInState<WallPlacementState>())
                    {
                        if (debugFSM) Debug.Log("GameStateController: Syncing FSM to WallPlacementState");
                        stateMachine.ChangeState<WallPlacementState>();
                    }
                    break;
                    
                case GameState.GameOver:
                    if (!stateMachine.IsInState<GameOverState>())
                    {
                        if (debugFSM) Debug.Log("GameStateController: Syncing FSM to GameOverState");
                        stateMachine.ChangeState<GameOverState>();
                    }
                    break;
            }
        }
        
        #region Event Handlers
private void OnPlayerTurnChanged(int playerIndex)
        {
            if (debugFSM)
                Debug.Log($"GameStateController: Player turn changed to {playerIndex} - FSM will sync passively");
            
            // DO NOT force FSM state transitions here!
            // Let the legacy system complete its turn change logic first
            // The SyncWithLegacyState() method will handle FSM synchronization
            
            if (debugFSM)
            {
                Debug.Log($"GameStateController: FSM State = {GetCurrentFSMState()}, Legacy State = {gameManager?.GetCurrentState()}");
            }
        }
        
        private void OnPlayerVictory(int winningPlayer)
        {
            if (debugFSM)
                Debug.Log($"GameStateController: Player {winningPlayer} victory detected");
            
            // Transition to game over state
            if (enableFSM)
            {
                stateMachine.ChangeState<GameOverState>();
            }
        }
        #endregion
        
        #region Public API for Integration
        /// <summary>
        /// Called when external systems initiate pawn movement
        /// </summary>
        public void OnPawnMoveInitiated()
        {
            if (enableFSM && stateMachine.IsInState<GameplayState>())
            {
                gameplayState.OnPawnMoveInitiated();
            }
        }
        
        /// <summary>
        /// Called when external systems complete pawn movement
        /// </summary>
        public void OnPawnMoveCompleted()
        {
            if (enableFSM && stateMachine.IsInState<PawnMovingState>())
            {
                pawnMovingState.OnPawnMoveCompleted();
            }
        }
        
        /// <summary>
        /// Called when external systems initiate wall placement
        /// </summary>
        public void OnWallPlacementInitiated()
        {
            if (enableFSM && stateMachine.IsInState<GameplayState>())
            {
                gameplayState.OnWallPlacementInitiated();
            }
        }
        
        /// <summary>
        /// Called when external systems complete wall placement
        /// </summary>
        public void OnWallPlacementCompleted()
        {
            if (enableFSM && stateMachine.IsInState<WallPlacementState>())
            {
                wallPlacementState.OnWallPlaced();
            }
        }
        
        /// <summary>
        /// Get current FSM state for debugging
        /// </summary>
        public string GetCurrentFSMState()
        {
            return stateMachine.CurrentState?.StateName ?? "None";
        }
        
        /// <summary>
        /// Toggle FSM on/off at runtime for testing
        /// </summary>
        public void SetFSMEnabled(bool enabled)
        {
            enableFSM = enabled;
            Debug.Log($"GameStateController: FSM {(enabled ? "enabled" : "disabled")}");
        }
        #endregion
        
        #region Debug Methods
        [ContextMenu("Debug/Print FSM State")]
        private void DebugPrintFSMState()
        {
            if (stateMachine != null)
            {
                Debug.Log($"FSM State: {GetCurrentFSMState()}, Legacy State: {gameManager?.GetCurrentState()}");
            }
        }
        
        [ContextMenu("Debug/Force Sync FSM")]
        private void DebugForceSyncFSM()
        {
            if (gameManager != null)
            {
                SyncFSMToLegacyState(gameManager.GetCurrentState());
                Debug.Log("Forced FSM sync with legacy state");
            }
        }
        #endregion
        
        void OnDestroy()
        {
            // Unsubscribe from events
            WallChessGameManager.OnPlayerTurnChanged -= OnPlayerTurnChanged;
            WallChessGameManager.OnPlayerVictory -= OnPlayerVictory;
        }
    

[ContextMenu("Debug/Disable FSM Temporarily")]
        private void DebugDisableFSM()
        {
            SetFSMEnabled(false);
            Debug.Log("GameStateController: FSM disabled for testing. Turn management will use legacy system only.");
        }
        
        [ContextMenu("Debug/Enable FSM")]
        private void DebugEnableFSM()
        {
            SetFSMEnabled(true);
            if (enableFSM && gameManager != null)
            {
                // Re-sync with current legacy state
                SyncFSMToLegacyState(gameManager.GetCurrentState());
            }
            Debug.Log("GameStateController: FSM re-enabled and synced with legacy state.");
        }
        
        [ContextMenu("Debug/Test Turn Change With FSM")]
        private void DebugTestTurnChangeWithFSM()
        {
            if (gameManager == null) return;
            
            Debug.Log($"=== TESTING TURN CHANGE WITH FSM ===");
            Debug.Log($"FSM Enabled: {enableFSM}");
            Debug.Log($"Current FSM State: {GetCurrentFSMState()}");
            Debug.Log($"Current Legacy State: {gameManager.GetCurrentState()}");
            Debug.Log($"Active Player Index: {gameManager.GetActivePawnIndex()}");
            
            Debug.Log($"\n--- Calling gameManager.EndTurn() ---");
            gameManager.EndTurn();
            
            Debug.Log($"\n--- AFTER EndTurn() ---");
            Debug.Log($"FSM State: {GetCurrentFSMState()}");
            Debug.Log($"Legacy State: {gameManager.GetCurrentState()}");
            Debug.Log($"Active Player Index: {gameManager.GetActivePawnIndex()}");
            Debug.Log($"=== TEST COMPLETE ===");
        }
}
}