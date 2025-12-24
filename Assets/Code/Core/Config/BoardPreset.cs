using UnityEngine;
using WallChess.Core.Data;

namespace WallChess.Core.Config
{
    /// <summary>
    /// Pre-configured board layout for quick game setup.
    /// Create via: Assets > Create > WallChess > Board Preset
    /// </summary>
    [CreateAssetMenu(fileName = "BoardPreset", menuName = "WallChess/Board Preset")]
    public class BoardPreset : ScriptableObject
    {
        [Header("Preset Info")]
        public string presetName = "Standard 2-Player";

        [TextArea(2, 4)]
        public string description = "Standard 9x9 Quoridor with 10 walls per player";

        [Header("References")]
        public BoardConfig boardConfig;
        public GameRulesConfig rulesConfig;

        [Header("Player Setup")]
        public PlayerSetup[] playerSetups = new PlayerSetup[2];

        [Header("AI Defaults")]
        public AIPersonalityConfig defaultAIPersonality;

        /// <summary>
        /// Creates a GameState from this preset.
        /// </summary>
        public GameState CreateGameState()
        {
            int gridWidth = boardConfig != null ? boardConfig.gridWidth : 9;
            int gridHeight = boardConfig != null ? boardConfig.gridHeight : 9;
            int wallsPerPlayer = rulesConfig != null ? rulesConfig.wallsPerPlayer : 10;

            var players = new PlayerState[playerSetups.Length];

            for (int i = 0; i < playerSetups.Length; i++)
            {
                var setup = playerSetups[i];
                var startPos = GetStartPosition(i, gridWidth, gridHeight, playerSetups.Length);
                var goal = GetGoalDirection(i, playerSetups.Length);

                players[i] = new PlayerState(
                    i,
                    setup.displayName,
                    setup.playerType,
                    startPos,
                    goal,
                    wallsPerPlayer,
                    setup.aiDifficulty,
                    setup.aiPersonality != null ? setup.aiPersonality.personalityName : "Balanced"
                );
            }

            return new GameState(gridWidth, gridHeight, players);
        }

        /// <summary>
        /// Gets the standard start position for a player index.
        /// For 2-player: Players start at opposite ends of the vertical axis (top/bottom).
        /// For 4-player: Two on vertical axis, two on horizontal axis.
        /// </summary>
        public static BoardPosition GetStartPosition(int playerIndex, int gridWidth, int gridHeight, int playerCount)
        {
            int centerX = gridWidth / 2;
            int centerY = gridHeight / 2;

            return playerCount switch
            {
                2 => playerIndex switch
                {
                    0 => new BoardPosition(centerX, 0),                // Bottom center
                    1 => new BoardPosition(centerX, gridHeight - 1),   // Top center
                    _ => new BoardPosition(centerX, 0)
                },
                4 => playerIndex switch
                {
                    0 => new BoardPosition(centerX, 0),                // Bottom
                    1 => new BoardPosition(centerX, gridHeight - 1),   // Top
                    2 => new BoardPosition(0, centerY),                // Left
                    3 => new BoardPosition(gridWidth - 1, centerY),    // Right
                    _ => new BoardPosition(centerX, 0)
                },
                _ => new BoardPosition(centerX, 0)
            };
        }

        /// <summary>
        /// Backwards compatibility for square boards.
        /// </summary>
        public static BoardPosition GetStartPosition(int playerIndex, int gridSize, int playerCount) =>
            GetStartPosition(playerIndex, gridSize, gridSize, playerCount);

        /// <summary>
        /// Gets the goal direction for a player index.
        /// </summary>
        public static GoalDirection GetGoalDirection(int playerIndex, int playerCount)
        {
            return playerCount switch
            {
                2 => playerIndex switch
                {
                    0 => GoalDirection.North,
                    1 => GoalDirection.South,
                    _ => GoalDirection.North
                },
                4 => playerIndex switch
                {
                    0 => GoalDirection.North,
                    1 => GoalDirection.South,
                    2 => GoalDirection.East,
                    3 => GoalDirection.West,
                    _ => GoalDirection.North
                },
                _ => GoalDirection.North
            };
        }

        private void OnValidate()
        {
            // Ensure we have at least 2 player setups
            if (playerSetups == null || playerSetups.Length < 2)
            {
                playerSetups = new PlayerSetup[2];
                playerSetups[0] = new PlayerSetup { displayName = "Player 1", playerType = PlayerType.Human };
                playerSetups[1] = new PlayerSetup { displayName = "Player 2", playerType = PlayerType.AI };
            }
        }

        private void Reset()
        {
            presetName = "Standard 2-Player";
            description = "Standard 9x9 Quoridor with 10 walls per player";

            playerSetups = new PlayerSetup[]
            {
                new() { displayName = "Player 1", playerType = PlayerType.Human, playerColor = new Color(0.2f, 0.6f, 1f) },
                new() { displayName = "Player 2", playerType = PlayerType.AI, aiDifficulty = AIDifficulty.Medium, playerColor = new Color(1f, 0.4f, 0.2f) }
            };
        }
    }

    /// <summary>
    /// Player setup data for presets.
    /// </summary>
    [System.Serializable]
    public class PlayerSetup
    {
        public string displayName = "Player";
        public PlayerType playerType = PlayerType.Human;
        public AIDifficulty aiDifficulty = AIDifficulty.Medium;
        public AIPersonalityConfig aiPersonality;
        public Color playerColor = Color.white;
    }
}
