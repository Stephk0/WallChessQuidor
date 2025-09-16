using UnityEngine;

namespace WallChess.Data
{
    /// <summary>
    /// Core game configuration settings
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Wall Chess/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Game Configuration")]
        [Tooltip("Size of the game grid (NxN)")]
        [Range(6, 16)]
        public int gridSize = 9;
        
        [Tooltip("Number of walls each player starts with")]
        [Range(5, 15)]
        public int wallsPerPlayer = 9;
        
        [Tooltip("Number of players (2 or 4 only)")]
        [Range(2, 4)]
        public int numberOfPlayers = 2;

        void OnValidate()
        {
            // Ensure even number of players (2 or 4)
            if (numberOfPlayers == 3) numberOfPlayers = 4;
            if (numberOfPlayers < 2) numberOfPlayers = 2;
            if (numberOfPlayers > 4) numberOfPlayers = 4;
        }
    }
}