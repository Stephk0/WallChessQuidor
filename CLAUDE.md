# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WallChess is a Unity-based strategic board game inspired by Quoridor, featuring pawn movement and wall placement mechanics. The project is actively being refactored from a monolithic architecture to a clean, modular state machine-based system.

## Commands

### Unity Development
- Open project in Unity Editor (Unity 2023.3+)
- Use Unity's built-in testing framework via **Window > General > Test Runner**
- Build via **File > Build Settings**

### Testing and Debugging
The project includes extensive testing components in `Assets/Code/Testing/`:
- Use Unity's **Window > General > Console** for runtime logs
- Many managers have **[ContextMenu]** debug actions accessible via component right-click
- Enable debug modes on various managers for detailed logging

### Key Unity Packages
- Unity Input System (for input handling)
- Universal Render Pipeline (URP) for rendering
- Unity Test Framework for testing
- Custom MCP bridge package (`com.coplaydev.unity-mcp`)

## Architecture Overview

### State Management Architecture (Refactored)
The project uses **three independent state machines** for clean separation of concerns:

1. **Main Application FSM** (`MainApplicationController`)
   - Manages: Start → Menu → Settings → Game → GameOver → Menu
   - High-level application flow
   - Coordinates with game session

2. **Game Session FSM** (`SessionStateManager`) 
   - Manages: Start → BuildTiles → SpawnPawns → Gameplay → GameOver
   - Handles game initialization sequence
   - Ensures proper component initialization timing
   - **Critical**: Resolves pawn dragging issues by proper initialization order

3. **Player Action FSM** (`PlayerActionStateMachine`)
   - Manages: Idle → MovingPawn/PlacingWall → Confirm/Cancel → Idle
   - Per-player action state management
   - Used by both human players (`PawnController`) and AI (`AIOpponent`)

### Core Systems

#### Game Management
- **`WallChessGameManager`**: Central coordinator (refactored to be event-driven, no longer monolithic)
- **`SessionStateManager`**: Orchestrates game flow and initialization
- **`MainApplicationController`**: High-level application state management

#### Player System
- **`PawnManager`**: Manages all pawns and turn logic
- **`PawnController`**: Handles human player input and pawn movement
- **`AIOpponent`**: AI player implementation
- **`Pawn`**: Individual pawn entities with movement and victory logic

#### Grid & Walls
- **`GridSystem`**: Core grid management and coordinate system
- **`WallManager`**: Wall placement and validation
- **`WallValidator`**: Wall placement rule enforcement
- **`GridPathfinder`**: Pathfinding for pawn movement validation

#### Configuration System
Uses ScriptableObjects for clean configuration:
- **`GameSettings`**: Core game rules (grid size, walls per player, etc.)
- **`GridSettings`**: Visual and layout settings
- **`PrefabReferences`**: Centralized prefab management
- **`DebugSettings`**: Development and testing configuration

### Key Architectural Principles

#### Event-Driven Communication
The refactored system uses events for loose coupling:
```csharp
// Wall placement events
WallChessGameManager.OnWallPlacedComplete
// Turn management events  
WallChessGameManager.OnPlayerTurnChanged
// Pawn movement events
WallChessGameManager.OnPawnMoveComplete
// Victory events
WallChessGameManager.OnPlayerVictory
```

#### State Machine Integration
- **No state conflicts**: Each FSM manages its own domain
- **Proper initialization order**: Session states ensure components initialize in correct sequence
- **Clean transitions**: States handle entry/exit logic properly

#### Legacy Compatibility
The refactor maintains backward compatibility:
- Legacy methods redirect to new systems
- Existing APIs preserved where possible
- ScriptableObject configuration system added without breaking existing setups

## Important Implementation Notes

### Pawn Movement System
The project was recently fixed to resolve pawn dragging issues:
- **Root cause**: `PawnController` was initialized before pawns existed
- **Solution**: `SessionStateManager` ensures proper initialization order
- **Implementation**: `SpawnPawnsState` creates pawns, then initializes controllers

### Turn Management 
Turn advancement is controlled by action confirmation:
- Human: Drag pawn → Release → Confirm action → Advance turn
- AI: Decide move → Execute → Confirm action → Advance turn
- Wall placement follows same pattern

### Debug Mode
Many systems support debug mode via `DebugSettings.debugMode`:
- Allows any pawn to move regardless of turn
- Bypasses various game rule restrictions
- Useful for testing and development

### Testing Components
The project includes extensive testing infrastructure:
- Components in `Assets/Code/Testing/` for various system validation
- Context menu debug actions on most managers
- Compilation validation systems
- FSM integration testers

## Common Development Tasks

### Adding New Game Features
1. Determine which state machine(s) are involved
2. Create new states if needed (inherit from `BaseState` or implement `IState`)
3. Add events for communication between systems
4. Update relevant managers to handle new feature
5. Add configuration to appropriate ScriptableObject

### Debugging State Issues
1. Enable debug logs on relevant state machines
2. Use context menu debug actions on managers
3. Check Unity Console for state transition logs
4. Use Testing components to isolate issues

### Modifying Game Rules
1. Update relevant ScriptableObject (likely `GameSettings`)
2. Ensure validators and managers respect new rules
3. Test with debug mode enabled/disabled
4. Validate with Testing components

## Architecture Evolution

The project is transitioning from:
- **Before**: Monolithic `WallChessGameManager` handling everything
- **After**: Modular state machines with clear responsibilities

This refactor improves:
- Maintainability and debugging
- Feature extensibility  
- Component initialization reliability
- Separation of concerns
- Testing capabilities

The documentation in `Assets/Code/Documentation/` provides detailed explanations of the architectural changes and their benefits.