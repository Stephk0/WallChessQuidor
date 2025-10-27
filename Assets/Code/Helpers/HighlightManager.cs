using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Manages highlight object pooling to avoid creating/destroying objects during gameplay.
    /// Pre-pools highlight objects based on maximum grid size and reuses them.
    /// Supports two types of highlights: normal valid move highlights and drag confirm highlights.
    /// </summary>
    public class HighlightManager : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private int maxHighlights = 8; // Max possible valid moves on largest grid (increased for safety)
        [SerializeField] private int maxConfirmHighlights = 8; // Max confirm highlights needed
        [SerializeField] private int maxOpponentHighlights = 8; // Max opponent move highlights
        
        private GameObject highlightPrefab;
        private GameObject highlightConfirmPrefab;
        
        // Valid move highlights (always visible during pawn's turn)
        private List<GameObject> highlightPool = new List<GameObject>();
        private List<GameObject> activeHighlights = new List<GameObject>();
        private Transform highlightParent;
        
        // Confirm highlights (show during drag operations)
        private List<GameObject> confirmHighlightPool = new List<GameObject>();
        private List<GameObject> activeConfirmHighlights = new List<GameObject>();
        private Transform confirmHighlightParent;
        
        // Opponent highlights (show for AI/opponent moves with different styling)
        private List<GameObject> opponentHighlightPool = new List<GameObject>();
        private List<GameObject> activeOpponentHighlights = new List<GameObject>();
        private Transform opponentHighlightParent;

        public void Initialize(GameObject prefab, GameObject confirmPrefab = null)
        {
            // Prevent multiple initializations
            if (highlightPool != null && highlightPool.Count > 0)
            {
                Debug.LogWarning("[HighlightManager] Already initialized! Skipping duplicate initialization.");
                return;
            }
            
            Debug.Log($"[HighlightManager] Starting initialization with prefabs: {prefab?.name}, {confirmPrefab?.name}");
            
            highlightPrefab = prefab;
            highlightConfirmPrefab = confirmPrefab;
            
            // Create parent objects to organize highlights in hierarchy
            highlightParent = new GameObject("HighlightPool").transform;
            highlightParent.SetParent(transform);
            
            confirmHighlightParent = new GameObject("ConfirmHighlightPool").transform;
            confirmHighlightParent.SetParent(transform);
            
            opponentHighlightParent = new GameObject("OpponentHighlightPool").transform;
            opponentHighlightParent.SetParent(transform);
            
            // Pre-pool highlight objects
            CreateHighlightPool();
            CreateConfirmHighlightPool();
            CreateOpponentHighlightPool();
            
            Debug.Log($"[HighlightManager] Initialized with {highlightPool.Count} valid move highlights, {confirmHighlightPool.Count} confirm highlights, and {opponentHighlightPool.Count} opponent highlights. Prefab null check: highlight={highlightPrefab == null}, confirm={highlightConfirmPrefab == null}");
            
            // Log current state for debugging
            Debug.Log($"[HighlightManager] Pool state - Normal pool size: {maxHighlights}, Confirm pool size: {maxConfirmHighlights}, Opponent pool size: {maxOpponentHighlights}");
        }

        private void CreateHighlightPool()
        {
            for (int i = 0; i < maxHighlights; i++)
            {
                GameObject highlight = CreateHighlightObject(i, highlightPrefab, "HighlightPool_", highlightParent);
                highlightPool.Add(highlight);
            }
        }
        
        private void CreateConfirmHighlightPool()
        {
            for (int i = 0; i < maxConfirmHighlights; i++)
            {
                GameObject highlight = CreateHighlightObject(i, highlightConfirmPrefab, "ConfirmHighlightPool_", confirmHighlightParent);
                confirmHighlightPool.Add(highlight);
            }
        }
        
        private void CreateOpponentHighlightPool()
        {
            for (int i = 0; i < maxOpponentHighlights; i++)
            {
                GameObject highlight = CreateOpponentHighlightObject(i, highlightPrefab, "OpponentHighlightPool_", opponentHighlightParent);
                opponentHighlightPool.Add(highlight);
            }
        }

        private GameObject CreateHighlightObject(int index, GameObject prefab, string namePrefix, Transform parent)
        {
            GameObject highlight;
            
            if (prefab != null)
            {
                highlight = Instantiate(prefab, parent);
            }
            else
            {
                // Fallback: create simple highlight if prefab is missing
                highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                highlight.name = "Highlight_Fallback";
                highlight.transform.SetParent(parent);
                highlight.transform.localScale = Vector3.one * 0.3f;
                
                // Setup fallback material with different colors for different types
                Renderer renderer = highlight.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (namePrefix.Contains("Confirm"))
                {
                    mat.color = new Color(1, 1, 0, 0.8f); // Yellow for confirm highlights
                }
                else
                {
                    mat.color = new Color(0, 1, 0, 0.7f); // Green for valid move highlights
                }
                renderer.material = mat;
                
                // Remove collider to prevent interference
                Collider col = highlight.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
            }
            
            highlight.name = $"{namePrefix}{index}";
            highlight.SetActive(false);
            
            return highlight;
        }
        
        private GameObject CreateOpponentHighlightObject(int index, GameObject prefab, string namePrefix, Transform parent)
        {
            GameObject highlight;
            
            if (prefab != null)
            {
                highlight = Instantiate(prefab, parent);
                
                // Modify the material to show this is an opponent highlight
                Renderer renderer = highlight.GetComponentInChildren<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    // Create a new material with different color for opponent
                    Material opponentMat = new Material(renderer.material);
                    opponentMat.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange color for opponents
                    renderer.material = opponentMat;
                }
            }
            else
            {
                // Fallback: create simple opponent highlight if prefab is missing
                highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                highlight.name = "OpponentHighlight_Fallback";
                highlight.transform.SetParent(parent);
                highlight.transform.localScale = Vector3.one * 0.3f;
                
                // Setup fallback material with orange color for opponents
                Renderer renderer = highlight.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange for opponent highlights
                renderer.material = mat;
                
                // Remove collider to prevent interference
                Collider col = highlight.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
            }
            
            highlight.name = $"{namePrefix}{index}";
            highlight.SetActive(false);
            
            return highlight;
        }

        /// <summary>
        /// Show valid move highlights (always visible during active pawn's turn)
        /// </summary>
/// <summary>
        /// Show valid move highlights (always visible during active pawn's turn)
        /// </summary>
        /// <param name="positions">List of valid move positions</param>
        /// <param name="gridSystem">Grid system for position conversion</param>
        /// <param name="isPlayerPawn">True for human player, false for AI opponent</param>
        public void ShowValidMoveHighlights(List<Vector2Int> positions, GridSystem gridSystem, bool isPlayerPawn = true)
        {
            if (positions == null || gridSystem == null) return;
            
            // Clear both player and opponent highlights to avoid conflicts
            ClearValidMoveHighlights();
            ClearOpponentHighlights();
            
            if (isPlayerPawn)
            {
                ShowPlayerHighlights(positions, gridSystem);
            }
            else
            {
                ShowOpponentHighlights(positions, gridSystem);
            }
        }
        
                /// <summary>
        /// Update confirm highlight position (clears and recreates at new position)
        /// Used when target moves from one valid position to another while staying in confirm zone
        /// </summary>
        public void UpdateConfirmHighlightPosition(Vector2Int newPosition, GridSystem gridSystem)
        {
            if (gridSystem == null) return;
            
            // Clear current highlight and show at new position
            ClearConfirmHighlights();
            ShowConfirmHighlight(newPosition, gridSystem);
        }

/// <summary>
        /// Show highlights for player moves (green)
        /// </summary>
        private void ShowPlayerHighlights(List<Vector2Int> positions, GridSystem gridSystem)
        {
            // Safety check: ensure pool is initialized
            if (highlightPool == null || highlightPool.Count == 0)
            {
                Debug.LogWarning("[HighlightManager] Pool not initialized! Attempting emergency initialization...");
                TryEmergencyInitialization();
                
                // Check again after emergency init
                if (highlightPool == null || highlightPool.Count == 0)
                {
                    Debug.LogError("[HighlightManager] Emergency initialization failed! Cannot show highlights.");
                    return;
                }
            }
            
            // Get required number of highlights from pool
            int requiredHighlights = Mathf.Min(positions.Count, highlightPool.Count);
            
            for (int i = 0; i < requiredHighlights; i++)
            {
                GameObject highlight = GetHighlightFromPool();
                if (highlight != null)
                {
                    Vector3 worldPos = gridSystem.GridToWorldPosition(positions[i]);
                    highlight.transform.position = worldPos + Vector3.back * 0.1f; // Slight offset to avoid z-fighting
                    highlight.SetActive(true);
                    activeHighlights.Add(highlight);
                }
            }
            
            if (positions.Count > highlightPool.Count)
            {
                Debug.LogWarning($"[HighlightManager] Not enough player highlights in pool! Requested: {positions.Count}, Available: {highlightPool.Count}");
            }
            
            Debug.Log($"[HighlightManager] Showing {activeHighlights.Count} player move highlights (green)");
        }

/// <summary>
        /// Show highlights for opponent moves (orange)
        /// </summary>
        private void ShowOpponentHighlights(List<Vector2Int> positions, GridSystem gridSystem)
        {
            // Safety check: ensure pool is initialized
            if (opponentHighlightPool == null || opponentHighlightPool.Count == 0)
            {
                Debug.LogWarning("[HighlightManager] Opponent pool not initialized! Attempting emergency initialization...");
                TryEmergencyInitialization();
                
                // Check again after emergency init
                if (opponentHighlightPool == null || opponentHighlightPool.Count == 0)
                {
                    Debug.LogError("[HighlightManager] Emergency initialization failed! Cannot show opponent highlights.");
                    return;
                }
            }
            
            // Get required number of highlights from opponent pool
            int requiredHighlights = Mathf.Min(positions.Count, opponentHighlightPool.Count);
            
            for (int i = 0; i < requiredHighlights; i++)
            {
                GameObject highlight = GetOpponentHighlightFromPool();
                if (highlight != null)
                {
                    Vector3 worldPos = gridSystem.GridToWorldPosition(positions[i]);
                    highlight.transform.position = worldPos + Vector3.back * 0.1f; // Slight offset to avoid z-fighting
                    highlight.SetActive(true);
                    activeOpponentHighlights.Add(highlight);
                }
            }
            
            if (positions.Count > opponentHighlightPool.Count)
            {
                Debug.LogWarning($"[HighlightManager] Not enough opponent highlights in pool! Requested: {positions.Count}, Available: {opponentHighlightPool.Count}");
            }
            
            Debug.Log($"[HighlightManager] Showing {activeOpponentHighlights.Count} opponent move highlights (orange)");
        }

/// <summary>
        /// Legacy method for backward compatibility - defaults to player highlights
        /// </summary>
        public void ShowValidMoveHighlights(List<Vector2Int> positions, GridSystem gridSystem)
        {
            ShowValidMoveHighlights(positions, gridSystem, true); // Default to player highlights
        }



        
/// <summary>
        /// Show confirm highlight at a specific position during drag operations
        /// </summary>
        public void ShowConfirmHighlight(Vector2Int position, GridSystem gridSystem)
        {
            if (gridSystem == null) return;
            
            // Clear any currently active confirm highlights
            ClearConfirmHighlights();
            
            // Get one confirm highlight from pool
            GameObject confirmHighlight = GetConfirmHighlightFromPool();
            if (confirmHighlight != null)
            {
                Vector3 worldPos = gridSystem.GridToWorldPosition(position);
                confirmHighlight.transform.position = worldPos + Vector3.back * 0.05f; // Slight offset, closer than normal highlights
                confirmHighlight.SetActive(true);
                activeConfirmHighlights.Add(confirmHighlight);
            }
        }
        
        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        public void ShowHighlights(List<Vector2Int> positions, GridSystem gridSystem)
        {
            ShowValidMoveHighlights(positions, gridSystem);
        }

        /// <summary>
        /// Clear valid move highlights
        /// </summary>
        public void ClearValidMoveHighlights()
        {
            foreach (GameObject highlight in activeHighlights)
            {
                if (highlight != null)
                {
                    highlight.SetActive(false);
                    ReturnHighlightToPool(highlight);
                }
            }
            activeHighlights.Clear();
        }
        
        /// <summary>
        /// Clear confirm highlights
        /// </summary>
        public void ClearConfirmHighlights()
        {
            foreach (GameObject highlight in activeConfirmHighlights)
            {
                if (highlight != null)
                {
                    highlight.SetActive(false);
                    ReturnConfirmHighlightToPool(highlight);
                }
            }
            activeConfirmHighlights.Clear();
        }

/// <summary>
        /// Clear opponent highlights
        /// </summary>
        public void ClearOpponentHighlights()
        {
            foreach (GameObject highlight in activeOpponentHighlights)
            {
                if (highlight != null)
                {
                    highlight.SetActive(false);
                    ReturnOpponentHighlightToPool(highlight);
                }
            }
            activeOpponentHighlights.Clear();
        }

        
        /// <summary>
        /// Clear all highlights (both valid move and confirm)
        /// </summary>
/// <summary>
        /// Clear all highlights (player, opponent, and confirm)
        /// </summary>
        public void ClearAllHighlights()
        {
            ClearValidMoveHighlights();
            ClearOpponentHighlights();
            ClearConfirmHighlights();
        }
        
        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        public void ClearHighlights()
        {
            ClearValidMoveHighlights();
        }

        private GameObject GetHighlightFromPool()
        {
            for (int i = 0; i < highlightPool.Count; i++)
            {
                if (!highlightPool[i].activeInHierarchy)
                {
                    return highlightPool[i];
                }
            }
            
            Debug.LogWarning("No available highlights in pool!");
            return null;
        }
        
        private GameObject GetConfirmHighlightFromPool()
        {
            for (int i = 0; i < confirmHighlightPool.Count; i++)
            {
                if (!confirmHighlightPool[i].activeInHierarchy)
                {
                    return confirmHighlightPool[i];
                }
            }
            
            Debug.LogWarning("No available confirm highlights in pool!");
            return null;
        }

private GameObject GetOpponentHighlightFromPool()
        {
            for (int i = 0; i < opponentHighlightPool.Count; i++)
            {
                if (!opponentHighlightPool[i].activeInHierarchy)
                {
                    return opponentHighlightPool[i];
                }
            }
            
            Debug.LogWarning("No available opponent highlights in pool!");
            return null;
        }


        private void ReturnHighlightToPool(GameObject highlight)
        {
            // Highlight is already deactivated in ClearValidMoveHighlights()
            // Just ensure it's in the pool
            if (!highlightPool.Contains(highlight))
            {
                Debug.LogWarning("Highlight not found in pool!");
            }
        }
        
        private void ReturnConfirmHighlightToPool(GameObject highlight)
        {
            // Highlight is already deactivated in ClearConfirmHighlights()
            // Just ensure it's in the pool
            if (!confirmHighlightPool.Contains(highlight))
            {
                Debug.LogWarning("Confirm highlight not found in pool!");
            }
        }

private void ReturnOpponentHighlightToPool(GameObject highlight)
        {
            // Highlight is already deactivated in ClearOpponentHighlights()
            // Just ensure it's in the pool
            if (!opponentHighlightPool.Contains(highlight))
            {
                Debug.LogWarning("Opponent highlight not found in pool!");
            }
        }


        public int GetActiveHighlightCount()
        {
            return activeHighlights.Count;
        }
        
        public int GetActiveConfirmHighlightCount()
        {
            return activeConfirmHighlights.Count;
        }

public int GetActiveOpponentHighlightCount()
        {
            return activeOpponentHighlights.Count;
        }


        public int GetPoolSize()
        {
            return highlightPool.Count;
        }
        
        public int GetConfirmPoolSize()
        {
            return confirmHighlightPool.Count;
        }

public int GetOpponentPoolSize()
        {
            return opponentHighlightPool.Count;
        }


        public void ResizePool(int newSize)
        {
            if (newSize < highlightPool.Count)
            {
                // Remove excess highlights
                for (int i = highlightPool.Count - 1; i >= newSize; i--)
                {
                    if (highlightPool[i] != null)
                    {
                        DestroyImmediate(highlightPool[i]);
                    }
                    highlightPool.RemoveAt(i);
                }
            }
            else if (newSize > highlightPool.Count)
            {
                // Add more highlights
                int currentSize = highlightPool.Count;
                for (int i = currentSize; i < newSize; i++)
                {
                    GameObject highlight = CreateHighlightObject(i, highlightPrefab, "HighlightPool_", highlightParent);
                    highlightPool.Add(highlight);
                }
            }
            
            maxHighlights = newSize;
            Debug.Log($"HighlightManager pool resized to {highlightPool.Count} objects");
        }

        /// <summary>
        /// Emergency initialization fallback when pool is accessed but not initialized
        /// </summary>
/// <summary>
        /// Emergency initialization fallback when pool is accessed but not initialized
        /// </summary>
        private void TryEmergencyInitialization()
        {
            Debug.LogWarning("[HighlightManager] Performing emergency initialization with fallback settings");
            
            // Try to find prefab references from the game manager
            var gameManager = FindObjectOfType<WallChessGameManager>();
            if (gameManager != null)
            {
                var highlightPrefab = gameManager.highlightPrefab;
                var confirmPrefab = gameManager.highlightConfirmPrefab;
                
                if (highlightPrefab != null)
                {
                    Initialize(highlightPrefab, confirmPrefab);
                    Debug.Log("[HighlightManager] Emergency initialization successful with game manager prefabs");
                    return;
                }
            }
            
            // Fallback to null prefabs (will create primitive fallbacks)
            Initialize(null, null);
            Debug.Log("[HighlightManager] Emergency initialization with fallback primitives");
        }
        
        private void OnDestroy()
        {
            ClearAllHighlights();
        }
    }
}