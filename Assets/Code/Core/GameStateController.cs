using UnityEngine;
using WallChess.Core.States;

namespace WallChess.Core
{
    /// <summary>
    /// GameStateController - Fixed version with all required methods
    /// Manages detailed gameplay states (GameplayState, PawnMovingState, WallPlacementState)
    /// Works in coordination with SessionStateManager
    /// </summary>
    public class GameStateController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool enableFSM = false; // Start disabled, enable when session is ready
        [SerializeField] private bool debugFSM = true;
        
        [Header("References")]
        [SerializeField] private WallChessGameManager gameManager;
        [SerializeField] private StateMachine stateMachine;
        
        // State instances
        private GameplayState gameplayState;
        private PawnMovingState pawnMovingState;
        private WallPlacementState wallPlacementState;
        private GameOverState gameOverState;
        
        // Legacy state tracking
        private GameState lastLegacyState;
        
        void Awake()
        {
            InitializeComponents();
        }
        
        void InitializeComponents()
        {
            // Auto-find required components
            if (gameManager == null)
            {
                gameManager = GetComponent<WallChessGameManager>();
                if (gameManager == null)
                    gameManager = FindFirstObjectByType<WallChessGameManager>();
            }
            
            // IMPORTANT: GameStateController needs its own dedicated StateMachine
            // Don't interfere with SessionStateManager or MainApplicationController
            if (stateMachine == null)
            {
                stateMachine = GetComponent<StateMachine>();
            }
            
            // Always create a fresh StateMachine specifically for gameplay states
            if (stateMachine == null)
            {
                GameObject gameplaySMObject = new GameObject("GameplayStateMachine");
                gameplaySMObject.transform.SetParent(transform);
                stateMachine = gameplaySMObject.AddComponent<StateMachine>();
                Debug.Log("GameStateController: Created dedicated StateMachine for gameplay states");
            }
        }
        
void Start()
        {
            // ALWAYS initialize FSM states regardless of enableFSM flag
            // The flag only controls whether the FSM is active, not whether states exist
            InitializeFSM();
            
            if (enableFSM)
            {
                SubscribeToEvents();
                Debug.Log("GameStateController: FSM initialized and enabled");
            }
            else
            {
                Debug.Log("GameStateController: FSM initialized but disabled, ready to enable when needed");
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
            
            // IMPORTANT: Don't start state immediately - wait for SessionStateManager
            // The SessionStateManager will notify us when to start gameplay states
            
            lastLegacyState = gameManager.GetCurrentState();
            
            if (debugFSM)
                Debug.Log("GameStateController: FSM initialized, waiting for session ready signal");
        }
        
        #region State Management Methods
        
        /// <summary>
        /// Start gameplay states when session is ready for active gameplay
        /// Called by SessionStateManager when reaching ActiveGameplay state
        /// </summary>
public void StartGameplayStates()
        {
            // Ensure FSM is initialized if it wasn't already
            if (gameplayState == null)
            {
                Debug.LogWarning("GameStateController: States not initialized, initializing now...");
                InitializeFSM();
            }
            
            if (stateMachine == null || gameplayState == null)
            {
                Debug.LogError("GameStateController: Cannot start gameplay states - initialization failed");
                return;
            }
            
            // Enable FSM and subscribe to events if not already done
            if (!enableFSM)
            {
                enableFSM = true;
                SubscribeToEvents();
            }
            
            // Start with gameplay state
            stateMachine.ChangeState(gameplayState);
            
            if (debugFSM)
                Debug.Log("GameStateController: Enabled FSM and started gameplay states");
        }

        /// <summary>
        /// Stop gameplay states when session ends
        /// </summary>
        public void StopGameplayStates()
        {
            enableFSM = false;
            
            if (stateMachine != null)
            {
                // Could transition to a null state or just stop the state machine
                if (debugFSM)
                    Debug.Log("GameStateController: Disabled FSM and stopped gameplay states");
            }
        }
        
        #endregion
        
        #region Event Handling
        
        void SubscribeToEvents()
        {
            // Subscribe to game manager events for FSM coordination
            if (debugFSM)
                Debug.Log("GameStateController: Event subscriptions initialized");
        }
        
        #endregion
        
        #region Legacy State Integration
        
        void Update()
        {
            if (!enableFSM || gameManager == null) return;
            
            // Monitor legacy state and sync with FSM if needed
            GameState currentLegacyState = gameManager.GetCurrentState();
            
            if (currentLegacyState != lastLegacyState)
            {
                HandleLegacyStateChange(lastLegacyState, currentLegacyState);
                lastLegacyState = currentLegacyState;
            }
        }
        
        void HandleLegacyStateChange(GameState oldState, GameState newState)
        {
            if (!enableFSM) return;
            
            // Map legacy states to FSM states
            switch (newState)
            {
                case GameState.PlayerTurn:
                    if (!stateMachine.IsInState<GameplayState>())
                    {
                        if (debugFSM) Debug.Log($"GameStateController: Legacy {newState} -> GameplayState");
                        stateMachine.ChangeState<GameplayState>();
                    }
                    break;
                    
                case GameState.PawnMoving:
                    if (!stateMachine.IsInState<PawnMovingState>())
                    {
                        if (debugFSM) Debug.Log($"GameStateController: Legacy {newState} -> PawnMovingState");
                        stateMachine.ChangeState<PawnMovingState>();
                    }
                    break;
                    
                case GameState.WallPlacement:
                    if (!stateMachine.IsInState<WallPlacementState>())
                    {
                        if (debugFSM) Debug.Log($"GameStateController: Legacy {newState} -> WallPlacementState");
                        stateMachine.ChangeState<WallPlacementState>();
                    }
                    break;
                    
                case GameState.GameOver:
                    if (!stateMachine.IsInState<GameOverState>())
                    {
                        if (debugFSM) Debug.Log($"GameStateController: Legacy {newState} -> GameOverState");
                        stateMachine.ChangeState<GameOverState>();
                    }
                    break;
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get current state name
        /// </summary>
        public string GetCurrentStateName()
        {
            return stateMachine.CurrentState?.StateName ?? "None";
        }
        
        /// <summary>
        /// Get current FSM state name (alias for GetCurrentStateName for backward compatibility)
        /// </summary>
        public string GetCurrentFSMState()
        {
            return GetCurrentStateName();
        }
        
        /// <summary>
        /// Handle pawn move initiation - transitions to PawnMovingState
        /// </summary>
        public void OnPawnMoveInitiated()
        {
            if (!enableFSM || stateMachine == null) return;
            
            if (debugFSM)
                Debug.Log("GameStateController: Pawn move initiated - transitioning to PawnMovingState");
                
            stateMachine.ChangeState<PawnMovingState>();
        }
        
        /// <summary>
        /// Handle pawn move completion - transitions back to GameplayState
        /// </summary>
        public void OnPawnMoveCompleted()
        {
            if (!enableFSM || stateMachine == null) return;
            
            if (debugFSM)
                Debug.Log("GameStateController: Pawn move completed - transitioning to GameplayState");
                
            stateMachine.ChangeState<GameplayState>();
        }
        
        /// <summary>
        /// Handle wall placement initiation - transitions to WallPlacementState
        /// </summary>
        public void OnWallPlacementInitiated()
        {
            if (!enableFSM || stateMachine == null) return;
            
            if (debugFSM)
                Debug.Log("GameStateController: Wall placement initiated - transitioning to WallPlacementState");
                
            stateMachine.ChangeState<WallPlacementState>();
        }
        
        /// <summary>
        /// Handle wall placement completion - transitions back to GameplayState
        /// </summary>
        public void OnWallPlacementCompleted()
        {
            if (!enableFSM || stateMachine == null) return;
            
            if (debugFSM)
                Debug.Log("GameStateController: Wall placement completed - transitioning to GameplayState");
                
            stateMachine.ChangeState<GameplayState>();
        }
        
        /// <summary>
        /// Set FSM enabled state
        /// </summary>
        public void SetFSMEnabled(bool enabled)
        {
            enableFSM = enabled;
            
            if (debugFSM)
                Debug.Log($"GameStateController: FSM {(enabled ? "enabled" : "disabled")}");
        }
        
        #endregion
    }
}
