using UnityEngine;
using WallChess.Core;
using WallChess.Core.Config;
using WallChess.Core.Data;
using WallChess.View.Camera;

namespace WallChess.View
{
    /// <summary>
    /// Central model-view binding system. Subscribes to GameEvents and updates views.
    /// </summary>
    public class GameViewBinder : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ViewConfig viewConfig;
        [SerializeField] private BoardConfig boardConfig;

        [Header("View References")]
        [SerializeField] private Board.BoardView boardView;
        [SerializeField] private Transform pawnContainer;
        [SerializeField] private Transform wallContainer;

        [Header("Camera References")]
        [SerializeField] private CameraFramingController cameraFraming;

        [Header("UI References")]
        [SerializeField] private UI.GameUIView gameUI;

        // Runtime state
        private Pawn.PawnView[] _pawnViews;
        private GameState _boundState;

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            GameEvents.OnGameInitialized += HandleGameInitialized;
            GameEvents.OnGameStarted += HandleGameStarted;
            GameEvents.OnTurnStarted += HandleTurnStarted;
            GameEvents.OnPawnMoved += HandlePawnMoved;
            GameEvents.OnWallPlaced += HandleWallPlaced;
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnValidMovesUpdated += HandleValidMovesUpdated;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnGameInitialized -= HandleGameInitialized;
            GameEvents.OnGameStarted -= HandleGameStarted;
            GameEvents.OnTurnStarted -= HandleTurnStarted;
            GameEvents.OnPawnMoved -= HandlePawnMoved;
            GameEvents.OnWallPlaced -= HandleWallPlaced;
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnValidMovesUpdated -= HandleValidMovesUpdated;
        }

        #endregion

        #region Event Handlers

        private void HandleGameInitialized(GameState state)
        {
            _boundState = state;

            // Create board visuals
            if (boardView != null)
            {
                boardView.Initialize(state.BoardWidth, state.BoardHeight, viewConfig);
            }

            // Initialize camera framing (after board is initialized so we know bounds)
            InitializeCameraFraming();

            // Create pawns
            CreatePawns(state);

            // Setup UI
            if (gameUI != null)
            {
                gameUI.Initialize(state, viewConfig);
            }
        }

        private void InitializeCameraFraming()
        {
            // Get framing config from board config
            var framingConfig = boardConfig?.cameraFramingConfig;

            if (cameraFraming != null && boardConfig != null && framingConfig != null)
            {
                // Use runtime dimensions from bound game state if available
                if (_boundState != null)
                {
                    cameraFraming.Initialize(boardConfig, framingConfig, _boundState.BoardWidth, _boundState.BoardHeight);
                }
                else
                {
                    cameraFraming.Initialize(boardConfig, framingConfig);
                }
            }
        }

        private void HandleGameStarted()
        {
            if (gameUI != null)
            {
                gameUI.ShowGameUI();
            }
        }

        private void HandleTurnStarted(int playerIndex)
        {
            if (gameUI != null)
            {
                gameUI.UpdateTurnIndicator(playerIndex, _boundState?.GetPlayer(playerIndex));
            }

            // Clear previous highlights
            if (boardView != null)
            {
                boardView.ClearHighlights();
            }
        }

        private void HandlePawnMoved(int playerIndex, BoardPosition from, BoardPosition to, bool wasJump)
        {
            if (_pawnViews != null && playerIndex < _pawnViews.Length)
            {
                var pawnView = _pawnViews[playerIndex];
                if (pawnView != null)
                {
                    pawnView.MoveTo(to);
                }
            }

            // Clear move highlights
            if (boardView != null)
            {
                boardView.ClearHighlights();
            }
        }

        private void HandleWallPlaced(int playerIndex, WallPlacement wall)
        {
            // Spawn wall view
            SpawnWallView(wall);

            // Update UI wall count
            if (gameUI != null && _boundState != null)
            {
                var player = _boundState.GetPlayer(playerIndex);
                gameUI.UpdateWallCount(playerIndex, player?.WallsRemaining ?? 0);
            }
        }

        private void HandleGameOver(int winnerIndex)
        {
            if (gameUI != null)
            {
                var winner = _boundState?.GetPlayer(winnerIndex);
                gameUI.ShowGameOver(winnerIndex, winner?.DisplayName ?? $"Player {winnerIndex + 1}");
            }
        }

        private void HandleValidMovesUpdated(System.Collections.Generic.List<BoardPosition> validMoves)
        {
            if (boardView != null)
            {
                boardView.ShowMoveHighlights(validMoves, viewConfig.validMoveColor);
            }
        }

        #endregion

        #region View Creation

        private void CreatePawns(GameState state)
        {
            // Clean up existing pawns
            if (_pawnViews != null)
            {
                foreach (var pawn in _pawnViews)
                {
                    if (pawn != null)
                        Destroy(pawn.gameObject);
                }
            }

            _pawnViews = new Pawn.PawnView[state.PlayerCount];

            for (int i = 0; i < state.PlayerCount; i++)
            {
                var player = state.GetPlayer(i);
                var prefab = viewConfig.GetPawnPrefab(i);

                if (prefab == null)
                {
                    Debug.LogWarning($"No pawn prefab for player {i}");
                    continue;
                }

                var pawnObj = Instantiate(prefab, pawnContainer);
                var pawnView = pawnObj.GetComponent<Pawn.PawnView>();

                if (pawnView == null)
                {
                    pawnView = pawnObj.AddComponent<Pawn.PawnView>();
                }

                pawnView.Initialize(i, player.CurrentPosition, viewConfig, boardView);
                _pawnViews[i] = pawnView;
            }
        }

        private void SpawnWallView(WallPlacement wall)
        {
            if (viewConfig.wallPrefab == null)
            {
                Debug.LogWarning("No wall prefab assigned");
                return;
            }

            var wallObj = Instantiate(viewConfig.wallPrefab, wallContainer);
            var wallView = wallObj.GetComponent<Wall.WallView>();

            if (wallView == null)
            {
                wallView = wallObj.AddComponent<Wall.WallView>();
            }

            wallView.Initialize(wall, boardView);
        }

        #endregion

        #region Public Methods

        public void ShowMoveConfirmHighlight(BoardPosition position)
        {
            if (boardView != null)
            {
                boardView.ShowConfirmHighlight(position, viewConfig.confirmColor);
            }
        }

        public void ClearConfirmHighlight()
        {
            if (boardView != null)
            {
                boardView.ClearConfirmHighlight();
            }
        }

        public void ShowWallPreview(WallPlacement wall, bool isValid)
        {
            if (boardView != null)
            {
                var color = isValid ? viewConfig.wallPreviewValid : viewConfig.wallPreviewInvalid;
                boardView.ShowWallPreview(wall, color);
            }
        }

        public void HideWallPreview()
        {
            if (boardView != null)
            {
                boardView.HideWallPreview();
            }
        }

        #endregion
    }
}
