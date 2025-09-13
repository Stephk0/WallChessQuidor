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
        [SerializeField] private int maxHighlights = 4; // Max possible valid moves on largest grid
        [SerializeField] private int maxConfirmHighlights = 8; // Max confirm highlights needed
        
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

        public void Initialize(GameObject prefab, GameObject confirmPrefab = null)
        {
            highlightPrefab = prefab;
            highlightConfirmPrefab = confirmPrefab;
            
            // Create parent objects to organize highlights in hierarchy
            highlightParent = new GameObject("HighlightPool").transform;
            highlightParent.SetParent(transform);
            
            confirmHighlightParent = new GameObject("ConfirmHighlightPool").transform;
            confirmHighlightParent.SetParent(transform);
            
            // Pre-pool highlight objects
            CreateHighlightPool();
            CreateConfirmHighlightPool();
            
            Debug.Log($"HighlightManager initialized with {highlightPool.Count} valid move highlights and {confirmHighlightPool.Count} confirm highlights");
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

        /// <summary>
        /// Show valid move highlights (always visible during active pawn's turn)
        /// </summary>
        public void ShowValidMoveHighlights(List<Vector2Int> positions, GridSystem gridSystem)
        {
            if (positions == null || gridSystem == null) return;
            
            // Clear any currently active highlights
            ClearValidMoveHighlights();
            
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
                Debug.LogWarning($"Not enough valid move highlights in pool! Requested: {positions.Count}, Available: {highlightPool.Count}");
            }
            
            Debug.Log($"Showing {activeHighlights.Count} valid move highlights");
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
        /// Clear all highlights (both valid move and confirm)
        /// </summary>
        public void ClearAllHighlights()
        {
            ClearValidMoveHighlights();
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

        public int GetActiveHighlightCount()
        {
            return activeHighlights.Count;
        }
        
        public int GetActiveConfirmHighlightCount()
        {
            return activeConfirmHighlights.Count;
        }

        public int GetPoolSize()
        {
            return highlightPool.Count;
        }
        
        public int GetConfirmPoolSize()
        {
            return confirmHighlightPool.Count;
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

        private void OnDestroy()
        {
            ClearAllHighlights();
        }
    }
}