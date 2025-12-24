using UnityEngine;
using WallChess.Core.Data;

namespace WallChess.View.UI
{
    /// <summary>
    /// Root UI controller that manages all game UI elements.
    /// </summary>
    public class GameUIView : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Sub-views")]
        [SerializeField] private TurnIndicatorView turnIndicator;
        [SerializeField] private WallCountView[] wallCountViews;
        [SerializeField] private GameOverView gameOverView;

        [Header("Safe Area")]
        [SerializeField] private RectTransform safeAreaRect;

        private ViewConfig _viewConfig;
        private GameState _gameState;

        private void Awake()
        {
            ApplySafeArea();
        }

        public void Initialize(GameState state, ViewConfig config)
        {
            _gameState = state;
            _viewConfig = config;

            // Initialize wall count views for each player
            if (wallCountViews != null)
            {
                for (int i = 0; i < wallCountViews.Length && i < state.PlayerCount; i++)
                {
                    var player = state.GetPlayer(i);
                    if (wallCountViews[i] != null && player != null)
                    {
                        wallCountViews[i].Initialize(i, player.WallsRemaining, player.DisplayName);
                    }
                }
            }

            // Hide game over panel
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            // Initialize turn indicator
            if (turnIndicator != null)
            {
                turnIndicator.Initialize(state.PlayerCount);
            }
        }

        public void ShowGameUI()
        {
            if (gamePanel != null)
            {
                gamePanel.SetActive(true);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        public void UpdateTurnIndicator(int playerIndex, PlayerState player)
        {
            if (turnIndicator != null)
            {
                string playerName = player?.DisplayName ?? $"Player {playerIndex + 1}";
                bool isAI = player?.IsAI ?? false;
                turnIndicator.SetCurrentPlayer(playerIndex, playerName, isAI);
            }
        }

        public void UpdateWallCount(int playerIndex, int wallsRemaining)
        {
            if (wallCountViews != null && playerIndex < wallCountViews.Length)
            {
                wallCountViews[playerIndex]?.UpdateCount(wallsRemaining);
            }
        }

        public void ShowGameOver(int winnerIndex, string winnerName)
        {
            if (gameOverView != null)
            {
                gameOverView.Show(winnerIndex, winnerName);
            }

            if (gameOverPanel != null)
            {
                // Delayed show for dramatic effect
                float delay = _viewConfig?.gameOverDelay ?? 1.5f;
                Invoke(nameof(ShowGameOverPanel), delay);
            }
        }

        private void ShowGameOverPanel()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        private void ApplySafeArea()
        {
            if (safeAreaRect == null) return;

            var safeArea = Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;
        }

        #region Button Handlers

        public void OnRestartClicked()
        {
            // Fire restart event - GameController will handle
            Core.GameEvents.RequestRestart();
        }

        public void OnMainMenuClicked()
        {
            // Fire main menu event
            Core.GameEvents.RequestMainMenu();
        }

        public void OnUndoClicked()
        {
            Core.GameEvents.RequestUndo();
        }

        #endregion
    }
}
