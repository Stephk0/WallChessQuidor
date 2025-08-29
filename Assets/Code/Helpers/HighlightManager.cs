using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Manages highlight object pooling to avoid creating/destroying objects during gameplay.
    /// Pre-pools highlight objects based on maximum grid size and reuses them.
    /// </summary>
    public class HighlightManager : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private int maxHighlights = 32; // Max possible valid moves on largest grid
        
        private GameObject highlightPrefab;
        private List<GameObject> highlightPool = new List<GameObject>();
        private List<GameObject> activeHighlights = new List<GameObject>();
        private Transform highlightParent;

        public void Initialize(GameObject prefab)
        {
            highlightPrefab = prefab;
            
            // Create parent object to organize highlights in hierarchy
            highlightParent = new GameObject("HighlightPool").transform;
            highlightParent.SetParent(transform);
            
            // Pre-pool highlight objects
            CreateHighlightPool();
            
            Debug.Log($"HighlightManager initialized with {highlightPool.Count} pooled highlights");
        }

        private void CreateHighlightPool()
        {
            for (int i = 0; i < maxHighlights; i++)
            {
                GameObject highlight = CreateHighlightObject(i);
                highlightPool.Add(highlight);
            }
        }

        private GameObject CreateHighlightObject(int index)
        {
            GameObject highlight;
            
            if (highlightPrefab != null)
            {
                highlight = Instantiate(highlightPrefab, highlightParent);
            }
            else
            {
                // Fallback: create simple highlight if prefab is missing
                highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                highlight.name = "Highlight_Fallback";
                highlight.transform.SetParent(highlightParent);
                highlight.transform.localScale = Vector3.one * 0.3f;
                
                // Setup fallback material
                Renderer renderer = highlight.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0, 1, 0, 0.7f);
                renderer.material = mat;
                
                // Remove collider to prevent interference
                Collider col = highlight.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
            }
            
            highlight.name = $"HighlightPool_{index}";
            highlight.SetActive(false);
            
            return highlight;
        }

        public void ShowHighlights(List<Vector2Int> positions, GridSystem gridSystem)
        {
            if (positions == null || gridSystem == null) return;
            
            // Clear any currently active highlights
            ClearHighlights();
            
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
                Debug.LogWarning($"Not enough highlights in pool! Requested: {positions.Count}, Available: {highlightPool.Count}");
            }
            
            Debug.Log($"Showing {activeHighlights.Count} highlights for valid moves");
        }

        public void ClearHighlights()
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

        private void ReturnHighlightToPool(GameObject highlight)
        {
            // Highlight is already deactivated in ClearHighlights()
            // Just ensure it's in the pool
            if (!highlightPool.Contains(highlight))
            {
                Debug.LogWarning("Highlight not found in pool!");
            }
        }

        public int GetActiveHighlightCount()
        {
            return activeHighlights.Count;
        }

        public int GetPoolSize()
        {
            return highlightPool.Count;
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
                    GameObject highlight = CreateHighlightObject(i);
                    highlightPool.Add(highlight);
                }
            }
            
            maxHighlights = newSize;
            Debug.Log($"HighlightManager pool resized to {highlightPool.Count} objects");
        }

        private void OnDestroy()
        {
            ClearHighlights();
        }
    }
}