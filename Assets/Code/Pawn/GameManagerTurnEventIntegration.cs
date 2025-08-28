using UnityEngine;

namespace WallChess.Pawn
{
    /// <summary>
    /// Integration helper that extends WallChessGameManager with turn event broadcasting.
    /// Attach this to the same GameObject as WallChessGameManager to enable event-driven pawn visuals.
    /// Follows model-view principle by adding view notifications without modifying game logic.
    /// </summary>
    public class GameManagerTurnEventIntegration : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool enableEventBroadcasting = true;
        [SerializeField] private bool enableDebugLogs = false;
        
        // Dependencies
        private WallChessGameManager gameManager;
        
        // State tracking
        private int lastActivePlayerIndex = -1;
        private GameState lastGameState;
        
        void Start()
        {
            Initialize();
        }
        
        void Initialize()
        {
            // Find game manager on same GameObject
            gameManager = GetComponent<WallChessGameManager>();
            if (gameManager == null)
            {
                LogWarning("No WallChessGameManager found on this GameObject");
                return;
            }
            
            // Initialize state tracking
            lastActivePlayerIndex = gameManager.activePlayerIndex;
            lastGameState = gameManager.GetCurrentState();
            
            LogDebug("Turn event integration initialized");
            
            // Start monitoring
            InvokeRepeating(nameof(MonitorTurnChanges), 0.1f, 0.1f);
        }
        
        void MonitorTurnChanges()
        {
            if (!enableEventBroadcasting || gameManager == null) return;
            
            int currentActivePlayer = gameManager.activePlayerIndex;
            GameState currentState = gameManager.GetCurrentState();
            
            // Check if active player changed
            if (currentActivePlayer != lastActivePlayerIndex)
            {
                BroadcastTurnChange(lastActivePlayerIndex, currentActivePlayer, currentState);
                lastActivePlayerIndex = currentActivePlayer;
                LogDebug($"Active player changed: {lastActivePlayerIndex} -> {currentActivePlayer}");
            }
            
            // Track state changes for future use
            lastGameState = currentState;
        }
        
        void BroadcastTurnChange(int previousPlayer, int newPlayer, GameState state)
        {
            if (enableEventBroadcasting)
            {
                TurnEventBroadcaster.BroadcastTurnChange(previousPlayer, newPlayer, state);
                LogDebug($"Turn change event broadcasted: {previousPlayer} -> {newPlayer} (State: {state})");
            }
        }
        
        #region Public API
        /// <summary>
        /// Manually trigger turn change event
        /// </summary>
        public void ManualTriggerTurnChange()
        {
            if (gameManager != null)
            {
                BroadcastTurnChange(lastActivePlayerIndex, gameManager.activePlayerIndex, gameManager.GetCurrentState());
            }
        }
        
        /// <summary>
        /// Enable/disable event broadcasting
        /// </summary>
        public void SetEventBroadcasting(bool enabled)
        {
            enableEventBroadcasting = enabled;
            LogDebug($"Event broadcasting {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Force refresh all pawn visuals
        /// </summary>
        public void RefreshAllPawnVisuals()
        {
            // Find all pawn visual controllers and refresh them
            var visualControllers = FindObjectsByType<PawnTurnVisualController>(FindObjectsSortMode.None);
            foreach (var controller in visualControllers)
            {
                controller.RefreshVisualState();
            }
            
            var eventControllers = FindObjectsByType<PawnTurnVisualControllerEvents>(FindObjectsSortMode.None);
            foreach (var controller in eventControllers)
            {
                controller.RefreshVisualState();
            }
            
            LogDebug($"Refreshed {visualControllers.Length + eventControllers.Length} pawn visual controllers");
        }
        #endregion
        
        #region Debug Logging
        void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[TurnEventIntegration] {message}");
        }
        
        void LogWarning(string message)
        {
            Debug.LogWarning($"[TurnEventIntegration] {message}");
        }
        #endregion
        
        void OnDestroy()
        {
            // Clean up
            CancelInvoke();
            
            if (enableEventBroadcasting)
            {
                TurnEventBroadcaster.ClearSubscriptions();
            }
        }
        
        #region Context Menu Actions
        [ContextMenu("Manual Trigger Turn Change")]
        private void Ctx_ManualTrigger() => ManualTriggerTurnChange();
        
        [ContextMenu("Refresh All Pawn Visuals")]
        private void Ctx_RefreshVisuals() => RefreshAllPawnVisuals();
        
        [ContextMenu("Toggle Event Broadcasting")]
        private void Ctx_ToggleBroadcasting() => SetEventBroadcasting(!enableEventBroadcasting);
        #endregion
    }
}