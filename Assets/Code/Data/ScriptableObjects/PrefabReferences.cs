using UnityEngine;

namespace WallChess.Data
{
    /// <summary>
    /// References to all prefabs used in the game
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabReferences", menuName = "Wall Chess/Prefab References")]
    public class PrefabReferences : ScriptableObject
    {
        [Header("Core Prefabs")]
        [Tooltip("Prefab used for grid tiles")]
        public GameObject tilePrefab;
        
        [Tooltip("Prefab used for walls")]
        public GameObject wallPrefab;

        [Header("Player Prefabs")]
        [Tooltip("Array of player prefabs for different colors/players")]
        public GameObject[] playerPrefabs = new GameObject[4];

        [Header("Highlight Prefabs")]
        [Tooltip("Prefab for highlighting valid moves")]
        public GameObject highlightPrefab;
        
        [Tooltip("Prefab for confirming drag operations")]
        public GameObject highlightConfirmPrefab;
        
        [Tooltip("Prefab for wall placement preview")]
        public GameObject wallPreviewPrefab;

        /// <summary>
        /// Get player prefab by index, with fallback to first prefab if index is out of range
        /// </summary>
        public GameObject GetPlayerPrefab(int index)
        {
            if (playerPrefabs == null || playerPrefabs.Length == 0)
                return null;
                
            if (index >= 0 && index < playerPrefabs.Length && playerPrefabs[index] != null)
                return playerPrefabs[index];
                
            // Fallback to first non-null prefab
            for (int i = 0; i < playerPrefabs.Length; i++)
            {
                if (playerPrefabs[i] != null)
                    return playerPrefabs[i];
            }
            
            return null;
        }
    }
}