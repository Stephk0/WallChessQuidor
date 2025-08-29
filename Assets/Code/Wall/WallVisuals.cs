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
                
                previewRenderer = preview.GetComponent<Renderer>();
                if (previewRenderer != null)
                {
                    if (mat != null)
                    {
                        previewRenderer.material = mat;
                    }
                    previewRenderer.material.color = placing;
                }
            }
            else
            {
                // Debug mode: Create single preview cube (segments will be created on placement)
                preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                preview.name = "WallPreview_Debug";
                
                previewRenderer = preview.GetComponent<Renderer>();
                if (previewRenderer != null)
                {
                    if (mat != null)
                    {
                        previewRenderer.material = mat;
                    }
                    previewRenderer.material.color = placing;
                }
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
            if (wallManager != null && wallManager.IsDebugMode())
            {
                // Debug mode: Create 3 separate boxes for wall segments and intersection
                return CreateDebugWallSegments(position, scale, rotation);
            }
            
            // Production mode: Use prefab with rotation
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
        
        /// <summary>
        /// Creates 3 separate debug boxes: 2 wall segments + 1 intersection
        /// Uses the same scaling logic as the working single preview box
        /// </summary>
        private GameObject CreateDebugWallSegments(Vector3 centerPosition, Vector3 wallScale, Quaternion rotation)
        {
            // Create parent object to hold all segments
            GameObject wallParent = new GameObject("DebugWall_Segments");
            wallParent.transform.position = centerPosition;
            wallParent.tag = "Wall";
            
            // Get grid settings for positioning calculations
            var gridSettings = wallManager.GetGridSystem().GetGridSettings();
            float tileSize = gridSettings.tileSize;
            float tileGap = gridSettings.tileGap;
            float spacing = tileSize + tileGap;
            
            // Determine orientation from the wallScale (same logic as GetWallScale)
            bool isHorizontal = wallScale.x > wallScale.y;
            
            Vector3[] segmentPositions = new Vector3[3];
            Vector3[] segmentScales = new Vector3[3];
            
            if (isHorizontal)
            {
                // Horizontal wall: divide the working scale into 3 parts
                float gapWidth = wallScale.y; // Use the working thickness
                float gapLength = (wallScale.x - gapWidth) / 2f; // Each gap gets half the length minus intersection
                
                // Scales: 2 gap segments + 1 square intersection
                Vector3 gapScale = new Vector3(gapLength, gapWidth, wallScale.z);
                Vector3 intersectionScale = new Vector3(gapWidth, gapWidth, wallScale.z);
                
                // Positions: spread along X axis
                float offset = gapLength / 2f + gapWidth / 2f;
                segmentPositions[0] = new Vector3(-offset, 0, 0); // Left gap
                segmentPositions[1] = new Vector3(0, 0, 0);       // Center intersection
                segmentPositions[2] = new Vector3(offset, 0, 0);  // Right gap
                
                segmentScales[0] = gapScale;
                segmentScales[1] = intersectionScale;
                segmentScales[2] = gapScale;
            }
            else
            {
                // Vertical wall: divide the working scale into 3 parts
                float gapWidth = wallScale.x; // Use the working thickness
                float gapLength = (wallScale.y - gapWidth) / 2f; // Each gap gets half the length minus intersection
                
                // Scales: 2 gap segments + 1 square intersection
                Vector3 gapScale = new Vector3(gapWidth, gapLength, wallScale.z);
                Vector3 intersectionScale = new Vector3(gapWidth, gapWidth, wallScale.z);
                
                // Positions: spread along Y axis
                float offset = gapLength / 2f + gapWidth / 2f;
                segmentPositions[0] = new Vector3(0, -offset, 0); // Bottom gap
                segmentPositions[1] = new Vector3(0, 0, 0);       // Center intersection
                segmentPositions[2] = new Vector3(0, offset, 0);  // Top gap
                
                segmentScales[0] = gapScale;
                segmentScales[1] = intersectionScale;
                segmentScales[2] = gapScale;
            }
            
            // Create the 3 boxes with proper positioning and scaling
            for (int i = 0; i < 3; i++)
            {
                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.transform.SetParent(wallParent.transform);
                segment.transform.localPosition = segmentPositions[i];
                segment.transform.localScale = segmentScales[i];
                
                if (i == 1)
                {
                    segment.name = "Intersection";
                }
                else
                {
                    segment.name = $"GapSegment_{i}";
                }
                
                // Apply material and color
                var renderer = segment.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (mat != null)
                    {
                        renderer.material = mat;
                    }
                    else
                    {
                        // Different colors for segments vs intersection
                        renderer.material.color = i == 1 ? Color.red : Color.yellow;
                    }
                }
                
                // Remove colliders to avoid interference
                var col = segment.GetComponent<Collider>();
                if (col) WallState.SafeDestroy(col);
            }
            
            spawned.Add(wallParent);
            return wallParent;
        }

        public void DestroyAll()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
                if (spawned[i] != null) WallState.SafeDestroy(spawned[i]);
            spawned.Clear();
        }
    }
}
