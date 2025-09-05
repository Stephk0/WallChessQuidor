using UnityEngine;
using System.Collections.Generic;
using System;

namespace WallChess.Core.Session
{
    /// <summary>
    /// Enum defining different types of players in the session
    /// </summary>
    public enum PlayerType
    {
        Human,
        AI,
        Remote // For future network support
    }
    
    /// <summary>
    /// Enum defining possible session end reasons
    /// </summary>
    public enum SessionEndReason
    {
        PlayerVictory,
        AIVictory,
        PlayerQuit,
        Timeout,
        Disconnect,
        Error
    }
    
    /// <summary>
    /// Data structure representing a single player in the session
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public int playerIndex;
        public PlayerType playerType;
        public string playerName;
        public Vector2Int startPosition;
        public Vector2Int currentPosition;
        public Vector2Int winPosition;
        public int wallsRemaining;
        public bool isActive;
        public Color playerColor;
        
        // AI-specific data
        public AIOpponent.Difficulty aiDifficulty;
        
        // Runtime state
        public float turnStartTime;
        public int moveCount;
        public int wallsPlaced;
        
        public PlayerData(int index, PlayerType type, Vector2Int start, Vector2Int win, int initialWalls)
        {
            playerIndex = index;
            playerType = type;
            startPosition = start;
            currentPosition = start;
            winPosition = win;
            wallsRemaining = initialWalls;
            isActive = false;
            moveCount = 0;
            wallsPlaced = 0;
            
            // Default values
            playerName = type == PlayerType.AI ? $"AI Player {index + 1}" : $"Player {index + 1}";
            aiDifficulty = AIOpponent.Difficulty.Intermediate;
        }
        
        /// <summary>
        /// Update player position after a move
        /// </summary>
        public void UpdatePosition(Vector2Int newPosition)
        {
            currentPosition = newPosition;
            moveCount++;
        }
        
        /// <summary>
        /// Use a wall (decrease remaining count)
        /// </summary>
        public bool UseWall()
        {
            if (wallsRemaining > 0)
            {
                wallsRemaining--;
                wallsPlaced++;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Check if player has reached win condition
        /// </summary>
        public bool HasWon()
        {
            // For standard 2-player: Player 0 wins by reaching top row, Player 1 wins by reaching bottom row
            // This can be extended for 4-player scenarios
            return currentPosition.y == winPosition.y;
        }
        
        /// <summary>
        /// Start this player's turn
        /// </summary>
        public void StartTurn()
        {
            isActive = true;
            turnStartTime = Time.time;
        }
        
        /// <summary>
        /// End this player's turn
        /// </summary>
        public void EndTurn()
        {
            isActive = false;
        }
        
        /// <summary>
        /// Get turn duration in seconds
        /// </summary>
        public float GetTurnDuration()
        {
            return isActive ? Time.time - turnStartTime : 0f;
        }
    }
    
    /// <summary>
    /// Configuration for a single player in the session
    /// </summary>
    [System.Serializable]
    public class PlayerConfiguration
    {
        public PlayerType playerType;
        public string playerName;
        public Color playerColor;
        public AIOpponent.Difficulty aiDifficulty; // Only used if playerType is AI
        
        public PlayerConfiguration(PlayerType type, string name = null, Color? color = null)
        {
            playerType = type;
            playerName = name ?? (type == PlayerType.AI ? "AI Player" : "Player");
            playerColor = color ?? Color.white;
            aiDifficulty = AIOpponent.Difficulty.Intermediate;
        }
    }
    
    /// <summary>
    /// Settings for configuring a game session
    /// </summary>
    [System.Serializable]
    public class SessionSettings
    {
        [Header("Game Configuration")]
        public int gridSize = 9;
        public int wallsPerPlayer = 9;
        public int playerCount = 2;
        
        [Header("Turn Settings")]
        public float turnTimeLimit = 30f;
        public bool enableTurnTimer = false;
        
        [Header("Player Configurations")]
        public List<PlayerConfiguration> playerConfigurations = new List<PlayerConfiguration>();
        
        /// <summary>
        /// Create default 2-player settings (Human vs AI)
        /// </summary>
        public static SessionSettings CreateDefault()
        {
            var settings = new SessionSettings();
            settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.Human, "Player 1", Color.blue));
            settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.AI, "AI Opponent", Color.red));
            return settings;
        }
        
        /// <summary>
        /// Create 2-player human vs human settings
        /// </summary>
        public static SessionSettings CreateTwoPlayer()
        {
            var settings = new SessionSettings();
            settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.Human, "Player 1", Color.blue));
            settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.Human, "Player 2", Color.red));
            return settings;
        }
        
        /// <summary>
        /// Create 4-player settings
        /// </summary>
        public static SessionSettings CreateFourPlayer()
        {
            var settings = new SessionSettings
            {
                playerCount = 4
            };
            settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.Human, "Player 1", Color.blue));
            settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.AI, "AI 1", Color.red));
            settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.AI, "AI 2", Color.green));
            settings.playerConfigurations.Add(new PlayerConfiguration(PlayerType.AI, "AI 3", Color.yellow));
            return settings;
        }
    }
    
    /// <summary>
    /// Runtime session data containing all session state
    /// </summary>
    [System.Serializable]
    public class SessionData
    {
        [Header("Session Info")]
        public SessionSettings settings;
        public bool isActive;
        public float sessionStartTime;
        public int currentPlayerIndex;
        public int currentTurn;
        
        [Header("Session Results")]
        public SessionEndReason endReason;
        public int winnerIndex = -1;
        public float sessionDuration;
        
        [Header("Players")]
        public List<PlayerData> players = new List<PlayerData>();
        
        public int playerCount => players.Count;
        
        public SessionData(SessionSettings sessionSettings)
        {
            settings = sessionSettings;
            isActive = true;
            sessionStartTime = Time.time;
            currentPlayerIndex = 0;
            currentTurn = 1;
            
            InitializePlayers();
        }
        
        private void InitializePlayers()
        {
            players.Clear();
            
            // Define start and win positions based on player count
            var positions = GetPlayerPositions(settings.playerCount, settings.gridSize);
            
            for (int i = 0; i < settings.playerCount; i++)
            {
                var config = settings.playerConfigurations[i];
                var playerData = new PlayerData(i, config.playerType, positions[i].start, positions[i].win, settings.wallsPerPlayer);
                
                playerData.playerName = config.playerName;
                playerData.playerColor = config.playerColor;
                
                if (config.playerType == PlayerType.AI)
                {
                    playerData.aiDifficulty = config.aiDifficulty;
                }
                
                players.Add(playerData);
            }
            
            // Activate first player
            if (players.Count > 0)
            {
                players[0].StartTurn();
            }
        }
        
        /// <summary>
        /// Get starting and winning positions for players based on game configuration
        /// </summary>
        private List<(Vector2Int start, Vector2Int win)> GetPlayerPositions(int playerCount, int gridSize)
        {
            var positions = new List<(Vector2Int, Vector2Int)>();
            int center = gridSize / 2;
            
            switch (playerCount)
            {
                case 2:
                    // Standard 2-player: bottom center vs top center
                    positions.Add((new Vector2Int(center, 0), new Vector2Int(center, gridSize - 1)));
                    positions.Add((new Vector2Int(center, gridSize - 1), new Vector2Int(center, 0)));
                    break;
                    
                case 4:
                    // 4-player: center of each side
                    positions.Add((new Vector2Int(center, 0), new Vector2Int(center, gridSize - 1))); // Bottom to top
                    positions.Add((new Vector2Int(0, center), new Vector2Int(gridSize - 1, center))); // Left to right
                    positions.Add((new Vector2Int(center, gridSize - 1), new Vector2Int(center, 0))); // Top to bottom
                    positions.Add((new Vector2Int(gridSize - 1, center), new Vector2Int(0, center))); // Right to left
                    break;
                    
                default:
                    Debug.LogError($"Unsupported player count: {playerCount}");
                    // Fallback to 2-player
                    positions.Add((new Vector2Int(center, 0), new Vector2Int(center, gridSize - 1)));
                    positions.Add((new Vector2Int(center, gridSize - 1), new Vector2Int(center, 0)));
                    break;
            }
            
            return positions;
        }
        
        /// <summary>
        /// Get player data by index
        /// </summary>
        public PlayerData GetPlayer(int index)
        {
            return index >= 0 && index < players.Count ? players[index] : null;
        }
        
        /// <summary>
        /// Get currently active player
        /// </summary>
        public PlayerData GetCurrentPlayer()
        {
            return GetPlayer(currentPlayerIndex);
        }
        
        /// <summary>
        /// Move to next player's turn
        /// </summary>
        public PlayerData NextPlayer()
        {
            // End current player's turn
            var currentPlayer = GetCurrentPlayer();
            currentPlayer?.EndTurn();
            
            // Move to next player
            currentPlayerIndex = (currentPlayerIndex + 1) % playerCount;
            if (currentPlayerIndex == 0) // Completed a full round
            {
                currentTurn++;
            }
            
            // Start new player's turn
            var nextPlayer = GetCurrentPlayer();
            nextPlayer?.StartTurn();
            
            return nextPlayer;
        }
        
        /// <summary>
        /// End the session
        /// </summary>
        public void EndSession(SessionEndReason reason, int winner = -1)
        {
            isActive = false;
            endReason = reason;
            winnerIndex = winner;
            sessionDuration = Time.time - sessionStartTime;
            
            // End current player's turn
            GetCurrentPlayer()?.EndTurn();
        }
        
        /// <summary>
        /// Get session statistics
        /// </summary>
        public SessionStats GetStats()
        {
            return new SessionStats
            {
                totalTurns = currentTurn,
                sessionDuration = isActive ? Time.time - sessionStartTime : sessionDuration,
                winner = winnerIndex >= 0 ? GetPlayer(winnerIndex) : null,
                totalMoves = players.ConvertAll(p => p.moveCount),
                totalWallsPlaced = players.ConvertAll(p => p.wallsPlaced)
            };
        }
    }
    
    /// <summary>
    /// Statistics about a completed session
    /// </summary>
    public class SessionStats
    {
        public int totalTurns;
        public float sessionDuration;
        public PlayerData winner;
        public List<int> totalMoves;
        public List<int> totalWallsPlaced;
        
        public override string ToString()
        {
            return $"Session Stats:\n" +
                   $"Duration: {sessionDuration:F1}s\n" +
                   $"Turns: {totalTurns}\n" +
                   $"Winner: {winner?.playerName ?? "None"}";
        }
    }
}