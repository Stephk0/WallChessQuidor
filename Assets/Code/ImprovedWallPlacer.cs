using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

public class ImprovedWallPlacer : MonoBehaviour
{
    #region Grid configuration (single source of truth)
    public const int Cells = 8;                              // number of cells per side
    private const int HCols = Cells + 1;                     // horizontal gap columns (x)
    private const int HRows = Cells;                         // horizontal gap rows (y)
    private const int VCols = Cells;                         // vertical gap columns (x)
    private const int VRows = Cells + 1;                     // vertical gap rows (y)
    #endregion

    #region Types
    public enum Orientation { Horizontal, Vertical }

    public readonly struct WallInfo
    {
        public readonly Orientation orientation;
        public readonly int x;        // grid index [0..Cells-1]
        public readonly int y;        // grid index [0..Cells-1]
        public readonly Vector3 position;
        public readonly Vector3 scale;

        public WallInfo(Orientation o, int x, int y, Vector3 pos, Vector3 scale)
        {
            this.orientation = o;
            this.x = x;
            this.y = y;
            this.position = pos;
            this.scale = scale;
        }
    }
    #endregion

    #region State
    private GameObject wallPreview;
    private Renderer wallPreviewRenderer;     // (5) cache the renderer once
    private bool isPlacing = false;
    public int wallsLeft = 9;

    // (7) Track spawned walls instead of scene-wide tag scans
    private readonly List<GameObject> placedWalls = new List<GameObject>(32);

    // Efficient gap tracking with boolean arrays (pair-aware indices)
    // horizontalGaps: [x, y] with size [HCols, HRows] == [Cells+1, Cells]
    // verticalGaps:   [x, y] with size [VCols, VRows] == [Cells, Cells+1]
    private readonly bool[,] horizontalGaps = new bool[HCols, HRows];
    private readonly bool[,] verticalGaps   = new bool[VCols, VRows];
    #endregion

    #region Inspector settings
    [Header("Grid Settings")]
    public float spacing = 1.2f;
    public float wallThickness = 0.15f;
    public float wallLength = 2.4f;
    public float wallHeight = 1f;

    [Header("Gap Detection Settings")]
    public float gapSnapMargin = 0.25f;   // safe margin around gaps to stabilize orientation

    [Header("Gap Detection (Safe Margins)")]
    [Tooltip("World-units half-width of snap stripes around horizontal (Y) and vertical (X) gap lanes.")]
    public float laneSnapMargin = 0.3f;

    [Tooltip("How much farther you must move out of the locked lane to unlock orientation (hysteresis).")]
    public float unlockMultiplier = 1.5f;

    // Tracks the last chosen orientation while the cursor stays in its lane stripe.
    private Orientation? orientationLock = null;

    [Header("Gap Grid Offsets")]
    public float horizontalGapOffsetX = 0.5f;   // center offset in grid units
    public float horizontalGapOffsetY = 0.0f;   // FIXED: horizontal gaps are between rows
    public float verticalGapOffsetX = 0.0f;     // FIXED: vertical gaps are between columns
    public float verticalGapOffsetY = 0.5f;

    [Header("Input & Camera")]
    public LayerMask placementMask = ~0;        // optional; used by plane Raycast fallback
    public float placementPlaneZ = 0f;          // Z of placement plane for ScreenPointToRay
    #endregion

    #region Unity lifecycle
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && wallsLeft > 0)
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

    #region Placement flow
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
            wallPreviewRenderer = wallPreview.GetComponent<Renderer>(); // cache once
            wallPreviewRenderer.material.color = new Color(1, 1, 0, 0.5f);
            var collider = wallPreview.GetComponent<Collider>();
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(collider);
            else Destroy(collider);
#else
            Destroy(collider);
#endif
        }
    }

    void UpdatePreview()
    {
        if (wallPreview == null) return;

        Vector3 mousePos = GetMouseWorld();
        if (!IsWithinGridBounds(mousePos)) { wallPreview.SetActive(false); return; }

        // (2) Index-first candidate computation — no distance scans
        bool hasCandidate = TryFindNearestGap(mousePos, out WallInfo info);
        if (!hasCandidate)
        {
            wallPreview.SetActive(false);
            return;
        }

        wallPreview.SetActive(true);
        wallPreview.transform.position = info.position;
        wallPreview.transform.localScale = info.scale;

        // tint based on validity
        bool canPlace = CanPlaceWall(info);
        wallPreviewRenderer.material.color = canPlace ? new Color(0, 1, 0, 0.7f)
                                                      : new Color(1, 0, 0, 0.7f);
    }

    void PlaceWall()
    {
        if (wallPreview != null && wallPreview.activeInHierarchy)
        {
            Vector3 mousePos = GetMouseWorld();
            if (TryFindNearestGap(mousePos, out WallInfo info) && CanPlaceWall(info))
            {
                wallPreviewRenderer.material.color = Color.yellow;
                wallPreview.name = "Wall";
                wallPreview.tag = "Wall";

                // Mark gaps as occupied
                OccupyWallPositions(info);

                // Track spawned wall
                placedWalls.Add(wallPreview);

                wallsLeft--;
                UnityEngine.Debug.Log($"Wall placed at ({info.x},{info.y}) {info.orientation}! Remaining: {wallsLeft}");

                wallPreview = null;
                wallPreviewRenderer = null;
            }
            else
            {
                SafeDestroy(wallPreview);
                wallPreview = null;
                wallPreviewRenderer = null;
            }
        }
        else if (wallPreview != null)
        {
            SafeDestroy(wallPreview);
            wallPreview = null;
            wallPreviewRenderer = null;
        }
        orientationLock = null; // reset after a placement attempt finishes

        isPlacing = false;
    }
    #endregion

    #region Core logic
    // (3) Centralized bounds derived from Cells
    bool IsWithinGridBounds(Vector3 p)
    {
        float gridMin = 0f - spacing * 0.5f;
        float gridMax = Cells * spacing + spacing * 0.5f;
        return p.x >= gridMin && p.x <= gridMax && p.y >= gridMin && p.y <= gridMax;
    }
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

    // (2) compute nearest candidate by direct index math; test two candidates per orientation
    bool TryFindNearestGap(in Vector3 mousePos, out WallInfo result)
    {
        // Lane distances (stripe logic)
        float yLane = NearestHorizontalLaneY(mousePos.y);
        float xLane = NearestVerticalLaneX(mousePos.x);
        float dY = Mathf.Abs(mousePos.y - yLane);   // distance to horizontal lane (controls Horizontal orientation)
        float dX = Mathf.Abs(mousePos.x - xLane);   // distance to vertical lane (controls Vertical orientation)

        bool inHStripe = dY <= laneSnapMargin;
        bool inVStripe = dX <= laneSnapMargin;

        // Hysteresis: if we’re locked to an orientation, keep it until we leave a wider stripe
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
            // If neither stripe is hit, allow both so we can still find a nearest center anywhere on the board.
            if (!inHStripe && !inVStripe) { inHStripe = true; inVStripe = true; }
            // If both stripes are hit (cross area), we’ll resolve by distance below and then lock.
        }

        // ----- ORIGINAL candidate math (unchanged) -----
        // Horizontal candidates
        int hx0, hy0;  // floor
        int hx1, hy1;  // round
        MapToGapIndices(mousePos, Orientation.Horizontal, out hx0, out hy0, floor: true);
        MapToGapIndices(mousePos, Orientation.Horizontal, out hx1, out hy1, floor: false);

        // Vertical candidates
        int vx0, vy0;  // floor
        int vx1, vy1;  // round
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
            // If we selected while outside both narrow stripes (far away), don’t lock.
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
            // Don’t switch orientation if both candidates are nearly tied
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
            // For horizontal walls: x can be [0..Cells-1], y can be [0..Cells-1]
            // But we need to check x+1 stays within HCols
            x = Mathf.Clamp(x, 0, Cells - 1);
            y = Mathf.Clamp(y, 0, Cells - 1);
            return x + 1 < HCols && y >= 0 && y < HRows;
        }
        else
        {
            // For vertical walls: x can be [0..Cells-1], y can be [0..Cells-1] 
            // But we need to check y+1 stays within VRows
            x = Mathf.Clamp(x, 0, Cells - 1);
            y = Mathf.Clamp(y, 0, Cells - 1);
            return x >= 0 && x < VCols && y + 1 < VRows;
        }
    }

    WallInfo CreateWallInfo(Orientation o, int x, int y, in Vector3 center)
    {
        Vector3 scale = (o == Orientation.Horizontal)
            ? new Vector3(wallLength, wallThickness, wallHeight)
            : new Vector3(wallThickness, wallLength, wallHeight);

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

    // (1) CanPlace uses index/pair-aware crossing and no distance loops
    bool CanPlaceWall(in WallInfo w)
    {
        // (4) explicit guards for +1 access
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

        return !WouldCrossExistingWall(w);
    }

    // (1) Pair-aware crossing detection (no scans)
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

    // (11) Consolidated helpers
    bool IsOccupied(Orientation o, int x, int y)
    {
        return (o == Orientation.Horizontal) ? horizontalGaps[x, y]
                                             : verticalGaps[x, y];
    }

    void SetOccupied(Orientation o, int x, int y, bool value)
    {
        if (o == Orientation.Horizontal) horizontalGaps[x, y] = value;
        else verticalGaps[x, y] = value;
    }

    void OccupyWallPositions(in WallInfo w)
    {
        // (4) extra safety even though CanPlace guards above
        if (w.orientation == Orientation.Horizontal)
        {
            System.Diagnostics.Debug.Assert(w.x + 1 < HCols && w.y < HRows, "Horizontal occupy out of range");
            SetOccupied(Orientation.Horizontal, w.x,     w.y, true);
            SetOccupied(Orientation.Horizontal, w.x + 1, w.y, true);
        }
        else
        {
            System.Diagnostics.Debug.Assert(w.y + 1 < VRows && w.x < VCols, "Vertical occupy out of range");
            SetOccupied(Orientation.Vertical, w.x, w.y,     true);
            SetOccupied(Orientation.Vertical, w.x, w.y + 1, true);
        }
    }
    #endregion

    #region Input & Camera (13)
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
    #endregion

    #region Public API & utilities
    public void ClearWalls()
    {
        // Clear arrays
        for (int i = 0; i < HCols; i++)
            for (int j = 0; j < HRows; j++)
                horizontalGaps[i, j] = false;

        for (int i = 0; i < VCols; i++)
            for (int j = 0; j < VRows; j++)
                verticalGaps[i, j] = false;

        wallsLeft = 9;

        // Destroy tracked walls without global tag search
        for (int i = placedWalls.Count - 1; i >= 0; i--)
        {
            var go = placedWalls[i];
            if (go != null) SafeDestroy(go);
            placedWalls.RemoveAt(i);
        }
    }

    // (9) Strongly typed occupancy query; (legacy string kept as thin adapter)
    public bool IsGapOccupied(Orientation o, int x, int y)
    {
        return IsOccupied(o, x, y);
    }

    // Legacy: accepts "H_1_2" or "V_3_4"
    public bool IsGapOccupied(string gapKey)
    {
        string[] parts = gapKey.Split('_');
        Orientation o = (parts[0] == "H") ? Orientation.Horizontal : Orientation.Vertical;
        int gx = int.Parse(parts[1]);
        int gy = int.Parse(parts[2]);
        return IsGapOccupied(o, gx, gy);
    }

    void SafeDestroy(Object obj)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(obj);   // (6) Editor-only immediate destroy
        else Destroy(obj);
#else
        Destroy(obj);                                        // runtime-safe destroy
#endif
    }
    #endregion

    #region Gizmos (12)
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw board bounds
        Gizmos.color = Color.white;
        Vector3 min = new Vector3(-spacing * 0.5f, -spacing * 0.5f, 0f);
        Vector3 max = new Vector3(Cells * spacing + spacing * 0.5f,
                                  Cells * spacing + spacing * 0.5f, 0f);
        Vector3 size = max - min;
        Gizmos.DrawWireCube(min + size * 0.5f, size);

        // Draw gap centers and occupancy
        // Horizontal centers
        for (int y = 0; y < Cells; y++)
        {
            for (int x = 0; x < Cells; x++)
            {
                Vector3 cH = GapCenter(Orientation.Horizontal, x, y);
                Vector3 cV = GapCenter(Orientation.Vertical,   x, y);

                Gizmos.color = (verticalGaps[x, y] && y + 1 < VRows && verticalGaps[x, y + 1]) ? Color.red : Color.gray;
                Gizmos.DrawSphere(cV, 0.03f);

                Gizmos.color = (horizontalGaps[x, y] && x + 1 < HCols && horizontalGaps[x + 1, y]) ? Color.red : Color.gray;
                Gizmos.DrawCube(cH + Vector3.forward * -0.05f, new Vector3(0.05f, 0.05f, 0.01f));
            }
        }
    }
#endif
    #endregion
}
