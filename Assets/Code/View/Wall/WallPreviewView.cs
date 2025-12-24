using UnityEngine;
using WallChess.Core.Data;
using WallChess.View.Board;

namespace WallChess.View.Wall
{
    /// <summary>
    /// Ghost preview for wall placement. Shows valid/invalid placement.
    /// </summary>
    public class WallPreviewView : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Renderer previewRenderer;
        [SerializeField] private Color validColor = new Color(0.2f, 0.6f, 0.9f, 0.6f);
        [SerializeField] private Color invalidColor = new Color(0.9f, 0.2f, 0.2f, 0.6f);

        private MaterialPropertyBlock _propBlock;
        private BoardView _boardView;
        private WallPlacement _currentPlacement;

        public WallPlacement CurrentPlacement => _currentPlacement;

        private void Awake()
        {
            if (previewRenderer == null)
            {
                previewRenderer = GetComponentInChildren<Renderer>();
            }
            _propBlock = new MaterialPropertyBlock();
        }

        public void Initialize(BoardView boardView, ViewConfig config)
        {
            _boardView = boardView;

            if (config != null)
            {
                validColor = config.wallPreviewValid;
                invalidColor = config.wallPreviewInvalid;
            }

            Hide();
        }

        public void Show(WallPlacement wall, bool isValid)
        {
            _currentPlacement = wall;

            // Position
            if (_boardView != null)
            {
                transform.position = _boardView.GetWallWorldPosition(wall);
            }
            else
            {
                transform.position = new Vector3(wall.Position.X + 0.5f, 0, wall.Position.Y + 0.5f);
            }

            // Rotation
            float yRotation = wall.Orientation == WallOrientation.Horizontal ? 0f : 90f;
            transform.rotation = Quaternion.Euler(0, yRotation, 0);

            // Color based on validity
            SetValid(isValid);

            gameObject.SetActive(true);
        }

        public void SetValid(bool isValid)
        {
            if (previewRenderer == null) return;

            var color = isValid ? validColor : invalidColor;

            previewRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", color);
            _propBlock.SetColor("_BaseColor", color);
            previewRenderer.SetPropertyBlock(_propBlock);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void UpdateOrientation(WallOrientation orientation)
        {
            _currentPlacement = new WallPlacement(_currentPlacement.Position, orientation);
            float yRotation = orientation == WallOrientation.Horizontal ? 0f : 90f;
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
        }
    }
}
