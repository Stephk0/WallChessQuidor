using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WallChess; // For WallChessGameManager
using WallChess.Core.States;

namespace WallChess.Migration
{
    /// <summary>
    /// Adapter for transitioning from legacy state system to unified state manager.
    /// Provides compatibility layer during migration phase.
    /// Part of Phase 2: State Management Consolidation (MVP Implementation Guide)
    /// </summary>
    public class StateSystemAdapter : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private WallChessGameManager legacyManager;
        [SerializeField] private UnifiedStateManager unifiedManager;
        
        [Header("Migration Settings")]
        [SerializeField] private MigrationMode currentMode = MigrationMode.Legacy;
        [SerializeField] private bool autoDetectSystems = true;
        [SerializeField] private float syncCheckInterval = 0.1f;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool logStateMismatches = true;
        
        public enum MigrationMode
        {
            Legacy,     // Use old system only
            Unified,    // Use new system only
            Dual,       // Use both with sync (testing)
            Adaptive    // Automatically switch based on state
        }
        
        private Coroutine syncCoroutine;
        private Dictionary<string, UnifiedStateManager.StateType> stateMapping;
        private HashSet<string> reportedMismatches = new HashSet<string>();
        
        #region Unity Lifecycle
        void Awake()
        {
            InitializeStateMapping();
            
            if (autoDetectSystems)
            {
                DetectSystems();
            }
        }
        
        void Start()
        {
            ValidateSystems();
            
            if (currentMode == MigrationMode.Dual || currentMode == MigrationMode.Adaptive)
            {
                SubscribeToEvents();
                syncCoroutine = StartCoroutine(MonitorStateSynchronization());
            }
            
            LogMigrationStatus();
        }
        
        void OnDestroy()
        {
            if (syncCoroutine != null)
            {
                StopCoroutine(syncCoroutine);
            }
            
            UnsubscribeFromEvents();
        }
        #endregion
        
        #region Initialization
        private void InitializeStateMapping()
        {
            // Map legacy state names/enums to unified state types
            stateMapping = new Dictionary<string, UnifiedStateManager.StateType>
            {
                // Common legacy state names
                { "Idle", UnifiedStateManager.StateType.PlayerTurn },
                { "PlayerTurn", UnifiedStateManager.StateType.PlayerTurn },
                { "SelectingPawn", UnifiedStateManager.StateType.PawnSelection },
                { "MovingPawn", UnifiedStateManager.StateType.PawnMoving },
                { "PlacingWall", UnifiedStateManager.StateType.WallPlacement },
                { "WallPlacement", UnifiedStateManager.StateType.WallPlacement },
                { "EndTurn", UnifiedStateManager.StateType.TurnTransition },
                { "CheckingWin", UnifiedStateManager.StateType.TurnValidation },
                { "GameOver", UnifiedStateManager.StateType.GameOver },
                { "Paused", UnifiedStateManager.StateType.Paused },
                { "Menu", UnifiedStateManager.StateType.MainMenu },
                { "Setup", UnifiedStateManager.StateType.GameSetup }
            };
        }
        
        private void DetectSystems()
        {
            // Auto-detect available systems
            if (legacyManager == null)
            {
                legacyManager = FindObjectOfType<WallChessGameManager>();
            }
            
            if (unifiedManager == null)
            {
                unifiedManager = UnifiedStateManager.Instance;
            }
            
            // Determine mode based on what's available
            if (legacyManager != null && unifiedManager != null)
            {
                if (currentMode == MigrationMode.Legacy || currentMode == MigrationMode.Unified)
                {
                    // Keep current setting
                }
                else
                {
                    currentMode = MigrationMode.Dual;
                }
            }
            else if (unifiedManager != null)
            {
                currentMode = MigrationMode.Unified;
            }
            else if (legacyManager != null)
            {
                currentMode = MigrationMode.Legacy;
            }
            else
            {
                Debug.LogError("[Migration] No state management system found!");
            }
        }
        
        private void ValidateSystems()
        {
            switch (currentMode)
            {
                case MigrationMode.Legacy:
                    if (legacyManager == null)
                    {
                        Debug.LogError("[Migration] Legacy mode selected but WallChessGameManager not found!");
                    }
                    break;
                    
                case MigrationMode.Unified:
                    if (unifiedManager == null)
                    {
                        Debug.LogError("[Migration] Unified mode selected but UnifiedStateManager not found!");
                    }
                    break;
                    
                case MigrationMode.Dual:
                case MigrationMode.Adaptive:
                    if (legacyManager == null || unifiedManager == null)
                    {
                        Debug.LogError("[Migration] Dual/Adaptive mode requires both systems!");
                    }
                    break;
            }
        }
        #endregion
        
        #region Event Handling
        private void SubscribeToEvents()
        {
            // Subscribe to unified state changes
            UnifiedStateManager.OnStateChanged += OnUnifiedStateChanged;
            
            // Subscribe to legacy state changes if available
            // (Would need to add events to legacy system)
        }
        
        private void UnsubscribeFromEvents()
        {
            UnifiedStateManager.OnStateChanged -= OnUnifiedStateChanged;
        }
        
        private void OnUnifiedStateChanged(UnifiedStateManager.StateType fromState, UnifiedStateManager.StateType toState)
        {
            if (currentMode == MigrationMode.Dual && enableDebugLogging)
            {
                Debug.Log($"[Migration] Unified state changed: {fromState} -> {toState}");
            }
            
            // In adaptive mode, sync legacy system
            if (currentMode == MigrationMode.Adaptive && legacyManager != null)
            {
                SyncLegacyToUnified(toState);
            }
        }
        #endregion
        
        #region State Synchronization
        private IEnumerator MonitorStateSynchronization()
        {
            while (currentMode == MigrationMode.Dual || currentMode == MigrationMode.Adaptive)
            {
                yield return new WaitForSeconds(syncCheckInterval);
                
                if (legacyManager != null && unifiedManager != null)
                {
                    CheckStateConsistency();
                }
            }
        }
        
        private void CheckStateConsistency()
        {
            var legacyState = GetLegacyStateName();
            var unifiedState = unifiedManager.CurrentStateType;
            
            // Map legacy state to expected unified state
            var expectedUnified = MapLegacyToUnified(legacyState);
            
            if (unifiedState != expectedUnified)
            {
                string mismatchKey = $"{legacyState}_{unifiedState}";
                
                if (logStateMismatches && !reportedMismatches.Contains(mismatchKey))
                {
                    Debug.LogWarning($"[Migration] State mismatch detected:\n" +
                                   $"  Legacy: {legacyState}\n" +
                                   $"  Unified: {unifiedState}\n" +
                                   $"  Expected: {expectedUnified}");
                    
                    reportedMismatches.Add(mismatchKey);
                    
                    // In adaptive mode, try to reconcile
                    if (currentMode == MigrationMode.Adaptive)
                    {
                        ReconcileStateMismatch(legacyState, unifiedState, expectedUnified);
                    }
                }
            }
        }
        
        private void ReconcileStateMismatch(string legacyState, UnifiedStateManager.StateType unifiedState, UnifiedStateManager.StateType expectedState)
        {
            // Attempt to reconcile the mismatch
            if (enableDebugLogging)
            {
                Debug.Log($"[Migration] Attempting to reconcile state mismatch...");
            }
            
            // Priority: Keep unified state as source of truth
            // Update legacy system to match
            if (legacyManager != null)
            {
                // This would require adding state sync methods to legacy manager
                // For now, just log the intention
                Debug.Log($"[Migration] Would sync legacy state to match unified: {unifiedState}");
            }
        }
        
        private string GetLegacyStateName()
        {
            if (legacyManager == null) return "Unknown";
            
            // Get current state from legacy manager
            // This assumes the legacy manager exposes its state somehow
            // You may need to add a GetCurrentState() method to WallChessGameManager
            
            // Placeholder - would need actual implementation based on legacy system
            return "PlayerTurn";
        }
        
        private UnifiedStateManager.StateType MapLegacyToUnified(string legacyState)
        {
            if (stateMapping.TryGetValue(legacyState, out var unifiedState))
            {
                return unifiedState;
            }
            
            // Default mapping if not found
            Debug.LogWarning($"[Migration] No mapping found for legacy state: {legacyState}");
            return UnifiedStateManager.StateType.PlayerTurn;
        }
        
        private void SyncLegacyToUnified(UnifiedStateManager.StateType unifiedState)
        {
            // Sync legacy system to match unified state
            // This would require modifying the legacy manager
            if (enableDebugLogging)
            {
                Debug.Log($"[Migration] Syncing legacy system to unified state: {unifiedState}");
            }
        }
        #endregion
        
        #region Public API
        /// <summary>
        /// Switch migration mode at runtime
        /// </summary>
        public void SetMigrationMode(MigrationMode mode)
        {
            var previousMode = currentMode;
            currentMode = mode;
            
            if (enableDebugLogging)
            {
                Debug.Log($"[Migration] Mode changed: {previousMode} -> {mode}");
            }
            
            // Handle mode change
            if (mode == MigrationMode.Dual || mode == MigrationMode.Adaptive)
            {
                if (syncCoroutine == null)
                {
                    syncCoroutine = StartCoroutine(MonitorStateSynchronization());
                }
            }
            else if (syncCoroutine != null)
            {
                StopCoroutine(syncCoroutine);
                syncCoroutine = null;
            }
        }
        
        /// <summary>
        /// Get current active state based on migration mode
        /// </summary>
        public string GetCurrentState()
        {
            switch (currentMode)
            {
                case MigrationMode.Legacy:
                    return GetLegacyStateName();
                    
                case MigrationMode.Unified:
                case MigrationMode.Dual:
                case MigrationMode.Adaptive:
                    return unifiedManager?.CurrentStateType.ToString() ?? "Unknown";
                    
                default:
                    return "Unknown";
            }
        }
        
        /// <summary>
        /// Request state change through appropriate system
        /// </summary>
        public bool RequestStateChange(string stateName)
        {
            switch (currentMode)
            {
                case MigrationMode.Legacy:
                    // Change state in legacy system
                    Debug.Log($"[Migration] Would change legacy state to: {stateName}");
                    return true;
                    
                case MigrationMode.Unified:
                case MigrationMode.Dual:
                case MigrationMode.Adaptive:
                    // Map to unified state and request change
                    if (stateMapping.TryGetValue(stateName, out var unifiedState))
                    {
                        return unifiedManager.RequestStateChange(unifiedState);
                    }
                    Debug.LogWarning($"[Migration] Unknown state: {stateName}");
                    return false;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Generate migration report for debugging
        /// </summary>
        public void GenerateMigrationReport()
        {
            Debug.Log("=== Migration Status Report ===");
            Debug.Log($"Mode: {currentMode}");
            Debug.Log($"Legacy System: {(legacyManager != null ? "Found" : "Missing")}");
            Debug.Log($"Unified System: {(unifiedManager != null ? "Found" : "Missing")}");
            
            if (legacyManager != null && unifiedManager != null)
            {
                Debug.Log($"Legacy State: {GetLegacyStateName()}");
                Debug.Log($"Unified State: {unifiedManager.CurrentStateType}");
            }
            
            Debug.Log($"Mismatches Detected: {reportedMismatches.Count}");
            Debug.Log("==============================");
        }
        #endregion
        
        #region Utility
        private void LogMigrationStatus()
        {
            if (!enableDebugLogging) return;
            
            Debug.Log($"[Migration] Adapter initialized in {currentMode} mode");
            
            if (legacyManager != null)
            {
                Debug.Log("[Migration] Legacy WallChessGameManager detected");
            }
            
            if (unifiedManager != null)
            {
                Debug.Log("[Migration] Unified StateManager active");
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Editor helper for migration testing
    /// </summary>
    [System.Serializable]
    public class MigrationTestCase
    {
        public string testName;
        public string legacyState;
        public UnifiedStateManager.StateType expectedUnifiedState;
        public bool passed;
        
        public void Execute(StateSystemAdapter adapter)
        {
            // Test state mapping
            adapter.RequestStateChange(legacyState);
            // Check if states match expected
        }
    }
}