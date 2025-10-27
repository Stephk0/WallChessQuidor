using UnityEngine;
using WallChess.Core.Session;

// Force recompilation - compiler error fixes applied
namespace WallChess.Gameplay.Pawns
{
    /// <summary>
    /// Represents a single pawn/player piece in the game
    /// Contains position data, player information, and movement logic
    /// Clean, focused class following single responsibility principle
    /// </summary>
    public class Pawn : MonoBehaviour
    {
        [Header("Pawn State")]
        [SerializeField] private bool isActive = false;
        [SerializeField] private Vector2Int currentPosition;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        // Core data
        private PlayerData playerData;
        private GridSystem gridSystem;
        
        // Visual components
        private Renderer pawnRenderer;
        private Collider pawnCollider;
        
        // Movement
        private Vector3 targetWorldPosition;
        private bool isMoving = false;
        private float moveSpeed = 5f;
        
        // Events
        public static System.Action<Pawn, Vector2Int> OnPawnMoveStarted;
        public static System.Action<Pawn, Vector2Int, Vector2Int> OnPawnMoveCompleted;
        
        // Properties
        public PlayerData PlayerData => playerData;
        public Vector2Int CurrentPosition => currentPosition;
        public bool IsActive => isActive;
        public bool IsMoving => isMoving;
        public bool IsAI => playerData?.playerType == PlayerType.AI;
                
        // Compatibility properties for legacy code
        public int wallsRemaining => GetWallsRemaining();
        public Vector2Int position => currentPosition;
        public Vector2Int winPosition => playerData?.winPosition ?? Vector2Int.zero;
        public Vector2Int startPosition => playerData?.startPosition ?? Vector2Int.zero;
         // Force recompile
public GameObject avatar => gameObject; // The pawn GameObject itself serves as the avatar
        
        
        
        
        // Implicit conversion to PlayerData for methods expecting PawnData
        public static implicit operator PlayerData(Pawn pawn)
        {
            return pawn?.playerData;
                }
        
        // Additional compatibility for methods expecting PawnData type
        public PlayerData PawnData => playerData; // Alias for PlayerData
        
        public bool IsHuman => playerData?.playerType == PlayerType.Human;
        
        #region Initialization
        
        /// <summary>
        /// Initialize this pawn with player data and grid reference
        /// </summary>
        public void Initialize(PlayerData data, GridSystem grid)
        {
            playerData = data;
            gridSystem = grid;
            currentPosition = data.currentPosition;
            
            // Cache components
            pawnRenderer = GetComponentInChildren<Renderer>();
            pawnCollider = GetComponentInChildren<Collider>();
            
            // Set initial world position
            if (gridSystem != null)
            {
                targetWorldPosition = gridSystem.GridToWorldPosition(currentPosition);
                transform.position = targetWorldPosition;
            }
            
            // Configure visual state
            SetActive(data.isActive);
            
            LogInfo($"Initialized pawn for {data.playerName} at {currentPosition}");
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Set whether this pawn is the active player
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            
            if (playerData != null)
            {
                if (active)
                    playerData.StartTurn();
                else
                    playerData.EndTurn();
            }
            
            // Update visual indication of active state
            UpdateActiveVisuals();
            
            LogInfo($"Pawn {playerData?.playerName} active state: {active}");
        }
        
        private void UpdateActiveVisuals()
        {
            // Example: Scale up active pawn slightly, or add glow effect
            float scale = isActive ? 1.1f : 1.0f;
            transform.localScale = Vector3.one * scale;
            
            // Could also modify material emission for glow effect
            if (pawnRenderer != null && pawnRenderer.material != null)
            {
                Color emissionColor = isActive ? playerData.playerColor * 0.3f : Color.black;
                pawnRenderer.material.SetColor("_EmissionColor", emissionColor);
            }
        }
        
        #endregion
        
        #region Movement
        
        /// <summary>
        /// Move this pawn to a new grid position
        /// </summary>
        public void MoveTo(Vector2Int newPosition)
        {
            if (isMoving)
            {
                LogWarning("Cannot move: pawn is already moving");
                return;
            }
            
            Vector2Int oldPosition = currentPosition;
            
            // Update logical position
            currentPosition = newPosition;
            if (playerData != null)
            {
                playerData.UpdatePosition(newPosition);
            }
            
            // Update visual position
            if (gridSystem != null)
            {
                targetWorldPosition = gridSystem.GridToWorldPosition(newPosition);
                StartCoroutine(MoveToPositionCoroutine(oldPosition, newPosition));
            }
            else
            {
                // Immediate move if no grid system
                transform.position = targetWorldPosition;
            }
            
            OnPawnMoveStarted?.Invoke(this, newPosition);
            LogInfo($"Moving {playerData?.playerName} from {oldPosition} to {newPosition}");
        }
        
        private System.Collections.IEnumerator MoveToPositionCoroutine(Vector2Int fromPos, Vector2Int toPos)
        {
            isMoving = true;
            Vector3 startPosition = transform.position;
            float journeyTime = 0f;
            float journeyLength = Vector3.Distance(startPosition, targetWorldPosition);
            float duration = journeyLength / moveSpeed;
            
            while (journeyTime <= duration)
            {
                journeyTime += Time.deltaTime;
                float fractionOfJourney = journeyTime / duration;
                
                // Smooth movement with easing
                float easedFraction = Mathf.SmoothStep(0f, 1f, fractionOfJourney);
                transform.position = Vector3.Lerp(startPosition, targetWorldPosition, easedFraction);
                
                yield return null;
            }
            
            // Ensure exact final position
            transform.position = targetWorldPosition;
            isMoving = false;
            
            OnPawnMoveCompleted?.Invoke(this, fromPos, toPos);
            LogInfo($"Completed move for {playerData?.playerName} to {toPos}");
        }
        
        /// <summary>
        /// Instantly snap to position without animation
        /// </summary>
        public void SnapToPosition(Vector2Int position)
        {
            currentPosition = position;
            if (playerData != null)
            {
                playerData.UpdatePosition(position);
            }
            
            if (gridSystem != null)
            {
                transform.position = gridSystem.GridToWorldPosition(position);
            }
            
            LogInfo($"Snapped {playerData?.playerName} to {position}");
        }
        
        #endregion
        
        #region Victory Checking
        
        /// <summary>
        /// Check if this pawn has reached its win condition
        /// </summary>
        public bool HasWon()
        {
            return playerData?.HasWon() ?? false;
        }
        
        /// <summary>
        /// Get distance to win position (for AI heuristics)
        /// </summary>
        public int GetDistanceToWin()
        {
            if (playerData == null) return int.MaxValue;
            
            Vector2Int winPos = playerData.winPosition;
            return Mathf.Abs(currentPosition.x - winPos.x) + Mathf.Abs(currentPosition.y - winPos.y);
        }
        
        #endregion
        
        #region Wall Management
        
        /// <summary>
        /// Check if this pawn can place a wall
        /// </summary>
        public bool CanPlaceWall()
        {
            return playerData != null && playerData.wallsRemaining > 0;
        }
        
        /// <summary>
        /// Use a wall (decrease count)
        /// </summary>
        public bool UseWall()
        {
            bool result = playerData?.UseWall() ?? false;
            if (result)
            {
                LogInfo($"{playerData.playerName} used wall. Remaining: {playerData.wallsRemaining}");
            }
            return result;
        }
        
        /// <summary>
        /// Get remaining wall count
        /// </summary>
                
        /// <summary>
        /// Reset wall count to the specified amount
        /// </summary>
        public void ResetWalls(int wallCount)
        {
            playerData?.ResetWalls(wallCount);
            LogInfo($"{playerData?.playerName} walls reset to {wallCount}");
        }
public int GetWallsRemaining()
        {
            return playerData?.wallsRemaining ?? 0;
        }
        
        #endregion
        
        #region Input Handling
        
        void OnMouseDown()
        {
            // Only respond to clicks if this is the active pawn and it's a human player
            if (isActive && IsHuman)
            {
                // Could trigger selection or movement UI
                LogInfo($"Clicked on active pawn: {playerData?.playerName}");
            }
        }
        
        #endregion
        
        #region Debug and Utilities
        
        private void LogInfo(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[Pawn:{playerData?.playerName}] {message}");
        }
        
        private void LogWarning(string message)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[Pawn:{playerData?.playerName}] {message}");
        }
        
        /// <summary>
        /// Get a string representation of this pawn's current state
        /// </summary>
        public override string ToString()
        {
            if (playerData == null) return "Uninitialized Pawn";
            
            return $"{playerData.playerName} ({playerData.playerType}) at {currentPosition} " +
                   $"- Walls: {playerData.wallsRemaining}, Active: {isActive}";
        }
        
        [ContextMenu("Debug/Print Pawn State")]
        private void DebugPrintState()
        {
            Debug.Log($"Pawn State: {ToString()}");
            if (playerData != null)
            {
                Debug.Log($"- Start: {playerData.startPosition}");
                Debug.Log($"- Win: {playerData.winPosition}");
                Debug.Log($"- Distance to win: {GetDistanceToWin()}");
                Debug.Log($"- Can place wall: {CanPlaceWall()}");
                Debug.Log($"- Has won: {HasWon()}");
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        void Update()
        {
            // Update any visual effects or animations if needed
            // Keep this minimal for performance
        }
        
        void OnDestroy()
        {
            // Cleanup if needed
            if (isMoving)
            {
                StopAllCoroutines();
            }
        }
        
        #endregion
    }
}