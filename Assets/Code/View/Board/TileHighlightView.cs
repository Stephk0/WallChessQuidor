using UnityEngine;

namespace WallChess.View.Board
{
    /// <summary>
    /// Pooled highlight indicator for valid moves or selections.
    /// </summary>
    public class TileHighlightView : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Renderer highlightRenderer;
        [SerializeField] private float heightOffset = 0.01f;

        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            if (highlightRenderer == null)
            {
                highlightRenderer = GetComponentInChildren<Renderer>();
            }
            _propBlock = new MaterialPropertyBlock();
        }

        public void Show(Vector3 worldPosition, Color color)
        {
            transform.position = worldPosition + Vector3.up * heightOffset;
            gameObject.SetActive(true);

            if (highlightRenderer != null)
            {
                highlightRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_Color", color);
                _propBlock.SetColor("_BaseColor", color);
                highlightRenderer.SetPropertyBlock(_propBlock);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
