using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WallChess.Data
{
    /// <summary>
    /// Utility to create default ScriptableObject assets for Wall Chess
    /// </summary>
    public class CreateDefaultAssets : MonoBehaviour
    {
        #if UNITY_EDITOR
        [ContextMenu("Create Default Assets")]
        public void CreateDefaults()
        {
            string basePath = "Assets/Code/Data/ScriptableObjects/";
            
            // Ensure directory exists
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(basePath));
            
            // Create GameSettings with default values (from original WallChessGameManager)
            GameSettings gameSettings = ScriptableObject.CreateInstance<GameSettings>();
            gameSettings.gridSize = 9;
            gameSettings.wallsPerPlayer = 9;
            gameSettings.numberOfPlayers = 2;
            AssetDatabase.CreateAsset(gameSettings, basePath + "DefaultGameSettings.asset");
            
            // Create GridSettings with default values
            GridSettings gridSettings = ScriptableObject.CreateInstance<GridSettings>();
            gridSettings.tileSize = 1f;
            gridSettings.tileGap = 0.2f;
            gridSettings.wallThickness = 0.15f;
            gridSettings.wallHeight = 1f;
            AssetDatabase.CreateAsset(gridSettings, basePath + "DefaultGridSettings.asset");
            
            // Create PrefabReferences (empty - to be populated in inspector)
            PrefabReferences prefabReferences = ScriptableObject.CreateInstance<PrefabReferences>();
            AssetDatabase.CreateAsset(prefabReferences, basePath + "DefaultPrefabReferences.asset");
            
            // Create DebugSettings with default values
            DebugSettings debugSettings = ScriptableObject.CreateInstance<DebugSettings>();
            debugSettings.debugMode = false;
            debugSettings.enableVerboseLogging = true;
            debugSettings.showDebugUI = false;
            debugSettings.skipAnimations = false;
            debugSettings.enableDevHotkeys = true;
            debugSettings.enableContextMenus = true;
            AssetDatabase.CreateAsset(debugSettings, basePath + "DefaultDebugSettings.asset");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("Default ScriptableObject assets created successfully!");
        }
        
        [MenuItem("Wall Chess/Create Default Assets")]
        public static void CreateDefaultsMenuItem()
        {
            GameObject temp = new GameObject("TempCreateDefaultAssets");
            CreateDefaultAssets creator = temp.AddComponent<CreateDefaultAssets>();
            creator.CreateDefaults();
            DestroyImmediate(temp);
        }
        #endif
    }
}