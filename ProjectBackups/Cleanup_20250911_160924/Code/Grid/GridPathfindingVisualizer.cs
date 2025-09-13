using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace WallChess.Grid
{
    /// <summary>
    /// Visualizes pathfinding by placing colored spheres on board tiles
    /// Green = tiles in valid path to goal
    /// Red = inaccessible tiles (no path to goal)
    /// Magenta = last known accessible tiles before path was blocked
    /// Orange = possible endpoints for pawns (goal row tiles)
    /// </summary>
    public class GridPathfindingVisualizer : MonoBehaviour
    {
        [System.Serializable]
        public struct PathfindingColors
        {
            public Color validPathColor;
            public Color inaccessibleColor;
            public Color lastAccessibleColor;
            public Color neutralColor;
            public Color endpointColor;

            public static PathfindingColors Default => new PathfindingColors
            {
                validPathColor = Color.green,
                inaccessibleColor = Color.red,
                lastAccessibleColor = Color.magenta,
                neutralColor = Color.white,
                endpointColor = new Color(1f, 0.5f, 0f, 1f) // Orange
            };
        }

        [System.Serializable]
        public struct VisualizerSettings
        {
            public float sphereSize;
            public float sphereHeight;
            public bool useTransparentMaterial;
            public float transparency;
            public bool highlightEndpoints;

            public static VisualizerSettings Default => new VisualizerSettings
            {
                sphereSize = 0.3f,
                sphereHeight = 0.1f,
                useTransparentMaterial = true,
                transparency = 0.7f,
                highlightEndpoints = true
            };
        }

        public enum DebugMode
        {
            Off,
            Pawn1Only,
            Pawn2Only,
            BothPawns
        }

        [Header("Debug Settings")]
        [SerializeField] private DebugMode debugMode = DebugMode.Off;
        [SerializeField] private bool enableRealTimeUpdates = false;
        [Tooltip("Update visualization every frame (expensive)")]
        
        [Header("Visual Settings")]
        [SerializeField] private PathfindingColors colors = PathfindingColors.Default;
        [SerializeField] private VisualizerSettings settings = VisualizerSettings.Default;
        
        [Header("Materials")]
        [SerializeField] private Material sphereMaterial;
        [SerializeField] private Material transparentSphereMaterial;

        private GridSystem gridSystem;
        private WallChessGameManager gameManager;
        private Dictionary<Vector2Int, GameObject> debugSpheres = new Dictionary<Vector2Int, GameObject>();
        private Dictionary<Vector2Int, Color> lastKnownAccessibleColors = new Dictionary<Vector2Int, Color>();
        private bool isInitialized = false;

        public void Initialize(GridSystem grid, WallChessGameManager gm)
        {
            gridSystem = grid;
            gameManager = gm;
            
            CreateDebugMaterials();
            CreateAllDebugSpheres();
            
            isInitialized = true;
            UpdateVisualization();
            
            Debug.Log($"GridPathfindingVisualizer initialized with {debugSpheres.Count} spheres");
        }

        private void CreateDebugMaterials()
        {
            if (sphereMaterial == null)
            {
                sphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                sphereMaterial.name = "PathfindingSphere";
            }

            if (transparentSphereMaterial == null && settings.useTransparentMaterial)
            {
                transparentSphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                transparentSphereMaterial.name = "PathfindingSphereTransparent";
                transparentSphereMaterial.SetFloat("_Mode", 3); // Transparent mode
                transparentSphereMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                transparentSphereMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                transparentSphereMaterial.SetInt("_ZWrite", 0);
                transparentSphereMaterial.DisableKeyword("_ALPHATEST_ON");
                transparentSphereMaterial.EnableKeyword("_ALPHABLEND_ON");
                transparentSphereMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                transparentSphereMaterial.renderQueue = 3000;
            }
        }

        private void CreateAllDebugSpheres()
        {
            if (gridSystem == null) return;

            int gridSize = gridSystem.GetGridSize();
            
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector2Int tilePos = new Vector2Int(x, y);
                    CreateDebugSphere(tilePos);
                }
            }
        }

        private void CreateDebugSphere(Vector2Int tilePos)
        {
            if (debugSpheres.ContainsKey(tilePos)) return;

            Vector3 worldPos = gridSystem.GridToWorldPosition(tilePos);
            worldPos.z = settings.sphereHeight;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"PathDebugSphere_{tilePos.x}_{tilePos.y}";
            sphere.transform.position = worldPos;
            sphere.transform.localScale = Vector3.one * settings.sphereSize;
            sphere.transform.parent = transform;

            // Remove collider to avoid interference
            if (sphere.GetComponent<Collider>() != null)
                DestroyImmediate(sphere.GetComponent<Collider>());

            // Apply material
            Renderer renderer = sphere.GetComponent<Renderer>();
            Material activeMaterial = settings.useTransparentMaterial ? transparentSphereMaterial : sphereMaterial;
            renderer.material = activeMaterial;

            // Initially set to neutral color and hide
            SetSphereColor(sphere, colors.neutralColor);
            sphere.SetActive(debugMode != DebugMode.Off);

            debugSpheres[tilePos] = sphere;
        }

        private void SetSphereColor(GameObject sphere, Color color)
        {
            if (sphere == null) return;

            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = renderer.material;
                material.color = new Color(color.r, color.g, color.b, 
                    settings.useTransparentMaterial ? settings.transparency : color.a);
            }
        }

        public void UpdateVisualization()
        {
            if (!isInitialized || gridSystem == null || gameManager == null || debugMode == DebugMode.Off)
            {
                HideAllSpheres();
                return;
            }

            ShowAllSpheres();
            
            switch (debugMode)
            {
                case DebugMode.Pawn1Only:
                    if (gameManager.pawns.Count > 0)
                        UpdateVisualizationForPawn(gameManager.pawns[0]);
                    break;
                    
                case DebugMode.Pawn2Only:
                    if (gameManager.pawns.Count > 1)
                        UpdateVisualizationForPawn(gameManager.pawns[1]);
                    break;
                    
                case DebugMode.BothPawns:
                    UpdateVisualizationForBothPawns();
                    break;
            }
        }

        private void UpdateVisualizationForPawn(WallChessGameManager.PawnData pawnData)
        {
            if (pawnData == null) return;

            Vector2Int pawnPos = pawnData.position;
            Vector2Int goalRow = GetGoalRowForPawn(pawnData);
            
            // Reset all spheres to neutral
            ResetAllSpheres();
            
            // Highlight endpoints first if enabled
            if (settings.highlightEndpoints)
            {
                HighlightEndpointsForPawn(pawnData);
            }
            
            // Color all tiles based on pathfinding results
            int gridSize = gridSystem.GetGridSize();
            
            for (int x = 0; x < gridSize; x++)
            {
                // Check each tile in the goal row
                Vector2Int goalPos = new Vector2Int(x, goalRow.y);
                List<Vector2Int> path = GridPathfinder.FindPath(gridSystem, pawnPos, goalPos);
                
                if (path != null && path.Count > 0)
                {
                    // Valid path found - color all tiles in this path as green (except endpoints)
                    foreach (Vector2Int pathTile in path)
                    {
                        if (debugSpheres.ContainsKey(pathTile))
                        {
                            // Don't override endpoint color if it's already set
                            if (!IsEndpointTile(pathTile, pawnData) || !settings.highlightEndpoints)
                            {
                                SetSphereColor(debugSpheres[pathTile], colors.validPathColor);
                            }
                        }
                    }
                    
                    // Found a valid path, so we don't need to check inaccessible tiles for this goal
                    return;
                }
            }
            
            // Handle inaccessible tiles
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector2Int tilePos = new Vector2Int(x, y);
                    if (!debugSpheres.ContainsKey(tilePos)) continue;
                    
                    // Skip if already colored (path or endpoint)
                    if (HasValidPathToGoal(pawnPos, tilePos, goalRow.y) || 
                        (IsEndpointTile(tilePos, pawnData) && settings.highlightEndpoints))
                        continue;
                    
                    // Check if tile is reachable from current pawn position
                    List<Vector2Int> pathToTile = GridPathfinder.FindPath(gridSystem, pawnPos, tilePos);
                    
                    if (pathToTile == null || pathToTile.Count == 0)
                    {
                        // Tile is completely inaccessible
                        if (lastKnownAccessibleColors.ContainsKey(tilePos))
                        {
                            // This tile was accessible before, mark as last accessible
                            SetSphereColor(debugSpheres[tilePos], colors.lastAccessibleColor);
                        }
                        else
                        {
                            // This tile was never accessible, mark as inaccessible
                            SetSphereColor(debugSpheres[tilePos], colors.inaccessibleColor);
                        }
                    }
                    else
                    {
                        // Tile is accessible - store this information
                        lastKnownAccessibleColors[tilePos] = colors.validPathColor;
                    }
                }
            }
        }

        private void UpdateVisualizationForBothPawns()
        {
            if (gameManager.pawns.Count < 2) return;
            
            // Reset all spheres
            ResetAllSpheres();
            
            // Get both pawns
            var pawn1 = gameManager.pawns[0];
            var pawn2 = gameManager.pawns[1];
            
            Vector2Int pawn1Pos = pawn1.position;
            Vector2Int pawn2Pos = pawn2.position;
            Vector2Int goal1 = GetGoalRowForPawn(pawn1);
            Vector2Int goal2 = GetGoalRowForPawn(pawn2);
            
            // Highlight endpoints for both pawns if enabled
            if (settings.highlightEndpoints)
            {
                HighlightEndpointsForPawn(pawn1);
                HighlightEndpointsForPawn(pawn2);
            }
            
            int gridSize = gridSystem.GetGridSize();
            
            // Check pathfinding for both pawns and blend colors
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector2Int tilePos = new Vector2Int(x, y);
                    if (!debugSpheres.ContainsKey(tilePos)) continue;
                    
                    // Skip endpoints if already colored
                    if ((IsEndpointTile(tilePos, pawn1) || IsEndpointTile(tilePos, pawn2)) && settings.highlightEndpoints)
                        continue;
                    
                    bool pawn1CanReach = false;
                    bool pawn2CanReach = false;
                    
                    // Check if pawn1 can reach any goal
                    for (int goalX = 0; goalX < gridSize; goalX++)
                    {
                        Vector2Int goalPos = new Vector2Int(goalX, goal1.y);
                        var path = GridPathfinder.FindPath(gridSystem, pawn1Pos, goalPos);
                        if (path != null && path.Contains(tilePos))
                        {
                            pawn1CanReach = true;
                            break;
                        }
                    }
                    
                    // Check if pawn2 can reach any goal
                    for (int goalX = 0; goalX < gridSize; goalX++)
                    {
                        Vector2Int goalPos = new Vector2Int(goalX, goal2.y);
                        var path = GridPathfinder.FindPath(gridSystem, pawn2Pos, goalPos);
                        if (path != null && path.Contains(tilePos))
                        {
                            pawn2CanReach = true;
                            break;
                        }
                    }
                    
                    // Color based on reachability
                    Color finalColor;
                    if (pawn1CanReach && pawn2CanReach)
                    {
                        // Both can use this tile - blend green with cyan
                        finalColor = Color.Lerp(colors.validPathColor, Color.cyan, 0.5f);
                    }
                    else if (pawn1CanReach)
                    {
                        finalColor = colors.validPathColor;
                    }
                    else if (pawn2CanReach)
                    {
                        finalColor = Color.blue; // Different shade for pawn2
                    }
                    else
                    {
                        finalColor = colors.inaccessibleColor;
                    }
                    
                    SetSphereColor(debugSpheres[tilePos], finalColor);
                }
            }
        }

        private void HighlightEndpointsForPawn(WallChessGameManager.PawnData pawnData)
        {
            if (pawnData == null) return;
            
            Vector2Int goalRow = GetGoalRowForPawn(pawnData);
            int gridSize = gridSystem.GetGridSize();
            
            // Color all tiles in the goal row as endpoints
            for (int x = 0; x < gridSize; x++)
            {
                Vector2Int endpointPos = new Vector2Int(x, goalRow.y);
                if (debugSpheres.ContainsKey(endpointPos))
                {
                    SetSphereColor(debugSpheres[endpointPos], colors.endpointColor);
                }
            }
        }

        private bool IsEndpointTile(Vector2Int tilePos, WallChessGameManager.PawnData pawnData)
        {
            if (pawnData == null) return false;
            
            Vector2Int goalRow = GetGoalRowForPawn(pawnData);
            return tilePos.y == goalRow.y;
        }

        private bool HasValidPathToGoal(Vector2Int startPos, Vector2Int currentTile, int goalRowY)
        {
            int gridSize = gridSystem.GetGridSize();
            
            for (int x = 0; x < gridSize; x++)
            {
                Vector2Int goalPos = new Vector2Int(x, goalRowY);
                var path = GridPathfinder.FindPath(gridSystem, startPos, goalPos);
                if (path != null && path.Contains(currentTile))
                {
                    return true;
                }
            }
            return false;
        }

        private Vector2Int GetGoalRowForPawn(WallChessGameManager.PawnData pawnData)
        {
            // Pawn 1 starts at bottom (y=0), goals at top (y=gridSize-1)
            // Pawn 2 starts at top (y=gridSize-1), goals at bottom (y=0)
            int gridSize = gridSystem.GetGridSize();
            
            if (pawnData.position.y < gridSize / 2)
            {
                return new Vector2Int(0, gridSize - 1); // Goal is at the top
            }
            else
            {
                return new Vector2Int(0, 0); // Goal is at the bottom
            }
        }

        private void ResetAllSpheres()
        {
            foreach (var kvp in debugSpheres)
            {
                SetSphereColor(kvp.Value, colors.neutralColor);
            }
        }

        private void ShowAllSpheres()
        {
            foreach (var sphere in debugSpheres.Values)
            {
                if (sphere != null)
                    sphere.SetActive(true);
            }
        }

        private void HideAllSpheres()
        {
            foreach (var sphere in debugSpheres.Values)
            {
                if (sphere != null)
                    sphere.SetActive(false);
            }
        }

        public void SetDebugMode(DebugMode mode)
        {
            debugMode = mode;
            UpdateVisualization();
        }

        public void ToggleDebugMode()
        {
            debugMode = (DebugMode)(((int)debugMode + 1) % System.Enum.GetValues(typeof(DebugMode)).Length);
            UpdateVisualization();
            Debug.Log($"Pathfinding debug mode: {debugMode}");
        }

        public void RefreshVisualization()
        {
            // Clear last known accessible data to get fresh pathfinding results
            lastKnownAccessibleColors.Clear();
            UpdateVisualization();
        }

        public void ToggleEndpointHighlighting()
        {
            settings.highlightEndpoints = !settings.highlightEndpoints;
            UpdateVisualization();
            Debug.Log($"Endpoint highlighting: {settings.highlightEndpoints}");
        }

        void Update()
        {
            if (enableRealTimeUpdates && isInitialized)
            {
                UpdateVisualization();
            }
        }

        void OnDestroy()
        {
            // Clean up all debug spheres
            foreach (var sphere in debugSpheres.Values)
            {
                if (sphere != null)
                    DestroyImmediate(sphere);
            }
            debugSpheres.Clear();
        }

        // Public API for external control
        public bool IsVisualizationActive => debugMode != DebugMode.Off;
        public DebugMode CurrentDebugMode => debugMode;
        public bool EndpointHighlightingEnabled => settings.highlightEndpoints;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (isInitialized)
            {
                StartCoroutine(DelayedUpdateVisualization());
            }
        }

        private IEnumerator DelayedUpdateVisualization()
        {
            yield return null;
            UpdateVisualization();
        }
#endif
    }
}
