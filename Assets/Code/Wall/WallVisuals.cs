using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Handles preview & wall object creation.
    /// Enhanced with rotation support and proper prefab preview.
    /// </summary>
    public class WallVisuals
    {
        private readonly GameObject prefab;
        private readonly Material mat;
        private readonly Color ok, bad, placing;
        private readonly List<GameObject> spawned = new List<GameObject>();
        private GameObject preview;
        private Renderer previewRenderer;
        private WallManager wallManager; // Reference to get debug mode and prefab info

        public WallVisuals(GameObject prefab, Material material, Color valid, Color invalid, Color placing)
        {
            this.prefab = prefab; mat = material; ok = valid; bad = invalid; this.placing = placing;
        }

        // Set wall manager reference for advanced preview features
        public void SetWallManager(WallManager manager)
        {
            wallManager = manager;
        }

        public void EnsurePreview()
        {
            if (preview != null) return;
            
            // Create preview using appropriate method based on mode
            if (wallManager != null && !wallManager.IsDebugMode() && prefab != null)
            {
                // Use actual prefab for preview in production mode
                preview = GameObject.Instantiate(prefab);
                preview.name = "WallPreview_Prefab";
            }
            else
            {
                // Use primitive cube for debug mode or when no prefab available
                preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                preview.name = "WallPreview_Debug";
            }
            
            previewRenderer = preview.GetComponent<Renderer>();
            if (previewRenderer != null)
            {
                if (mat != null)
                {
                    previewRenderer.material = mat;
                }
                previewRenderer.material.color = placing;
            }
            
            var col = preview.GetComponent<Collider>();
            if (col) WallState.SafeDestroy(col);
        }

        public void UpdatePreview(Vector3 pos, Vector3 scale, bool canPlace)
        {
            EnsurePreview();
            preview.SetActive(true);
            preview.transform.position = pos;
            preview.transform.localScale = scale;
            if (previewRenderer != null)
                previewRenderer.material.color = canPlace ? ok : bad;
        }

        // Enhanced preview method with rotation support
        public void UpdatePreview(Vector3 pos, Vector3 scale, Quaternion rotation, bool canPlace)
        {
            EnsurePreview();
            preview.SetActive(true);
            preview.transform.position = pos;
            preview.transform.rotation = rotation;
            preview.transform.localScale = scale;
            if (previewRenderer != null)
                previewRenderer.material.color = canPlace ? ok : bad;
        }

        public void HidePreview() { if (preview) preview.SetActive(false); }

        public void CleanupPreview()
        {
            if (preview) WallState.SafeDestroy(preview);
            preview = null; previewRenderer = null;
        }

        public GameObject CreateWall(GapDetector.WallInfo w, WallState state)
        {
            var go = prefab != null ? GameObject.Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Wall_{w.orientation}_{w.x}_{w.y}";
            go.transform.position = w.pos;
            go.transform.localScale = w.scale;
            go.tag = "Wall";

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                if (mat != null) r.material = mat;
                else r.material.color = Color.yellow;
            }
            spawned.Add(go);
            state.AddManaged(go);
            return go;
        }
        
        // New overload for unified system that doesn't require WallState
        public GameObject CreateWall(Vector3 position, Vector3 scale)
        {
            var go = prefab != null ? GameObject.Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall_Unified";
            go.transform.position = position;
            go.transform.localScale = scale;
            go.tag = "Wall";

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                if (mat != null) r.material = mat;
                else r.material.color = Color.yellow;
            }
            spawned.Add(go);
            return go;
        }

        // Enhanced overload for new rotation-based prefab system
        public GameObject CreateWall(Vector3 position, Vector3 scale, Quaternion rotation, GameObject wallPrefab = null)
        {
            GameObject prefabToUse = wallPrefab ?? prefab;
            var go = prefabToUse != null ? GameObject.Instantiate(prefabToUse) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall_Prefab";
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = scale;
            go.tag = "Wall";

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                if (mat != null) r.material = mat;
                else if (prefabToUse == null) r.material.color = Color.yellow; // Only set color if using primitive
            }
            spawned.Add(go);
            return go;
        }

        public void DestroyAll()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
                if (spawned[i] != null) WallState.SafeDestroy(spawned[i]);
            spawned.Clear();
        }
    }
}
