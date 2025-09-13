using UnityEngine;
using System;
using WallChess.Core.States;
using WallChess.Core;
using WallChess.Gameplay.Pawns;

namespace WallChess.Input
{
    /// <summary>
    /// Centralized input management system extracted from WallChessGameManager.
    /// Handles all player input and routes to appropriate systems based on game state.
    /// Part of Phase 3: God Object Extraction (MVP Implementation Guide)
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        #region Input Schemes
        public interface IInputScheme
        {
            void ProcessInput(UnifiedStateManager.StateType currentState);
            void OnEnable();
            void OnDisable();
        }
        
        /// <summary>
        /// Standard click and drag input scheme
        /// </summary>
        public class ClickDragScheme : IInputScheme
        {
            private InputManager manager;
            private bool isDragging = false;
            private Vector3 dragStartPos;
            private GameObject draggedObject;
            
            public ClickDragScheme(InputManager manager)
            {
                this.manager = manager;
            }
            
            public void OnEnable() 
            {
                isDragging = false;
                draggedObject = null;
            }
            
            public void OnDisable() 
            {
                if (isDragging && draggedObject != null)
                {
                    // Cancel any ongoing drag
                    isDragging = false;
                    draggedObject = null;
                }
            }
            
            public void ProcessInput(UnifiedStateManager.StateType currentState)
            {
                switch (currentState)
                {
                    case UnifiedStateManager.StateType.PlayerTurn:
                        HandlePlayerTurnInput();
                        break;
                        
                    case UnifiedStateManager.StateType.PawnSelection:
                        HandlePawnSelectionInput();
                        break;
                        
                    case UnifiedStateManager.StateType.PawnMoving:
                        HandlePawnMovementInput();
                        break;
                        
                    case UnifiedStateManager.StateType.WallSelection:
                    case UnifiedStateManager.StateType.WallPlacement:
                        HandleWallPlacementInput();
                        break;
                }
                
                // Global inputs
                HandleGlobalInput();
            }
            
            private void HandlePlayerTurnInput()
            {
                if (UnityEngine.Input.GetMouseButtonDown(0))
                {
                    var hit = manager.GetMouseHit();
                    
                    if (hit.hitType == HitType.Pawn)
                    {
                        manager.OnPawnClicked?.Invoke(hit.playerId);
                    }
                    else if (hit.hitType == HitType.Board)
                    {
                        manager.OnBoardClicked?.Invoke(hit.gridPosition);
                    }
                }
                
                // Quick wall placement with W key
                if (UnityEngine.Input.GetKeyDown(KeyCode.W))
                {
                    manager.OnWallPlacementRequested?.Invoke();
                }
            }
            
            private void HandlePawnSelectionInput()
            {
                if (UnityEngine.Input.GetMouseButtonDown(0))
                {
                    var hit = manager.GetMouseHit();
                    
                    if (hit.hitType == HitType.Pawn)
                    {
                        manager.OnPawnClicked?.Invoke(hit.playerId);
                    }
                }
                
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    manager.OnCancelRequested?.Invoke();
                }
            }
            
            private void HandlePawnMovementInput()
            {
                // Handle drag for pawn movement
                if (UnityEngine.Input.GetMouseButtonDown(0))
                {
                    var hit = manager.GetMouseHit();
                    if (hit.hitType == HitType.Pawn)
                    {
                        isDragging = true;
                        dragStartPos = UnityEngine.Input.mousePosition;
                        draggedObject = hit.hitObject;
                    }
                }
                
                if (isDragging && UnityEngine.Input.GetMouseButton(0))
                {
                    // Update drag preview
                    manager.OnPawnDragging?.Invoke(UnityEngine.Input.mousePosition);
                }
                
                if (isDragging && UnityEngine.Input.GetMouseButtonUp(0))
                {
                    isDragging = false;
                    var hit = manager.GetMouseHit();
                    
                    if (hit.hitType == HitType.Board)
                    {
                        manager.OnPawnDropped?.Invoke(hit.gridPosition);
                    }
                    
                    draggedObject = null;
                }
                
                // Alternative: Click to move
                if (!isDragging && UnityEngine.Input.GetMouseButtonDown(0))
                {
                    var hit = manager.GetMouseHit();
                    if (hit.hitType == HitType.Board || hit.hitType == HitType.Highlight)
                    {
                        manager.OnMoveTargetClicked?.Invoke(hit.gridPosition);
                    }
                }
                
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    if (isDragging)
                    {
                        isDragging = false;
                        draggedObject = null;
                    }
                    manager.OnCancelRequested?.Invoke();
                }
            }
            
            private void HandleWallPlacementInput()
            {
                // Wall rotation
                if (UnityEngine.Input.GetKeyDown(KeyCode.R) || UnityEngine.Input.GetMouseButtonDown(1))
                {
                    manager.OnWallRotateRequested?.Invoke();
                }
                
                // Wall placement
                if (UnityEngine.Input.GetMouseButtonDown(0))
                {
                    var hit = manager.GetMouseHit();
                    if (hit.hitType == HitType.Board || hit.hitType == HitType.WallGap)
                    {
                        manager.OnWallPlaceRequested?.Invoke(hit.gridPosition);
                    }
                }
                
                // Cancel
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    manager.OnCancelRequested?.Invoke();
                }
            }
            
            private void HandleGlobalInput()
            {
                // Pause
                if (UnityEngine.Input.GetKeyDown(KeyCode.P) || UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    manager.OnPauseRequested?.Invoke();
                }
                
                // Camera controls
                float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    manager.OnCameraZoom?.Invoke(scroll);
                }
            }
        }
        
        /// <summary>
        /// Touch input scheme for mobile
        /// </summary>
        public class TouchScheme : IInputScheme
        {
            private InputManager manager;
            
            public TouchScheme(InputManager manager)
            {
                this.manager = manager;
            }
            
            public void OnEnable() { }
            public void OnDisable() { }
            
            public void ProcessInput(UnifiedStateManager.StateType currentState)
            {
                // Touch input implementation
                // Would handle touch, pinch, swipe gestures
            }
        }
        #endregion
        
        #region Types
        public enum HitType
        {
            None,
            Board,
            Pawn,
            Wall,
            WallGap,
            Highlight,
            UI
        }
        
        public struct HitInfo
        {
            public HitType hitType;
            public Vector2Int gridPosition;
            public GameObject hitObject;
            public int playerId;
            public Vector3 worldPosition;
        }
        #endregion
        
        #region Configuration
        [Header("Input Settings")]
        [SerializeField] private LayerMask boardLayer;
        [SerializeField] private LayerMask pawnLayer;
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private LayerMask uiLayer;
        [SerializeField] private float raycastDistance = 100f;
        
        [Header("Input Scheme")]
        [SerializeField] private bool useTouchInput = false;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool logInputEvents = false;
        #endregion
        
        #region State
        private IInputScheme currentScheme;
        private UnifiedStateManager stateManager;
        private PawnManager pawnManager;
        private WallPlacer wallPlacer;
        private bool inputEnabled = true;
        #endregion
        
        #region Events
        // Pawn events
        public Action<int> OnPawnClicked;
        public Action<Vector2Int> OnPawnDropped;
        public Action<Vector3> OnPawnDragging;
        public Action<Vector2Int> OnMoveTargetClicked;
        
        // Wall events
        public Action OnWallPlacementRequested;
        public Action<Vector2Int> OnWallPlaceRequested;
        public Action OnWallRotateRequested;
        
        // Board events
        public Action<Vector2Int> OnBoardClicked;
        
        // System events
        public Action OnCancelRequested;
        public Action OnPauseRequested;
        
        // Camera events
        public Action<float> OnCameraZoom;
        #endregion
        
        #region Unity Lifecycle
        void Awake()
        {
            stateManager = UnifiedStateManager.Instance;
            pawnManager = FindObjectOfType<PawnManager>();
            wallPlacer = FindObjectOfType<WallPlacer>();
            
            InitializeInputScheme();
            SubscribeToEvents();
        }
        
        void Start()
        {
            currentScheme?.OnEnable();
        }
        
        void Update()
        {
            if (!inputEnabled) return;
            
            var currentState = stateManager?.CurrentStateType ?? UnifiedStateManager.StateType.Initialization;
            currentScheme?.ProcessInput(currentState);
        }
        
        void OnDestroy()
        {
            currentScheme?.OnDisable();
            UnsubscribeFromEvents();
        }
        #endregion
        
        #region Initialization
        private void InitializeInputScheme()
        {
            if (useTouchInput && UnityEngine.Input.touchSupported)
            {
                SetInputScheme(new TouchScheme(this));
            }
            else
            {
                SetInputScheme(new ClickDragScheme(this));
            }
        }
        
        private void SubscribeToEvents()
        {
            // Subscribe to state changes
            UnifiedStateManager.OnStateChanged += OnStateChanged;
            
            // Connect input events to game systems
            OnPawnClicked += HandlePawnClick;
            OnMoveTargetClicked += HandleMoveClick;
            OnWallPlacementRequested += HandleWallRequest;
            OnWallPlaceRequested += HandleWallPlace;
            OnCancelRequested += HandleCancel;
            OnPauseRequested += HandlePause;
        }
        
        private void UnsubscribeFromEvents()
        {
            UnifiedStateManager.OnStateChanged -= OnStateChanged;
            
            OnPawnClicked -= HandlePawnClick;
            OnMoveTargetClicked -= HandleMoveClick;
            OnWallPlacementRequested -= HandleWallRequest;
            OnWallPlaceRequested -= HandleWallPlace;
            OnCancelRequested -= HandleCancel;
            OnPauseRequested -= HandlePause;
        }
        #endregion
        
        #region Public API
        /// <summary>
        /// Set the input scheme
        /// </summary>
        public void SetInputScheme(IInputScheme scheme)
        {
            currentScheme?.OnDisable();
            currentScheme = scheme;
            currentScheme?.OnEnable();
            
            if (debugMode)
            {
                Debug.Log($"[InputManager] Input scheme changed to: {scheme.GetType().Name}");
            }
        }
        
        /// <summary>
        /// Enable or disable input processing
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            
            if (!enabled)
            {
                currentScheme?.OnDisable();
            }
            else
            {
                currentScheme?.OnEnable();
            }
        }
        
        /// <summary>
        /// Get hit information from mouse position
        /// </summary>
        public HitInfo GetMouseHit()
        {
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            return GetRaycastHit(ray);
        }
        
        /// <summary>
        /// Get hit information from screen position
        /// </summary>
        public HitInfo GetScreenHit(Vector2 screenPosition)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            return GetRaycastHit(ray);
        }
        #endregion
        
        #region Private Methods - Raycasting
        private HitInfo GetRaycastHit(Ray ray)
        {
            HitInfo hitInfo = new HitInfo();
            
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
            {
                hitInfo.worldPosition = hit.point;
                
                // Check what was hit
                GameObject hitObject = hit.collider.gameObject;
                int layer = hitObject.layer;
                
                // Check for pawn
                if (IsInLayerMask(layer, pawnLayer))
                {
                    hitInfo.hitType = HitType.Pawn;
                    hitInfo.hitObject = hitObject;
                    
                    var pawnId = hitObject.GetComponent<PawnIdentifier>();
                    if (pawnId != null)
                    {
                        hitInfo.playerId = pawnId.PlayerId;
                    }
                }
                // Check for wall
                else if (IsInLayerMask(layer, wallLayer))
                {
                    hitInfo.hitType = HitType.Wall;
                    hitInfo.hitObject = hitObject;
                }
                // Check for board
                else if (IsInLayerMask(layer, boardLayer))
                {
                    hitInfo.hitType = HitType.Board;
                    hitInfo.hitObject = hitObject;
                    
                    // Convert to grid position
                    hitInfo.gridPosition = WorldToGrid(hit.point);
                }
                // Check for UI
                else if (IsInLayerMask(layer, uiLayer))
                {
                    hitInfo.hitType = HitType.UI;
                    hitInfo.hitObject = hitObject;
                }
                
                // Check for special markers
                if (hitObject.CompareTag("Highlight"))
                {
                    hitInfo.hitType = HitType.Highlight;
                    hitInfo.gridPosition = WorldToGrid(hit.point);
                }
                else if (hitObject.CompareTag("WallGap"))
                {
                    hitInfo.hitType = HitType.WallGap;
                    hitInfo.gridPosition = WorldToGrid(hit.point);
                }
            }
            
            return hitInfo;
        }
        
        private bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }
        
        private Vector2Int WorldToGrid(Vector3 worldPos)
        {
            // Convert world position to grid coordinates
            // This should match your grid system's conversion
            float tileSize = 1f; // Get from grid system
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / tileSize),
                Mathf.RoundToInt(worldPos.z / tileSize)
            );
        }
        #endregion
        
        #region Event Handlers
        private void OnStateChanged(UnifiedStateManager.StateType fromState, UnifiedStateManager.StateType toState)
        {
            // Handle state-specific input setup
            switch (toState)
            {
                case UnifiedStateManager.StateType.PawnSelection:
                case UnifiedStateManager.StateType.PawnMoving:
                case UnifiedStateManager.StateType.WallPlacement:
                    SetInputEnabled(true);
                    break;
                    
                case UnifiedStateManager.StateType.TurnValidation:
                case UnifiedStateManager.StateType.TurnTransition:
                    SetInputEnabled(false);
                    break;
            }
        }
        
        private void HandlePawnClick(int playerId)
        {
            if (logInputEvents)
                Debug.Log($"[InputManager] Pawn clicked: Player {playerId}");
            
            // Route to appropriate system based on state
            var state = stateManager.CurrentStateType;
            
            if (state == UnifiedStateManager.StateType.PlayerTurn || 
                state == UnifiedStateManager.StateType.PawnSelection)
            {
                if (pawnManager != null && playerId == stateManager.GetCurrentPlayerId())
                {
                    pawnManager.SelectPawn(playerId);
                    stateManager.RequestStateChange(UnifiedStateManager.StateType.PawnMoving);
                }
            }
        }
        
        private void HandleMoveClick(Vector2Int gridPosition)
        {
            if (logInputEvents)
                Debug.Log($"[InputManager] Move target clicked: {gridPosition}");
            
            if (stateManager.IsInState(UnifiedStateManager.StateType.PawnMoving))
            {
                int currentPlayer = stateManager.GetCurrentPlayerId();
                var result = pawnManager?.MovePawn(currentPlayer, gridPosition);
                
                if (result != null && result.success)
                {
                    stateManager.RequestStateChange(UnifiedStateManager.StateType.TurnValidation);
                }
            }
        }
        
        private void HandleWallRequest()
        {
            if (logInputEvents)
                Debug.Log("[InputManager] Wall placement requested");
            
            if (stateManager.IsInState(UnifiedStateManager.StateType.PlayerTurn))
            {
                stateManager.RequestStateChange(UnifiedStateManager.StateType.WallSelection);
            }
        }
        
        private void HandleWallPlace(Vector2Int gridPosition)
        {
            if (logInputEvents)
                Debug.Log($"[InputManager] Wall place requested at: {gridPosition}");
            
            if (stateManager.IsInState(UnifiedStateManager.StateType.WallPlacement))
            {
                // Wall placement handled by WallPlacer
                // This just triggers the state transition
                stateManager.RequestStateChange(UnifiedStateManager.StateType.TurnValidation);
            }
        }
        
        private void HandleCancel()
        {
            if (logInputEvents)
                Debug.Log("[InputManager] Cancel requested");
            
            var state = stateManager.CurrentStateType;
            
            // Return to player turn from any action state
            if (state == UnifiedStateManager.StateType.PawnSelection ||
                state == UnifiedStateManager.StateType.PawnMoving ||
                state == UnifiedStateManager.StateType.WallSelection ||
                state == UnifiedStateManager.StateType.WallPlacement)
            {
                stateManager.RequestStateChange(UnifiedStateManager.StateType.PlayerTurn);
            }
        }
        
        private void HandlePause()
        {
            if (logInputEvents)
                Debug.Log("[InputManager] Pause requested");
            
            if (!stateManager.IsInState(UnifiedStateManager.StateType.Paused))
            {
                stateManager.RequestStateChange(UnifiedStateManager.StateType.Paused);
            }
        }
        #endregion
    }
}