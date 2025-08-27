using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

namespace WallChess
{
    public partial class WallManager : MonoBehaviour
    {
        #region Types
        public enum Orientation { Horizontal, Vertical }
        #endregion

        #region Inspector Settings
        [Header("Wall Visual Settings")]
        [SerializeField] private Material wallMaterial;
        [SerializeField] private GameObject wallPrefab;
        
        [Header("Gap Detection Settings")]
        [Tooltip("Safe margin around gaps to stabilize orientation")]
        public float gapSnapMargin = 0.25f;

        [Header("Lane Detection (Safe Margins)")]
        [Tooltip("World-units half-width of snap stripes around horizontal (Y) and vertical (X) gap lanes.")]
        public float laneSnapMargin = 0.3f;

        [Tooltip("How much farther you must move out of the locked lane to unlock orientation (hysteresis).")]
        public float unlockMultiplier = 1.5f;

        [Header("Preview Settings")]
        [SerializeField] private Color validPreviewColor = new Color(0, 1, 0, 0.7f);
        [SerializeField] private Color invalidPreviewColor = new Color(1, 0, 0, 0.7f);
        [SerializeField] private Color placingPreviewColor = new Color(1, 1, 0, 0.5f);
        
        [Header("Input & Camera")]
        public LayerMask placementMask = ~0;        // optional; used by plane Raycast fallback
        public float placementPlaneZ = 0f;          // Z of placement plane for ScreenPointToRay
        #endregion

        #region State
        private WallChessGameManager gameManager;
        private GridSystem gridSystem;
        private GameObject wallPreview;
        private Renderer wallPreviewRenderer;
        private bool isPlacing = false;
        private Orientation? orientationLock = null; // Hysteresis for lane detection
        
        // Track spawned walls efficiently
        private readonly List<GameObject> managedWalls = new List<GameObject>(32);
        
        // Efficient gap tracking with boolean arrays (from ImprovedWallPlacer logic)
        private bool[,] horizontalGaps; // [HCols, HRows] 
        private bool[,] verticalGaps;   // [VCols, VRows]
        
        // Grid configuration - corrected for proper gap placement between tiles
        private int Cells => gameManager?.gridSize ?? 8;
        private int HCols => Cells + 1;                     // horizontal gap columns (x) 
        private int HRows => Cells - 1;                     // horizontal gap rows (y) - FIXED: gaps between tile rows
        private int VCols => Cells - 1;                     // vertical gap columns (x) - FIXED: gaps between tile columns
        private int VRows => Cells + 1;                     // vertical gap rows (y)
        private float spacing => gameManager?.tileSize + gameManager?.tileGap ?? 1.2f;
        
        // Gap offsets (aligned with grid centers)
        private float horizontalGapOffsetX = 0.5f;
        private float horizontalGapOffsetY = 0.5f;   // horizontal gaps are between rows
        private float verticalGapOffsetX = 0.5f;     // vertical gaps are between columns  
        private float verticalGapOffsetY = 0.5f;
        #endregion

        #region Initialization
        public void Initialize(WallChessGameManager gm)
        {
            gameManager = gm;
            gridSystem = gm.GetGridSystem();
            
            // Initialize gap arrays
            InitializeGapArrays();
            
            UnityEngine.Debug.Log("WallManager initialized with advanced grid system integration");
            
            // Run wall blocking test after initialization
           // Invoke(nameof(RunAutomaticWallTest), 0.5f);
        }
        
        private void InitializeGapArrays()
        {
            horizontalGaps = new bool[HCols, HRows];
            verticalGaps = new bool[VCols, VRows];
            ClearGapArrays();
        }
        
        private void ClearGapArrays()
        {
            for (int i = 0; i < HCols; i++)
                for (int j = 0; j < HRows; j++)
                    horizontalGaps[i, j] = false;

            for (int i = 0; i < VCols; i++)
                for (int j = 0; j < VRows; j++)
                    verticalGaps[i, j] = false;
        }
        #endregion
        
        #region Debug Testing
        public void RunAutomaticWallTest()
        {
            UnityEngine.Debug.Log("=== AUTOMATIC WALL BLOCKING TEST ===");
            
            // Clear all walls first
            ClearGapArrays();
            
            // Test the specific scenario reported by user
            UnityEngine.Debug.Log("\n--- SPECIFIC TEST: Horizontal wall at (4,7) ---");
            
            // Place horizontal wall at (4,7) - this should occupy horizontal gaps (4,7) and (5,7)
            SetGapOccupiedForTesting(Orientation.Horizontal, 4, 7, true);
            SetGapOccupiedForTesting(Orientation.Horizontal, 5, 7, true);
            UnityEngine.Debug.Log("Placed horizontal wall at (4,7) - occupying gaps (4,7) and (5,7)");
            
            // Test 1: Player at starting position (4,8) moving down to (4,7) - should be BLOCKED
            Vector2Int startPos = new Vector2Int(4, 8);
            Vector2Int downTarget1 = new Vector2Int(4, 7);
            bool blocked1 = IsMovementBlocked(startPos, downTarget1);
            UnityEngine.Debug.Log($"Movement from {startPos} DOWN to {downTarget1}: {(blocked1 ? "BLOCKED" : "ALLOWED")} (should be BLOCKED)");
            
            // Test 2: Player moves right to (5,8) then tries to move down to (5,7) - should be BLOCKED
            Vector2Int rightPos = new Vector2Int(5, 8);
            Vector2Int downTarget2 = new Vector2Int(5, 7);
            bool blocked2 = IsMovementBlocked(rightPos, downTarget2);
            UnityEngine.Debug.Log($"Movement from {rightPos} DOWN to {downTarget2}: {(blocked2 ? "BLOCKED" : "ALLOWED")} (should be BLOCKED)");
            
            // Test 3: Check if the wall properly blocks adjacent positions
            Vector2Int rightPos2 = new Vector2Int(6, 8);
            Vector2Int downTarget3 = new Vector2Int(6, 7);
            bool blocked3 = IsMovementBlocked(rightPos2, downTarget3);
            UnityEngine.Debug.Log($"Movement from {rightPos2} DOWN to {downTarget3}: {(blocked3 ? "BLOCKED" : "ALLOWED")} (should be ALLOWED - no wall here)");
            
            UnityEngine.Debug.Log("=== SPECIFIC TEST COMPLETE ===");
            
            // Original vertical wall tests
            ClearGapArrays();
            Vector2Int pawn = new Vector2Int(4, 4);
            UnityEngine.Debug.Log($"\nTesting vertical walls with pawn at {pawn}");
            
            // Test 1: Place vertical wall to the RIGHT (gap at x=4)
            UnityEngine.Debug.Log("--- Test 1: RIGHT wall ---");
            SetGapOccupiedForTesting(Orientation.Vertical, 4, 4, true);
            SetGapOccupiedForTesting(Orientation.Vertical, 4, 5, true);
            
            Vector2Int rightTarget = new Vector2Int(5, 4);
            bool rightBlocked = IsMovementBlocked(pawn, rightTarget);
            UnityEngine.Debug.Log($"Movement RIGHT to {rightTarget}: {(rightBlocked ? "BLOCKED" : "ALLOWED")} (should be BLOCKED)");
            
            // Test 2: Clear and place wall to LEFT (gap at x=3)
            ClearGapArrays();
            UnityEngine.Debug.Log("--- Test 2: LEFT wall ---");
            SetGapOccupiedForTesting(Orientation.Vertical, 3, 4, true);
            SetGapOccupiedForTesting(Orientation.Vertical, 3, 5, true);
            
            Vector2Int leftTarget = new Vector2Int(3, 4);
            bool leftBlocked = IsMovementBlocked(pawn, leftTarget);
            UnityEngine.Debug.Log($"Movement LEFT to {leftTarget}: {(leftBlocked ? "BLOCKED" : "ALLOWED")} (should be BLOCKED)");
            
            UnityEngine.Debug.Log("=== AUTOMATIC TEST COMPLETE ===");
        }
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void TestWallBlocking()
        {
            UnityEngine.Debug.Log("=== TESTING WALL BLOCKING LOGIC ===");
            UnityEngine.Debug.Log($"Grid info: Cells={Cells}, VCols={VCols}, VRows={VRows}, HCols={HCols}, HRows={HRows}");
            
            // Test case: Pawn at (4,4) - middle of a 9x9 grid
            Vector2Int testPawn = new Vector2Int(4, 4);
            
            // Clear all walls first
            ClearGapArrays();
            
            // Test 1: Place vertical wall to RIGHT of pawn
            UnityEngine.Debug.Log("\n--- TEST 1: Vertical wall to RIGHT of pawn ---");
            UnityEngine.Debug.Log($"Pawn at tile ({testPawn.x},{testPawn.y})");
            
            // The RIGHT wall gap should be at x=pawn.x, but what about y?
            // Let's try gap y = pawn.y (which is 4)
            int rightGapX = testPawn.x; // 4
            int rightGapY = testPawn.y; // 4
            
            UnityEngine.Debug.Log($"Trying to place vertical wall at gap ({rightGapX},{rightGapY}) and ({rightGapX},{rightGapY + 1})");
            
            // Check bounds first
            if (rightGapX >= 0 && rightGapX < VCols && rightGapY >= 0 && rightGapY + 1 < VRows)
            {
                SetGapOccupiedForTesting(Orientation.Vertical, rightGapX, rightGapY, true);
                SetGapOccupiedForTesting(Orientation.Vertical, rightGapX, rightGapY + 1, true);
                
                UnityEngine.Debug.Log($"Successfully placed wall at gaps ({rightGapX},{rightGapY}) and ({rightGapX},{rightGapY + 1})");
                
                // Test rightward movement (should be blocked)
                Vector2Int rightTarget = testPawn + Vector2Int.right; // (5,4)
                bool rightBlocked = IsPathBlocked(testPawn, rightTarget);
                UnityEngine.Debug.Log($"Movement RIGHT to ({rightTarget.x},{rightTarget.y}): {(rightBlocked ? "BLOCKED" : "ALLOWED")} (should be BLOCKED)");
            }
            else
            {
                UnityEngine.Debug.Log($"ERROR: Gap position ({rightGapX},{rightGapY}) or ({rightGapX},{rightGapY + 1}) is out of bounds!");
                UnityEngine.Debug.Log($"Valid ranges: x=[0,{VCols-1}], y=[0,{VRows-1}]");
            }
            
            // Clear and test wall to LEFT
            ClearGapArrays();
            
            UnityEngine.Debug.Log("\n--- TEST 2: Vertical wall to LEFT of pawn ---");
            
            // For leftward movement from (4,4) to (3,4), we check gap at target column (3)
            int leftGapX = testPawn.x - 1; // 3 (gap between columns 3 and 4)
            int leftGapY = testPawn.y; // 4
            
            UnityEngine.Debug.Log($"Trying to place vertical wall at gap ({leftGapX},{leftGapY}) and ({leftGapX},{leftGapY + 1})");
            
            // Check bounds first
            if (leftGapX >= 0 && leftGapX < VCols && leftGapY >= 0 && leftGapY + 1 < VRows)
            {
                SetGapOccupiedForTesting(Orientation.Vertical, leftGapX, leftGapY, true);
                SetGapOccupiedForTesting(Orientation.Vertical, leftGapX, leftGapY + 1, true);
                
                UnityEngine.Debug.Log($"Successfully placed wall at gaps ({leftGapX},{leftGapY}) and ({leftGapX},{leftGapY + 1})");
                
                // Test leftward movement (should be blocked)
                Vector2Int leftTarget = testPawn + Vector2Int.left; // (3,4)
                bool leftBlocked = IsPathBlocked(testPawn, leftTarget);
                UnityEngine.Debug.Log($"Movement LEFT to ({leftTarget.x},{leftTarget.y}): {(leftBlocked ? "BLOCKED" : "ALLOWED")} (should be BLOCKED)");
            }
            else
            {
                UnityEngine.Debug.Log($"ERROR: Gap position ({leftGapX},{leftGapY}) or ({leftGapX},{leftGapY + 1}) is out of bounds!");
                UnityEngine.Debug.Log($"Valid ranges: x=[0,{VCols-1}], y=[0,{VRows-1}]");
            }
            
            UnityEngine.Debug.Log("=== END WALL BLOCKING TEST ===");
        }
        #endregion
        
        #region Unity Lifecycle
        void Update()
        {
            // Press Y to run horizontal wall test
            if (Input.GetKeyDown(KeyCode.Y))
            {
                RunAutomaticWallTest();
            }
            
            // Press T to run wall blocking test
            if (Input.GetKeyDown(KeyCode.T))
            {
                TestWallBlocking();
            }
            // Handle wall placement input only if GameManager allows it AND we're not clicking on an avatar
            if (Input.GetMouseButtonDown(0) && gameManager.CanInitiateWallPlacement())
            {
                // Check if we're clicking on an avatar first
                if (!IsClickingOnAvatar())
                {
                    if (gameManager.TryStartWallPlacement())
                    {
                        TryStartPlacing();
                    }
                }
            }
            else if (Input.GetMouseButton(0) && isPlacing)
            {
                UpdatePreview();
            }
            else if (Input.GetMouseButtonUp(0) && isPlacing)
            {
                PlaceWall();
            }
            // Handle cancellation when mouse is released outside bounds or invalid area
            else if (Input.GetMouseButtonUp(0) && gameManager.GetCurrentState() == GameState.WallPlacement && !isPlacing)
            {
                // Cancel wall placement if we were in wall placement state but not actively placing
                gameManager.CompleteWallPlacement(false);
            }
        }

        /// <summary>
        /// Check if the mouse is clicking on a player avatar to avoid conflicts
        /// </summary>
        bool IsClickingOnAvatar()
        {
            Vector3 mousePos = GetMouseWorld();
            
            // Check player avatar position
            Vector3 playerWorldPos = gameManager.GetGridSystem().GridToWorldPosition(gameManager.playerPosition);
            if (Vector3.Distance(mousePos, playerWorldPos) < gameManager.tileSize * 0.6f)
            {
                return true;
            }
            
            // Check opponent avatar position
            Vector3 opponentWorldPos = gameManager.GetGridSystem().GridToWorldPosition(gameManager.opponentPosition);
            if (Vector3.Distance(mousePos, opponentWorldPos) < gameManager.tileSize * 0.6f)
            {
                return true;
            }
            
            return false;
        }
        #endregion

        #region Wall Placement Flow
        void TryStartPlacing()
        {
            Vector3 mousePos = GetMouseWorld();
            if (IsWithinGridBounds(mousePos))
            {
                isPlacing = true;
                CreatePreview();
            }
        }

        void CreatePreview()
        {
            if (wallPreview == null)
            {
                wallPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallPreview.name = "WallPreview";
                wallPreviewRenderer = wallPreview.GetComponent<Renderer>();
                wallPreviewRenderer.material.color = placingPreviewColor;
                var collider = wallPreview.GetComponent<Collider>();
                SafeDestroy(collider);
            }
        }

        void UpdatePreview()
        {
            if (wallPreview == null) return;

            Vector3 mousePos = GetMouseWorld();
            if (!IsWithinGridBounds(mousePos)) 
            { 
                wallPreview.SetActive(false);
                // If we've been out of bounds for a while, cancel wall placement
                // This prevents the state from getting stuck
                return; 
            }

            // Use advanced gap detection from ImprovedWallPlacer
            bool hasCandidate = TryFindNearestGap(mousePos, out WallInfo info);
            if (!hasCandidate)
            {
                wallPreview.SetActive(false);
                return;
            }

            wallPreview.SetActive(true);
            wallPreview.transform.position = info.position;
            wallPreview.transform.localScale = info.scale;

            // Tint based on validity
            bool canPlace = CanPlaceWall(info);
            wallPreviewRenderer.material.color = canPlace ? validPreviewColor : invalidPreviewColor;
        }

        void PlaceWall()
        {
            bool wallPlacementSuccessful = false;
            
            if (wallPreview != null && wallPreview.activeInHierarchy)
            {
                Vector3 mousePos = GetMouseWorld();
                
                // Check if we're still within bounds when trying to place
                if (IsWithinGridBounds(mousePos) && TryFindNearestGap(mousePos, out WallInfo info) && CanPlaceWall(info))
                {
                    // Create actual wall
                    CreateWallVisual(info);
                    
                    // Mark gaps as occupied
                    OccupyWallPositions(info);
                    
                    // Notify game manager and grid system
                    OnWallPlaced(info);
                    
                    // Let GameManager handle turn transition and wall count
                    // (This is now handled in GameManager.OnWallPlaced event)
                    
                    wallPlacementSuccessful = true;
                    UnityEngine.Debug.Log($"Wall placed at ({info.x},{info.y}) {info.orientation}!");
                }
                else
                {
                    UnityEngine.Debug.Log("Wall placement failed - invalid position or out of bounds");
                }
                
                // Clean up preview
                SafeDestroy(wallPreview);
                wallPreview = null;
                wallPreviewRenderer = null;
            }
            else
            {
                UnityEngine.Debug.Log("Wall placement cancelled - no valid preview");
            }
            
            // Always complete wall placement, regardless of success
            gameManager.CompleteWallPlacement(wallPlacementSuccessful);
            
            // Reset placement state
            orientationLock = null; 
            isPlacing = false;
        }
        #endregion

        #region Advanced Gap Detection (from ImprovedWallPlacer)
        // Nearest "lane line" (world Y) for horizontal walls (rows)
        float NearestHorizontalLaneY(float worldY)
        {
            float ky = Mathf.Round(worldY / spacing - horizontalGapOffsetY);
            return (ky + horizontalGapOffsetY) * spacing;
        }

        // Nearest "lane line" (world X) for vertical walls (columns)
        float NearestVerticalLaneX(float worldX)
        {
            float kx = Mathf.Round(worldX / spacing - verticalGapOffsetX);
            return (kx + verticalGapOffsetX) * spacing;
        }

        // Advanced gap detection with lane-based orientation locking
        bool TryFindNearestGap(in Vector3 mousePos, out WallInfo result)
        {
            // Lane distances (stripe logic)
            float yLane = NearestHorizontalLaneY(mousePos.y);
            float xLane = NearestVerticalLaneX(mousePos.x);
            float dY = Mathf.Abs(mousePos.y - yLane);   // distance to horizontal lane
            float dX = Mathf.Abs(mousePos.x - xLane);   // distance to vertical lane

            bool inHStripe = dY <= laneSnapMargin;
            bool inVStripe = dX <= laneSnapMargin;

            // Hysteresis: if we're locked to an orientation, keep it until we leave a wider stripe
            if (orientationLock.HasValue)
            {
                if (orientationLock.Value == Orientation.Horizontal)
                {
                    if (dY <= laneSnapMargin * unlockMultiplier)
                    {
                        inHStripe = true; inVStripe = false;
                    }
                    else orientationLock = null; // left the stripe -> unlock
                }
                else
                {
                    if (dX <= laneSnapMargin * unlockMultiplier)
                    {
                        inVStripe = true; inHStripe = false;
                    }
                    else orientationLock = null;
                }
            }
            else
            {
                // If neither stripe is hit, allow both so we can still find a nearest center
                if (!inHStripe && !inVStripe) { inHStripe = true; inVStripe = true; }
            }

            // Candidate computation using direct index math
            int hx0, hy0, hx1, hy1; // horizontal candidates (floor & round)
            int vx0, vy0, vx1, vy1; // vertical candidates (floor & round)
            
            MapToGapIndices(mousePos, Orientation.Horizontal, out hx0, out hy0, floor: true);
            MapToGapIndices(mousePos, Orientation.Horizontal, out hx1, out hy1, floor: false);
            MapToGapIndices(mousePos, Orientation.Vertical, out vx0, out vy0, floor: true);
            MapToGapIndices(mousePos, Orientation.Vertical, out vx1, out vy1, floor: false);

            bool hValid0 = ClampToGapRange(ref hx0, ref hy0, Orientation.Horizontal);
            bool hValid1 = ClampToGapRange(ref hx1, ref hy1, Orientation.Horizontal);
            bool vValid0 = ClampToGapRange(ref vx0, ref vy0, Orientation.Vertical);
            bool vValid1 = ClampToGapRange(ref vx1, ref vy1, Orientation.Vertical);

            float bestDistSq = float.PositiveInfinity;
            bool found = false;
            result = default;

            // Gate evaluation by lane stripes
            if (inHStripe)
            {
                EvaluateCandidate(Orientation.Horizontal, hx0, hy0, hValid0, mousePos, ref bestDistSq, ref found, ref result);
                EvaluateCandidate(Orientation.Horizontal, hx1, hy1, hValid1, mousePos, ref bestDistSq, ref found, ref result);
            }
            if (inVStripe)
            {
                EvaluateCandidate(Orientation.Vertical, vx0, vy0, vValid0, mousePos, ref bestDistSq, ref found, ref result);
                EvaluateCandidate(Orientation.Vertical, vx1, vy1, vValid1, mousePos, ref bestDistSq, ref found, ref result);
            }

            // Lock orientation if we picked within the narrow stripe (prevents flicker)
            if (found)
            {
                if (result.orientation == Orientation.Horizontal && dY <= laneSnapMargin)
                    orientationLock = Orientation.Horizontal;
                else if (result.orientation == Orientation.Vertical && dX <= laneSnapMargin)
                    orientationLock = Orientation.Vertical;
            }

            return found;
        }

        void EvaluateCandidate(Orientation o, int x, int y, bool valid, in Vector3 mousePos,
                               ref float bestDistSq, ref bool found, ref WallInfo best)
        {
            if (!valid) return;

            Vector3 center = GapCenter(o, x, y);
            float d2 = (mousePos - center).sqrMagnitude;

            // Apply margin: if within margin of current best, prefer to KEEP same orientation
            if (found && Mathf.Abs(d2 - bestDistSq) < gapSnapMargin * gapSnapMargin)
            {
                // Don't switch orientation if both candidates are nearly tied
                if (best.orientation == Orientation.Vertical && o == Orientation.Horizontal)
                    return; // keep vertical
            }

            if (d2 < bestDistSq)
            {
                bestDistSq = d2;
                best = CreateWallInfo(o, x, y, center);
                found = true;
            }
        }

        void MapToGapIndices(in Vector3 p, Orientation o, out int gx, out int gy, bool floor)
        {
            if (o == Orientation.Horizontal)
            {
                float fx = p.x / spacing - horizontalGapOffsetX;
                float fy = p.y / spacing - horizontalGapOffsetY;
                gx = floor ? Mathf.FloorToInt(fx) : Mathf.RoundToInt(fx);
                gy = floor ? Mathf.FloorToInt(fy) : Mathf.RoundToInt(fy);
            }
            else
            {
                float fx = p.x / spacing - verticalGapOffsetX;
                float fy = p.y / spacing - verticalGapOffsetY;
                gx = floor ? Mathf.FloorToInt(fx) : Mathf.RoundToInt(fx);
                gy = floor ? Mathf.FloorToInt(fy) : Mathf.RoundToInt(fy);
            }
        }

        bool ClampToGapRange(ref int x, ref int y, Orientation o)
        {
            if (o == Orientation.Horizontal)
            {
                // Horizontal walls: spans 2 gaps horizontally (x and x+1)
                // Must ensure both x and x+1 are within valid bounds AND within board
                // For a 9x9 board, valid horizontal wall x positions are [0, 6] 
                // (so x+1 spans [1, 7], keeping the wall within the 8 interior columns)
                int maxX = Mathf.Min(HCols - 2, Cells - 2); // Ensure wall doesn't extend beyond board
                x = Mathf.Clamp(x, 0, maxX);
                
                // y must be within the gap rows between tiles [0..HRows-1]
                y = Mathf.Clamp(y, 0, HRows - 1);
                
                bool valid = x >= 0 && x + 1 < HCols && y >= 0 && y < HRows && x + 1 <= Cells - 1;
                
                if (!valid)
                {
                    UnityEngine.Debug.Log($"[CLAMP] Horizontal wall ({x},{y}) invalid: maxX={maxX}, HCols={HCols}, HRows={HRows}");
                }
                
                return valid;
            }
            else
            {
                // Vertical walls: spans 2 gaps vertically (y and y+1)
                // Must ensure both y and y+1 are within valid bounds AND within board
                // For a 9x9 board, valid vertical wall y positions are [0, 6]
                // (so y+1 spans [1, 7], keeping the wall within the 8 interior rows)
                int maxY = Mathf.Min(VRows - 2, Cells - 2); // Ensure wall doesn't extend beyond board
                y = Mathf.Clamp(y, 0, maxY);
                
                // x must be within the gap columns between tiles [0..VCols-1]
                x = Mathf.Clamp(x, 0, VCols - 1);
                
                bool valid = x >= 0 && x < VCols && y >= 0 && y + 1 < VRows && y + 1 <= Cells - 1;
                
                if (!valid)
                {
                    UnityEngine.Debug.Log($"[CLAMP] Vertical wall ({x},{y}) invalid: maxY={maxY}, VCols={VCols}, VRows={VRows}");
                }
                
                return valid;
            }
        }

        WallInfo CreateWallInfo(Orientation o, int x, int y, in Vector3 center)
        {
            Vector3 scale = (o == Orientation.Horizontal)
                ? new Vector3(gameManager.tileSize * 2f + gameManager.tileGap, gameManager.wallThickness, gameManager.wallHeight)
                : new Vector3(gameManager.wallThickness, gameManager.tileSize * 2f + gameManager.tileGap, gameManager.wallHeight);

            return new WallInfo(o, x, y, new Vector3(center.x, center.y, -0.1f), scale);
        }

        Vector3 GapCenter(Orientation o, int x, int y)
        {
            if (o == Orientation.Horizontal)
                return new Vector3((x + horizontalGapOffsetX) * spacing,
                                   (y + horizontalGapOffsetY) * spacing,
                                   0f);
            else
                return new Vector3((x + verticalGapOffsetX) * spacing,
                                   (y + verticalGapOffsetY) * spacing,
                                   0f);
        }
        #endregion

        #region Wall Validation & Placement Logic  
        bool CanPlaceWall(in WallInfo w)
        {
            // TEMPORARY DEBUG VERSION - Remove this after fixing
            UnityEngine.Debug.Log($"[DEBUG] CanPlaceWall called for {w.orientation} wall at ({w.x},{w.y})");
            
            // Check if GameManager allows wall placement
            if (gameManager == null)
            {
                UnityEngine.Debug.Log($"[DEBUG] gameManager is null!");
                return false;
            }
            
            bool canPlaceWalls = gameManager.CanPlaceWalls();
            UnityEngine.Debug.Log($"[DEBUG] gameManager.CanPlaceWalls() = {canPlaceWalls}");
            UnityEngine.Debug.Log($"[DEBUG] gameManager currentAction = {gameManager.GetCurrentAction()}");
            if (!canPlaceWalls) return false;
            
            // Check if current player has walls remaining
            bool hasWalls = gameManager.CurrentPlayerHasWalls();
            UnityEngine.Debug.Log($"[DEBUG] gameManager.CurrentPlayerHasWalls() = {hasWalls}");
            UnityEngine.Debug.Log($"[DEBUG] currentPlayer = {gameManager.GetCurrentPlayer()}");
            UnityEngine.Debug.Log($"[DEBUG] playerWallsRemaining = {gameManager.playerWallsRemaining}");
            UnityEngine.Debug.Log($"[DEBUG] opponentWallsRemaining = {gameManager.opponentWallsRemaining}");
            if (!hasWalls) return false;
            
            // For now, assume all other checks pass to focus on the state issue
            UnityEngine.Debug.Log($"[DEBUG] State checks passed! Now checking bounds, occupancy, crossing, and paths...");
            
            // Add back bounds and occupancy checks
            if (w.orientation == Orientation.Horizontal)
            {
                // Bounds check
                if (w.x < 0 || w.x + 1 >= HCols || w.y < 0 || w.y >= HRows) {
                    UnityEngine.Debug.Log($"[DEBUG] FAILED: Horizontal bounds (x={w.x}, y={w.y}, HCols={HCols}, HRows={HRows})");
                    return false;
                }
                // Occupancy check
                if (IsOccupied(Orientation.Horizontal, w.x, w.y)) {
                    UnityEngine.Debug.Log($"[DEBUG] FAILED: Horizontal position ({w.x},{w.y}) occupied");
                    return false;
                }
                if (IsOccupied(Orientation.Horizontal, w.x + 1, w.y)) {
                    UnityEngine.Debug.Log($"[DEBUG] FAILED: Horizontal position ({w.x + 1},{w.y}) occupied");
                    return false;
                }
            }
            else // Vertical
            {
                // Bounds check
                if (w.x < 0 || w.x >= VCols || w.y < 0 || w.y + 1 >= VRows) {
                    UnityEngine.Debug.Log($"[DEBUG] FAILED: Vertical bounds (x={w.x}, y={w.y}, VCols={VCols}, VRows={VRows})");
                    return false;
                }
                // Occupancy check
                if (IsOccupied(Orientation.Vertical, w.x, w.y)) {
                    UnityEngine.Debug.Log($"[DEBUG] FAILED: Vertical position ({w.x},{w.y}) occupied");
                    return false;
                }
                if (IsOccupied(Orientation.Vertical, w.x, w.y + 1)) {
                    UnityEngine.Debug.Log($"[DEBUG] FAILED: Vertical position ({w.x},{w.y + 1}) occupied");
                    return false;
                }
            }
            
            // Check crossing and path blocking with debug
            bool crossCheck = !WouldCrossExistingWall(w);
            bool pathCheck = !WouldBlockPlayerPaths(w);
            UnityEngine.Debug.Log($"[DEBUG] Crossing check: {crossCheck}, Path check: {pathCheck}");
            
            if (!crossCheck) {
                UnityEngine.Debug.Log($"[DEBUG] FAILED: Would cross existing wall");
                return false;
            }
            if (!pathCheck) {
                UnityEngine.Debug.Log($"[DEBUG] FAILED: Would block player paths");
                return false;
            }
            
            UnityEngine.Debug.Log($"[DEBUG] SUCCESS: All validation passed!");
            return true;
    
        }

        // Efficient pair-aware crossing detection
        bool WouldCrossExistingWall(in WallInfo newWall)
        {
            int x = newWall.x;
            int y = newWall.y;

            if (newWall.orientation == Orientation.Horizontal)
            {
                // crossing only if BOTH vertical halves at (x,y) and (x,y+1) are set
                if (y + 1 >= VRows) return false; // bound-safe
                return verticalGaps[x, y] && verticalGaps[x, y + 1];
            }
            else
            {
                // crossing only if BOTH horizontal halves at (x,y) and (x+1,y) are set
                if (x + 1 >= HCols) return false; // bound-safe
                return horizontalGaps[x, y] && horizontalGaps[x + 1, y];
            }
        }
        
        // Ensure players always have a path to their goal (Quoridor rule)
        bool WouldBlockPlayerPaths(in WallInfo wallInfo)
        {
            if (gameManager == null) return false;
            
            // Temporarily place the wall to test pathfinding
            OccupyWallPositions(wallInfo);
            
            // Check if both players still have valid paths
            bool playerCanReach = HasPathToGoal(gameManager.playerPosition, true);
            bool opponentCanReach = HasPathToGoal(gameManager.opponentPosition, false);
            
            // Remove the temporary wall placement
            ClearWallPositions(wallInfo);
            
            return !(playerCanReach && opponentCanReach);
        }
        
        // Simple BFS pathfinding to check if a player can reach their goal
        bool HasPathToGoal(Vector2Int startPos, bool isPlayer)
        {
            if (gameManager == null) return false;
            
            // Goal: player needs to reach y=0, opponent needs to reach y=gridSize-1
            int goalY = isPlayer ? gameManager.gridSize - 1 : 0;
            
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            
            queue.Enqueue(startPos);
            visited.Add(startPos);
            
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                
                // Check if reached goal
                if (current.y == goalY) return true;
                
                // Explore neighbors
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int next = current + direction;
                    
                    // Check bounds
                    if (next.x < 0 || next.x >= gameManager.gridSize || 
                        next.y < 0 || next.y >= gameManager.gridSize) continue;
                    
                    // Check if already visited
                    if (visited.Contains(next)) continue;
                    
                    // Check if path is blocked by wall using corrected logic
                    if (IsPathBlocked(current, next)) continue;
                    
                    queue.Enqueue(next);
                    visited.Add(next);
                }
            }
            
            return false; // No path found
        }
        
        // Check if movement between two adjacent tiles is blocked by a wall
        bool IsPathBlocked(Vector2Int from, Vector2Int to)
        {
            Vector2Int diff = to - from;
            
            // UnityEngine.Debug.Log($"[PATH DEBUG] Checking movement from {from} to {to}, diff={diff}");
            
            // Determine which gap to check based on movement direction
            if (diff.y == 1) // Moving up
            {
                // Check horizontal wall above 'from' position
                int gapX = from.x;
                int gapY = from.y; // Gap is at the 'from' position for upward movement
                
                // UnityEngine.Debug.Log($"[PATH DEBUG] Moving UP: checking horizontal gap ({gapX},{gapY})");
                return IsHorizontalWallBlocking(gapX, gapY);
            }
            else if (diff.y == -1) // Moving down
            {
                // Check horizontal wall below 'from' position
                int gapX = from.x;
                int gapY = to.y; // Gap between row 'to.y' and row 'from.y' (use target row)
                
                // UnityEngine.Debug.Log($"[PATH DEBUG] Moving DOWN: checking horizontal gap ({gapX},{gapY})");
                return IsHorizontalWallBlocking(gapX, gapY);
            }
            else if (diff.x == 1) // Moving right
            {
                // Check vertical wall to the right of 'from' position
                int gapX = from.x; // Gap is at the 'from' position for rightward movement
                int gapY = from.y;
                
                // UnityEngine.Debug.Log($"[PATH DEBUG] Moving RIGHT: checking vertical gap ({gapX},{gapY})");
                return IsVerticalWallBlocking(gapX, gapY);
            }
            else if (diff.x == -1) // Moving left
            {
                // Check vertical wall to the left of 'from' position
                // The gap between tile column 'to.x' and 'from.x' is at gap position 'to.x'
                int gapX = to.x; // FIXED: Use target column instead of from.x - 1
                int gapY = from.y;
                
                // UnityEngine.Debug.Log($"[PATH DEBUG] Moving LEFT: checking vertical gap ({gapX},{gapY})");
                return IsVerticalWallBlocking(gapX, gapY);
            }
            
            return false; // Invalid movement
        }
        
        /// <summary>
        /// Check if a horizontal wall is blocking movement
        /// A horizontal wall blocks if BOTH of its gap positions are occupied
        /// FIXED: Check both possible wall positions that could block this gap
        /// </summary>
        bool IsHorizontalWallBlocking(int gapX, int gapY)
        {
            // Check bounds first
            if (gapX < 0 || gapY < 0 || gapY >= HRows) return false;
            
            // A horizontal wall can block movement at gapX in two ways:
            // 1. Wall starts at gapX: occupies (gapX, gapY) and (gapX+1, gapY)
            // 2. Wall starts at gapX-1: occupies (gapX-1, gapY) and (gapX, gapY)
            
            // Check possibility 1: Wall starts at gapX
            if (gapX + 1 < HCols)
            {
                bool wall1Left = horizontalGaps[gapX, gapY];
                bool wall1Right = horizontalGaps[gapX + 1, gapY];
                if (wall1Left && wall1Right)
                {
                    UnityEngine.Debug.Log($"[HORIZONTAL DEBUG] Wall found starting at ({gapX},{gapY}): left={wall1Left}, right={wall1Right}");
                    return true;
                }
            }
            
            // Check possibility 2: Wall starts at gapX-1
            if (gapX - 1 >= 0)
            {
                bool wall2Left = horizontalGaps[gapX - 1, gapY];
                bool wall2Right = horizontalGaps[gapX, gapY];
                if (wall2Left && wall2Right)
                {
                    UnityEngine.Debug.Log($"[HORIZONTAL DEBUG] Wall found starting at ({gapX - 1},{gapY}): left={wall2Left}, right={wall2Right}");
                    return true;
                }
            }
            
            return false; // No wall blocks this position
        }
        
        /// <summary>
        /// Check if a vertical wall is blocking movement
        /// A vertical wall blocks if BOTH of its gap positions are occupied
        /// FIXED: Check both possible wall positions that could block this gap
        /// </summary>
        bool IsVerticalWallBlocking(int gapX, int gapY)
        {
            // Check bounds first
            if (gapX < 0 || gapY < 0 || gapX >= VCols) return false;
            
            // A vertical wall can block movement at gapY in two ways:
            // 1. Wall starts at gapY: occupies (gapX, gapY) and (gapX, gapY+1)
            // 2. Wall starts at gapY-1: occupies (gapX, gapY-1) and (gapX, gapY)
            
            // Check possibility 1: Wall starts at gapY
            if (gapY + 1 < VRows)
            {
                bool wall1Bottom = verticalGaps[gapX, gapY];
                bool wall1Top = verticalGaps[gapX, gapY + 1];
                if (wall1Bottom && wall1Top)
                {
                    // UnityEngine.Debug.Log($"[VERTICAL DEBUG] Wall found starting at ({gapX},{gapY}): bottom={wall1Bottom}, top={wall1Top}");
                    return true;
                }
            }
            
            // Check possibility 2: Wall starts at gapY-1
            if (gapY - 1 >= 0)
            {
                bool wall2Bottom = verticalGaps[gapX, gapY - 1];
                bool wall2Top = verticalGaps[gapX, gapY];
                if (wall2Bottom && wall2Top)
                {
                    // UnityEngine.Debug.Log($"[VERTICAL DEBUG] Wall found starting at ({gapX},{gapY - 1}): bottom={wall2Bottom}, top={wall2Top}");
                    return true;
                }
            }
            
            return false; // No wall blocks this position
        }

        void OccupyWallPositions(in WallInfo w)
        {
            if (w.orientation == Orientation.Horizontal)
            {
                System.Diagnostics.Debug.Assert(w.x + 1 < HCols && w.y < HRows, "Horizontal occupy out of range");
                SetOccupied(Orientation.Horizontal, w.x, w.y, true);
                SetOccupied(Orientation.Horizontal, w.x + 1, w.y, true);
            }
            else
            {
                System.Diagnostics.Debug.Assert(w.y + 1 < VRows && w.x < VCols, "Vertical occupy out of range");
                SetOccupied(Orientation.Vertical, w.x, w.y, true);
                SetOccupied(Orientation.Vertical, w.x, w.y + 1, true);
            }
        }
        
        void ClearWallPositions(in WallInfo w)
        {
            if (w.orientation == Orientation.Horizontal)
            {
                SetOccupied(Orientation.Horizontal, w.x, w.y, false);
                SetOccupied(Orientation.Horizontal, w.x + 1, w.y, false);
            }
            else
            {
                SetOccupied(Orientation.Vertical, w.x, w.y, false);
                SetOccupied(Orientation.Vertical, w.x, w.y + 1, false);
            }
        }
        #endregion

        #region Gap Management Helpers
        bool IsOccupied(Orientation o, int x, int y)
        {
            // Bounds checking to prevent out-of-bounds access
            if (o == Orientation.Horizontal)
            {
                if (x < 0 || x >= HCols || y < 0 || y >= HRows) return true; // Treat out-of-bounds as occupied
                return horizontalGaps[x, y];
            }
            else
            {
                if (x < 0 || x >= VCols || y < 0 || y >= VRows) return true; // Treat out-of-bounds as occupied
                return verticalGaps[x, y];
            }
        }

        void SetOccupied(Orientation o, int x, int y, bool value)
        {
            // Bounds checking to prevent out-of-bounds access
            if (o == Orientation.Horizontal)
            {
                if (x >= 0 && x < HCols && y >= 0 && y < HRows)
                    horizontalGaps[x, y] = value;
            }
            else
            {
                if (x >= 0 && x < VCols && y >= 0 && y < VRows)
                    verticalGaps[x, y] = value;
            }
        }
        
        // Legacy compatibility for GridSystem integration
        public bool IsGapOccupied(Orientation o, int x, int y)
        {
            return IsOccupied(o, x, y);
        }
        #endregion

        #region Wall Visual Management
        private void CreateWallVisual(WallInfo wallInfo)
        {
            GameObject wallObj;
            
            if (wallPrefab != null)
            {
                wallObj = Instantiate(wallPrefab);
            }
            else
            {
                wallObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }

            wallObj.name = $"Wall_{wallInfo.orientation}_{wallInfo.x}_{wallInfo.y}";
            wallObj.transform.position = wallInfo.position;
            wallObj.transform.localScale = wallInfo.scale;
            wallObj.tag = "Wall";

            // Apply material
            var renderer = wallObj.GetComponent<Renderer>();
            if (renderer != null && wallMaterial != null)
            {
                renderer.material = wallMaterial;
            }
            else if (renderer != null)
            {
                renderer.material.color = Color.yellow;
            }

            managedWalls.Add(wallObj);
        }

        private void OnWallPlaced(WallInfo wallInfo)
        {
            // Sync with GridSystem for compatibility
            if (gridSystem != null)
            {
                var gridWallInfo = new GridSystem.WallInfo(
                    wallInfo.orientation == Orientation.Horizontal ? GridSystem.Orientation.Horizontal : GridSystem.Orientation.Vertical,
                    wallInfo.x, wallInfo.y, wallInfo.position, wallInfo.scale
                );
                
                // Update GridSystem state to maintain sync
                gridSystem.SetGapOccupied(gridWallInfo.orientation, wallInfo.x, wallInfo.y, true);
                if (wallInfo.orientation == Orientation.Horizontal)
                    gridSystem.SetGapOccupied(gridWallInfo.orientation, wallInfo.x + 1, wallInfo.y, true);
                else
                    gridSystem.SetGapOccupied(gridWallInfo.orientation, wallInfo.x, wallInfo.y + 1, true);
            }
        }
        #endregion

        #region Input & Camera
        Vector3 GetMouseWorld()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            // Raycast to a fixed Z plane (compatible with ortho/perspective)
            Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, placementPlaneZ));
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            // Fallback (should rarely happen)
            Vector3 mouse = Input.mousePosition;
            mouse.z = Mathf.Abs(cam.transform.position.z - placementPlaneZ);
            return cam.ScreenToWorldPoint(mouse);
        }
        
        /// <summary>
        /// FIXED: Check if position is within the full grid bounds including boundary gaps
        /// Now allows wall placement on outermost boundary gaps
        /// </summary>
        bool IsWithinGridBounds(Vector3 p)
        {
            // Calculate the full extent of valid gap positions including boundaries
            // For horizontal walls: valid X range is [0 to (Cells-1)*spacing] for gap centers
            // For vertical walls: valid Y range is [0 to (Cells-1)*spacing] for gap centers
            
            // Use a reasonable margin around the grid to catch boundary placements
            float gridMargin = spacing * 0.75f; // Allow some flexibility near boundaries
            
            // Full grid bounds including boundary gaps
            float minPos = -gridMargin; // Allow placements slightly before first tile
            float maxPos = (Cells - 1) * spacing + gridMargin; // Allow placements slightly after last tile
            
            bool inBounds = p.x >= minPos && p.x <= maxPos && p.y >= minPos && p.y <= maxPos;
            
            if (!inBounds)
            {
                UnityEngine.Debug.Log($"[BOUNDS] Position ({p.x:F2},{p.y:F2}) outside bounds [{minPos:F2},{maxPos:F2}]");
            }
            
            return inBounds;
        }
        #endregion

        #region Game Manager Integration
        public bool TryPlaceWall(Vector3 worldPosition)
        {
            if (!gameManager.CanInitiateWallPlacement()) return false;
            
            if (!gameManager.TryStartWallPlacement()) return false;
            
            if (TryFindNearestGap(worldPosition, out WallInfo info) && CanPlaceWall(info))
            {
                CreateWallVisual(info);
                OccupyWallPositions(info);
                OnWallPlaced(info);
                return true;
            }
            
            // Failed to place wall - complete with failure
            gameManager.CompleteWallPlacement(false);
            return false;
        }
        #endregion

        #region Public API
        public int GetManagedWallCount() => managedWalls.Count;
        
        public List<GameObject> GetManagedWalls() => new List<GameObject>(managedWalls);
        
        // Public method for testing - allows external scripts to set gap occupancy
        public void SetGapOccupiedForTesting(Orientation orientation, int x, int y, bool occupied)
        {
            SetOccupied(orientation, x, y, occupied);
        }
        
        public void ClearAllWalls()
        {
            // Clear visual walls
            for (int i = managedWalls.Count - 1; i >= 0; i--)
            {
                var go = managedWalls[i];
                if (go != null) SafeDestroy(go);
                managedWalls.RemoveAt(i);
            }
            
            // Clear gap arrays
            ClearGapArrays();
            
            // Reset wall counts
            if (gameManager != null)
            {
                gameManager.playerWallsRemaining = gameManager.wallsPerPlayer;
                gameManager.opponentWallsRemaining = gameManager.wallsPerPlayer;
            }
            
            // Sync with GridSystem
            if (gridSystem != null)
                gridSystem.ClearGrid();
                
            UnityEngine.Debug.Log("WallManager cleared all walls");
        }
        
        // Get wall info at specific coordinates
        public WallInfo? GetWallAt(Orientation orientation, int x, int y)
        {
            if (IsOccupied(orientation, x, y))
            {
                Vector3 center = GapCenter(orientation, x, y);
                return CreateWallInfo(orientation, x, y, center);
            }
            return null;
        }
        
        // Check if a specific gap position has a wall
        public bool HasWallAt(Orientation orientation, int x, int y)
        {
            return IsOccupied(orientation, x, y);
        }
        
        // Get all valid wall positions for current state
        public List<WallInfo> GetValidWallPositions()
        {
            List<WallInfo> validPositions = new List<WallInfo>();
            
            // Check all horizontal wall positions (corrected bounds to prevent hanging outside board)
            int maxHorizontalX = Mathf.Min(HCols - 2, Cells - 2); // Ensure x+1 stays within board
            for (int y = 0; y < HRows; y++)  // Only in gap rows between tiles
            {
                for (int x = 0; x <= maxHorizontalX; x++)  // Use corrected max bounds
                {
                    WallInfo hWall = CreateWallInfo(Orientation.Horizontal, x, y, GapCenter(Orientation.Horizontal, x, y));
                    if (CanPlaceWall(hWall))
                        validPositions.Add(hWall);
                }
            }
            
            // Check all vertical wall positions (corrected bounds to prevent hanging outside board)
            int maxVerticalY = Mathf.Min(VRows - 2, Cells - 2); // Ensure y+1 stays within board
            for (int x = 0; x < VCols; x++)  // Only in gap columns between tiles
            {
                for (int y = 0; y <= maxVerticalY; y++)  // Use corrected max bounds
                {
                    WallInfo vWall = CreateWallInfo(Orientation.Vertical, x, y, GapCenter(Orientation.Vertical, x, y));
                    if (CanPlaceWall(vWall))
                        validPositions.Add(vWall);
                }
            }
            
            return validPositions;
        }
        
        // Check if movement between two adjacent grid positions is blocked
        public bool IsMovementBlocked(Vector2Int from, Vector2Int to)
        {
            return IsPathBlocked(from, to);
        }
        #endregion

        #region Utility
        void SafeDestroy(Object obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(obj);
            else Destroy(obj);
#else
            Destroy(obj);
#endif
        }
        #endregion

        #region Debug & Gizmos
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (gameManager == null || Cells <= 0) return;

            // Draw CORRECTED board bounds - now includes boundary gaps  
            Gizmos.color = Color.green;
            float gridMargin = spacing * 0.75f;
            float minPos = -gridMargin;
            float maxPos = (Cells - 1) * spacing + gridMargin;
            Vector3 boundsMin = new Vector3(minPos, minPos, 0f);
            Vector3 boundsMax = new Vector3(maxPos, maxPos, 0f);
            Vector3 boundsSize = boundsMax - boundsMin;
            Gizmos.DrawWireCube(boundsMin + boundsSize * 0.5f, boundsSize);
            
            // Draw old restricted bounds for comparison (in red)
            Gizmos.color = Color.red;
            float minGapOld = 0.5f * spacing;
            float maxGapOld = (Cells - 1.5f) * spacing;
            Vector3 gapMinOld = new Vector3(minGapOld, minGapOld, 0f);
            Vector3 gapMaxOld = new Vector3(maxGapOld, maxGapOld, 0f);
            Vector3 gapSizeOld = gapMaxOld - gapMinOld;
            Gizmos.DrawWireCube(gapMinOld + gapSizeOld * 0.5f, gapSizeOld);
            
            // Draw tile area for reference
            Gizmos.color = Color.blue;
            Vector3 tileMin = new Vector3(0f, 0f, 0f);
            Vector3 tileMax = new Vector3((Cells - 1) * spacing, (Cells - 1) * spacing, 0f);
            Vector3 tileSize = tileMax - tileMin;
            Gizmos.DrawWireCube(tileMin + tileSize * 0.5f, tileSize);

            if (horizontalGaps == null || verticalGaps == null) return;

            // Draw gap centers and occupancy with corrected loop bounds
            int maxHorizontalX = Mathf.Min(HCols - 2, Cells - 2);
            int maxVerticalY = Mathf.Min(VRows - 2, Cells - 2);
            
            for (int y = 0; y < HRows; y++)  // Fixed: use HRows
            {
                for (int x = 0; x <= maxHorizontalX; x++)  // Use corrected bounds
                {
                    // Draw horizontal gaps if within valid range
                    if (x < HCols && y < HRows)
                    {
                        Vector3 cH = GapCenter(Orientation.Horizontal, x, y);
                        bool hOccupied = (x + 1 < HCols && horizontalGaps[x, y] && horizontalGaps[x + 1, y]);
                        Gizmos.color = hOccupied ? Color.red : Color.gray;
                        Gizmos.DrawCube(cH + Vector3.forward * -0.05f, new Vector3(0.05f, 0.05f, 0.01f));
                        
                        // Draw wall extent preview
                        if (!hOccupied)
                        {
                            Gizmos.color = Color.white;
                            float wallLength = gameManager.tileSize * 2f + gameManager.tileGap;
                            Gizmos.DrawWireCube(cH, new Vector3(wallLength, 0.02f, 0.02f));
                        }
                    }
                }
            }
            
            for (int x = 0; x < VCols; x++)  // Fixed: use VCols
            {
                for (int y = 0; y <= maxVerticalY; y++)  // Use corrected bounds
                {
                    // Draw vertical gaps if within valid range
                    if (x < VCols && y < VRows)
                    {
                        Vector3 cV = GapCenter(Orientation.Vertical, x, y);
                        bool vOccupied = (y + 1 < VRows && verticalGaps[x, y] && verticalGaps[x, y + 1]);
                        Gizmos.color = vOccupied ? Color.red : Color.gray;
                        Gizmos.DrawSphere(cV, 0.03f);
                        
                        // Draw wall extent preview
                        if (!vOccupied)
                        {
                            Gizmos.color = Color.white;
                            float wallLength = gameManager.tileSize * 2f + gameManager.tileGap;
                            Gizmos.DrawWireCube(cV, new Vector3(0.02f, wallLength, 0.02f));
                        }
                    }
                }
            }
            
            // Draw orientation lock indicator
            if (orientationLock.HasValue)
            {
                Gizmos.color = Color.cyan;
                Vector3 mousePos = GetMouseWorld();
                Gizmos.DrawWireSphere(mousePos, 0.1f);
                
#if UNITY_EDITOR
                string lockText = orientationLock.Value.ToString();
                UnityEditor.Handles.Label(mousePos + Vector3.up * 0.2f, $"Locked: {lockText}");
#endif
            }
        }
#endif
        #endregion
        
        void OnDestroy()
        {
            // Clean up preview if it exists
            if (wallPreview != null)
            {
                SafeDestroy(wallPreview);
            }
            
            // Clean up managed walls
            ClearAllWalls();
        }
    }
}