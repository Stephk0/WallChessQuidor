using UnityEngine;
using WallChess.Core.Data;
using WallChess.View.Board;

namespace WallChess.View.Wall
{
    /// <summary>
    /// Visual representation of a placed wall.
    /// </summary>
    public class WallView : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Transform modelTransform;

        private WallPlacement _wallData;

        public WallPlacement WallData => _wallData;

        public void Initialize(WallPlacement wall, BoardView boardView)
        {
            _wallData = wall;

            if (modelTransform == null)
            {
                modelTransform = transform;
            }

            // Position
            if (boardView != null)
            {
                transform.position = boardView.GetWallWorldPosition(wall);
            }
            else
            {
                // Fallback positioning
                transform.position = new Vector3(wall.Position.X + 0.5f, 0, wall.Position.Y + 0.5f);
            }

            // Rotation based on orientation
            float yRotation = wall.Orientation == WallOrientation.Horizontal ? 0f : 90f;
            transform.rotation = Quaternion.Euler(0, yRotation, 0);

            gameObject.name = $"Wall_{wall.Position.X}_{wall.Position.Y}_{wall.Orientation}";
        }
    }
}
