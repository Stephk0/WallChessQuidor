using UnityEngine;
using WallChess.Core;
using WallChess.Core.Data;

namespace WallChess.Input
{
    /// <summary>
    /// Unified input handler that routes to appropriate handlers.
    /// Touch-first design, works with mouse as fallback.
    /// </summary>
    public class InputRouter : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private LayerMask tileLayer;
        [SerializeField] private LayerMask pawnLayer;
        [SerializeField] private LayerMask wallGapLayer;
        [SerializeField] private float raycastDistance = 100f;

        [Header("References")]
        [SerializeField] private GameController gameController;

        // State
        private bool _inputEnabled = true;
        private IInputHandler _activeHandler;
        private InputData _currentInput;

        // Handlers
        private PawnInputHandler _pawnHandler;
        private WallInputHandler _wallHandler;

        // Combined layer mask
        private int _combinedLayers;

        #region Unity Lifecycle

        private void Awake()
        {
            if (gameCamera == null)
                gameCamera = Camera.main;

            _combinedLayers = tileLayer | pawnLayer | wallGapLayer;

            // Create handlers
            _pawnHandler = new PawnInputHandler(gameController);
            _wallHandler = new WallInputHandler(gameController);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStarted += OnGameStarted;
            GameEvents.OnGameOver += OnGameOver;
            GameEvents.OnGamePaused += OnGamePaused;
            GameEvents.OnGameResumed += OnGameResumed;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStarted -= OnGameStarted;
            GameEvents.OnGameOver -= OnGameOver;
            GameEvents.OnGamePaused -= OnGamePaused;
            GameEvents.OnGameResumed -= OnGameResumed;
        }

        private void Update()
        {
            if (!_inputEnabled) return;
            if (gameController?.State?.IsPlaying != true) return;
            if (gameController.IsCurrentPlayerAI()) return;

            ProcessInput();
        }

        #endregion

        #region Event Handlers

        private void OnGameStarted()
        {
            _inputEnabled = true;
        }

        private void OnGameOver(int winner)
        {
            _inputEnabled = false;
            CancelCurrentHandler();
        }

        private void OnGamePaused()
        {
            _inputEnabled = false;
            CancelCurrentHandler();
        }

        private void OnGameResumed()
        {
            _inputEnabled = true;
        }

        #endregion

        #region Input Processing

        private void ProcessInput()
        {
            // Unified touch/mouse handling
            bool hasTouchInput = UnityEngine.Input.touchCount > 0;

            if (hasTouchInput)
            {
                ProcessTouch(UnityEngine.Input.GetTouch(0));
            }
            else
            {
                ProcessMouse();
            }
        }

        private void ProcessTouch(Touch touch)
        {
            var input = CreateInputData(touch.position);
            input.Phase = touch.phase switch
            {
                TouchPhase.Began => InputPhase.Begin,
                TouchPhase.Moved => InputPhase.Move,
                TouchPhase.Stationary => InputPhase.Move,
                TouchPhase.Ended => InputPhase.End,
                TouchPhase.Canceled => InputPhase.Cancel,
                _ => InputPhase.Cancel
            };

            HandleInput(input);
        }

        private void ProcessMouse()
        {
            var input = CreateInputData(UnityEngine.Input.mousePosition);

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                input.Phase = InputPhase.Begin;
                HandleInput(input);
            }
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                input.Phase = InputPhase.Move;
                HandleInput(input);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                input.Phase = InputPhase.End;
                HandleInput(input);
            }

            // Right-click or Escape to cancel
            if (UnityEngine.Input.GetMouseButtonDown(1) || UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                CancelCurrentHandler();
            }
        }

        private void HandleInput(InputData input)
        {
            _currentInput = input;

            switch (input.Phase)
            {
                case InputPhase.Begin:
                    HandleInputBegin(input);
                    break;
                case InputPhase.Move:
                    HandleInputMove(input);
                    break;
                case InputPhase.End:
                    HandleInputEnd(input);
                    break;
                case InputPhase.Cancel:
                    CancelCurrentHandler();
                    break;
            }
        }

        private void HandleInputBegin(InputData input)
        {
            // Determine which handler should receive input
            if (_pawnHandler.CanHandle(input))
            {
                _activeHandler = _pawnHandler;
            }
            else if (_wallHandler.CanHandle(input))
            {
                _activeHandler = _wallHandler;
            }
            else
            {
                _activeHandler = null;
            }

            _activeHandler?.OnInputBegin(input);
        }

        private void HandleInputMove(InputData input)
        {
            _activeHandler?.OnInputMove(input);
        }

        private void HandleInputEnd(InputData input)
        {
            _activeHandler?.OnInputEnd(input);
            _activeHandler = null;
        }

        private void CancelCurrentHandler()
        {
            _activeHandler?.OnCancel();
            _activeHandler = null;
            GameEvents.RaiseValidMovesCleared();
        }

        #endregion

        #region Raycasting

        private InputData CreateInputData(Vector2 screenPos)
        {
            var data = new InputData
            {
                ScreenPosition = screenPos,
                IsValid = false
            };

            if (gameCamera == null) return data;

            Ray ray = gameCamera.ScreenPointToRay(screenPos);

            // First check for pawn hits (higher priority)
            if (Physics.Raycast(ray, out RaycastHit pawnHit, raycastDistance, pawnLayer))
            {
                data.WorldPosition = pawnHit.point;
                data.HitType = HitType.Pawn;
                data.IsValid = true;

                // Try to get player index from pawn
                var pawnView = pawnHit.collider.GetComponentInParent<View.Pawn.PawnView>();
                if (pawnView != null)
                {
                    data.HitPlayerIndex = pawnView.PlayerIndex;
                    data.GridPosition = pawnView.CurrentPosition;
                }
                else if (gameController?.Board != null)
                {
                    data.GridPosition = gameController.Board.WorldToGrid(pawnHit.point);
                }

                return data;
            }

            // Then check for wall gap hits
            if (Physics.Raycast(ray, out RaycastHit gapHit, raycastDistance, wallGapLayer))
            {
                data.WorldPosition = gapHit.point;
                data.HitType = HitType.WallGap;
                data.IsValid = true;

                if (gameController?.Board != null)
                {
                    data.GridPosition = gameController.Board.WorldToGrid(gapHit.point);
                }

                return data;
            }

            // Finally check for tile hits
            if (Physics.Raycast(ray, out RaycastHit tileHit, raycastDistance, tileLayer))
            {
                data.WorldPosition = tileHit.point;
                data.HitType = HitType.Tile;
                data.IsValid = true;

                if (gameController?.Board != null)
                {
                    data.GridPosition = gameController.Board.WorldToGrid(tileHit.point);
                }

                return data;
            }

            return data;
        }

        #endregion

        #region Public Methods

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                CancelCurrentHandler();
            }
        }

        public void SetGameController(GameController controller)
        {
            gameController = controller;
            _pawnHandler = new PawnInputHandler(controller);
            _wallHandler = new WallInputHandler(controller);
        }

        #endregion
    }
}
