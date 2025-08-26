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
        
        #region Unity Lifecycle
        void Update()
        {
            // Handle wall placement input
            if (Input.GetMouseButtonDown(0) && CanPlaceWalls())
            {
                TryStartPlacing();
            }
            else if (Input.GetMouseButton(0) && isPlacing)
            {
                UpdatePreview();
            }
            else if (Input.GetMouseButtonUp(0) && isPlacing)
            {
                PlaceWall();
            }
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
            if (wallPreview != null && wallPreview.activeInHierarchy)
            {
                Vector3 mousePos = GetMouseWorld();
                if (TryFindNearestGap(mousePos, out WallInfo info) && CanPlaceWall(info))
                {
                    // Create actual wall
                    CreateWallVisual(info);
                    
                    // Mark gaps as occupied
                    OccupyWallPositions(info);
                    
                    // Notify game manager
                    OnWallPlaced(info);
                    
                    // Decrement wall count and end turn
                    DecrementWallCount();
                    gameManager.EndTurn();
                    
                    UnityEngine.Debug.Log($"Wall placed at ({info.x},{info.y}) {info.orientation}! Player walls remaining: {GetCurrentPlayerWallCount()}");
                }
                
                // Clean up preview
                SafeDestroy(wallPreview);
                wallPreview = null;
                wallPreviewRenderer = null;
            }
            
            orientationLock = null; // reset after placement attempt
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
                // Horizontal walls: x spans 2 gaps, so x can be [0..HCols-2] to ensure x+1 is valid
                // y must be within the gap rows between tiles [0..HRows-1]
                x = Mathf.Clamp(x, 0, HCols - 2);  
                y = Mathf.Clamp(y, 0, HRows - 1);
                return x >= 0 && x + 1 < HCols && y >= 0 && y < HRows;
            }
            else
            {
                // Vertical walls: y spans 2 gaps, so y can be [0..VRows-2] to ensure y+1 is valid
                // x must be within the gap columns between tiles [0..VCols-1]
                x = Mathf.Clamp(x, 0, VCols - 1);
                y = Mathf.Clamp(y, 0, VRows - 2);
                return x >= 0 && x < VCols && y >= 0 && y + 1 < VRows;
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
            // Check if current player has walls remaining
            if (!CanPlaceWalls()) return false;
            
            // Bounds and occupancy checks
            if (w.orientation == Orientation.Horizontal)
            {
                if (w.x < 0 || w.x + 1 >= HCols || w.y < 0 || w.y >= HRows) return false;
                if (IsOccupied(Orientation.Horizontal, w.x, w.y)) return false;
                if (IsOccupied(Orientation.Horizontal, w.x + 1, w.y)) return false;
            }
            else
            {
                if (w.x < 0 || w.x >= VCols || w.y < 0 || w.y + 1 >= VRows) return false;
                if (IsOccupied(Orientation.Vertical, w.x, w.y)) return false;
                if (IsOccupied(Orientation.Vertical, w.x, w.y + 1)) return false;
            }

            // Check crossing prevention
            return !WouldCrossExistingWall(w) && !WouldBlockPlayerPaths(w);
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
            int goalY = isPlayer ? 0 : gameManager.gridSize - 1;
            
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
                    
                    // Check if path is blocked by wall
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
            
            // Determine which gap to check based on movement direction
            if (diff.y == 1) // Moving up
            {
                // Check horizontal gap above 'from'
                int gapX = from.x;
                int gapY = from.y;
                return gapX < HCols && gapY < HRows && 
                       horizontalGaps[gapX, gapY] && 
                       (gapX + 1 >= HCols || horizontalGaps[gapX + 1, gapY]);
            }
            else if (diff.y == -1) // Moving down
            {
                // Check horizontal gap below 'to'
                int gapX = to.x;
                int gapY = to.y;
                return gapX < HCols && gapY < HRows &&
                       horizontalGaps[gapX, gapY] && 
                       (gapX + 1 >= HCols || horizontalGaps[gapX + 1, gapY]);
            }
            else if (diff.x == 1) // Moving right
            {
                // Check vertical gap to the right of 'from'
                int gapX = from.x;
                int gapY = from.y;
                return gapX < VCols && gapY < VRows &&
                       verticalGaps[gapX, gapY] && 
                       (gapY + 1 >= VRows || verticalGaps[gapX, gapY + 1]);
            }
            else if (diff.x == -1) // Moving left
            {
                // Check vertical gap to the left of 'to'
                int gapX = to.x;
                int gapY = to.y;
                return gapX < VCols && gapY < VRows &&
                       verticalGaps[gapX, gapY] && 
                       (gapY + 1 >= VRows || verticalGaps[gapX, gapY + 1]);
            }
            
            return false; // Invalid movement
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

        public bool IsGapOccupied(string gapKey)
        {
            string[] parts = gapKey.Split('_');
            Orientation o = (parts[0] == "H") ? Orientation.Horizontal : Orientation.Vertical;
            int gx = int.Parse(parts[1]);
            int gy = int.Parse(parts[2]);
            return IsGapOccupied(o, gx, gy);
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
        
        bool IsWithinGridBounds(Vector3 p)
        {
            // Walls can only be placed in gaps between tiles
            // For a 9x9 tile grid, gaps are between positions 0.5 to 7.5 (8 gap positions)
            float minGapPos = 0.5f * spacing;  // First gap between tiles 0 and 1
            float maxGapPos = (Cells - 1.5f) * spacing;  // Last gap between tiles 7 and 8
            
            return p.x >= minGapPos && p.x <= maxGapPos && p.y >= minGapPos && p.y <= maxGapPos;
        }
        #endregion

        #region Game Manager Integration
        bool CanPlaceWalls()
        {
            return gameManager != null && 
                   ((gameManager.currentState == GameState.PlayerTurn && gameManager.playerWallsRemaining > 0) ||
                    (gameManager.currentState == GameState.OpponentTurn && gameManager.opponentWallsRemaining > 0));
        }
        
        void DecrementWallCount()
        {
            if (gameManager.currentState == GameState.PlayerTurn)
                gameManager.playerWallsRemaining--;
            else if (gameManager.currentState == GameState.OpponentTurn)
                gameManager.opponentWallsRemaining--;
        }
        
        int GetCurrentPlayerWallCount()
        {
            return gameManager.currentState == GameState.PlayerTurn ? 
                   gameManager.playerWallsRemaining : gameManager.opponentWallsRemaining;
        }
        #endregion

        #region Public API
        public int GetManagedWallCount() => managedWalls.Count;
        
        public List<GameObject> GetManagedWalls() => new List<GameObject>(managedWalls);
        
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
        
        public bool TryPlaceWall(Vector3 worldPosition)
        {
            if (!CanPlaceWalls()) return false;
            
            if (TryFindNearestGap(worldPosition, out WallInfo info) && CanPlaceWall(info))
            {
                CreateWallVisual(info);
                OccupyWallPositions(info);
                OnWallPlaced(info);
                DecrementWallCount();
                gameManager.EndTurn();
                return true;
            }
            
            return false;
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
            
            // Check all horizontal wall positions (corrected bounds)
            for (int y = 0; y < HRows; y++)  // Only in gap rows between tiles
            {
                for (int x = 0; x < HCols - 1; x++)  // Ensure wall doesn't extend outside
                {
                    WallInfo hWall = CreateWallInfo(Orientation.Horizontal, x, y, GapCenter(Orientation.Horizontal, x, y));
                    if (CanPlaceWall(hWall))
                        validPositions.Add(hWall);
                }
            }
            
            // Check all vertical wall positions (corrected bounds)
            for (int x = 0; x < VCols; x++)  // Only in gap columns between tiles
            {
                for (int y = 0; y < VRows - 1; y++)  // Ensure wall doesn't extend outside
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

            // Draw corrected board bounds (gap placement area only)
            Gizmos.color = Color.white;
            float minGap = 0.5f * spacing;
            float maxGap = (Cells - 1.5f) * spacing;
            Vector3 gapMin = new Vector3(minGap, minGap, 0f);
            Vector3 gapMax = new Vector3(maxGap, maxGap, 0f);
            Vector3 gapSize = gapMax - gapMin;
            Gizmos.DrawWireCube(gapMin + gapSize * 0.5f, gapSize);
            
            // Draw tile area for reference
            Gizmos.color = Color.blue;
            Vector3 tileMin = new Vector3(0f, 0f, 0f);
            Vector3 tileMax = new Vector3((Cells - 1) * spacing, (Cells - 1) * spacing, 0f);
            Vector3 tileSize = tileMax - tileMin;
            Gizmos.DrawWireCube(tileMin + tileSize * 0.5f, tileSize);

            if (horizontalGaps == null || verticalGaps == null) return;

            // Draw gap centers and occupancy with corrected loop bounds
            for (int y = 0; y < HRows; y++)  // Fixed: use HRows instead of Cells
            {
                for (int x = 0; x < HCols - 1; x++)  // Fixed: use HCols-1 to avoid out-of-bounds
                {
                    // Draw horizontal gaps if within valid range
                    if (x < HCols && y < HRows)
                    {
                        Vector3 cH = GapCenter(Orientation.Horizontal, x, y);
                        bool hOccupied = (x + 1 < HCols && horizontalGaps[x, y] && horizontalGaps[x + 1, y]);
                        Gizmos.color = hOccupied ? Color.red : Color.gray;
                        Gizmos.DrawCube(cH + Vector3.forward * -0.05f, new Vector3(0.05f, 0.05f, 0.01f));
                    }
                }
            }
            
            for (int y = 0; y < VRows - 1; y++)  // Fixed: use VRows-1 to avoid out-of-bounds
            {
                for (int x = 0; x < VCols; x++)  // Fixed: use VCols instead of Cells
                {
                    // Draw vertical gaps if within valid range
                    if (x < VCols && y < VRows)
                    {
                        Vector3 cV = GapCenter(Orientation.Vertical, x, y);
                        bool vOccupied = (y + 1 < VRows && verticalGaps[x, y] && verticalGaps[x, y + 1]);
                        Gizmos.color = vOccupied ? Color.red : Color.gray;
                        Gizmos.DrawSphere(cV, 0.03f);
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