using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WallChess.Grid;

namespace WallChess
{
    /// <summary>
    /// Handles animated tile creation during the BuildTiles game state.
    /// Supports two animation modes: row-by-row and circular pattern around player positions.
    /// Uses object pooling principles and integrates with the existing GridSystem.
    /// </summary>
    public class TileAnimationController : MonoBehaviour
    {
        public enum AnimationType
        {
            OneByOne,       // Animate tiles one by one (old row-by-row implementation)
            RowByRow,       // Animate entire rows at once from bottom to top
            CircularSpiral, // Animate in circular pattern around player positions
            Ripple          // Animate in ripple waves around player positions
        }

        [Header("Animation Settings")]
        [SerializeField] private AnimationType animationType = AnimationType.RowByRow;
        [SerializeField] private float delayBeforeAnimation = 0.5f;
        [SerializeField] private float timeBetweenTiles = 0.05f;
        [SerializeField] private float timeBetweenRows = 0.1f;
        [SerializeField] private bool enableScaleAnimation = true;
        [SerializeField] private AnimationCurve scaleAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float scaleAnimationDuration = 0.3f;
        
        [Header("Overall Pacing Control")]
        [SerializeField] private bool useOverallPacingCurve = true;
        [SerializeField] private AnimationCurve overallPacingCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.1f);
        [Tooltip("How the timing multiplier changes over the animation duration. Y=1 means normal speed, Y=0.1 means 10x faster")]
        [SerializeField] private float overallAnimationDuration = 3f;
        [Tooltip("Total duration for the pacing curve evaluation")]

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip tileAppearSound;
        [SerializeField] private float soundVolume = 0.5f;

        // Events
        public System.Action OnTileAnimationStarted;
        public System.Action OnTileAnimationCompleted;

        // Internal references
        private GridSystem gridSystem;
        private WallChessGameManager gameManager;
        private GridTileManager tileManager;
        private List<Vector2Int> playerPositions;

        // Animation state
        private bool isAnimating = false;
        private List<GameObject> animatedTiles = new List<GameObject>();
        private float animationStartTime;
        private int totalTilesToAnimate;
        private int tilesAnimated;

        #region Initialization
        public void Initialize(GridSystem gridSys, WallChessGameManager gameMgr)
        {
            gridSystem = gridSys;
            gameManager = gameMgr;
            
            // Get tile manager through reflection or direct access if available
            tileManager = FindTileManager();
            
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private GridTileManager FindTileManager()
        {
            // This would need to be adapted based on how GridTileManager is exposed
            // For now, we'll work with GridSystem's public API
            return null;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Start the tile animation sequence. Should be called when entering BuildTiles state.
        /// </summary>
        public void StartTileAnimation()
        {
            if (isAnimating)
            {
                Debug.LogWarning("TileAnimationController: Animation already in progress");
                return;
            }

            if (gridSystem == null || gameManager == null)
            {
                Debug.LogError("TileAnimationController: Not properly initialized");
                return;
            }

            // Collect player starting positions for circular animation
            CollectPlayerPositions();

            // Start the animation sequence
            StartCoroutine(AnimateTileCreation());
        }

        /// <summary>
        /// Stop any ongoing animation and show all tiles immediately
        /// </summary>
        public void CompleteAnimationImmediately()
        {
            if (isAnimating)
            {
                StopAllCoroutines();
                ShowAllTilesImmediately();
                OnAnimationComplete();
            }
        }
        #endregion
        
        #region Dynamic Timing Control
        /// <summary>
        /// Get current timing multiplier based on animation progress
        /// </summary>
        private float GetCurrentTimingMultiplier()
        {
            if (!useOverallPacingCurve)
                return 1f;
                
            // Calculate progress based on time
            float timeProgress = (Time.time - animationStartTime) / overallAnimationDuration;
            timeProgress = Mathf.Clamp01(timeProgress);
            
            // Sample the curve to get timing multiplier
            float multiplier = overallPacingCurve.Evaluate(timeProgress);
            return Mathf.Max(0.01f, multiplier); // Prevent zero or negative timing
        }
        
        /// <summary>
        /// Get current timing multiplier based on tile progress
        /// </summary>
        private float GetCurrentTimingMultiplierByTiles()
        {
            if (!useOverallPacingCurve || totalTilesToAnimate <= 0)
                return 1f;
                
            // Calculate progress based on tiles completed
            float tileProgress = (float)tilesAnimated / totalTilesToAnimate;
            tileProgress = Mathf.Clamp01(tileProgress);
            
            // Sample the curve to get timing multiplier
            float multiplier = overallPacingCurve.Evaluate(tileProgress);
            return Mathf.Max(0.01f, multiplier); // Prevent zero or negative timing
        }
        
        /// <summary>
        /// Get dynamically adjusted tile timing
        /// </summary>
        private float GetCurrentTileTiming()
        {
            return timeBetweenTiles * GetCurrentTimingMultiplierByTiles();
        }
        
        /// <summary>
        /// Get dynamically adjusted row timing
        /// </summary>
        private float GetCurrentRowTiming()
        {
            return timeBetweenRows * GetCurrentTimingMultiplierByTiles();
        }
        #endregion

        #region Animation Coroutines
        private IEnumerator AnimateTileCreation()
        {
            isAnimating = true;
            animatedTiles.Clear();
            
            // Initialize animation progress tracking
            animationStartTime = Time.time;
            int gridSize = gridSystem.GetGridSize();
            totalTilesToAnimate = gridSize * gridSize;
            tilesAnimated = 0;
            
            OnTileAnimationStarted?.Invoke();

            // Initial delay before animation starts
            yield return new WaitForSeconds(delayBeforeAnimation);

            switch (animationType)
            {
                case AnimationType.OneByOne:
                    yield return StartCoroutine(AnimateOneByOne());
                    break;
                case AnimationType.RowByRow:
                    yield return StartCoroutine(AnimateRowByRow());
                    break;
                case AnimationType.CircularSpiral:
                    yield return StartCoroutine(AnimateCircularSpiral());
                    break;
                case AnimationType.Ripple:
                    yield return StartCoroutine(AnimateRipple());
                    break;
            }

            OnAnimationComplete();
        }

        private IEnumerator AnimateOneByOne()
        {
            int gridSize = gridSystem.GetGridSize();
            
            // Create tiles one by one, row by row from bottom (y=0) to top (y=gridSize-1)
            for (int y = 0; y < gridSize; y++)
            {
                // Create all tiles in this row one by one
                for (int x = 0; x < gridSize; x++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    yield return StartCoroutine(CreateAnimatedTile(gridPos));
                    
                    // Increment progress counter
                    tilesAnimated++;
                    
                    // Dynamic delay between tiles (gets faster as animation progresses)
                    yield return new WaitForSeconds(GetCurrentTileTiming());
                }
            }
        }
        
        private IEnumerator AnimateRowByRow()
        {
            int gridSize = gridSystem.GetGridSize();
            
            // Create entire rows at once from bottom (y=0) to top (y=gridSize-1)
            for (int y = 0; y < gridSize; y++)
            {
                // Create all tiles in this row simultaneously
                List<Coroutine> rowTileCoroutines = new List<Coroutine>();
                
                for (int x = 0; x < gridSize; x++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    Coroutine tileCoroutine = StartCoroutine(CreateAnimatedTile(gridPos));
                    rowTileCoroutines.Add(tileCoroutine);
                }
                
                // Wait for all tiles in this row to complete
                foreach (Coroutine coroutine in rowTileCoroutines)
                {
                    yield return coroutine;
                }
                
                // Update progress for entire row
                tilesAnimated += gridSize;
                
                // Dynamic delay between rows (gets faster as animation progresses)
                if (y < gridSize - 1)
                    yield return new WaitForSeconds(GetCurrentRowTiming());
            }
        }

        private IEnumerator AnimateCircularSpiral()
        {
            int gridSize = gridSystem.GetGridSize();
            List<Vector2Int> spiralOrder = GenerateCircularOrder(gridSize);
            
            foreach (Vector2Int gridPos in spiralOrder)
            {
                yield return StartCoroutine(CreateAnimatedTile(gridPos));
                
                // Increment progress counter
                tilesAnimated++;
                
                // Dynamic delay between tiles (gets faster as animation progresses)
                yield return new WaitForSeconds(GetCurrentTileTiming());
            }
        }
        
        private IEnumerator AnimateRipple()
        {
            int gridSize = gridSystem.GetGridSize();
            List<List<Vector2Int>> rippleRings = GenerateRippleRings(gridSize);
            
            // Animate each ring simultaneously
            foreach (List<Vector2Int> ring in rippleRings)
            {
                if (ring.Count > 0)
                {
                    // Start all tiles in this ring simultaneously
                    List<Coroutine> ringCoroutines = new List<Coroutine>();
                    
                    foreach (Vector2Int gridPos in ring)
                    {
                        Coroutine tileCoroutine = StartCoroutine(CreateAnimatedTile(gridPos));
                        ringCoroutines.Add(tileCoroutine);
                    }
                    
                    // Wait for all tiles in this ring to complete
                    foreach (Coroutine coroutine in ringCoroutines)
                    {
                        yield return coroutine;
                    }
                    
                    // Update progress for entire ring
                    tilesAnimated += ring.Count;
                    
                    // Dynamic delay before next ripple ring (gets faster as animation progresses)
                    yield return new WaitForSeconds(GetCurrentRowTiming());
                }
            }
        }

        private IEnumerator CreateAnimatedTile(Vector2Int gridPos)
        {
            // Create the tile through GridSystem (assuming it has this capability)
            GameObject tile = CreateTileAt(gridPos);
            
            if (tile != null)
            {
                animatedTiles.Add(tile);
                
                if (enableScaleAnimation)
                {
                    yield return StartCoroutine(AnimateTileScale(tile));
                }
                
                // Play sound effect
                PlayTileSound();
            }
        }

        private IEnumerator AnimateTileScale(GameObject tile)
        {
            Vector3 originalScale = tile.transform.localScale;
            tile.transform.localScale = Vector3.zero;
            
            float elapsed = 0;
            while (elapsed < scaleAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scaleAnimationDuration;
                float scaleValue = scaleAnimationCurve.Evaluate(t);
                tile.transform.localScale = originalScale * scaleValue;
                yield return null;
            }
            
            tile.transform.localScale = originalScale;
        }
        #endregion

        #region Tile Creation Integration
        private GameObject CreateTileAt(Vector2Int gridPos)
        {
            // Use GridSystem's new animation support method
            return gridSystem.CreateAnimatedTileAt(gridPos);
        }

        // Note: Tile creation now handled entirely by GridSystem.CreateAnimatedTileAt()
        #endregion

        #region Pattern Generation
        private List<Vector2Int> GenerateCircularOrder(int gridSize)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            
            // Start from player positions and spiral outward
            foreach (Vector2Int playerPos in playerPositions)
            {
                AddCircularRing(playerPos, 0, gridSize, visited, result);
            }
            
            // Add any remaining tiles in distance order from nearest player
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (!visited.Contains(pos))
                    {
                        result.Add(pos);
                    }
                }
            }
            
            return result;
        }

        private void AddCircularRing(Vector2Int center, int radius, int gridSize, HashSet<Vector2Int> visited, List<Vector2Int> result)
        {
            if (radius == 0)
            {
                if (IsValidPosition(center, gridSize) && !visited.Contains(center))
                {
                    visited.Add(center);
                    result.Add(center);
                }
                AddCircularRing(center, radius + 1, gridSize, visited, result);
                return;
            }

            List<Vector2Int> ringPositions = new List<Vector2Int>();
            
            // Generate positions in a ring around the center
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) == radius || Mathf.Abs(dy) == radius)
                    {
                        Vector2Int pos = center + new Vector2Int(dx, dy);
                        if (IsValidPosition(pos, gridSize) && !visited.Contains(pos))
                        {
                            ringPositions.Add(pos);
                            visited.Add(pos);
                        }
                    }
                }
            }
            
            // Sort ring positions by angle for smooth spiral effect
            ringPositions.Sort((a, b) => 
            {
                Vector2 dirA = (Vector2)(a - center);
                Vector2 dirB = (Vector2)(b - center);
                float angleA = Mathf.Atan2(dirA.y, dirA.x);
                float angleB = Mathf.Atan2(dirB.y, dirB.x);
                return angleA.CompareTo(angleB);
            });
            
            result.AddRange(ringPositions);
            
            // Continue with next ring if there are more positions to fill
            if (ringPositions.Count > 0 && radius < gridSize)
            {
                AddCircularRing(center, radius + 1, gridSize, visited, result);
            }
        }

        private bool IsValidPosition(Vector2Int pos, int gridSize)
        {
            return pos.x >= 0 && pos.x < gridSize && pos.y >= 0 && pos.y < gridSize;
        }
        
        /// <summary>
        /// Generate ripple rings around player positions using Manhattan distance
        /// </summary>
        private List<List<Vector2Int>> GenerateRippleRings(int gridSize)
        {
            Dictionary<int, List<Vector2Int>> distanceGroups = new Dictionary<int, List<Vector2Int>>();
            
            // Calculate distance from each tile to nearest player
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    int minDistance = GetMinDistanceToPlayers(pos);
                    
                    if (!distanceGroups.ContainsKey(minDistance))
                        distanceGroups[minDistance] = new List<Vector2Int>();
                        
                    distanceGroups[minDistance].Add(pos);
                }
            }
            
            // Convert to ordered list of rings
            List<List<Vector2Int>> rings = new List<List<Vector2Int>>();
            
            // Sort by distance to create ripple effect
            for (int distance = 0; distance <= gridSize * 2; distance++)
            {
                if (distanceGroups.ContainsKey(distance))
                {
                    rings.Add(distanceGroups[distance]);
                }
            }
            
            return rings;
        }
        
        /// <summary>
        /// Get Manhattan distance to nearest player position
        /// </summary>
        private int GetMinDistanceToPlayers(Vector2Int pos)
        {
            int minDistance = int.MaxValue;
            
            foreach (Vector2Int playerPos in playerPositions)
            {
                int distance = Mathf.Abs(pos.x - playerPos.x) + Mathf.Abs(pos.y - playerPos.y);
                minDistance = Mathf.Min(minDistance, distance);
            }
            
            return minDistance;
        }
        #endregion

        #region Helper Methods
        private void CollectPlayerPositions()
        {
            playerPositions = new List<Vector2Int>();
            
            if (gameManager != null)
            {
                // Get starting positions from the game manager's pawn system
                for (int i = 0; i < gameManager.pawns.Count; i++)
                {
                    playerPositions.Add(gameManager.pawns[i].startPosition);
                }
            }
            else
            {
                // Fallback: use center positions
                int center = Mathf.FloorToInt((gridSystem.GetGridSize() - 1) / 2);
                playerPositions.Add(new Vector2Int(center, 0)); // Bottom center
                playerPositions.Add(new Vector2Int(center, gridSystem.GetGridSize() - 1)); // Top center
            }
        }

        private void PlayTileSound()
        {
            if (audioSource != null && tileAppearSound != null)
            {
                audioSource.PlayOneShot(tileAppearSound, soundVolume);
            }
        }

        private void ShowAllTilesImmediately()
        {
            // Use GridSystem's immediate tile creation method
            gridSystem.CreateAllTilesImmediate();
        }

        private void OnAnimationComplete()
        {
            isAnimating = false;
            Debug.Log($"TileAnimationController: Animation completed with {animatedTiles.Count} tiles");
            
            OnTileAnimationCompleted?.Invoke();
            
            // Transition to PlayerTurn state
            if (gameManager != null)
            {
                gameManager.ChangeState(GameState.PlayerTurn);
            }
        }
        #endregion

        #region Public API
        public bool IsAnimating => isAnimating;
        
        public void SetAnimationType(AnimationType type)
        {
            if (!isAnimating)
            {
                animationType = type;
                Debug.Log($"TileAnimationController: Animation type set to {type}");
            }
        }

        public void SetAnimationTiming(float delayBefore, float betweenTiles, float betweenRows)
        {
            if (!isAnimating)
            {
                delayBeforeAnimation = delayBefore;
                timeBetweenTiles = betweenTiles;
                timeBetweenRows = betweenRows;
            }
        }
        
        /// <summary>
        /// Set the overall pacing curve settings
        /// </summary>
        public void SetPacingCurve(bool enabled, AnimationCurve curve, float duration)
        {
            if (!isAnimating)
            {
                useOverallPacingCurve = enabled;
                overallPacingCurve = curve;
                overallAnimationDuration = duration;
                Debug.Log($"TileAnimationController: Pacing curve updated - enabled: {enabled}, duration: {duration}");
            }
        }
        
        /// <summary>
        /// Get current animation progress (0 to 1)
        /// </summary>
        public float GetAnimationProgress()
        {
            if (!isAnimating || totalTilesToAnimate <= 0)
                return 0f;
                
            return Mathf.Clamp01((float)tilesAnimated / totalTilesToAnimate);
        }
        
        /// <summary>
        /// Get current timing multiplier for debugging
        /// </summary>
        public float GetCurrentTimingMultiplierDebug()
        {
            return GetCurrentTimingMultiplierByTiles();
        }
        
        /// <summary>
        /// Toggle pacing curve on/off at runtime
        /// </summary>
        public void TogglePacingCurve()
        {
            useOverallPacingCurve = !useOverallPacingCurve;
            Debug.Log($"TileAnimationController: Pacing curve {(useOverallPacingCurve ? "ENABLED" : "DISABLED")}");
        }
        
        /// <summary>
        /// Check if pacing curve is enabled
        /// </summary>
        public bool IsPacingCurveEnabled()
        {
            return useOverallPacingCurve;
        }
        #endregion

        #region Debug
        [ContextMenu("Debug/Test One By One Animation")]
        private void DebugTestOneByOneAnimation()
        {
            if (Application.isPlaying)
            {
                animationType = AnimationType.OneByOne;
                StartTileAnimation();
            }
        }
        
        [ContextMenu("Debug/Test Row Animation")]
        private void DebugTestRowAnimation()
        {
            if (Application.isPlaying)
            {
                animationType = AnimationType.RowByRow;
                StartTileAnimation();
            }
        }

        [ContextMenu("Debug/Test Circular Animation")]
        private void DebugTestCircularAnimation()
        {
            if (Application.isPlaying)
            {
                animationType = AnimationType.CircularSpiral;
                StartTileAnimation();
            }
        }
        
        [ContextMenu("Debug/Test Ripple Animation")]
        private void DebugTestRippleAnimation()
        {
            if (Application.isPlaying)
            {
                animationType = AnimationType.Ripple;
                StartTileAnimation();
            }
        }
        
        [ContextMenu("Debug/Toggle Pacing Curve")]
        private void DebugTogglePacingCurve()
        {
            useOverallPacingCurve = !useOverallPacingCurve;
            Debug.Log($"TileAnimationController: Pacing curve {(useOverallPacingCurve ? "ENABLED" : "DISABLED")}");
        }
        
        [ContextMenu("Debug/Show Animation Stats")]
        private void DebugShowAnimationStats()
        {
            if (isAnimating)
            {
                Debug.Log($"Animation Progress: {GetAnimationProgress():P1} ({tilesAnimated}/{totalTilesToAnimate})");
                Debug.Log($"Current Timing Multiplier: {GetCurrentTimingMultiplierDebug():F3}");
                Debug.Log($"Current Tile Timing: {GetCurrentTileTiming():F3}s");
                Debug.Log($"Current Row Timing: {GetCurrentRowTiming():F3}s");
            }
            else
            {
                Debug.Log("No animation currently running");
            }
        }
        #endregion
    }
}