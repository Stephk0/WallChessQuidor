using UnityEngine;

namespace WallChess.Pawn
{
    /// <summary>
    /// Event arguments for turn change notifications
    /// </summary>
    [System.Serializable]
    public class TurnChangeEventArgs
    {
        public int previousPlayerIndex;
        public int newPlayerIndex;
        public GameState gameState;
        
        public TurnChangeEventArgs(int previous, int current, GameState state)
        {
            previousPlayerIndex = previous;
            newPlayerIndex = current;
            gameState = state;
        }
    }
    
    /// <summary>
    /// Simple event broadcaster for turn changes
    /// Follows model-view principle by decoupling game logic from visual updates
    /// </summary>
    public static class TurnEventBroadcaster
    {
        public delegate void TurnChangeHandler(TurnChangeEventArgs eventArgs);
        public static event TurnChangeHandler OnTurnChanged;
        
        /// <summary>
        /// Broadcast turn change event (called by WallChessGameManager)
        /// </summary>
        public static void BroadcastTurnChange(int previousPlayer, int newPlayer, GameState state)
        {
            TurnChangeEventArgs args = new TurnChangeEventArgs(previousPlayer, newPlayer, state);
            OnTurnChanged?.Invoke(args);
        }
        
        /// <summary>
        /// Clear all event subscriptions (useful for scene cleanup)
        /// </summary>
        public static void ClearSubscriptions()
        {
            OnTurnChanged = null;
        }
    }
}