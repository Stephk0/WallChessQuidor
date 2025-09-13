using UnityEngine;

namespace WallChess.Pawn
{
    /// <summary>
    /// Visual controller for pawn turn states following model-view principle.
    /// Intercepts turn changes and updates visual effects accordingly.
    /// </summary>
    public class PawnTurnVisualController : MonoBehaviour
    {
        [Header("Visual Effects")]
        [SerializeField] private GameObject activeEffect;
        [SerializeField] private GameObject inactiveEffect;
        
        [Header("Settings")]
        [SerializeField] private bool autoFindEffects = true;
        [SerializeField] private float updateDelay = 0.1f;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        // Internal state
        private WallChessGameManager gameManager;
        private int pawnIndex = -1;
        private bool lastActiveState = false;
        
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
            
            // Initialize visual state
            if (pawnIndex >= 0)
            {
                UpdateVisualState();
                
                // Start monitoring with slight delay
                InvokeRepeating(nameof(CheckTurnState), updateDelay, updateDelay);
                
                LogDebug($"Initialized pawn visual controller for pawn {pawnIndex}");
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
            // Find which pawn this avatar belongs to
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
        
        void CheckTurnState()
        {
            if (gameManager == null || pawnIndex < 0) return;
            
            // Get current active state
            bool currentActiveState = IsPawnActive();
            
            // Update visual state if changed
            if (currentActiveState != lastActiveState)
            {
                lastActiveState = currentActiveState;
                UpdateVisualState();
                
                LogDebug($"Pawn {pawnIndex} active state changed to: {currentActiveState}");
            }
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
                Debug.Log($"[PawnTurnVisual] {message}");
        }
        
        void LogWarning(string message)
        {
            Debug.LogWarning($"[PawnTurnVisual] {message}");
        }
        #endregion
        
        void OnDestroy()
        {
            CancelInvoke();
        }
    }
}