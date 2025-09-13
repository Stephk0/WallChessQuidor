using UnityEngine;

namespace WallChess
{
    /// <summary>
    /// Simple test script to verify the Player Pawn System refactoring
    /// Attach this to any GameObject to test the new WallChessGameManager
    /// </summary>
    public class PlayerPawnSystemTester : MonoBehaviour
    {
        [Header("Test Results")]
        public bool newSystemFound = false;
        public int numberOfPawns = 0;
        public int activePlayerIndex = -1;
        public string testResults = "Not tested yet";

        [ContextMenu("Test Player Pawn System")]
        public void TestPlayerPawnSystem()
        {
            testResults = "Testing...";
            
            // Find the new WallChessGameManager
            WallChessGameManager gameManager = FindObjectOfType<WallChessGameManager>();
            
            if (gameManager == null)
            {
                testResults = "❌ FAIL: No WallChessGameManager found";
                newSystemFound = false;
                return;
            }

            // Check if it has the new pawn system
            try 
            {
                // Test accessing new pawn system properties
                numberOfPawns = gameManager.pawns != null ? gameManager.pawns.Count : 0;
                activePlayerIndex = gameManager.activePlayerIndex;
                
                Debug.Log($"=== PLAYER PAWN SYSTEM TEST ===");
                Debug.Log($"✅ Found WallChessGameManager with new pawn system");
                Debug.Log($"📊 Number of pawns: {numberOfPawns}");
                Debug.Log($"👤 Active player index: {activePlayerIndex}");
                Debug.Log($"🎯 Grid size: {gameManager.gridSize}");
                
                if (numberOfPawns > 0)
                {
                    for (int i = 0; i < numberOfPawns; i++)
                    {
                        var pawn = gameManager.pawns[i];
                        Debug.Log($"   Pawn {i}: Position={pawn.position}, Walls={pawn.wallsRemaining}, Active={pawn.isActive}");
                    }
                }

                // Test method calls
                var activePawn = gameManager.GetActivePawn();
                bool canMovePawn0 = gameManager.CanMovePawn(0);
                bool canPlaceWalls = gameManager.CanPlaceWalls();
                
                Debug.Log($"🔧 Active pawn: {(activePawn != null ? $"Player {activePlayerIndex}" : "None")}");
                Debug.Log($"🚶 Can move pawn 0: {canMovePawn0}");
                Debug.Log($"🧱 Can place walls: {canPlaceWalls}");
                Debug.Log($"🎮 Current state: {gameManager.GetCurrentState()}");
                Debug.Log($"⚡ Current action: {gameManager.GetCurrentAction()}");

                testResults = $"✅ SUCCESS: New pawn system working! {numberOfPawns} pawns, active player {activePlayerIndex}";
                newSystemFound = true;
                
                Debug.Log("=== TEST COMPLETE ✅ ===");
            }
            catch (System.Exception e)
            {
                testResults = $"❌ FAIL: Error accessing new properties - {e.Message}";
                newSystemFound = false;
                Debug.LogError($"Player Pawn System test failed: {e.Message}");
            }
        }

        [ContextMenu("Test Legacy Compatibility")]
        public void TestLegacyCompatibility()
        {
            WallChessGameManager gameManager = FindObjectOfType<WallChessGameManager>();
            if (gameManager == null) 
            {
                Debug.LogError("No WallChessGameManager found for legacy compatibility test");
                return;
            }

            Debug.Log("=== LEGACY COMPATIBILITY TEST ===");
            
            // Test legacy properties (should still work via compatibility layer)
            var playerPos = gameManager.playerPosition;
            var opponentPos = gameManager.opponentPosition;
            var playerWalls = gameManager.playerWallsRemaining;
            var opponentWalls = gameManager.opponentWallsRemaining;
            
            Debug.Log($"🔄 Legacy playerPosition: {playerPos}");
            Debug.Log($"🔄 Legacy opponentPosition: {opponentPos}");  
            Debug.Log($"🧱 Legacy playerWallsRemaining: {playerWalls}");
            Debug.Log($"🧱 Legacy opponentWallsRemaining: {opponentWalls}");
            
            // Test legacy methods
            var playerAvatar = gameManager.GetPlayerAvatar();
            var opponentAvatar = gameManager.GetOpponentAvatar();
            
            Debug.Log($"👤 Legacy GetPlayerAvatar(): {(playerAvatar != null ? playerAvatar.name : "null")}");
            Debug.Log($"👤 Legacy GetOpponentAvatar(): {(opponentAvatar != null ? opponentAvatar.name : "null")}");
            Debug.Log("=== LEGACY TEST COMPLETE ===");
        }

        void Start()
        {
            // Auto-test on start
            Invoke(nameof(TestPlayerPawnSystem), 1f);
        }
    }
}
