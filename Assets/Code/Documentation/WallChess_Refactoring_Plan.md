# Wall Chess - Comprehensive Refactoring Plan

## Executive Summary
The Wall Chess (Quoridor) codebase requires systematic refactoring to address architectural issues, performance bottlenecks, and technical debt. This document outlines a 4-week phased approach to transform the codebase while maintaining functionality.

**Document Version**: 1.0  
**Date**: December 2024  
**Project Path**: `D:\WMH\Prototypes\WallChess\WallChessQuidor\Assets`

---

## Table of Contents
1. [Critical Issues](#critical-issues)
2. [Refactoring Plan](#refactoring-plan)
3. [Execution Flow Improvements](#execution-flow-improvements)
4. [Script-Specific Notes](#script-specific-notes)
5. [Performance Optimizations](#performance-optimizations)
6. [Implementation Timeline](#implementation-timeline)
7. [Best Practices](#best-practices)
8. [Immediate Actions](#immediate-actions)

---

## Critical Issues

### 1. Architecture & Design Patterns
- **No proper State Machine**: GameState enum exists but no actual FSM implementation
- **Mixed responsibilities**: WallChessGameManager is 800+ lines handling everything
- **No object pooling**: Creating/destroying objects at runtime (violates mobile performance requirements)
- **Legacy code pollution**: Backup files, MVP folder, multiple versions of same scripts
- **Circular dependencies**: Components reference each other directly without abstraction

### 2. Performance Issues
- **String operations**: Extensive string usage in Update loops for debugging
- **No object pooling**: Wall/highlight objects created/destroyed constantly  
- **Redundant calculations**: Pathfinding recalculated unnecessarily
- **Update loops everywhere**: Multiple scripts checking states every frame
- **Missing coroutine management**: No proper coroutine lifecycle management

### 3. Code Quality
- **Monolithic scripts**: WallChessGameManager (800+ lines), GridSystem (500+ lines)
- **Duplicate systems**: GridManager vs GridSystem vs GridTileManager
- **Mixed concerns**: UI logic in game logic, rendering in data structures
- **Poor encapsulation**: Public fields everywhere, no proper properties
- **Debug code in production**: Test scripts mixed with game code

---

## Refactoring Plan

### Phase 1: Core Architecture (Week 1)

#### 1.1 Implement Proper FSM
**Scripts to create:**
```
/Code/Core/StateMachine/
├── StateMachine.cs
├── IState.cs
├── StateTransition.cs
└── States/
    ├── MenuState.cs
    ├── PreSessionState.cs
    ├── GameplayState.cs
    ├── PauseState.cs
    └── GameOverState.cs
```

**Scripts to refactor:**
- `WallChessGameManager.cs` → Extract all state logic to FSM

#### 1.2 Extract Session Management
**Scripts to create:**
```
/Code/Core/Session/
├── SessionManager.cs
├── TurnManager.cs
├── GameRules.cs
└── SessionSettings.cs (ScriptableObject)
```

**Scripts to refactor:**
- `WallChessGameManager.cs` → Remove turn/session logic (reduce by ~300 lines)

#### 1.3 Clean Project Structure
**Actions:**
```
Current Structure → Target Structure

/Code/MVP/ → /Archives/MVP/ (preserve but isolate)
*.backup files → Delete all
Multiple grid scripts → Consolidate

New Structure:
/Code/
├── Core/
│   ├── StateMachine/
│   ├── Session/
│   ├── Grid/
│   └── Pooling/
├── Gameplay/
│   ├── Pawns/
│   ├── Walls/
│   └── AI/
├── Input/
│   ├── Schemes/
│   └── Commands/
├── UI/
│   ├── HUD/
│   └── Menus/
├── Utilities/
└── Data/
    └── ScriptableObjects/
```

---

### Phase 2: Grid System Consolidation (Week 1-2)

#### 2.1 Unify Grid Systems
**Merge Strategy:**
```
GridSystem.cs + GridManager.cs + GridTileManager.cs
→ UnifiedGridSystem.cs (max 200 lines)

Supporting Classes:
├── GridData.cs (pure data structure)
├── GridPathfinding.cs (A* algorithm only)
├── GridValidator.cs (validation logic)
└── GridConstants.cs (enums, constants)
```

#### 2.2 Separate Visual Concerns
**Extract from GridSystem:**
```
Visual elements → GridRenderer.cs
UI elements → GridUIController.cs  
Coordinate conversion → GridCoordinateUtility.cs (static utility)
Gap detection → GridGapManager.cs
```

---

### Phase 3: Input System Refactor (Week 2)

#### 3.1 Centralized Input Handler
**Architecture:**
```
InputManager.cs (Singleton)
├── IInputScheme.cs
├── InputSchemes/
│   ├── DragHoldScheme.cs
│   ├── ClickDragScheme.cs
│   └── ClickImmediateScheme.cs
└── Commands/
    ├── ICommand.cs
    ├── MoveCommand.cs
    ├── PlaceWallCommand.cs
    └── UndoCommand.cs
```

#### 3.2 Command Pattern Benefits
- **Undo/Redo**: Full move history
- **Replay System**: Record and playback games
- **Network Ready**: Commands can be serialized
- **AI Integration**: AI generates commands

---

### Phase 4: Object Pooling System (Week 2-3)

#### 4.1 Pool Infrastructure
**Core Classes:**
```csharp
// ObjectPool.cs
public class ObjectPool<T> where T : Component, IPoolable
{
    private Queue<T> pool;
    private T prefab;
    private Transform container;
    
    public T Get() { /* ... */ }
    public void Return(T obj) { /* ... */ }
}

// PoolManager.cs
public class PoolManager : Singleton<PoolManager>
{
    Dictionary<string, IObjectPool> pools;
    
    public void CreatePool<T>(string key, T prefab, int size);
    public T GetFromPool<T>(string key);
}
```

**Implementation Targets:**
- Walls: Pre-pool 20 objects (max 18 in play)
- Highlights: Pre-pool 10 objects
- Tiles: Pre-pool 81 objects (9x9 grid)
- UI Elements: Pre-pool based on usage

---

### Phase 5: Pawn System Enhancement (Week 3)

#### 5.1 Pawn Architecture
```
PawnManager.cs
├── Pawn.cs (Data + Logic)
│   ├── PawnData (position, walls, etc.)
│   ├── PawnMovement component
│   └── PawnVisuals component
├── PawnFactory.cs
└── PawnInputHandler.cs
```

#### 5.2 List-Based Management
```csharp
public class PawnManager : MonoBehaviour
{
    private List<Pawn> pawns = new List<Pawn>(4);
    private int activePawnIndex;
    
    public Pawn ActivePawn => pawns[activePawnIndex];
    public void NextTurn() => activePawnIndex = (activePawnIndex + 1) % pawns.Count;
}
```

---

### Phase 6: Wall System Optimization (Week 3-4)

#### 6.1 Wall Architecture
```
WallPlacementSystem.cs
├── WallData.cs (position, orientation)
├── WallValidator.cs (placement rules)
├── WallRenderer.cs (visual representation)
└── WallInventory.cs (per-player wall tracking)
```

#### 6.2 Placement Flow
```
Input → WallPlacementSystem → Validate → Place → Update Grid → Trigger Events
```

---

## Execution Flow Improvements

### Current Flow (Problematic)
```
WallChessGameManager (800+ lines)
├── Handles Everything
├── Direct Component References
├── Mixed UI/Game Logic
├── Circular Dependencies
└── No Clear Separation
```

### Target Flow (Clean Architecture)
```
MainGame.cs (Entry Point, <100 lines)
│
├── StateMachine
│   ├── MenuState
│   ├── GameplayState → SessionManager
│   ├── PauseState
│   └── GameOverState
│
├── SessionManager (Turn orchestration)
│   ├── TurnManager
│   ├── GameRules
│   └── WinConditionChecker
│
├── Core Systems (Decoupled via Events)
│   ├── UnifiedGridSystem
│   ├── InputManager
│   ├── PoolManager
│   └── EventBus
│
└── Gameplay Systems
    ├── PawnManager
    ├── WallPlacementSystem
    └── AIController
```

---

## Script-Specific Notes

### WallChessGameManager.cs (Current: 800+ lines)
**Split into:**
- `MainGame.cs` (<100 lines) - Entry point only
- `SessionManager.cs` (<200 lines) - Game session logic
- `TurnManager.cs` (<150 lines) - Turn handling
- `PawnManager.cs` (<200 lines) - Pawn management

**Removal List:**
- State management → FSM
- Turn logic → TurnManager
- Pawn arrays → PawnManager
- Victory checking → WinConditionChecker
- Grid initialization → GridInitializer

### GridSystem.cs (Current: 500+ lines)
**Split into:**
- `UnifiedGridSystem.cs` (<200 lines) - Core grid logic
- `GridData.cs` (<100 lines) - Data structures
- `GridRenderer.cs` (<150 lines) - Visual representation
- `GridPathfinding.cs` (<150 lines) - A* algorithm

### PlayerControllerV2.cs
**Refactor to:**
- Rename: `PawnController.cs`
- Remove: Direct input handling
- Add: Command receiver interface
- Focus: Pawn-specific movement logic only

### AIOpponent.cs
**Improvements:**
- Move to: `/Code/Gameplay/AI/AIController.cs`
- Implement: Strategy pattern for difficulties
- Add: Behavior tree or Utility AI system
- Remove: Update() polling, use event-driven

---

## Performance Optimizations

### 1. Object Pooling Implementation
```csharp
// Example for walls
public class WallPool : ObjectPool<Wall>
{
    protected override void OnGet(Wall wall)
    {
        wall.gameObject.SetActive(true);
        wall.Reset();
    }
    
    protected override void OnReturn(Wall wall)
    {
        wall.gameObject.SetActive(false);
    }
}
```

### 2. Caching Strategy
**Cache These:**
- Valid moves per position (invalidate on wall placement)
- Pathfinding results (invalidate on grid change)
- Grid-to-world conversions (static after init)
- Component references (cache in Awake)

### 3. Event-Driven Architecture
**Replace Update() with Events:**
```csharp
// Before (Bad)
void Update()
{
    if (gameManager.IsPlayerTurn()) { /* ... */ }
}

// After (Good)
void OnEnable()
{
    TurnManager.OnTurnChanged += HandleTurnChange;
}
```

### 4. Pathfinding Optimization
- Use Job System for parallel pathfinding
- Cache paths until walls change
- Implement hierarchical pathfinding for larger grids
- Use spatial partitioning for obstacle checks

---

## Implementation Timeline

### Week 1: Foundation
**Monday-Tuesday:**
- [ ] Backup current version
- [ ] Clean project structure
- [ ] Remove backup files
- [ ] Create folder hierarchy

**Wednesday-Thursday:**
- [ ] Implement core FSM
- [ ] Create state classes
- [ ] Wire up state transitions

**Friday:**
- [ ] Extract SessionManager
- [ ] Start grid consolidation
- [ ] Testing & validation

### Week 2: Core Systems
**Monday-Tuesday:**
- [ ] Complete grid unification
- [ ] Separate visual concerns
- [ ] Implement GridData structure

**Wednesday-Thursday:**
- [ ] Implement InputManager
- [ ] Create input schemes
- [ ] Implement command pattern

**Friday:**
- [ ] Start object pooling
- [ ] Create pool infrastructure
- [ ] Test with highlights

### Week 3: Gameplay Systems
**Monday-Tuesday:**
- [ ] Refactor pawn system
- [ ] Implement PawnManager
- [ ] Create pawn list structure

**Wednesday-Thursday:**
- [ ] Optimize wall system
- [ ] Split WallManager
- [ ] Implement wall pooling

**Friday:**
- [ ] Complete all pooling
- [ ] Integration testing
- [ ] Performance profiling

### Week 4: Polish & Optimization
**Monday-Tuesday:**
- [ ] Performance profiling
- [ ] Memory optimization
- [ ] Draw call batching

**Wednesday-Thursday:**
- [ ] Debug tools cleanup
- [ ] Remove test scripts
- [ ] Code documentation

**Friday:**
- [ ] Final testing
- [ ] Create build
- [ ] Performance validation

---

## Best Practices

### SOLID Principles
- **S**ingle Responsibility: One class, one purpose
- **O**pen/Closed: Extend via inheritance, not modification
- **L**iskov Substitution: Derived classes must be substitutable
- **I**nterface Segregation: Many specific interfaces
- **D**ependency Inversion: Depend on abstractions

### Design Patterns to Implement
1. **State Machine**: Game state management
2. **Command Pattern**: Input handling & undo
3. **Observer Pattern**: Event system
4. **Object Pool**: Performance optimization
5. **Factory Pattern**: Object creation
6. **Strategy Pattern**: AI behaviors
7. **Singleton**: Manager classes (use sparingly)

### Unity-Specific Best Practices
1. **ScriptableObjects** for configuration data
2. **Addressables** for asset management
3. **Assembly Definitions** for faster compile times
4. **Profiler** regular usage during development
5. **Frame Debugger** for draw call optimization

### Code Standards
```csharp
// File Organization
#region Dependencies
using System;
using UnityEngine;
#endregion

namespace WallChess.Core
{
    /// <summary>
    /// XML documentation for all public members
    /// </summary>
    public class ExampleClass : MonoBehaviour
    {
        #region Constants
        private const int MAX_SIZE = 100;
        #endregion
        
        #region Private Fields
        [SerializeField] private int value;
        private float timer;
        #endregion
        
        #region Properties
        public int Value => value;
        #endregion
        
        #region Unity Lifecycle
        private void Awake() { }
        private void Start() { }
        #endregion
        
        #region Public Methods
        public void DoSomething() { }
        #endregion
        
        #region Private Methods
        private void Helper() { }
        #endregion
    }
}
```

---

## Immediate Actions

### Day 1 Checklist
- [ ] Create Git branch: `refactor/phase-1-architecture`
- [ ] Full project backup to: `/Backups/pre-refactor-[date]/`
- [ ] Document current behavior with video capture
- [ ] List all current features for regression testing
- [ ] Set up unit test project

### Pre-Refactor Documentation
- [ ] Record gameplay video of all features
- [ ] Document all input schemes
- [ ] List all game rules and edge cases
- [ ] Create state diagram of current flow
- [ ] Note all known bugs for verification

### Risk Mitigation
1. **Always maintain working build** on main branch
2. **Feature flags** for switching between old/new systems
3. **Incremental refactoring** - one system at a time
4. **Automated tests** before major changes
5. **Daily backups** during refactoring phase

---

## Success Metrics

### Performance Targets
- **Frame Rate**: Stable 60 FPS on mobile
- **Memory**: < 100MB RAM usage
- **Build Size**: < 50MB APK
- **Load Time**: < 3 seconds
- **Battery**: < 10% drain per hour

### Code Quality Targets
- **Script Size**: No file > 200 lines
- **Cyclomatic Complexity**: < 10 per method
- **Coupling**: Low coupling via events
- **Test Coverage**: > 70% for core logic
- **Documentation**: 100% public API

### Architecture Goals
- **Modularity**: Swap any system without breaking others
- **Extensibility**: Add features without modifying core
- **Maintainability**: New developer onboarding < 1 day
- **Scalability**: Support 4-player mode easily
- **Testability**: All game logic unit testable

---

## Appendix: File Mapping

### Files to Delete
```
*.backup
*.backup.meta
/Code/MVP/* (after archiving)
/Code/PlayerController_*.cs.backup
/Code/ImprovedDragMovementV2.cs.backup
```

### Files to Merge
```
GridSystem.cs + GridManager.cs + GridTileManager.cs → UnifiedGridSystem.cs
WallManager.cs + WallValidator.cs + WallVisuals.cs → WallPlacementSystem.cs
Multiple highlight managers → Single HighlightManager.cs
```

### Files to Create
```
/Code/Core/MainGame.cs
/Code/Core/StateMachine/*.cs
/Code/Core/Session/*.cs
/Code/Core/Pooling/*.cs
/Code/Input/InputManager.cs
/Code/Gameplay/Pawns/PawnManager.cs
```

---

## Notes

**Version**: 1.0  
**Author**: AI Assistant  
**Date**: December 2024  
**Review**: Pending  

This plan is a living document. Update as the refactoring progresses and new insights emerge.

---

*End of Refactoring Plan*