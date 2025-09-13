# Part 5: High Priority - God Object Extraction
**WallChessQuidor Refactoring Implementation Guide**

**Page:** 46-54 of 101  
**Document:** 05-god-object-extraction.md  
**Priority:** 🟠 HIGH  
**Estimated Time:** 3-4 days  
**Prerequisites:** [Part 2: State Management](02-state-management-consolidation.md)  
**Next Document:** [Part 7: Input System](07-input-system-unification.md)  

---

## The God Object Problem

Currently, `WallChessGameManager` does EVERYTHING:
- Manages game state (should be StateManager's job)
- Handles input (should be InputManager's job)  
- Coordinates AI (should be AIController's job)
- Updates UI (should be UIManager's job)
- Renders pieces (should be RenderManager's job)

**Result**: 1000+ line monster class that's impossible to test or maintain.

**Goal**: Break it into focused, testable managers that each do one thing well.

---

## User Stories

### Story 1: Extract Turn Management
**As a developer, I want turn logic separated so I can easily modify game flow rules.**

**Acceptance Criteria:**
- `TurnManager` handles whose turn it is
- Manages turn timers and timeouts
- Handles turn transitions cleanly
- Easy to add new turn types (e.g. wall placement)

**Create the TurnManager:**
```csharp
// Assets/_Project/00_Core/Managers/TurnManager.cs
using UnityEngine;
using System;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    
    [Header("Turn Settings")]
    public float playerTurnTime = 30f;
    public float aiThinkTime = 2f;
    
    public PlayerType CurrentPlayer { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool IsTurnActive { get; private set; }
    
    public event Action<PlayerType> OnTurnStarted;
    public event Action<PlayerType> OnTurnEnded;
    public event Action OnTurnTimedOut;
    
    void Awake()
    {
        Instance = this;
    }
    
    public void StartPlayerTurn()
    {
        CurrentPlayer = PlayerType.Human;
        TimeRemaining = playerTurnTime;
        IsTurnActive = true;
        OnTurnStarted?.Invoke(CurrentPlayer);
    }
    
    public void StartAITurn()
    {
        CurrentPlayer = PlayerType.AI;
        TimeRemaining = aiThinkTime;
        IsTurnActive = true;
        OnTurnStarted?.Invoke(CurrentPlayer);
    }
    
    public void EndCurrentTurn()
    {
        if (!IsTurnActive) return;
        
        IsTurnActive = false;
        OnTurnEnded?.Invoke(CurrentPlayer);
    }
    
    void Update()
    {
        if (!IsTurnActive) return;
        
        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0)
        {
            OnTurnTimedOut?.Invoke();
            EndCurrentTurn();
        }
    }
}
```

### Story 2: Extract Movement Coordination
**As a developer, I want piece movement logic isolated so I can easily add new piece types.**

**Acceptance Criteria:**
- `MovementManager` handles all piece movement
- Validates moves before executing
- Coordinates with animation system
- Handles special moves (castling, en passant, etc.)

**Create the MovementManager:**
```csharp
// Assets/_Project/01_GameLogic/Managers/MovementManager.cs
using UnityEngine;
using System;

public class MovementManager : MonoBehaviour
{
    public static MovementManager Instance { get; private set; }
    
    [Header("Dependencies")]
    public ChessBoard chessBoard;
    public RulesEngine rulesEngine;
    
    public event Action<Move, PlayerType> OnMoveExecuted;
    public event Action<Move> OnMoveValidated;
    public event Action<Move, string> OnMoveRejected;
    
    void Awake()
    {
        Instance = this;
    }
    
    public bool TryExecuteMove(Move move, PlayerType player)
    {
        // Validate move
        if (!rulesEngine.IsValidMove(move, player))
        {
            OnMoveRejected?.Invoke(move, "Invalid move according to rules");
            return false;
        }
        
        if (!chessBoard.IsPathClear(move))
        {
            OnMoveRejected?.Invoke(move, "Path is blocked");
            return false;
        }
        
        // Execute move
        chessBoard.ExecuteMove(move);
        OnMoveExecuted?.Invoke(move, player);
        OnMoveValidated?.Invoke(move);
        
        return true;
    }
    
    public Move[] GetValidMoves(PlayerType player)
    {
        return rulesEngine.GetAllValidMoves(chessBoard.GetBoardState(), player);
    }
    
    public bool IsInCheck(PlayerType player)
    {
        return rulesEngine.IsInCheck(chessBoard.GetBoardState(), player);
    }
    
    public bool IsCheckmate(PlayerType player)
    {
        return rulesEngine.IsCheckmate(chessBoard.GetBoardState(), player);
    }
}
```

### Story 3: Extract Wall Management
**As a developer, I want wall placement logic separate so I can modify wall rules easily.**

**Acceptance Criteria:**
- `WallManager` handles all wall-related logic
- Validates wall placements
- Manages wall inventory
- Coordinates with pathfinding system

**Create the WallManager:**
```csharp
// Assets/_Project/01_GameLogic/Managers/WallManager.cs
using UnityEngine;
using System.Collections.Generic;
using System;

public class WallManager : MonoBehaviour
{
    public static WallManager Instance { get; private set; }
    
    [Header("Wall Settings")]
    public int wallsPerPlayer = 10;
    
    private Dictionary<PlayerType, int> wallCounts = new Dictionary<PlayerType, int>();
    private List<WallPlacement> placedWalls = new List<WallPlacement>();
    
    public event Action<WallPlacement, PlayerType> OnWallPlaced;
    public event Action<PlayerType, int> OnWallCountChanged;
    
    void Awake()
    {
        Instance = this;
        ResetWallCounts();
    }
    
    public void ResetWallCounts()
    {
        wallCounts[PlayerType.Human] = wallsPerPlayer;
        wallCounts[PlayerType.AI] = wallsPerPlayer;
        placedWalls.Clear();
    }
    
    public bool CanPlaceWall(PlayerType player)
    {
        return wallCounts[player] > 0;
    }
    
    public bool TryPlaceWall(WallPlacement placement, PlayerType player)
    {
        if (!CanPlaceWall(player))
        {
            Debug.Log($"{player} has no walls left!");
            return false;
        }
        
        if (!IsValidWallPlacement(placement))
        {
            Debug.Log("Invalid wall placement!");
            return false;
        }
        
        // Place the wall
        placedWalls.Add(placement);
        wallCounts[player]--;
        
        OnWallPlaced?.Invoke(placement, player);
        OnWallCountChanged?.Invoke(player, wallCounts[player]);
        
        return true;
    }
    
    private bool IsValidWallPlacement(WallPlacement placement)
    {
        // Check if wall intersects with existing walls
        foreach (var existingWall in placedWalls)
        {
            if (placement.IntersectsWith(existingWall))
                return false;
        }
        
        // Check if wall blocks all paths to goal
        // This is where pathfinding validation would go
        return !placement.BlocksAllPaths();
    }
    
    public int GetWallCount(PlayerType player)
    {
        return wallCounts[player];
    }
    
    public WallPlacement[] GetAllWalls()
    {
        return placedWalls.ToArray();
    }
}
```

### Story 4: Extract Game Flow Coordination
**As a developer, I want game flow logic centralized so I can easily modify game rules.**

**Acceptance Criteria:**
- `GameFlowManager` coordinates all other managers
- Handles win/loss conditions
- Manages game phases (setup, playing, ended)
- Orchestrates complex interactions

**Create the GameFlowManager:**
```csharp
// Assets/_Project/00_Core/Managers/GameFlowManager.cs
using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }
    
    [Header("Dependencies")]
    public TurnManager turnManager;
    public MovementManager movementManager;
    public WallManager wallManager;
    public UIManager uiManager;
    
    private UnifiedStateManager stateManager;
    
    void Awake()
    {
        Instance = this;
        stateManager = UnifiedStateManager.Instance;
    }
    
    void Start()
    {
        SetupEventListeners();
    }
    
    private void SetupEventListeners()
    {
        // Turn events
        turnManager.OnTurnStarted += HandleTurnStarted;
        turnManager.OnTurnEnded += HandleTurnEnded;
        turnManager.OnTurnTimedOut += HandleTurnTimeout;
        
        // Move events
        movementManager.OnMoveExecuted += HandleMoveExecuted;
        movementManager.OnMoveRejected += HandleMoveRejected;
        
        // Wall events
        wallManager.OnWallPlaced += HandleWallPlaced;
    }
    
    public void StartNewGame()
    {
        // Reset all systems
        wallManager.ResetWallCounts();
        movementManager.chessBoard.ResetBoard();
        
        // Start first turn
        stateManager.TransitionToState(GameStateType.Playing);
        turnManager.StartPlayerTurn();
    }
    
    private void HandleTurnStarted(PlayerType player)
    {
        if (player == PlayerType.Human)
        {
            stateManager.TransitionToState(GameStateType.PlayerTurn);
            uiManager.ShowPlayerTurnUI();
        }
        else
        {
            stateManager.TransitionToState(GameStateType.AITurn);
            uiManager.ShowAITurnUI();
        }
    }
    
    private void HandleTurnEnded(PlayerType player)
    {
        // Check win conditions
        if (CheckWinCondition(out var winner))
        {
            EndGame(winner);
            return;
        }
        
        // Switch to next player
        if (player == PlayerType.Human)
            turnManager.StartAITurn();
        else
            turnManager.StartPlayerTurn();
    }
    
    private void HandleMoveExecuted(Move move, PlayerType player)
    {
        uiManager.AnimateMove(move);
        uiManager.UpdateMoveHistory(move, player);
        
        // End turn after move
        turnManager.EndCurrentTurn();
    }
    
    private void HandleWallPlaced(WallPlacement wall, PlayerType player)
    {
        uiManager.AnimateWallPlacement(wall);
        uiManager.UpdateWallCounts(player, wallManager.GetWallCount(player));
        
        // End turn after wall placement
        turnManager.EndCurrentTurn();
    }
    
    private bool CheckWinCondition(out PlayerType winner)
    {
        winner = PlayerType.None;
        
        // Check if human reached goal
        if (movementManager.chessBoard.HasPlayerReachedGoal(PlayerType.Human))
        {
            winner = PlayerType.Human;
            return true;
        }
        
        // Check if AI reached goal
        if (movementManager.chessBoard.HasPlayerReachedGoal(PlayerType.AI))
        {
            winner = PlayerType.AI;
            return true;
        }
        
        // Check if either player is blocked
        if (movementManager.chessBoard.IsPlayerBlocked(PlayerType.Human))
        {
            winner = PlayerType.AI;
            return true;
        }
        
        if (movementManager.chessBoard.IsPlayerBlocked(PlayerType.AI))
        {
            winner = PlayerType.Human;
            return true;
        }
        
        return false;
    }
    
    private void EndGame(PlayerType winner)
    {
        if (winner == PlayerType.Human)
            stateManager.TransitionToState(GameStateType.Victory);
        else
            stateManager.TransitionToState(GameStateType.Defeat);
        
        uiManager.ShowGameEndScreen(winner);
    }
}
```

### Story 5: Replace the God Object
**As a developer, I want the old GameManager completely replaced so the architecture is clean.**

**Acceptance Criteria:**
- Old `WallChessGameManager` deleted entirely
- All references updated to use new managers
- Game works exactly the same as before
- Code is much easier to understand and modify

**Migration Steps:**

1. **Identify Dependencies**: Find all scripts that reference `WallChessGameManager`
```csharp
// Find these patterns and replace them:

// OLD WAY:
WallChessGameManager.Instance.ExecuteMove(move);
WallChessGameManager.Instance.IsPlayerTurn();
WallChessGameManager.Instance.PlaceWall(wall);

// NEW WAY:
MovementManager.Instance.TryExecuteMove(move, currentPlayer);
TurnManager.Instance.CurrentPlayer == PlayerType.Human;
WallManager.Instance.TryPlaceWall(wall, currentPlayer);
```

2. **Create Migration Scene**:
   - Create new GameObject: "Managers"
   - Add all new managers as child objects
   - Configure references between managers
   - Test that everything still works

3. **Update References Gradually**:
   - Start with UI scripts (lowest risk)
   - Then input handlers
   - Then AI system
   - Finally remove the old manager

---

## Testing the New Architecture

### Integration Test
```csharp
// Test that a complete game works with new managers
[UnityTest]
public IEnumerator TestCompleteGameFlow()
{
    var gameFlow = GameFlowManager.Instance;
    
    // Start game
    gameFlow.StartNewGame();
    yield return new WaitForSeconds(0.1f);
    
    // Verify initial state
    Assert.AreEqual(PlayerType.Human, TurnManager.Instance.CurrentPlayer);
    Assert.AreEqual(10, WallManager.Instance.GetWallCount(PlayerType.Human));
    
    // Make a player move
    var move = new Move(new Vector2Int(4, 0), new Vector2Int(4, 1));
    bool moveSuccess = MovementManager.Instance.TryExecuteMove(move, PlayerType.Human);
    Assert.IsTrue(moveSuccess);
    
    yield return new WaitForSeconds(0.5f);
    
    // Should now be AI's turn
    Assert.AreEqual(PlayerType.AI, TurnManager.Instance.CurrentPlayer);
}
```

### Unit Test Each Manager
```csharp
[Test]
public void WallManager_PlacesWallCorrectly()
{
    var wallManager = WallManager.Instance;
    var initialCount = wallManager.GetWallCount(PlayerType.Human);
    
    var wall = new WallPlacement(new Vector2Int(2, 2), WallOrientation.Horizontal);
    bool success = wallManager.TryPlaceWall(wall, PlayerType.Human);
    
    Assert.IsTrue(success);
    Assert.AreEqual(initialCount - 1, wallManager.GetWallCount(PlayerType.Human));
}
```

---

## Manager Setup Checklist

### Required GameObjects Structure
```
Managers/
├── TurnManager
├── MovementManager  
├── WallManager
├── GameFlowManager (coordinates others)
└── PoolManager (from Part 4)
```

### Configuration Steps
1. Create the Managers parent object
2. Add each manager as a child component
3. Configure references in inspector:
   - GameFlowManager needs references to all others
   - MovementManager needs ChessBoard and RulesEngine
   - UI references need updating
4. Test in play mode before removing old manager

### Migration Validation
- [ ] Game starts without errors
- [ ] Player can make moves
- [ ] AI makes moves  
- [ ] Walls can be placed
- [ ] Win conditions work
- [ ] UI updates correctly
- [ ] Turn timer works
- [ ] Performance is same or better

---

## Expected Results

After extracting the god object:

- **Much easier to modify** individual systems
- **Testable components** that can be unit tested
- **Clear separation of concerns** - each manager has one job
- **Easier debugging** - problems are isolated to specific managers
- **Better performance** - no mega-Update() method doing everything

The code becomes maintainable and extensible instead of a nightmare to modify.

---

**Time Investment**: 3-4 days  
**Risk Level**: Medium (touching core game logic)  
**Impact**: Very High (transforms entire architecture)

---

**Page 54 of 54**  
**Next Document**: [Part 7: Input System Unification](07-input-system-unification.md)  
**Previous Document**: [Part 4: Object Pooling](04-object-pooling-implementation.md)  
**Master Index**: [00-master-index.md](00-master-index.md)  

---
*WallChessQuidor Refactoring Implementation Guide - Break apart the monster, build focused systems*