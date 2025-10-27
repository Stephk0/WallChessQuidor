using UnityEngine;

namespace WallChess.Core.ApplicationStates
{
    /// <summary>
    /// Main menu state - handles menu interactions and navigation
    /// Can transition to SettingsState or directly to GameState
    /// </summary>
    public class MenuState : IState
    {
        public string StateName => "MainMenu";
        
        private readonly StateMachine stateMachine;
        private readonly WallChessGameManager gameManager;
        
        public MenuState(StateMachine stateMachine, WallChessGameManager gameManager)
        {
            this.stateMachine = stateMachine;
            this.gameManager = gameManager;
        }
        
        public void OnEnter()
        {
            Debug.Log("MenuState: Entered main menu");
            
            // Show main menu UI
            ShowMainMenuUI();
            
            // Hide game UI elements if visible
            HideGameUI();
        }
        
        public void OnUpdate()
        {
            // Handle menu input
            HandleMenuInput();
        }
        
        public void OnExit()
        {
            Debug.Log("MenuState: Leaving main menu");
            HideMainMenuUI();
        }
        
        private void ShowMainMenuUI()
        {
            // Future: Activate main menu UI panels
            Debug.Log("MenuState: Main menu UI shown");
        }
        
        private void HideMainMenuUI()
        {
            // Future: Deactivate main menu UI panels
            Debug.Log("MenuState: Main menu UI hidden");
        }
        
        private void HideGameUI()
        {
            // Future: Hide game-specific UI elements
            Debug.Log("MenuState: Game UI hidden");
        }
        
        private void HandleMenuInput()
        {
            // Temporary keyboard controls for testing
            if (Input.GetKeyDown(KeyCode.P)) // P for Play
            {
                StartNewGame();
            }
            
            if (Input.GetKeyDown(KeyCode.S)) // S for Settings
            {
                OpenSettings();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape)) // ESC to quit
            {
                QuitApplication();
            }
        }
        
        /// <summary>
        /// Start a new game - can go directly to game or through settings first
        /// </summary>
        public void StartNewGame()
        {
            Debug.Log("MenuState: Starting new game...");
            stateMachine.ChangeState<ApplicationGameState>();
        }
        
        /// <summary>
        /// Open settings menu
        /// </summary>
        public void OpenSettings()
        {
            Debug.Log("MenuState: Opening settings...");
            stateMachine.ChangeState<SettingsState>();
        }
        
        /// <summary>
        /// Quit the application
        /// </summary>
        public void QuitApplication()
        {
            Debug.Log("MenuState: Quitting application...");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}