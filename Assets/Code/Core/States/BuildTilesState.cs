using UnityEngine;
using WallChess.Core.Session;

namespace WallChess.Core.States
{
    /// <summary>
    /// State that handles the tile building animation phase
    /// Transitions to SpawnPawnsState when tiles are fully built
    /// </summary>
    public class BuildTilesState : BaseState
    {
        private TileAnimationController tileAnimationController;
        private bool animationStarted = false;
        private bool isCompleted = false;

        public BuildTilesState(WallChessGameManager gameManager, StateMachine stateMachine) 
            : base(gameManager, stateMachine)
        {
        }

        public override string StateName => "BuildTiles";

        public override void OnEnter()
        {
            base.OnEnter();
            
            LogInfo("Entering BuildTiles state - preparing to animate tiles");
            
            // Initialize tile animation controller
            InitializeTileAnimation();
            
            // Start tile animation sequence
            StartTileAnimation();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            
                        // Check if animation is completed and transition to next state
            if (isCompleted)
            {
                LogInfo("Tiles built successfully - transitioning to SpawnPawns state");
                stateMachine.ChangeState<SpawnPawnsState>();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            
            LogInfo("Exiting BuildTiles state");
            
            // Cleanup animation events
            if (tileAnimationController != null)
            {
                tileAnimationController.OnTileAnimationCompleted -= OnTileAnimationCompleted;
            }
        }

        #region Tile Animation Management

        private void InitializeTileAnimation()
        {
            tileAnimationController = gameManager.GetComponent<TileAnimationController>();
            
            if (tileAnimationController == null)
            {
                tileAnimationController = gameManager.gameObject.AddComponent<TileAnimationController>();
                LogInfo("Created TileAnimationController component");
            }

            // Initialize with grid system
            var gridSystem = gameManager.GetGridSystem();
            if (gridSystem != null)
            {
                tileAnimationController.Initialize(gridSystem, gameManager);
                LogInfo("TileAnimationController initialized with GridSystem");
            }
            else
            {
                LogError("GridSystem not found - cannot initialize tile animation");
                return;
            }

            // Subscribe to completion event
            tileAnimationController.OnTileAnimationCompleted += OnTileAnimationCompleted;
        }

        private void StartTileAnimation()
        {
            if (tileAnimationController == null)
            {
                LogError("TileAnimationController not available - skipping animation");
                OnTileAnimationCompleted(); // Skip directly to completion
                return;
            }

            if (animationStarted)
            {
                LogWarning("Tile animation already started");
                return;
            }

            LogInfo("Starting tile animation sequence");
            tileAnimationController.StartTileAnimation();
            animationStarted = true;
        }

        private void OnTileAnimationCompleted()
        {
            LogInfo("Tile animation completed");
            isCompleted = true;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Check if tiles are fully built
        /// </summary>
        public bool AreTilesBuilt()
        {
            return isCompleted;
        }

        /// <summary>
        /// Skip animation and complete immediately
        /// </summary>
        public void SkipAnimation()
        {
            if (tileAnimationController != null)
            {
                tileAnimationController.CompleteAnimationImmediately();
            }
            OnTileAnimationCompleted();
        }

        #endregion

        #region Context Menu Actions

        [ContextMenu("Debug/Skip Tile Animation")]
        private void DebugSkipAnimation()
        {
            if (Application.isPlaying)
            {
                SkipAnimation();
            }
        }

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            Debug.Log($"[BuildTilesState] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[BuildTilesState] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[BuildTilesState] {message}");
        }

        #endregion
    }
}