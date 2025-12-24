using UnityEngine;

namespace WallChess.Core.Config
{
    /// <summary>
    /// Action when turn timer expires.
    /// </summary>
    public enum TurnTimeoutAction
    {
        ForceRandomMove,
        SkipTurn,
        ForfeitGame
    }

    /// <summary>
    /// Win condition type.
    /// </summary>
    public enum WinConditionType
    {
        ReachOppositeSide,  // Standard Quoridor
        ReachAnyEdge,       // Variant
    }

    /// <summary>
    /// Game rules and constraints.
    /// Create via: Assets > Create > WallChess > Game Rules
    /// </summary>
    [CreateAssetMenu(fileName = "GameRules", menuName = "WallChess/Game Rules")]
    public class GameRulesConfig : ScriptableObject
    {
        [Header("Player Settings")]
        [Range(2, 4)]
        [Tooltip("Number of players in the game")]
        public int playerCount = 2;

        [Range(5, 20)]
        [Tooltip("Number of walls each player starts with")]
        public int wallsPerPlayer = 10;

        [Header("Movement Rules")]
        [Tooltip("Allow jumping over adjacent opponents")]
        public bool allowJumping = true;

        [Tooltip("Allow diagonal jumps when straight jump is blocked")]
        public bool allowDiagonalJumps = true;

        [Header("Wall Rules")]
        [Tooltip("Walls must not completely block a player's path to goal")]
        public bool enforcePathToGoal = true;

        [Tooltip("Allow placing walls on first turn")]
        public bool allowWallOnFirstTurn = true;

        [Header("Turn Timer")]
        [Tooltip("Enable turn time limit")]
        public bool enableTurnTimer = false;

        [Range(10f, 300f)]
        [Tooltip("Time limit per turn in seconds")]
        public float turnTimeLimit = 60f;

        [Tooltip("Action when timer expires")]
        public TurnTimeoutAction timeoutAction = TurnTimeoutAction.ForceRandomMove;

        [Header("Win Conditions")]
        [Tooltip("How to determine victory")]
        public WinConditionType winCondition = WinConditionType.ReachOppositeSide;

        [Header("Game Feel")]
        [Range(0.1f, 1f)]
        [Tooltip("Duration of pawn movement animation")]
        public float pawnMoveDuration = 0.3f;

        [Range(0.1f, 0.5f)]
        [Tooltip("Delay before AI makes its move")]
        public float aiThinkDelayMin = 0.3f;

        [Range(0.5f, 2f)]
        [Tooltip("Maximum AI think delay")]
        public float aiThinkDelayMax = 1.5f;

        private void OnValidate()
        {
            if (playerCount < 2) playerCount = 2;
            if (playerCount > 4) playerCount = 4;
            if (wallsPerPlayer < 1) wallsPerPlayer = 1;
            if (aiThinkDelayMax < aiThinkDelayMin) aiThinkDelayMax = aiThinkDelayMin;
        }
    }
}
