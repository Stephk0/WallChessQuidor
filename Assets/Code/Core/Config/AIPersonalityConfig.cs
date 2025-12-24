using UnityEngine;
using WallChess.Core.Data;

namespace WallChess.Core.Config
{
    /// <summary>
    /// AI behavior parameters. Different personalities create varied playstyles.
    /// Create via: Assets > Create > WallChess > AI Personality
    /// </summary>
    [CreateAssetMenu(fileName = "AIPersonality", menuName = "WallChess/AI Personality")]
    public class AIPersonalityConfig : ScriptableObject
    {
        [Header("Identity")]
        public string personalityName = "Balanced";

        [TextArea(2, 4)]
        public string description = "A balanced AI that considers both movement and wall placement";

        [Header("Difficulty Modifiers")]
        [Range(0f, 1f)]
        [Tooltip("0 = always makes mistakes, 1 = always optimal")]
        public float optimalPlayChance = 0.7f;

        [Range(0, 3)]
        [Tooltip("How many moves ahead to evaluate")]
        public int lookAheadDepth = 2;

        [Header("Strategy Weights")]
        [Range(0f, 2f)]
        [Tooltip("Higher = prefers moving over placing walls")]
        public float movePreference = 1.0f;

        [Range(0f, 2f)]
        [Tooltip("Higher = more aggressive blocking")]
        public float aggressiveness = 1.0f;

        [Range(0f, 2f)]
        [Tooltip("Higher = prioritizes own path efficiency")]
        public float pathEfficiencyWeight = 1.0f;

        [Range(0f, 2f)]
        [Tooltip("Higher = prioritizes center board control")]
        public float centerControlWeight = 0.5f;

        [Header("Wall Placement Strategy")]
        [Range(0, 10)]
        [Tooltip("Minimum walls to keep in reserve")]
        public int wallReserve = 2;

        [Range(1, 5)]
        [Tooltip("How far from opponent to consider wall placement")]
        public int wallPlacementRadius = 3;

        [Tooltip("Minimum path length increase to place wall instead of move")]
        public int wallAdvantageThreshold = 2;

        [Header("Randomization (for variety)")]
        [Range(0f, 0.3f)]
        [Tooltip("Random factor added to move scores")]
        public float moveScoreJitter = 0.1f;

        [Range(0f, 0.3f)]
        [Tooltip("Random factor added to wall scores")]
        public float wallScoreJitter = 0.15f;

        /// <summary>
        /// Gets effective parameters adjusted for difficulty.
        /// </summary>
        public AIEvaluationParams GetEvaluationParams(AIDifficulty difficulty)
        {
            float difficultyMod = difficulty switch
            {
                AIDifficulty.Easy => 0.5f,
                AIDifficulty.Medium => 0.75f,
                AIDifficulty.Hard => 1.0f,
                AIDifficulty.Expert => 1.2f,
                _ => 1.0f
            };

            return new AIEvaluationParams
            {
                OptimalChance = Mathf.Clamp01(optimalPlayChance * difficultyMod),
                LookAhead = Mathf.Max(1, Mathf.RoundToInt(lookAheadDepth * difficultyMod)),
                MoveWeight = movePreference,
                AggressionWeight = aggressiveness * difficultyMod,
                PathWeight = pathEfficiencyWeight,
                CenterWeight = centerControlWeight,
                WallThreshold = Mathf.Max(1, Mathf.RoundToInt(wallAdvantageThreshold / difficultyMod)),
                MoveJitter = moveScoreJitter * (2f - difficultyMod),
                WallJitter = wallScoreJitter * (2f - difficultyMod),
                WallReserve = wallReserve,
                WallRadius = wallPlacementRadius
            };
        }
    }

    /// <summary>
    /// Runtime AI evaluation parameters after difficulty adjustment.
    /// </summary>
    [System.Serializable]
    public struct AIEvaluationParams
    {
        public float OptimalChance;
        public int LookAhead;
        public float MoveWeight;
        public float AggressionWeight;
        public float PathWeight;
        public float CenterWeight;
        public int WallThreshold;
        public float MoveJitter;
        public float WallJitter;
        public int WallReserve;
        public int WallRadius;
    }
}
