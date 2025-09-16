using UnityEngine;

namespace WallChess.Pawn
{
    /// <summary>
    /// Event-driven visual controller for pawn turn states.
    /// More efficient alternative to PawnTurnVisualController using events instead of polling.
    /// </summary>
    public class PawnTurnVisualControllerEvents : MonoBehaviour
    {
        [Header("Visual Effects")]
        [SerializeField] private GameObject activeEffect;
        [SerializeField] private GameObject inactiveEffect;
        
        [Header("Settings")]
        [SerializeField] private bool autoFindEffects = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        // Internal state
        private WallChessGameManager gameManager;
        private int pawnIndex = -1;
        
        void Start()
        {
            Initialize();
        }
        
        void Initialize()
        {
            // Find game manager
            gameManager = FindFirstObjectByType<WallChessGameManager>();
            if (gameManager == null)
            {
                LogWarning("No WallChessGameManager found in scene");
                return;
            }
            
            // Auto-find effects if enabled
            if (autoFindEffects)
            {
                TryAutoFindEffects();
            }
            
            // Find our pawn index
            FindPawnIndex();
            
            if (pawnIndex >= 0)
            {
                // Subscribe to turn change events
                TurnEventBroadcaster.OnTurnChanged += OnTurnChanged;
                
                // Initialize visual state
                UpdateVisualState();
                
                LogDebug($"Event-driven pawn visual controller initialized for pawn {pawnIndex}");
            }
        }
        
        void TryAutoFindEffects()
        {
            if (activeEffect == null)
            {
                Transform activeTransform = transform.Find("ActiveEffect");
                if (activeTransform != null)
                {
                    activeEffect = activeTransform.gameObject;
                    LogDebug("Auto-found ActiveEffect");
                }
            }
            
            if (inactiveEffect == null)
            {
                Transform inactiveTransform = transform.Find("InactiveEffect");
                if (inactiveTransform != null)
                {
                    inactiveEffect = inactiveTransform.gameObject;
                    LogDebug("Auto-found InactiveEffect");
                }
            }
        }
        
        void FindPawnIndex()
        {
            for (int i = 0; i < gameManager.pawns.Count; i++)
            {
                if (gameManager.pawns[i].avatar == gameObject)
                {
                    pawnIndex = i;
                    LogDebug($"Found pawn index: {pawnIndex}");
                    return;
                }
            }
            
            LogWarning("Could not determine pawn index - this GameObject is not associated with any pawn avatar");
        }
        
        void OnTurnChanged(TurnChangeEventArgs eventArgs)
        {
            // Update visual state when turn changes
            UpdateVisualState();
            
            LogDebug($"Turn changed: {eventArgs.previousPlayerIndex} -> {eventArgs.newPlayerIndex}, " +
                    $"This pawn ({pawnIndex}) is now {(IsPawnActive() ? "ACTIVE" : "INACTIVE")}");
        }
        
        bool IsPawnActive()
        {
            if (gameManager == null || pawnIndex < 0 || pawnIndex >= gameManager.pawns.Count)
                return false;
                
            return gameManager.activePlayerIndex == pawnIndex;
        }
        
        void UpdateVisualState()
        {
            bool isActive = IsPawnActive();
            
            // Update active effect
            if (activeEffect != null)
            {
                activeEffect.SetActive(isActive);
            }
            
            // Update inactive effect
            if (inactiveEffect != null)
            {
                inactiveEffect.SetActive(!isActive);
            }
            
            LogDebug($"Visual state updated: Active={isActive}");
        }
        
        #region Public API
        public void SetEffects(GameObject active, GameObject inactive)
        {
            activeEffect = active;
            inactiveEffect = inactive;
            UpdateVisualState();
        }
        
        public void RefreshVisualState()
        {
            UpdateVisualState();
        }
        
        public int GetPawnIndex() => pawnIndex;
        
        public bool IsActive() => IsPawnActive();
        #endregion
        
        #region Debug Logging
        void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[PawnTurnVisualEvents] {message}");
        }
        
        void LogWarning(string message)
        {
            Debug.LogWarning($"[PawnTurnVisualEvents] {message}");
        }
        #endregion
        
        void OnDestroy()
        {
            // Unsubscribe from events
            TurnEventBroadcaster.OnTurnChanged -= OnTurnChanged;
        }
    }
}