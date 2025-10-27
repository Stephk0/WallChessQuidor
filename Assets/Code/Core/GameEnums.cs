using UnityEngine;

namespace WallChess.Core
{
    /// <summary>
    /// Game state enumeration moved from WallChessGameManager
    /// Now managed by proper state management systems
    /// </summary>
    public enum GameState
    {
        GameStart,
        BuildTiles,
        PlayerTurn,
        PawnMoving,
        WallPlacement,
        GameOver
    }

    /// <summary>
    /// Action type enumeration moved from WallChessGameManager
    /// Used for tracking current player action state
    /// </summary>
    
    /// <summary>
    /// Legacy PawnData structure for backward compatibility
    /// Originally in WallChessGameManager, moved here for shared access
    /// Consider migrating to PawnManager system for new code
    /// </summary>
    [System.Serializable]
    public class PawnData
    {
        public Vector2Int position;
        public Vector2Int startPosition;
        public Vector2Int winPosition;
        public int wallsRemaining;
        public GameObject avatar;
        public bool isActive;
        public bool isAI;
        
        public PawnData(Vector2Int start, Vector2Int win, int walls)
        {
            startPosition = start;
            position = start;
            winPosition = win;
            wallsRemaining = walls;
            isActive = false;
            isAI = false;
            avatar = null;
        }
    }
public enum ActionType
    {
        Idle,
        MovingPawn,
        PlacingWall
    }
}