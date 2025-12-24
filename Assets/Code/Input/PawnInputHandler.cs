using System.Collections.Generic;
using WallChess.Core;
using WallChess.Core.Data;

namespace WallChess.Input
{
    /// <summary>
    /// Handles pawn selection and movement via drag or tap.
    /// </summary>
    public class PawnInputHandler : IInputHandler
    {
        private readonly GameController _controller;

        private int _selectedPawnIndex = -1;
        private BoardPosition _startPosition;
        private List<BoardPosition> _validMoves;
        private BoardPosition _lastHighlightedPosition;

        public PawnInputHandler(GameController controller)
        {
            _controller = controller;
        }

        public bool CanHandle(InputData input)
        {
            if (_controller?.State?.IsPlaying != true) return false;

            // Check if input is on current player's pawn
            if (input.HitType == HitType.Pawn)
            {
                int currentPlayer = _controller.State.CurrentPlayerIndex;
                return input.HitPlayerIndex == currentPlayer;
            }

            return false;
        }

        public void OnInputBegin(InputData input)
        {
            _selectedPawnIndex = _controller.State.CurrentPlayerIndex;
            _startPosition = _controller.GetCurrentPawnPosition();

            // Get and broadcast valid moves
            _validMoves = _controller.GetValidMoves(_selectedPawnIndex);
            _lastHighlightedPosition = new BoardPosition(-1, -1);

            GameEvents.RaisePawnSelected(_selectedPawnIndex);
            GameEvents.RaiseValidMovesCalculated(_validMoves);
        }

        public void OnInputMove(InputData input)
        {
            if (_selectedPawnIndex < 0 || _validMoves == null) return;

            // Update highlight if position changed
            if (input.IsValid && input.GridPosition != _lastHighlightedPosition)
            {
                _lastHighlightedPosition = input.GridPosition;
                // View will handle confirm highlight based on valid moves
            }
        }

        public void OnInputEnd(InputData input)
        {
            if (_selectedPawnIndex < 0)
            {
                Reset();
                return;
            }

            // Check if dropped on a valid move position
            if (input.IsValid && _validMoves != null && _validMoves.Contains(input.GridPosition))
            {
                // Execute move
                var result = _controller.TryMovePawn(_selectedPawnIndex, input.GridPosition);

                if (!result.IsSuccess)
                {
                    // Move failed - could show feedback
                    UnityEngine.Debug.LogWarning($"Move failed: {result.FailureReason}");
                }
            }

            Reset();
        }

        public void OnCancel()
        {
            Reset();
        }

        private void Reset()
        {
            _selectedPawnIndex = -1;
            _validMoves = null;
            _lastHighlightedPosition = new BoardPosition(-1, -1);

            GameEvents.RaisePawnDeselected();
            GameEvents.RaiseValidMovesCleared();
        }
    }
}
