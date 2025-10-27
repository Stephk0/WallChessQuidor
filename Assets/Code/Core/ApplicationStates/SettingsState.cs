using UnityEngine;

namespace WallChess.Core.ApplicationStates
{
    /// <summary>
    /// Settings/Pre-session state - handles game configuration before starting
    /// Players can adjust game settings, player count, AI difficulty, etc.
    /// </summary>
    public class SettingsState : IState
    {
        public string StateName => "Settings";
        
        private readonly StateMachine stateMachine;
        private readonly WallChessGameManager gameManager;
        
        public SettingsState(StateMachine stateMachine, WallChessGameManager gameManager)
        {
            this.stateMachine = stateMachine;
            this.gameManager = gameManager;
        }
        
        public void OnEnter()
        {
            Debug.Log("SettingsState: Entered settings menu");
            ShowSettingsUI();
        }
        
        public void OnUpdate()
        {
            HandleSettingsInput();
        }
        
        public void OnExit()
        {
            Debug.Log("SettingsState: Leaving settings menu");
            HideSettingsUI();
        }
        
        private void ShowSettingsUI()
        {
            // Future: Show settings UI panels
            Debug.Log("SettingsState: Settings UI shown");
            
            // Display current settings
            if (gameManager != null)
            {
                Debug.Log($"Current Settings - Grid Size: {gameManager.gridSize}, Players: {gameManager.numberOfPlayers}");
            }
        }
        
        private void HideSettingsUI()
        {
            // Future: Hide settings UI panels
            Debug.Log("SettingsState: Settings UI hidden");
        }
        
        private void HandleSettingsInput()
        {
            // Temporary keyboard controls for testing
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                StartGameWithCurrentSettings();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReturnToMenu();
            }
            
            // Quick setting adjustments for testing
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetPlayerCount(2);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SetPlayerCount(4);
            }
            
            if (Input.GetKeyDown(KeyCode.Minus))
            {
                AdjustGridSize(-1);
            }
            
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
            {
                AdjustGridSize(1);
            }
        }
        
        /// <summary>
        /// Apply current settings and start the game
        /// </summary>
        public void StartGameWithCurrentSettings()
        {
            Debug.Log("SettingsState: Starting game with current settings...");
            
            // Settings are already applied to gameManager
            // Transition to game state
            stateMachine.ChangeState<ApplicationGameState>();
        }
        
        /// <summary>
        /// Return to main menu without starting game
        /// </summary>
        public void ReturnToMenu()
        {
            Debug.Log("SettingsState: Returning to main menu...");
            stateMachine.ChangeState<MenuState>();
        }
        
        /// <summary>
        /// Set the number of players
        /// </summary>
        public void SetPlayerCount(int count)
        {
            if (gameManager != null)
            {
                int clampedCount = Mathf.Clamp(count, 2, 4);
                if (clampedCount == 3) clampedCount = 4; // Round up as per requirements
                
                gameManager.numberOfPlayers = clampedCount;
                Debug.Log($"SettingsState: Player count set to {clampedCount}");
            }
        }
        
        /// <summary>
        /// Adjust grid size by delta
        /// </summary>
        public void AdjustGridSize(int delta)
        {
            if (gameManager != null)
            {
                int newSize = Mathf.Clamp(gameManager.gridSize + delta, 7, 15); // Reasonable bounds
                if (gameManager.gameSettings != null)
                {
                    gameManager.gameSettings.gridSize = newSize;
                }
                gameManager.UpdateGridConfiguration();
                Debug.Log($"SettingsState: Grid size adjusted to {newSize}x{newSize}");
            }
        }
        
        /// <summary>
        /// Reset to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            if (gameManager != null)
            {
                SetPlayerCount(2);
                if (gameManager.gameSettings != null)
                {
                    gameManager.gameSettings.gridSize = 9;
                }
                gameManager.UpdateGridConfiguration(); // Default 9x9
                Debug.Log("SettingsState: Settings reset to defaults");
            }
        }
    }
}