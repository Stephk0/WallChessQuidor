# Part 3: High Priority - Grid System Simplification
**WallChessQuidor Refactoring Implementation Guide**

**Page:** 21-34 of 101  
**Document:** 03-grid-system-simplification.md  
**Priority:** 🟠 HIGH  
**Estimated Time:** 4-5 days  
**Prerequisites:** [Part 2: State Management](02-state-management-consolidation.md)  
**Next Document:** [Part 4: Object Pooling](04-object-pooling-implementation.md)  

---

## The Grid System Problem

Currently, WallChessQuidor juggles THREE different coordinate systems:
1. **Unity World Coordinates** (Vector3 with floating point)
2. **Grid Array Indices** (int[,] for board data)
3. **Chess Algebraic Notation** (strings like "e4", "d7")

**Result**: Constant conversions, bugs, and confusion about which system to use when.

**Goal**: Unify to ONE coordinate system with clean conversion utilities.

---

## User Stories

### Story 1: Choose One Primary System
**As a developer, I want one clear coordinate system so I don't waste time on conversions.**

**Decision**: Use **Grid Coordinates** (Vector2Int) as the primary system.

**Why Grid Coordinates?**
- Matches game logic (8x8 board)
- Integer values (no floating point errors)
- Easy to validate bounds
- Natural for array indexing

**Implementation:**
```csharp
// Assets/_Project/01_GameLogic/Board/GridCoordinate.cs
using UnityEngine;

[System.Serializable]
public struct GridCoordinate
{
    public int x, y;
    
    public GridCoordinate(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
    
    public GridCoordinate(Vector2Int vector)
    {
        this.x = vector.x;
        this.y = vector.y;
    }
    
    // Easy conversion to Vector2Int
    public Vector2Int ToVector2Int() => new Vector2Int(x, y);
    
    // Validate bounds
    public bool IsValid() => x >= 0 && x < 8 && y >= 0 && y < 8;
    
    // Chess notation conversion
    public string ToChessNotation()
    {
        if (!IsValid()) return "invalid";
        
        char file = (char)('a' + x);
        char rank = (char)('1' + y);
        return $"{file}{rank}";
    }
    
    public static GridCoordinate FromChessNotation(string notation)
    {
        if (notation.Length != 2) return new GridCoordinate(-1, -1);
        
        int x = notation[0] - 'a';
        int y = notation[1] - '1';
        
        return new GridCoordinate(x, y);
    }
    
    // Unity world conversion
    public Vector3 ToWorldPosition(float tileSize = 1f, Vector3 boardOrigin = default)
    {
        return boardOrigin + new Vector3(x * tileSize, 0, y * tileSize);
    }
    
    public static GridCoordinate FromWorldPosition(Vector3 worldPos, float tileSize = 1f, Vector3 boardOrigin = default)
    {
        Vector3 localPos = worldPos - boardOrigin;
        int x = Mathf.RoundToInt(localPos.x / tileSize);
        int y = Mathf.RoundToInt(localPos.z / tileSize);
        
        return new GridCoordinate(x, y);
    }
    
    public override string ToString() => $"({x}, {y})";
    
    // Operators for easy math
    public static GridCoordinate operator +(GridCoordinate a, GridCoordinate b)
        => new GridCoordinate(a.x + b.x, a.y + b.y);
    
    public static GridCoordinate operator -(GridCoordinate a, GridCoordinate b)
        => new GridCoordinate(a.x - b.x, a.y - b.y);
    
    public static bool operator ==(GridCoordinate a, GridCoordinate b)
        => a.x == b.x && a.y == b.y;
    
    public static bool operator !=(GridCoordinate a, GridCoordinate b)
        => !(a == b);
    
    public override bool Equals(object obj)
        => obj is GridCoordinate coord && this == coord;
    
    public override int GetHashCode()
        => (x << 16) | y;
}
```

### Story 2: Replace All Vector2Int Usage
**As a developer, I want consistent coordinate types throughout the codebase.**

**Acceptance Criteria:**
- Replace `Vector2Int` with `GridCoordinate` in game logic
- Update all method signatures
- Ensure board access uses grid coordinates
- Maintain same functionality

**Find and Replace Pattern:**
```csharp
// OLD WAY:
public bool IsValidMove(Vector2Int from, Vector2Int to)
{
    if (from.x < 0 || from.x >= 8) return false;
    // ... lots of validation
}

// NEW WAY:
public bool IsValidMove(GridCoordinate from, GridCoordinate to)
{
    if (!from.IsValid() || !to.IsValid()) return false;
    // ... simpler validation
}

// OLD WAY:
Vector2Int pos = new Vector2Int(3, 4);
boardArray[pos.y, pos.x] = piece; // Easy to mix up x,y!

// NEW WAY:
GridCoordinate pos = new GridCoordinate(3, 4);
boardArray[pos.y, pos.x] = piece; // Still clear which is which
```

### Story 3: Simplify Board Access
**As a developer, I want safe, easy access to board tiles without coordinate confusion.**

**Acceptance Criteria:**
- Board class uses GridCoordinate everywhere
- Safe accessors that validate bounds
- Clear error messages for invalid coordinates
- Easy iteration over board positions

**Implementation:**
```csharp
// Assets/_Project/01_GameLogic/Board/ChessBoard.cs
using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    private ChessPiece[,] board = new ChessPiece[8, 8];
    private WallPlacement[] walls;
    
    [Header("Visual Settings")]
    public float tileSize = 1f;
    public Vector3 boardOrigin = Vector3.zero;
    
    // Safe board access
    public ChessPiece GetPieceAt(GridCoordinate coord)
    {
        if (!coord.IsValid())
        {
            Debug.LogWarning($"Invalid coordinate: {coord}");
            return null;
        }
        
        return board[coord.y, coord.x];
    }
    
    public void SetPieceAt(GridCoordinate coord, ChessPiece piece)
    {
        if (!coord.IsValid())
        {
            Debug.LogError($"Cannot place piece at invalid coordinate: {coord}");
            return;
        }
        
        board[coord.y, coord.x] = piece;
    }
    
    public bool IsEmpty(GridCoordinate coord)
    {
        return GetPieceAt(coord) == null;
    }
    
    public bool IsOccupied(GridCoordinate coord)
    {
        return !IsEmpty(coord);
    }
    
    // Easy iteration
    public void ForEachTile(System.Action<GridCoordinate, ChessPiece> action)
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                GridCoordinate coord = new GridCoordinate(x, y);
                action(coord, GetPieceAt(coord));
            }
        }
    }
    
    // World position conversion
    public Vector3 GetWorldPosition(GridCoordinate coord)
    {
        return coord.ToWorldPosition(tileSize, boardOrigin);
    }
    
    public GridCoordinate GetGridCoordinate(Vector3 worldPos)
    {
        return GridCoordinate.FromWorldPosition(worldPos, tileSize, boardOrigin);
    }
    
    // Validate moves
    public bool IsValidPosition(GridCoordinate coord)
    {
        return coord.IsValid();
    }
    
    public bool IsPathClear(GridCoordinate from, GridCoordinate to)
    {
        // Simple path checking - can be enhanced
        GridCoordinate direction = to - from;
        
        // Normalize direction to steps
        int stepX = direction.x == 0 ? 0 : direction.x > 0 ? 1 : -1;
        int stepY = direction.y == 0 ? 0 : direction.y > 0 ? 1 : -1;
        
        GridCoordinate current = from + new GridCoordinate(stepX, stepY);
        
        while (current != to)
        {
            if (IsOccupied(current)) return false;
            current = current + new GridCoordinate(stepX, stepY);
        }
        
        return true;
    }
}
```

### Story 4: Update Input System
**As a player, I want clicking on tiles to work correctly with the new coordinate system.**

**Acceptance Criteria:**
- Mouse clicks convert to grid coordinates correctly
- Touch input works with grid system
- Visual feedback uses consistent coordinates
- No more "click on wrong tile" bugs

**Implementation:**
```csharp
// Assets/_Project/02_Input/Handlers/BoardInputHandler.cs
using UnityEngine;

public class BoardInputHandler : MonoBehaviour
{
    [Header("Dependencies")]
    public ChessBoard chessBoard;
    public Camera gameCamera;
    
    [Header("Settings")]
    public LayerMask boardLayerMask = 1;
    
    private GridCoordinate selectedTile = new GridCoordinate(-1, -1);
    private bool hasTileSelected = false;
    
    void Update()
    {
        HandleMouseInput();
    }
    
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GridCoordinate clickedTile = GetTileUnderMouse();
            
            if (clickedTile.IsValid())
            {
                HandleTileClick(clickedTile);
            }
        }
    }
    
    private GridCoordinate GetTileUnderMouse()
    {
        Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, boardLayerMask))
        {
            return chessBoard.GetGridCoordinate(hit.point);
        }
        
        return new GridCoordinate(-1, -1); // Invalid coordinate
    }
    
    private void HandleTileClick(GridCoordinate coord)
    {
        if (!hasTileSelected)
        {
            // First click - select piece
            if (chessBoard.IsOccupied(coord))
            {
                SelectTile(coord);
            }
        }
        else
        {
            // Second click - try to move
            if (coord == selectedTile)
            {
                // Clicked same tile - deselect
                DeselectTile();
            }
            else
            {
                // Try to move to new tile
                AttemptMove(selectedTile, coord);
                DeselectTile();
            }
        }
    }
    
    private void SelectTile(GridCoordinate coord)
    {
        selectedTile = coord;
        hasTileSelected = true;
        
        // Visual feedback
        HighlightTile(coord);
        ShowValidMoves(coord);
    }
    
    private void DeselectTile()
    {
        if (hasTileSelected)
        {
            UnhighlightTile(selectedTile);
            HideValidMoves();
        }
        
        selectedTile = new GridCoordinate(-1, -1);
        hasTileSelected = false;
    }
    
    private void AttemptMove(GridCoordinate from, GridCoordinate to)
    {
        var move = new Move(from, to);
        MovementManager.Instance.TryExecuteMove(move, TurnManager.Instance.CurrentPlayer);
    }
    
    private void HighlightTile(GridCoordinate coord)
    {
        // Use object pooling from Part 4
        var highlight = PoolManager.Instance.Get<TileHighlight>("TileHighlight");
        highlight.transform.position = chessBoard.GetWorldPosition(coord);
        highlight.Show();
    }
    
    private void ShowValidMoves(GridCoordinate coord)
    {
        var piece = chessBoard.GetPieceAt(coord);
        if (piece == null) return;
        
        var validMoves = piece.GetValidMoves(chessBoard);
        
        foreach (var move in validMoves)
        {
            var moveHighlight = PoolManager.Instance.Get<MoveHighlight>("MoveHighlight");
            moveHighlight.transform.position = chessBoard.GetWorldPosition(move.to);
            moveHighlight.Show();
        }
    }
}
```

### Story 5: Update AI System
**As an AI player, I want to use the same coordinate system as the human player.**

**Acceptance Criteria:**
- AI calculations use GridCoordinate
- Move generation works with new system
- Position evaluation uses consistent coordinates
- No coordinate conversion bugs

**Implementation:**
```csharp
// Assets/_Project/03_AI/Core/AIBoardEvaluator.cs
using UnityEngine;

public class AIBoardEvaluator
{
    public float EvaluatePosition(ChessBoard board, PlayerType player)
    {
        float score = 0f;
        
        board.ForEachTile((coord, piece) => {
            if (piece != null)
            {
                float pieceValue = GetPieceValue(piece, coord, board);
                
                if (piece.Owner == player)
                    score += pieceValue;
                else
                    score -= pieceValue;
            }
        });
        
        return score;
    }
    
    private float GetPieceValue(ChessPiece piece, GridCoordinate coord, ChessBoard board)
    {
        float baseValue = GetBasePieceValue(piece);
        float positionValue = GetPositionValue(piece, coord);
        
        return baseValue + positionValue;
    }
    
    private float GetPositionValue(ChessPiece piece, GridCoordinate coord)
    {
        // Pawns are more valuable closer to goal
        if (piece is Pawn)
        {
            if (piece.Owner == PlayerType.Human)
            {
                // Human goal is top of board (y=7)
                return coord.y * 0.1f;
            }
            else
            {
                // AI goal is bottom of board (y=0) 
                return (7 - coord.y) * 0.1f;
            }
        }
        
        // Center control is valuable
        float centerDistance = Vector2.Distance(coord.ToVector2Int(), new Vector2(3.5f, 3.5f));
        return Mathf.Max(0, 3f - centerDistance) * 0.05f;
    }
}
```

---

## Migration Checklist

### Phase 1: Create GridCoordinate System
- [ ] Implement GridCoordinate struct
- [ ] Add conversion methods
- [ ] Write unit tests for coordinate math
- [ ] Test chess notation conversion

### Phase 2: Update Board System  
- [ ] Replace Vector2Int with GridCoordinate in ChessBoard
- [ ] Update all board access methods
- [ ] Test board initialization and piece placement
- [ ] Verify world position conversion

### Phase 3: Update Game Logic
- [ ] Update Move struct to use GridCoordinate
- [ ] Fix all movement validation code
- [ ] Update rules engine
- [ ] Test piece movement

### Phase 4: Update Input System
- [ ] Convert mouse/touch input to GridCoordinate
- [ ] Update tile selection logic
- [ ] Fix move highlighting
- [ ] Test input responsiveness

### Phase 5: Update AI System
- [ ] Convert AI move generation
- [ ] Update position evaluation
- [ ] Fix minimax algorithm
- [ ] Test AI decision making

### Phase 6: Clean Up
- [ ] Remove old conversion utilities
- [ ] Update all debug logs to use new format
- [ ] Run full integration tests
- [ ] Document the new coordinate system

---

## Common Migration Issues

**Issue**: "Index out of bounds" errors
**Solution**: GridCoordinate validation catches these early

**Issue**: Confusion between x,y and row,column  
**Solution**: GridCoordinate makes this explicit

**Issue**: Floating point precision errors in world positions
**Solution**: Clean conversion methods handle precision

**Issue**: Chess notation not working
**Solution**: Built-in conversion methods handle edge cases

---

## Expected Results

After grid system simplification:

- **50% fewer coordinate conversion bugs**
- **Easier to understand code** - one coordinate system
- **Better performance** - no constant conversions
- **Easier debugging** - clear coordinate representation
- **Simpler AI logic** - consistent coordinate math

The coordinate system becomes a tool that helps rather than hinders development.

---

**Time Investment**: 4-5 days  
**Risk Level**: Medium (touches all systems)  
**Impact**: High (affects entire codebase)

---

**Page 34 of 34**  
**Next Document**: [Part 4: Object Pooling Implementation](04-object-pooling-implementation.md)  
**Previous Document**: [Part 2: State Management](02-state-management-consolidation.md)  
**Master Index**: [00-master-index.md](00-master-index.md)  

---
*WallChessQuidor Refactoring Implementation Guide - One coordinate system to rule them all*