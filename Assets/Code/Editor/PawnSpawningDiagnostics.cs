using UnityEngine;
using UnityEditor;
using WallChess.Data;
using WallChess;

namespace WallChess.Editor
{
    /// <summary>
    /// Simplified diagnostics and utilities for pawn spawning
    /// </summary>
    public class PawnSpawningDiagnostics
    {
        [MenuItem("Wall Chess/Debug/Diagnose Pawn Spawning")]
        public static void DiagnosePawnSpawning()
        {
            Debug.Log("=== PAWN SPAWNING DIAGNOSTICS ===");
            
            // Find WallChessGameManager
            WallChessGameManager gameManager = Object.FindObjectOfType<WallChessGameManager>();
            if (gameManager == null)
            {
                Debug.LogError("❌ WallChessGameManager not found in scene!");
                return;
            }
            
            Debug.Log("✅ WallChessGameManager found");
            
            // Check PrefabReferences
            if (gameManager.prefabReferences == null)
            {
                Debug.LogError("❌ PrefabReferences is null! Creating one...");
                CreatePrefabReferences();
                return;
            }
            
            Debug.Log("✅ PrefabReferences assigned");
            
            // Check player prefabs
            if (gameManager.prefabReferences.playerPrefabs == null || 
                gameManager.prefabReferences.playerPrefabs.Length == 0)
            {
                Debug.LogError("❌ No player prefabs assigned!");
                ShowPrefabSolution();
                return;
            }
            
            // Check individual prefabs
            int nullPrefabs = 0;
            for (int i = 0; i < gameManager.prefabReferences.playerPrefabs.Length; i++)
            {
                if (gameManager.prefabReferences.playerPrefabs[i] == null)
                {
                    nullPrefabs++;
                    Debug.LogWarning($"⚠️ Player prefab {i} is null");
                }
            }
            
            if (nullPrefabs == gameManager.prefabReferences.playerPrefabs.Length)
            {
                Debug.LogError("❌ All player prefabs are null!");
                ShowPrefabSolution();
                return;
            }
            
            Debug.Log($"✅ {gameManager.prefabReferences.playerPrefabs.Length - nullPrefabs} valid player prefabs found");
            Debug.Log("=== DIAGNOSTICS COMPLETE ===");
            Debug.Log("If pawns still don't spawn, emergency pawns will be created automatically.");
        }
        
        [MenuItem("Wall Chess/Debug/Create Manual Emergency Pawns")]
        public static void CreateManualEmergencyPawns()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Emergency pawns can only be created in Play Mode");
                return;
            }
            
            // Find GameManager
            var gameManager = Object.FindObjectOfType<WallChessGameManager>();
            if (gameManager == null)
            {
                Debug.LogError("WallChessGameManager not found!");
                return;
            }
            
            // Get GridSystem using reflection to avoid compilation issues
            var gridSystemField = typeof(WallChessGameManager).GetField("gridSystem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (gridSystemField == null)
            {
                Debug.LogError("Could not access GridSystem");
                return;
            }
            
            var gridSystem = gridSystemField.GetValue(gameManager);
            if (gridSystem == null)
            {
                Debug.LogError("GridSystem is null! Make sure the game is initialized.");
                return;
            }
            
            CreateSimplePawns(gameManager, gridSystem);
            Debug.Log("Manual emergency pawns created!");
        }
        
        [MenuItem("Wall Chess/Setup/Create PrefabReferences")]
        public static void CreatePrefabReferences()
        {
            // Create ScriptableObject
            PrefabReferences prefabRefs = ScriptableObject.CreateInstance<PrefabReferences>();
            
            // Save it to Assets
            string path = "Assets/PrefabReferences.asset";
            AssetDatabase.CreateAsset(prefabRefs, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Assign to GameManager if found
            WallChessGameManager gameManager = Object.FindObjectOfType<WallChessGameManager>();
            if (gameManager != null)
            {
                gameManager.prefabReferences = prefabRefs;
                EditorUtility.SetDirty(gameManager);
            }
            
            // Select the new asset
            Selection.activeObject = prefabRefs;
            EditorGUIUtility.PingObject(prefabRefs);
            
            Debug.Log($"✅ Created PrefabReferences at {path}");
            Debug.Log("Now assign player prefabs in the inspector!");
        }
        
        [MenuItem("Wall Chess/Setup/Create Simple Player Prefabs")]
        public static void CreateSimplePlayerPrefabs()
        {
            // Create folder for prefabs
            string folderPath = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            
            // Create simple player prefabs
            for (int i = 0; i < 2; i++)
            {
                // Create cube
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"PlayerPawn_{i + 1}";
                cube.transform.localScale = Vector3.one * 0.8f;
                
                // Set color
                Color playerColor = (i == 0) ? Color.blue : Color.red;
                var renderer = cube.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = playerColor;
                    renderer.sharedMaterial = mat;
                }
                
                // Save as prefab
                string prefabPath = $"{folderPath}/PlayerPawn_{i + 1}.prefab";
                PrefabUtility.SaveAsPrefabAsset(cube, prefabPath);
                
                // Clean up scene object
                Object.DestroyImmediate(cube);
                
                Debug.Log($"✅ Created {prefabPath}");
            }
            
            Debug.Log("Simple player prefabs created! Now assign them to PrefabReferences.");
            AssetDatabase.Refresh();
        }
        
        private static void CreateSimplePawns(WallChessGameManager gameManager, object gridSystem)
        {
            // Get GridToWorldPosition method via reflection
            var gridToWorldMethod = gridSystem.GetType().GetMethod("GridToWorldPosition");
            if (gridToWorldMethod == null)
            {
                Debug.LogError("Could not find GridToWorldPosition method");
                return;
            }
            
            // Create Player 1 (Blue) at bottom center
            var player1Pos = new Vector2Int(4, 0);
            var player1WorldPos = (Vector3)gridToWorldMethod.Invoke(gridSystem, new object[] { player1Pos });
            CreateSimplePawn("Player1", player1WorldPos, Color.blue, gameManager.transform);
            
            // Create Player 2 (Red) at top center  
            var player2Pos = new Vector2Int(4, 8);
            var player2WorldPos = (Vector3)gridToWorldMethod.Invoke(gridSystem, new object[] { player2Pos });
            CreateSimplePawn("Player2", player2WorldPos, Color.red, gameManager.transform);
            
            Debug.Log("Created 2 simple emergency pawns");
        }
        
        private static void CreateSimplePawn(string name, Vector3 position, Color color, Transform parent)
        {
            GameObject pawn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pawn.name = $"ManualEmergencyPawn_{name}";
            pawn.transform.position = position;
            pawn.transform.localScale = Vector3.one * 0.8f;
            pawn.transform.SetParent(parent);
            
            // Color the pawn
            var renderer = pawn.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.material = mat;
            }
            
            Debug.Log($"Created manual emergency pawn: {name} at {position}");
        }
        
        private static void ShowPrefabSolution()
        {
            Debug.Log("=== SOLUTION ===");
            Debug.Log("1. Create player prefabs: Wall Chess > Setup > Create Simple Player Prefabs");
            Debug.Log("2. Assign them to PrefabReferences in the inspector");
            Debug.Log("3. Or use: Wall Chess > Debug > Create Manual Emergency Pawns (in Play Mode)");
        }
    }
}
