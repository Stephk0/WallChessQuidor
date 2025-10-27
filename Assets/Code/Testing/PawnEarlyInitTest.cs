using UnityEngine;
using WallChess.Gameplay.Pawns;
using WallChess.Core;

namespace WallChess.Testing
{
    /// <summary>
    /// Test script to verify early pawn initialization is working correctly
    /// </summary>
    public class PawnEarlyInitTest : MonoBehaviour
    {
        [Header("Test Controls")]
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("Status - Read Only")]
        [SerializeField] private string pawnManagerStatus = "Not checked";
        [SerializeField] private string sessionStateManagerStatus = "Not checked";
        [SerializeField] private int pawnCount = 0;
        [SerializeField] private bool pawnsHidden = false;
        [SerializeField] private bool earlyInitialized = false;

        void Start()
        {
            // Delay the test slightly to allow initialization
            Invoke(nameof(RunTest), 1f);
        }

        void Update()
        {
            UpdateStatusFields();
        }

        private void UpdateStatusFields()
        {
            var pawnManager = FindObjectOfType<PawnManager>();
            if (pawnManager != null)
            {
                pawnCount = pawnManager.PawnCount;
                pawnsHidden = pawnManager.ArePawnsInitializedButHidden();
                earlyInitialized = pawnsHidden || pawnManager.ArePawnsFullyReady();
                pawnManagerStatus = $"Found - {pawnCount} pawns, Hidden: {pawnsHidden}";
            }
            else
            {
                pawnManagerStatus = "Not found";
                pawnCount = 0;
                pawnsHidden = false;
                earlyInitialized = false;
            }

            var sessionStateManager = FindObjectOfType<SessionStateManager>();
            sessionStateManagerStatus = sessionStateManager != null ? 
                $"Found - Session: {sessionStateManager.IsSessionActive()}" : 
                "Not found";
        }

        private void RunTest()
        {
            LogInfo("=== PAWN EARLY INITIALIZATION TEST ===");
            
            TestPawnManager();
            TestSessionStateManager();
            TestEarlyInitFlow();
            
            LogInfo("=== TEST COMPLETE ===");
        }

        private void TestPawnManager()
        {
            LogInfo("Testing PawnManager...");
            
            var pawnManager = FindObjectOfType<PawnManager>();
            if (pawnManager == null)
            {
                LogError("❌ PawnManager not found!");
                return;
            }
            
            LogInfo("✅ PawnManager found");
            LogInfo($"   Pawn count: {pawnManager.PawnCount}");
            LogInfo($"   Early initialized: {pawnManager.ArePawnsInitializedButHidden()}");
            LogInfo($"   Fully ready: {pawnManager.ArePawnsFullyReady()}");
            
            if (pawnManager.ArePawnsInitializedButHidden())
            {
                LogInfo("✅ Pawns are early initialized and hidden - this is correct!");
            }
            else if (pawnManager.ArePawnsFullyReady())
            {
                LogInfo("✅ Pawns are fully ready and visible");
            }
            else if (pawnManager.PawnCount == 0)
            {
                LogWarning("⚠️ No pawns found - may need to trigger early initialization");
            }
        }

        private void TestSessionStateManager()
        {
            LogInfo("Testing SessionStateManager...");
            
            var sessionStateManager = FindObjectOfType<SessionStateManager>();
            if (sessionStateManager == null)
            {
                LogError("❌ SessionStateManager not found!");
                return;
            }
            
            LogInfo("✅ SessionStateManager found");
            LogInfo($"   Current state: {sessionStateManager.GetCurrentStateName()}");
            LogInfo($"   Session active: {sessionStateManager.IsSessionActive()}");
            LogInfo($"   Session ready: {sessionStateManager.IsSessionReady()}");
        }

        private void TestEarlyInitFlow()
        {
            LogInfo("Testing Early Initialization Flow...");
            
            var pawnManager = FindObjectOfType<PawnManager>();
            var sessionStateManager = FindObjectOfType<SessionStateManager>();
            
            if (pawnManager == null || sessionStateManager == null)
            {
                LogError("❌ Required components not found for flow test");
                return;
            }
            
            string currentState = sessionStateManager.GetCurrentStateName();
            bool pawnsHidden = pawnManager.ArePawnsInitializedButHidden();
            bool pawnsReady = pawnManager.ArePawnsFullyReady();
            
            LogInfo($"Current game flow state: {currentState}");
            
            switch (currentState)
            {
                case "BuildTiles":
                    if (pawnsHidden)
                    {
                        LogInfo("✅ CORRECT: Pawns are hidden during BuildTiles phase");
                    }
                    else
                    {
                        LogWarning("⚠️ Pawns should be hidden during BuildTiles phase");
                    }
                    break;
                    
                case "SpawnPawns":
                    LogInfo("ℹ️ In SpawnPawns phase - pawns should be shown soon");
                    break;
                    
                case "ActiveGameplay":
                    if (pawnsReady)
                    {
                        LogInfo("✅ CORRECT: Pawns are ready during ActiveGameplay");
                    }
                    else
                    {
                        LogWarning("⚠️ Pawns should be ready during ActiveGameplay");
                    }
                    break;
                    
                default:
                    LogInfo($"ℹ️ Game in state: {currentState}");
                    break;
            }
        }

        [ContextMenu("Force Test Early Initialization")]
        public void ForceTestEarlyInit()
        {
            LogInfo("Forcing early initialization test...");
            
            var pawnManager = FindObjectOfType<PawnManager>();
            if (pawnManager != null)
            {
                pawnManager.EarlyInitializePawns();
                LogInfo("Early initialization triggered manually");
            }
        }

        [ContextMenu("Test Show Pawns")]
        public void TestShowPawns()
        {
            LogInfo("Testing show pawns...");
            
            var pawnManager = FindObjectOfType<PawnManager>();
            if (pawnManager != null)
            {
                pawnManager.ShowPawns();
                LogInfo("Show pawns triggered manually");
            }
        }

        private void LogInfo(string message)
        {
            if (enableDebugLogs) 
                Debug.Log($"[PawnEarlyInitTest] {message}");
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs) 
                Debug.LogWarning($"[PawnEarlyInitTest] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[PawnEarlyInitTest] {message}");
        }
    }
}