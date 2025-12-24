using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WallChess.Core;
using WallChess.Core.Data;
using WallChess.Core.Config;
using WallChess.Core.Rules;
using WallChess.AI.Strategies;

namespace WallChess.AI
{
    /// <summary>
    /// Controller that manages AI turn execution.
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameController gameController;
        [SerializeField] private GameRulesConfig rulesConfig;

        [Header("Default Personalities")]
        [SerializeField] private AIPersonalityConfig balancedPersonality;
        [SerializeField] private AIPersonalityConfig aggressivePersonality;
        [SerializeField] private AIPersonalityConfig defensivePersonality;

        // Strategies
        private readonly Dictionary<string, IAIStrategy> _strategies = new();
        private QuoridorRules _rules;

        // State
        private Coroutine _currentTurnCoroutine;

        #region Unity Lifecycle

        private void Awake()
        {
            // Register built-in strategies
            _strategies["Balanced"] = new BalancedStrategy();
            _strategies["Aggressive"] = new AggressiveStrategy();
            _strategies["Defensive"] = new DefensiveStrategy();

            _rules = new QuoridorRules(rulesConfig);
        }

        private void OnEnable()
        {
            GameEvents.OnTurnStarted += OnTurnStarted;
            GameEvents.OnGameOver += OnGameOver;
        }

        private void OnDisable()
        {
            GameEvents.OnTurnStarted -= OnTurnStarted;
            GameEvents.OnGameOver -= OnGameOver;
        }

        #endregion

        #region Event Handlers

        private void OnTurnStarted(int playerIndex)
        {
            if (gameController?.State == null) return;

            var player = gameController.State.GetPlayer(playerIndex);
            if (player == null || !player.IsAI) return;

            // Start AI turn
            if (_currentTurnCoroutine != null)
                StopCoroutine(_currentTurnCoroutine);

            _currentTurnCoroutine = StartCoroutine(ExecuteAITurn(player));
        }

        private void OnGameOver(int winner)
        {
            if (_currentTurnCoroutine != null)
            {
                StopCoroutine(_currentTurnCoroutine);
                _currentTurnCoroutine = null;
            }
        }

        #endregion

        #region AI Execution

        private IEnumerator ExecuteAITurn(PlayerState player)
        {
            // Think delay for natural UX
            float thinkDelay = GetThinkDelay();
            yield return new WaitForSeconds(thinkDelay);

            // Get strategy
            IAIStrategy strategy = GetStrategy(player.AIPersonality);
            AIEvaluationParams parameters = GetParameters(player);

            // Create game view
            var gameView = new AIGameView(gameController.State, _rules, player.PlayerIndex);

            // Get decision
            AIDecision decision = strategy.Decide(gameView, parameters);

            // Apply difficulty-based error
            decision = ApplyDifficultyModifier(decision, player.Difficulty, gameView);

            Debug.Log($"AI [{player.DisplayName}] Decision: {decision}");

            // Execute decision
            if (decision.ActionType == AIActionType.Move)
            {
                var result = gameController.TryMovePawn(player.PlayerIndex, decision.MoveTarget);
                if (!result.IsSuccess)
                {
                    Debug.LogWarning($"AI move failed: {result.FailureReason}");
                    // Fallback: try any valid move
                    var fallbackMoves = gameView.GetMyValidMoves();
                    if (fallbackMoves.Count > 0)
                    {
                        gameController.TryMovePawn(player.PlayerIndex, fallbackMoves[0]);
                    }
                }
            }
            else
            {
                var result = gameController.TryPlaceWall(player.PlayerIndex, decision.WallPlacement);
                if (!result.IsSuccess)
                {
                    Debug.LogWarning($"AI wall placement failed: {result.FailureReason}");
                    // Fallback: move instead
                    var fallbackMoves = gameView.GetMyValidMoves();
                    if (fallbackMoves.Count > 0)
                    {
                        gameController.TryMovePawn(player.PlayerIndex, fallbackMoves[0]);
                    }
                }
            }

            _currentTurnCoroutine = null;
        }

        private float GetThinkDelay()
        {
            if (rulesConfig == null)
                return Random.Range(0.3f, 1.0f);

            return Random.Range(rulesConfig.aiThinkDelayMin, rulesConfig.aiThinkDelayMax);
        }

        private IAIStrategy GetStrategy(string personalityName)
        {
            if (_strategies.TryGetValue(personalityName, out var strategy))
                return strategy;

            return _strategies["Balanced"]; // Fallback
        }

        private AIEvaluationParams GetParameters(PlayerState player)
        {
            AIPersonalityConfig personality = GetPersonalityConfig(player.AIPersonality);

            if (personality != null)
                return personality.GetEvaluationParams(player.Difficulty);

            // Default parameters
            return new AIEvaluationParams
            {
                OptimalChance = 0.7f,
                LookAhead = 2,
                MoveWeight = 1f,
                AggressionWeight = 1f,
                PathWeight = 1f,
                CenterWeight = 0.5f,
                WallThreshold = 2,
                MoveJitter = 0.1f,
                WallJitter = 0.15f,
                WallReserve = 2,
                WallRadius = 3
            };
        }

        private AIPersonalityConfig GetPersonalityConfig(string name)
        {
            return name switch
            {
                "Aggressive" => aggressivePersonality,
                "Defensive" => defensivePersonality,
                "Balanced" => balancedPersonality,
                _ => balancedPersonality
            };
        }

        private AIDecision ApplyDifficultyModifier(AIDecision decision, AIDifficulty difficulty, IAIGameView gameView)
        {
            // Lower difficulties have chance to make suboptimal moves
            float mistakeChance = difficulty switch
            {
                AIDifficulty.Easy => 0.4f,
                AIDifficulty.Medium => 0.15f,
                AIDifficulty.Hard => 0.05f,
                AIDifficulty.Expert => 0f,
                _ => 0.1f
            };

            if (Random.value < mistakeChance)
            {
                // Pick a random valid move instead
                var validMoves = gameView.GetMyValidMoves();
                if (validMoves.Count > 0)
                {
                    var randomMove = validMoves[Random.Range(0, validMoves.Count)];
                    return AIDecision.CreateMove(
                        randomMove,
                        0.3f,
                        $"[Difficulty adjustment] Random move"
                    );
                }
            }

            return decision;
        }

        #endregion

        #region Public Methods

        public void SetGameController(GameController controller)
        {
            gameController = controller;
        }

        public void RegisterStrategy(string name, IAIStrategy strategy)
        {
            _strategies[name] = strategy;
        }

        #endregion
    }
}
