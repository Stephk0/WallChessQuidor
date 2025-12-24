using System.Collections;
using UnityEngine;
using WallChess.Core.Data;
using WallChess.View.Board;

namespace WallChess.View.Pawn
{
    /// <summary>
    /// Visual representation of a pawn with movement animation.
    /// </summary>
    public class PawnView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float jumpHeight = 0.3f;
        [SerializeField] private AnimationCurve moveCurve;

        [Header("Visual")]
        [SerializeField] private Transform modelTransform;
        [SerializeField] private float groundOffset = 0f;

        // State
        private int _playerIndex;
        private BoardPosition _currentPosition;
        private BoardView _boardView;
        private ViewConfig _viewConfig;
        private Coroutine _moveCoroutine;
        private bool _isMoving;

        public int PlayerIndex => _playerIndex;
        public BoardPosition CurrentPosition => _currentPosition;
        public bool IsMoving => _isMoving;

        public void Initialize(int playerIndex, BoardPosition startPosition, ViewConfig config, BoardView boardView)
        {
            _playerIndex = playerIndex;
            _currentPosition = startPosition;
            _viewConfig = config;
            _boardView = boardView;

            // Apply config settings
            if (config != null)
            {
                moveSpeed = config.pawnMoveSpeed;
                jumpHeight = config.pawnJumpHeight;
                moveCurve = config.moveCurve;
            }

            // Set initial position
            if (boardView != null)
            {
                transform.position = GetTargetPosition(startPosition);
            }

            // Find model transform if not assigned
            if (modelTransform == null)
            {
                modelTransform = transform;
            }
        }

        public void MoveTo(BoardPosition targetPosition)
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }

            _moveCoroutine = StartCoroutine(MoveAnimation(targetPosition));
        }

        public void SetPositionImmediate(BoardPosition position)
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            _currentPosition = position;
            _isMoving = false;

            if (_boardView != null)
            {
                transform.position = GetTargetPosition(position);
            }
        }

        private IEnumerator MoveAnimation(BoardPosition targetPosition)
        {
            _isMoving = true;

            Vector3 startPos = transform.position;
            Vector3 endPos = GetTargetPosition(targetPosition);

            // Calculate if this is a jump (more than 1 tile)
            int distance = Mathf.Abs(targetPosition.X - _currentPosition.X) +
                           Mathf.Abs(targetPosition.Y - _currentPosition.Y);
            bool isJump = distance > 1;
            float actualJumpHeight = isJump ? jumpHeight * 1.5f : jumpHeight;

            float duration = Vector3.Distance(startPos, endPos) / moveSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Apply curve if available
                float curvedT = moveCurve != null ? moveCurve.Evaluate(t) : t;

                // Horizontal movement
                Vector3 pos = Vector3.Lerp(startPos, endPos, curvedT);

                // Vertical arc (parabola)
                float arc = 4f * actualJumpHeight * curvedT * (1f - curvedT);
                pos.y += arc;

                transform.position = pos;

                yield return null;
            }

            // Ensure final position
            transform.position = endPos;
            _currentPosition = targetPosition;
            _isMoving = false;
            _moveCoroutine = null;
        }

        private Vector3 GetTargetPosition(BoardPosition boardPos)
        {
            if (_boardView == null)
            {
                return new Vector3(boardPos.X, groundOffset, boardPos.Y);
            }

            var worldPos = _boardView.GetWorldPosition(boardPos);
            worldPos.y += groundOffset;
            return worldPos;
        }

        /// <summary>
        /// Called when this pawn is selected/deselected.
        /// </summary>
        public void SetSelected(bool selected)
        {
            // Optional: Add visual feedback for selection
            if (modelTransform != null)
            {
                float targetY = selected ? groundOffset + 0.1f : groundOffset;
                // Could animate this, but keeping simple for now
            }
        }
    }
}
