using WallChess.Core;
using WallChess.Core.Data;

namespace WallChess.Input
{
    /// <summary>
    /// Handles wall placement preview and confirmation.
    /// </summary>
    public class WallInputHandler : IInputHandler
    {
        private readonly GameController _controller;

        private bool _isPlacing;
        private BoardPosition _currentPosition;
        private WallOrientation _currentOrientation;
        private bool _lastValidState;

        public WallInputHandler(GameController controller)
        {
            _controller = controller;
        }

        public bool CanHandle(InputData input)
        {
            if (_controller?.State?.IsPlaying != true) return false;

            // Check if current player has walls remaining
            var player = _controller.State.CurrentPlayer;
            if (player == null || !player.HasWallsRemaining) return false;

            // Handle wall gap hits or tile hits (for drag-to-place)
            return input.HitType == HitType.WallGap || input.HitType == HitType.Tile;
        }

        public void OnInputBegin(InputData input)
        {
            _isPlacing = true;
            _currentPosition = input.GridPosition;
            _currentOrientation = WallOrientation.Horizontal; // Default
            _lastValidState = false;

            GameEvents.RaiseWallPlacementStarted();
            UpdatePreview();
        }

        public void OnInputMove(InputData input)
        {
            if (!_isPlacing) return;

            // Update position if valid
            if (input.IsValid)
            {
                // Detect orientation from drag direction
                if (input.GridPosition != _currentPosition)
                {
                    int dx = input.GridPosition.X - _currentPosition.X;
                    int dy = input.GridPosition.Y - _currentPosition.Y;

                    // Horizontal drag = vertical wall, vertical drag = horizontal wall
                    if (System.Math.Abs(dx) > System.Math.Abs(dy))
                    {
                        _currentOrientation = WallOrientation.Vertical;
                    }
                    else if (System.Math.Abs(dy) > System.Math.Abs(dx))
                    {
                        _currentOrientation = WallOrientation.Horizontal;
                    }
                }

                _currentPosition = input.GridPosition;
                UpdatePreview();
            }
        }

        public void OnInputEnd(InputData input)
        {
            if (!_isPlacing)
            {
                Reset();
                return;
            }

            // Try to place wall at current position
            int playerIndex = _controller.State.CurrentPlayerIndex;
            var result = _controller.TryPlaceWall(playerIndex, _currentPosition, _currentOrientation);

            if (!result.IsSuccess)
            {
                UnityEngine.Debug.Log($"Wall placement failed: {result.FailureReason}");
            }

            Reset();
        }

        public void OnCancel()
        {
            Reset();
        }

        /// <summary>
        /// Rotate wall orientation (for tap-to-rotate UI).
        /// </summary>
        public void RotateWall()
        {
            if (!_isPlacing) return;

            _currentOrientation = _currentOrientation == WallOrientation.Horizontal
                ? WallOrientation.Vertical
                : WallOrientation.Horizontal;

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_controller == null) return;

            int playerIndex = _controller.State.CurrentPlayerIndex;
            bool isValid = _controller.CanPlaceWall(playerIndex, _currentPosition, _currentOrientation);

            if (isValid != _lastValidState || true) // Always update for position changes
            {
                _lastValidState = isValid;
                GameEvents.RaiseWallPreviewUpdated(_currentPosition, _currentOrientation, isValid);
            }
        }

        private void Reset()
        {
            _isPlacing = false;
            _lastValidState = false;
            GameEvents.RaiseWallPlacementCancelled();
        }
    }
}
