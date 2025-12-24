using UnityEngine;
using WallChess.Core.Data;

namespace WallChess.View.Board
{
    /// <summary>
    /// Visual representation of a single board tile.
    /// </summary>
    public class TileView : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private Material lightMaterial;
        [SerializeField] private Material darkMaterial;

        // State
        private BoardPosition _position;
        private bool _isLight;

        public BoardPosition Position => _position;
        public bool IsLight => _isLight;

        public void Initialize(BoardPosition position, bool isLight)
        {
            _position = position;
            _isLight = isLight;

            // Auto-find renderer if not assigned
            if (tileRenderer == null)
            {
                tileRenderer = GetComponentInChildren<Renderer>();
            }

            // Apply material based on checker pattern
            if (tileRenderer != null)
            {
                if (isLight && lightMaterial != null)
                {
                    tileRenderer.material = lightMaterial;
                }
                else if (!isLight && darkMaterial != null)
                {
                    tileRenderer.material = darkMaterial;
                }
            }
        }

        /// <summary>
        /// Temporarily highlight this tile (e.g., on hover).
        /// </summary>
        public void SetHighlight(bool highlighted, Color color = default)
        {
            if (tileRenderer == null) return;

            var propBlock = new MaterialPropertyBlock();
            tileRenderer.GetPropertyBlock(propBlock);

            if (highlighted)
            {
                propBlock.SetColor("_EmissionColor", color * 0.3f);
            }
            else
            {
                propBlock.SetColor("_EmissionColor", Color.black);
            }

            tileRenderer.SetPropertyBlock(propBlock);
        }
    }
}
