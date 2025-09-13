using UnityEngine;
using System.Collections.Generic;

namespace WallChess.Core.Session
{
    /// <summary>
    /// ScriptableObject for configuring session settings in the inspector
    /// Allows designers to create different game mode presets
    /// </summary>
    [CreateAssetMenu(fileName = "New Session Settings", menuName = "Wall Chess/Session Settings", order = 1)]
    public class SessionSettingsAsset : ScriptableObject
    {
        [Header("Game Configuration")]
        [SerializeField] private int gridSize = 9;
        [SerializeField] private int wallsPerPlayer = 9;
        [SerializeField] private int playerCount = 2;
        
        [Header("Turn Settings")]
        [SerializeField] private float turnTimeLimit = 30f;
        [SerializeField] private bool enableTurnTimer = false;
        
        [Header("Player Configurations")]
        [SerializeField] private List<PlayerConfigurationAsset> playerConfigurations = new List<PlayerConfigurationAsset>();
        
        [Header("Debug Settings")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private string description = "Game mode description";
        
        /// <summary>
        /// Convert this asset to runtime SessionSettings
        /// </summary>
        public SessionSettings ToSessionSettings()
        {
            var settings = new SessionSettings
            {
                gridSize = gridSize,
                wallsPerPlayer = wallsPerPlayer,
                playerCount = playerCount,
                turnTimeLimit = turnTimeLimit,
                enableTurnTimer = enableTurnTimer
            };
            
            // Convert player configuration assets to runtime configurations
            settings.playerConfigurations.Clear();
            
            if (playerConfigurations.Count > 0)
            {
                foreach (var configAsset in playerConfigurations)
                {
                    if (configAsset != null)
                    {
                        settings.playerConfigurations.Add(configAsset.ToPlayerConfiguration());
                    }
                }
            }
            else
            {
                // Create default configurations if none specified
                CreateDefaultConfigurations(settings);
            }
            
            // Ensure we have the right number of player configurations
            while (settings.playerConfigurations.Count < playerCount)
            {
                var config = new PlayerConfiguration(PlayerType.AI, $"AI Player {settings.playerConfigurations.Count + 1}");
                settings.playerConfigurations.Add(config);
            }
            
            // Remove excess configurations
            if (settings.playerConfigurations.Count > playerCount)
            {
                settings.playerConfigurations.RemoveRange(playerCount, settings.playerConfigurations.Count - playerCount);
            }
            
            return settings;
        }
        
        private void CreateDefaultConfigurations(SessionSettings settings)
        {
            switch (playerCount)
            {
                case 2:
                    settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.Human, "Player 1", Color.blue));
                    settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.AI, "AI Opponent", Color.red));
                    break;
                    
                case 4:
                    settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.Human, "Player 1", Color.blue));
                    settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.AI, "AI 1", Color.red));
                    settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.AI, "AI 2", Color.green));
                    settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.AI, "AI 3", Color.yellow));
                    break;
                    
                default:
                    // Fallback for other player counts
                    for (int i = 0; i < playerCount; i++)
                    {
                        var type = i == 0 ? PlayerType.Human : PlayerType.AI;
                        var name = type == PlayerType.Human ? "Player 1" : $"AI {i}";
                        var color = GetDefaultColor(i);
                        settings.playerConfigurations.Add(new PlayerConfiguration(type, name, color));
                    }
                    break;
            }
        }
        
        private Color GetDefaultColor(int index)
        {
            Color[] defaultColors = { Color.blue, Color.red, Color.green, Color.yellow, Color.magenta, Color.cyan };
            return index < defaultColors.Length ? defaultColors[index] : Color.white;
        }
        
        #region Validation
        
        void OnValidate()
        {
            // Ensure valid values
            gridSize = Mathf.Clamp(gridSize, 5, 20);
            wallsPerPlayer = Mathf.Clamp(wallsPerPlayer, 0, 20);
            playerCount = Mathf.Clamp(playerCount, 2, 4);
            turnTimeLimit = Mathf.Clamp(turnTimeLimit, 5f, 300f);
            
            // Ensure player configurations list has the right size
            while (playerConfigurations.Count < playerCount)
            {
                playerConfigurations.Add(null);
            }
            
            while (playerConfigurations.Count > playerCount)
            {
                playerConfigurations.RemoveAt(playerConfigurations.Count - 1);
            }
        }
        
        #endregion
        
        #region Editor Utilities
        
        [ContextMenu("Create Default 2-Player Setup")]
        private void CreateDefault2Player()
        {
            playerCount = 2;
            playerConfigurations.Clear();
            // These would be assigned in the inspector as ScriptableObject references
        }
        
        [ContextMenu("Create Default 4-Player Setup")]
        private void CreateDefault4Player()
        {
            playerCount = 4;
            playerConfigurations.Clear();
            // These would be assigned in the inspector as ScriptableObject references
        }
        
        [ContextMenu("Preview Session Settings")]
        private void PreviewSettings()
        {
            var settings = ToSessionSettings();
            Debug.Log($"Session Settings Preview:\n" +
                     $"Grid: {settings.gridSize}x{settings.gridSize}\n" +
                     $"Players: {settings.playerCount}\n" +
                     $"Walls per player: {settings.wallsPerPlayer}\n" +
                     $"Turn timer: {(settings.enableTurnTimer ? $"{settings.turnTimeLimit}s" : "Disabled")}\n" +
                     $"Description: {description}");
                     
            for (int i = 0; i < settings.playerConfigurations.Count; i++)
            {
                var config = settings.playerConfigurations[i];
                Debug.Log($"Player {i + 1}: {config.playerName} ({config.playerType})");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// ScriptableObject for individual player configuration
    /// </summary>
    [CreateAssetMenu(fileName = "New Player Config", menuName = "Wall Chess/Player Configuration", order = 2)]
    public class PlayerConfigurationAsset : ScriptableObject
    {
        [Header("Player Settings")]
        [SerializeField] private PlayerType playerType = PlayerType.Human;
        [SerializeField] private string playerName = "Player";
        [SerializeField] private Color playerColor = Color.blue;
        
        [Header("AI Settings (if AI player)")]
        [SerializeField] private AIOpponent.Difficulty aiDifficulty = AIOpponent.Difficulty.Intermediate;
        
        [Header("Visual Settings")]
        [SerializeField] private Sprite playerIcon;
        [SerializeField] private Material playerMaterial;
        
        /// <summary>
        /// Convert this asset to runtime PlayerConfiguration
        /// </summary>
        public PlayerConfiguration ToPlayerConfiguration()
        {
            var config = new PlayerConfiguration(playerType, playerName, playerColor)
            {
                aiDifficulty = aiDifficulty
            };
            
            return config;
        }
        
        #region Editor Utilities
        
        [ContextMenu("Set as Human Player")]
        private void SetAsHuman()
        {
            playerType = PlayerType.Human;
            if (string.IsNullOrEmpty(playerName) || playerName.Contains("AI"))
            {
                playerName = "Player";
            }
        }
        
        [ContextMenu("Set as AI Player")]
        private void SetAsAI()
        {
            playerType = PlayerType.AI;
            if (string.IsNullOrEmpty(playerName) || !playerName.Contains("AI"))
            {
                playerName = "AI Player";
            }
        }
        
        void OnValidate()
        {
            // Ensure name is not empty
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = playerType == PlayerType.AI ? "AI Player" : "Player";
            }
        }
        
        #endregion
    }
}