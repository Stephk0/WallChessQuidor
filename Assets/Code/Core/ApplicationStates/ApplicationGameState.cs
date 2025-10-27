using UnityEngine;
using WallChess.Core;
using WallChess.Core.States;
using GameStateEnum = WallChess.Core.GameState;

namespace WallChess.Core.ApplicationStates
{
    /// <summary>
    /// Main game state - handles the actual gameplay session
    /// This wraps and coordinates with the existing gameplay FSM
    /// (PlayerTurn, PawnMoving, WallPlacement states)
    /// </summary>
    public class ApplicationGameState : IState
    {
        public string StateName => "Game";
        
        private readonly StateMachine stateMachine;
        private readonly WallChessGameManager gameManager;
        private WallChess.Core.GameStateController gameplayController;
        
        private bool gameInitialized = false;
        private bool gameStarted = false;
        
        public ApplicationGameState(StateMachine stateMachine, WallChessGameManager gameManager)
        {
            this.stateMachine = stateMachine;
            this.gameManager = gameManager;
        }
        
        public void OnEnter()
        {
            Debug.Log("ApplicationGameState: Entered game session");
            
            InitializeGameSession();
            ShowGameUI();
        }
        
        public void OnUpdate()
        {
            // Check for game completion or pause requests
            HandleGameInput();
            
            // Monitor for victory condition
            CheckForGameEnd();
        }
        
        public void OnExit()
        {
            Debug.Log("ApplicationGameState: Leaving game session");
            HideGameUI();
            
            // Clean up game session if needed
            CleanupGameSession();
        }
        
        private void InitializeGameSession()
        {
            if (gameManager == null)
            {
                Debug.LogError("ApplicationGameState: WallChessGameManager is null!");
                return;
            }
            
            // Get or add the gameplay controller
            gameplayController = gameManager.GetComponent<WallChess.Core.GameStateController>();
            if (gameplayController == null)
            {
                gameplayController = gameManager.gameObject.AddComponent<WallChess.Core.GameStateController>();
                Debug.Log("ApplicationGameState: Added GameStateController component");
            }
            
            // Initialize PawnManager
            InitializePawnManager();
            
            // Initialize/restart the game
            RestartGame();
            
            gameInitialized = true;
        }
        
        /// <summary>
        /// Initialize the PawnManager for the game session
        /// </summary>
        private void InitializePawnManager()
        {
            var pawnManager = gameManager.GetComponent<WallChess.Gameplay.Pawns.PawnManager>();
            if (pawnManager == null)
            {
                pawnManager = gameManager.gameObject.AddComponent<WallChess.Gameplay.Pawns.PawnManager>();
                Debug.Log("ApplicationGameState: Added PawnManager component");
            }
            
            // Subscribe to pawn events for state transitions
            WallChess.Gameplay.Pawns.PawnManager.OnActivePawnChanged += HandleActivePawnChanged;
            WallChess.Gameplay.Pawns.PawnManager.OnPawnMoved += HandlePawnMoved;
            
            Debug.Log("ApplicationGameState: PawnManager initialized");
        }
        
        /// <summary>
        /// Handle pawn manager events
        /// </summary>
        private void HandleActivePawnChanged(int pawnIndex)
        {
            Debug.Log($"ApplicationGameState: Active pawn changed to {pawnIndex}");
            
            // Could trigger UI updates, camera focus changes, etc.
        }
        
        private void HandlePawnMoved(WallChess.Gameplay.Pawns.Pawn pawn)
        {
            Debug.Log($"ApplicationGameState: Pawn moved - {pawn.PlayerData.playerName} to {pawn.CurrentPosition}");
            
            // Check for victory condition
            if (pawn.HasWon())
            {
                Debug.Log($"ApplicationGameState: Victory detected for {pawn.PlayerData.playerName}!");
                TransitionToGameOver();
            }
        }
        
        private void RestartGame()
        {
            Debug.Log("ApplicationGameState: Starting new game session...");
            
            // Clean up any existing game state first
            gameManager.CleanupGameSession();
            
            // Initialize the game
            gameManager.InitializeGame();
            
            gameStarted = true;
            
            Debug.Log($"ApplicationGameState: Game session started with {gameManager.numberOfPlayers} players");
        }
        
        private void ShowGameUI()
        {
            // Future: Show game-specific UI elements
            // - Player turn indicator
            // - Wall count display
            // - Pause button
            // - Score/timer if applicable
            Debug.Log("ApplicationGameState: Game UI shown");
        }
        
        private void HideGameUI()
        {
            // Future: Hide game UI elements
            Debug.Log("ApplicationGameState: Game UI hidden");
        }
        
        private void HandleGameInput()
        {
            // Handle pause/menu requests
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseGame();
            }
            
            // Debug: Force return to menu
            if (Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("ApplicationGameState: Debug - Forcing return to menu");
                ReturnToMenu();
            }
        }
        
        private void CheckForGameEnd()
        {
            // The game manager handles victory detection and triggers events
            // We subscribe to those events to transition states
            if (gameManager != null && gameManager.GetCurrentState() == GameStateEnum.GameOver)
            {
                // Transition to application-level game over state
                TransitionToGameOver();
            }
        }
        
        /// <summary>
        /// Pause the game and return to menu (future: pause menu)
        /// </summary>
        public void PauseGame()
        {
            Debug.Log("ApplicationGameState: Game paused - returning to menu");
            // Future: Implement proper pause state
            ReturnToMenu();
        }
        
        /// <summary>
        /// End the current game and return to main menu
        /// </summary>
        public void ReturnToMenu()
        {
            Debug.Log("ApplicationGameState: Returning to main menu...");
            stateMachine.ChangeState<MenuState>();
        }
        
        /// <summary>
        /// Transition to game over state when someone wins
        /// </summary>
        private void TransitionToGameOver()
        {
            Debug.Log("ApplicationGameState: Game completed - transitioning to game over");
            stateMachine.ChangeState<ApplicationGameOverState>();
        }
        
        /// <summary>
        /// Restart the current game session
        /// </summary>
        public void RestartCurrentGame()
        {
            Debug.Log("ApplicationGameState: Restarting current game...");
            RestartGame();
        }
        
        private void CleanupGameSession()
        {
            // Unsubscribe from pawn events
            WallChess.Gameplay.Pawns.PawnManager.OnActivePawnChanged -= HandleActivePawnChanged;
            WallChess.Gameplay.Pawns.PawnManager.OnPawnMoved -= HandlePawnMoved;
            
            // Clean up pawn manager
            var pawnManager = gameManager?.GetComponent<WallChess.Gameplay.Pawns.PawnManager>();
            if (pawnManager != null)
            {
                pawnManager.Cleanup();
            }
            
            // Future: Save game statistics, clean up temporary objects, etc.
            Debug.Log("ApplicationGameState: Game session cleaned up");
        }
        
        /// <summary>
        /// Get the current game progress/status for UI
        /// </summary>
        public string GetGameStatus()
        {
            if (gameManager == null) return "No game manager";
            
            var activePawn = gameManager.GetActivePawn();
            if (activePawn == null) return "No active player";
            
            return $"Player {gameManager.GetActivePawnIndex() + 1}'s turn - {activePawn.wallsRemaining} walls remaining";
        }
    }
}