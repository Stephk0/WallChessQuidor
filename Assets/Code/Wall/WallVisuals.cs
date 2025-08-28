using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Handles preview & wall object creation.
    /// </summary>
    public class WallVisuals
    {
        private readonly GameObject prefab;
        private readonly Material mat;
        private readonly Color ok, bad, placing;
        private readonly List<GameObject> spawned = new List<GameObject>();
        private GameObject preview;
        private Renderer previewRenderer;

        public WallVisuals(GameObject prefab, Material material, Color valid, Color invalid, Color placing)
        {
            this.prefab = prefab; mat = material; ok = valid; bad = invalid; this.placing = placing;
        }

        public void EnsurePreview()
        {
            if (preview != null) return;
            preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.name = "WallPreview";
            previewRenderer = preview.GetComponent<Renderer>();
            previewRenderer.material.color = placing;
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

        public void DestroyAll()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
                if (spawned[i] != null) WallState.SafeDestroy(spawned[i]);
            spawned.Clear();
        }
    }
}
