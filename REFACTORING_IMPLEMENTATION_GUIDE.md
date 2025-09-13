# WallChessQuidor Refactoring Implementation Guide

## Overview
This document provides a comprehensive, step-by-step implementation guide for refactoring the WallChessQuidor Unity project. Each section contains detailed instructions, code examples, and validation steps to ensure successful implementation.

## Prerequisites
- Unity 2021.3+ LTS
- Version control system (Git) with clean working tree
- Full project backup before starting
- Unity Test Framework installed
- Performance profiling tools enabled

---

# 1. CONSOLIDATE STATE MANAGEMENT (Critical Priority)

## Current State Analysis
The codebase currently has dual state management systems:
1. **Enum-based GameState** in `WallChessGameManager.cs`
2. **FSM Pattern** in `Core/StateMachine.cs` and related state classes

### Identified Issues
- State synchronization problems between managers
- Duplicate state transition logic
- Unclear authority for state changes
- Race conditions during state transitions

## Implementation Plan

### Phase 1: Create Unified State Architecture (2-3 days)

#### Step 1.1: Create New State Management System

Create `/Assets/Code/Core/UnifiedStateSystem/`:

```csharp
// File: /Assets/Code/Core/UnifiedStateSystem/IGameState.cs
namespace WallChess.Core.UnifiedStateSystem
{
    using System;
    using UnityEngine;

    public interface IGameState
    {
        string StateName { get; }
        void OnEnter(GameStateContext context);
        void OnUpdate(GameStateContext context);
        void OnExit(GameStateContext context);
        bool CanTransitionTo(Type nextStateType);
    }

    // Context object for state data
    public class GameStateContext
    {
        public int ActivePlayerIndex { get; set; }
        public int TurnNumber { get; set; }
        public float StateTimer { get; set; }
        public object CustomData { get; set; }
        
        // Add game references
        public WallChessGameManager GameManager { get; set; }
        public GridSystem GridSystem { get; set; }
        public TurnManager TurnManager { get; set; }
    }
}
```

```csharp
// File: /Assets/Code/Core/UnifiedStateSystem/UnifiedStateManager.cs
namespace WallChess.Core.UnifiedStateSystem
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    public class UnifiedStateManager : MonoBehaviour
    {
        [SerializeField] private GameStateContext _context;
        private IGameState _currentState;
        private Dictionary<Type, IGameState> _stateCache;
        
        // Events for state transitions
        public UnityEvent<string, string> OnStateTransition;
        public UnityEvent<string> OnStateEntered;
        public UnityEvent<string> OnStateExited;
        
        // Thread-safe state access
        private readonly object _stateLock = new object();
        
        public string CurrentStateName => _currentState?.StateName ?? "None";
        
        private void Awake()
        {
            _stateCache = new Dictionary<Type, IGameState>();
            _context = new GameStateContext();
            InitializeStateCache();
        }
        
        private void InitializeStateCache()
        {
            // Pre-create all states to avoid runtime allocation
            RegisterState(new MenuState());
            RegisterState(new GameStartState());
            RegisterState(new PlayerTurnState());
            RegisterState(new PawnMovingState());
            RegisterState(new WallPlacementState());
            RegisterState(new TurnValidationState());
            RegisterState(new GameOverState());
        }
        
        private void RegisterState(IGameState state)
        {
            _stateCache[state.GetType()] = state;
        }
        
        public bool TransitionTo<T>() where T : IGameState
        {
            return TransitionTo(typeof(T));
        }
        
        public bool TransitionTo(Type stateType)
        {
            lock (_stateLock)
            {
                if (!_stateCache.ContainsKey(stateType))
                {
                    Debug.LogError($"State {stateType.Name} not registered!");
                    return false;
                }
                
                if (_currentState != null)
                {
                    if (!_currentState.CanTransitionTo(stateType))
                    {
                        Debug.LogWarning($"Cannot transition from {_currentState.StateName} to {stateType.Name}");
                        return false;
                    }
                    
                    string previousStateName = _currentState.StateName;
                    
                    OnStateExited?.Invoke(previousStateName);
                    _currentState.OnExit(_context);
                    
                    _currentState = _stateCache[stateType];
                    
                    OnStateTransition?.Invoke(previousStateName, _currentState.StateName);
                }
                else
                {
                    _currentState = _stateCache[stateType];
                }
                
                _context.StateTimer = 0f;
                _currentState.OnEnter(_context);
                OnStateEntered?.Invoke(_currentState.StateName);
                
                return true;
            }
        }
        
        private void Update()
        {
            if (_currentState != null)
            {
                _context.StateTimer += Time.deltaTime;
                _currentState.OnUpdate(_context);
            }
        }
        
        // Validation and debugging
        public void ValidateStateIntegrity()
        {
            if (_currentState == null)
            {
                Debug.LogError("No current state set!");
                return;
            }
            
            // Add custom validation logic
            Debug.Log($"State Validation: {CurrentStateName} - Context Valid: {_context != null}");
        }
    }
}
```

#### Step 1.2: Implement Concrete States

```csharp
// File: /Assets/Code/Core/UnifiedStateSystem/States/PlayerTurnState.cs
namespace WallChess.Core.UnifiedStateSystem
{
    using System;
    using UnityEngine;

    public class PlayerTurnState : IGameState
    {
        public string StateName => "PlayerTurn";
        
        private float _turnTimeLimit = 30f; // Configurable turn timer
        
        public void OnEnter(GameStateContext context)
        {
            Debug.Log($"Player {context.ActivePlayerIndex} turn started");
            
            // Enable player input
            if (context.GameManager != null)
            {
                context.GameManager.EnablePlayerInput(context.ActivePlayerIndex);
            }
            
            // Broadcast turn start event
            TurnEventBroadcaster.Instance?.BroadcastTurnStart(context.ActivePlayerIndex);
        }
        
        public void OnUpdate(GameStateContext context)
        {
            // Check for turn timeout
            if (context.StateTimer > _turnTimeLimit)
            {
                Debug.LogWarning($"Player {context.ActivePlayerIndex} turn timeout!");
                // Force end turn
                context.TurnManager?.ForceEndTurn();
            }
        }
        
        public void OnExit(GameStateContext context)
        {
            // Disable all player input
            if (context.GameManager != null)
            {
                context.GameManager.DisableAllInput();
            }
        }
        
        public bool CanTransitionTo(Type nextStateType)
        {
            // Define valid transitions
            return nextStateType == typeof(PawnMovingState) ||
                   nextStateType == typeof(WallPlacementState) ||
                   nextStateType == typeof(TurnValidationState) ||
                   nextStateType == typeof(GameOverState);
        }
    }
}
```

### Phase 2: Migration Strategy (3-4 days)

#### Step 2.1: Create Migration Adapter

```csharp
// File: /Assets/Code/Core/UnifiedStateSystem/StateMigrationAdapter.cs
namespace WallChess.Core.UnifiedStateSystem
{
    using UnityEngine;
    
    /// <summary>
    /// Temporary adapter to bridge old and new state systems during migration
    /// </summary>
    public class StateMigrationAdapter : MonoBehaviour
    {
        [SerializeField] private WallChessGameManager _oldGameManager;
        [SerializeField] private UnifiedStateManager _newStateManager;
        
        private bool _migrationEnabled = false;
        
        private void Start()
        {
            if (_migrationEnabled)
            {
                StartMigration();
            }
        }
        
        private void StartMigration()
        {
            // Subscribe to old system events
            _oldGameManager.OnStateChanged += HandleOldStateChange;
            
            // Subscribe to new system events
            _newStateManager.OnStateTransition.AddListener(HandleNewStateTransition);
        }
        
        private void HandleOldStateChange(GameState oldState)
        {
            // Map old states to new states
            switch (oldState)
            {
                case GameState.GameStart:
                    _newStateManager.TransitionTo<GameStartState>();
                    break;
                case GameState.PlayerTurn:
                    _newStateManager.TransitionTo<PlayerTurnState>();
                    break;
                case GameState.PawnMoving:
                    _newStateManager.TransitionTo<PawnMovingState>();
                    break;
                case GameState.WallPlacement:
                    _newStateManager.TransitionTo<WallPlacementState>();
                    break;
                case GameState.GameOver:
                    _newStateManager.TransitionTo<GameOverState>();
                    break;
            }
            
            LogMigration($"Migrated from old state: {oldState}");
        }
        
        private void HandleNewStateTransition(string from, string to)
        {
            // Optionally sync back to old system during migration
            if (!_migrationEnabled) return;
            
            // This ensures both systems stay in sync during migration
            LogMigration($"New state transition: {from} -> {to}");
        }
        
        private void LogMigration(string message)
        {
            Debug.Log($"[STATE MIGRATION] {message}");
        }
        
        public void EnableMigration()
        {
            _migrationEnabled = true;
            StartMigration();
        }
        
        public void CompleteMigration()
        {
            // Disable old system
            _oldGameManager.enabled = false;
            
            // Remove adapter
            _migrationEnabled = false;
            
            Debug.Log("[STATE MIGRATION] Migration completed. Old system disabled.");
        }
    }
}
```

#### Step 2.2: Testing Strategy

```csharp
// File: /Assets/Tests/StateManagement/UnifiedStateTests.cs
namespace WallChess.Tests
{
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using System.Collections;
    using WallChess.Core.UnifiedStateSystem;

    public class UnifiedStateTests
    {
        private UnifiedStateManager _stateManager;
        private GameObject _testObject;
        
        [SetUp]
        public void Setup()
        {
            _testObject = new GameObject("TestStateManager");
            _stateManager = _testObject.AddComponent<UnifiedStateManager>();
        }
        
        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_testObject);
        }
        
        [Test]
        public void StateManager_InitializesCorrectly()
        {
            Assert.IsNotNull(_stateManager);
            Assert.AreEqual("None", _stateManager.CurrentStateName);
        }
        
        [UnityTest]
        public IEnumerator StateManager_TransitionsCorrectly()
        {
            bool transitionSuccess = _stateManager.TransitionTo<GameStartState>();
            Assert.IsTrue(transitionSuccess);
            
            yield return new WaitForSeconds(0.1f);
            
            Assert.AreEqual("GameStart", _stateManager.CurrentStateName);
        }
        
        [Test]
        public void StateManager_PreventsInvalidTransitions()
        {
            _stateManager.TransitionTo<GameStartState>();
            
            // Try invalid transition (example)
            bool invalidTransition = _stateManager.TransitionTo<GameOverState>();
            
            Assert.IsFalse(invalidTransition);
            Assert.AreEqual("GameStart", _stateManager.CurrentStateName);
        }
    }
}
```

### Phase 3: Complete Migration (2 days)

#### Migration Checklist:
- [ ] Create backup of project
- [ ] Implement all state classes
- [ ] Set up migration adapter
- [ ] Run parallel systems for 1 day of testing
- [ ] Migrate all state-dependent code
- [ ] Remove old state system
- [ ] Update all references
- [ ] Run comprehensive tests

### Rollback Procedure
```bash
# If migration fails, rollback steps:
1. git stash  # Save current changes
2. git checkout -b failed-state-migration
3. git checkout main
4. Restore from backup
5. Document failure reasons
```

---

# 2. SIMPLIFY GRID SYSTEM (High Priority)

## Current State Analysis
Three coordinate systems exist:
1. **Grid coordinates** (x, y integers)
2. **World positions** (Vector3)
3. **Wall gap positions** (custom calculation)

## Implementation Plan

### Phase 1: Create Unified Grid System (2 days)

#### Step 1.1: Design Unified Coordinate System

```csharp
// File: /Assets/Code/Grid/UnifiedGrid/UnifiedGridSystem.cs
namespace WallChess.Grid
{
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// Unified grid system with single coordinate representation
    /// </summary>
    public class UnifiedGridSystem : MonoBehaviour
    {
        [System.Serializable]
        public struct GridCell
        {
            public int x;
            public int y;
            public Vector3 worldPosition;
            public GameObject tileObject;
            public bool isOccupied;
            
            public GridCell(int x, int y, Vector3 worldPos)
            {
                this.x = x;
                this.y = y;
                this.worldPosition = worldPos;
                this.tileObject = null;
                this.isOccupied = false;
            }
            
            public Vector2Int Coordinates => new Vector2Int(x, y);
            
            public override string ToString() => $"Cell({x},{y})";
        }
        
        [Header("Grid Configuration")]
        [SerializeField] private int _gridWidth = 9;
        [SerializeField] private int _gridHeight = 9;
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private float _cellGap = 0.2f;
        [SerializeField] private Vector3 _gridOrigin = Vector3.zero;
        
        private GridCell[,] _grid;
        private Dictionary<Vector2Int, GridCell> _cellLookup;
        
        // Singleton pattern for global access
        private static UnifiedGridSystem _instance;
        public static UnifiedGridSystem Instance => _instance;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeGrid();
        }
        
        private void InitializeGrid()
        {
            _grid = new GridCell[_gridWidth, _gridHeight];
            _cellLookup = new Dictionary<Vector2Int, GridCell>();
            
            float totalCellSize = _cellSize + _cellGap;
            
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    Vector3 worldPos = _gridOrigin + new Vector3(
                        x * totalCellSize,
                        0,
                        y * totalCellSize
                    );
                    
                    _grid[x, y] = new GridCell(x, y, worldPos);
                    _cellLookup[new Vector2Int(x, y)] = _grid[x, y];
                }
            }
        }
        
        // Conversion utilities
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            if (!IsValidGridPosition(gridPos))
                return Vector3.zero;
                
            return _grid[gridPos.x, gridPos.y].worldPosition;
        }
        
        public Vector3 GridToWorld(int x, int y)
        {
            return GridToWorld(new Vector2Int(x, y));
        }
        
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - _gridOrigin;
            float totalCellSize = _cellSize + _cellGap;
            
            int x = Mathf.RoundToInt(localPos.x / totalCellSize);
            int y = Mathf.RoundToInt(localPos.z / totalCellSize);
            
            return new Vector2Int(
                Mathf.Clamp(x, 0, _gridWidth - 1),
                Mathf.Clamp(y, 0, _gridHeight - 1)
            );
        }
        
        public bool IsValidGridPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < _gridWidth &&
                   pos.y >= 0 && pos.y < _gridHeight;
        }
        
        public GridCell GetCell(Vector2Int pos)
        {
            if (!IsValidGridPosition(pos))
                throw new System.ArgumentOutOfRangeException($"Invalid grid position: {pos}");
                
            return _grid[pos.x, pos.y];
        }
        
        public GridCell GetCell(int x, int y)
        {
            return GetCell(new Vector2Int(x, y));
        }
        
        // Wall position calculations
        public Vector3 GetWallPosition(Vector2Int gridPos, WallOrientation orientation)
        {
            Vector3 cellWorldPos = GridToWorld(gridPos);
            float halfCell = _cellSize * 0.5f;
            float halfGap = _cellGap * 0.5f;
            
            switch (orientation)
            {
                case WallOrientation.Horizontal:
                    return cellWorldPos + new Vector3(0, 0, halfCell + halfGap);
                    
                case WallOrientation.Vertical:
                    return cellWorldPos + new Vector3(halfCell + halfGap, 0, 0);
                    
                default:
                    return cellWorldPos;
            }
        }
        
        // Neighbor utilities
        public List<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            
            Vector2Int[] directions = {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };
            
            foreach (var dir in directions)
            {
                Vector2Int neighborPos = pos + dir;
                if (IsValidGridPosition(neighborPos))
                {
                    neighbors.Add(neighborPos);
                }
            }
            
            return neighbors;
        }
        
        // Update cell state
        public void SetCellOccupied(Vector2Int pos, bool occupied)
        {
            if (IsValidGridPosition(pos))
            {
                _grid[pos.x, pos.y].isOccupied = occupied;
                _cellLookup[pos] = _grid[pos.x, pos.y];
            }
        }
        
        public void SetCellTile(Vector2Int pos, GameObject tileObject)
        {
            if (IsValidGridPosition(pos))
            {
                _grid[pos.x, pos.y].tileObject = tileObject;
                _cellLookup[pos] = _grid[pos.x, pos.y];
            }
        }
        
        // Debug visualization
        private void OnDrawGizmos()
        {
            if (_grid == null) return;
            
            Gizmos.color = Color.cyan;
            
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    Vector3 pos = _grid[x, y].worldPosition;
                    Gizmos.DrawWireCube(pos, Vector3.one * _cellSize);
                    
                    if (_grid[x, y].isOccupied)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube(pos, Vector3.one * _cellSize * 0.8f);
                        Gizmos.color = Color.cyan;
                    }
                }
            }
        }
    }
    
    public enum WallOrientation
    {
        Horizontal,
        Vertical
    }
}
```

#### Step 1.2: Migration Utilities

```csharp
// File: /Assets/Code/Grid/UnifiedGrid/GridMigrationUtility.cs
namespace WallChess.Grid
{
    using UnityEngine;
    using UnityEditor;
    
    public static class GridMigrationUtility
    {
        /// <summary>
        /// Migrate old grid references to new unified system
        /// </summary>
        [MenuItem("WallChess/Grid/Migrate to Unified Grid")]
        public static void MigrateToUnifiedGrid()
        {
            Debug.Log("Starting Grid Migration...");
            
            // Find all components using old grid systems
            var oldGridManagers = Object.FindObjectsOfType<GridManager>();
            var oldGridSystems = Object.FindObjectsOfType<GridSystem>();
            
            int migrated = 0;
            
            // Migrate GridManager references
            foreach (var oldManager in oldGridManagers)
            {
                MigrateGridManager(oldManager);
                migrated++;
            }
            
            // Migrate GridSystem references
            foreach (var oldSystem in oldGridSystems)
            {
                MigrateGridSystem(oldSystem);
                migrated++;
            }
            
            Debug.Log($"Grid Migration Complete. Migrated {migrated} components.");
        }
        
        private static void MigrateGridManager(GridManager oldManager)
        {
            GameObject go = oldManager.gameObject;
            
            // Add new unified grid system
            var unifiedGrid = go.AddComponent<UnifiedGridSystem>();
            
            // Copy configuration
            // Note: Access old manager fields and map to new system
            
            // Disable old component
            oldManager.enabled = false;
            
            Debug.Log($"Migrated GridManager on {go.name}");
        }
        
        private static void MigrateGridSystem(GridSystem oldSystem)
        {
            // Similar migration logic
            Debug.Log($"Migrating GridSystem...");
        }
        
        /// <summary>
        /// Validate grid consistency after migration
        /// </summary>
        [MenuItem("WallChess/Grid/Validate Grid Consistency")]
        public static void ValidateGridConsistency()
        {
            var unifiedGrid = UnifiedGridSystem.Instance;
            if (unifiedGrid == null)
            {
                Debug.LogError("No UnifiedGridSystem found!");
                return;
            }
            
            // Validate all pawns are on valid grid positions
            var pawns = Object.FindObjectsOfType<AvatarDragController>();
            foreach (var pawn in pawns)
            {
                Vector2Int gridPos = unifiedGrid.WorldToGrid(pawn.transform.position);
                if (!unifiedGrid.IsValidGridPosition(gridPos))
                {
                    Debug.LogError($"Pawn {pawn.name} at invalid position: {gridPos}");
                }
            }
            
            Debug.Log("Grid validation complete.");
        }
    }
}
```

### Phase 2: Performance Optimization (1 day)

```csharp
// File: /Assets/Code/Grid/UnifiedGrid/GridPerformanceOptimizer.cs
namespace WallChess.Grid
{
    using UnityEngine;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    /// <summary>
    /// Performance optimizations for grid operations
    /// </summary>
    public class GridPerformanceOptimizer : MonoBehaviour
    {
        private UnifiedGridSystem _gridSystem;
        private NativeArray<int2> _gridPositions;
        private NativeArray<float3> _worldPositions;
        
        private void Start()
        {
            _gridSystem = UnifiedGridSystem.Instance;
            InitializeNativeArrays();
        }
        
        private void InitializeNativeArrays()
        {
            int gridSize = 9 * 9; // Assuming 9x9 grid
            _gridPositions = new NativeArray<int2>(gridSize, Allocator.Persistent);
            _worldPositions = new NativeArray<float3>(gridSize, Allocator.Persistent);
            
            // Pre-calculate all positions
            int index = 0;
            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    _gridPositions[index] = new int2(x, y);
                    Vector3 worldPos = _gridSystem.GridToWorld(x, y);
                    _worldPositions[index] = new float3(worldPos.x, worldPos.y, worldPos.z);
                    index++;
                }
            }
        }
        
        // Batch position conversion using Jobs
        public struct BatchConversionJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int2> gridPositions;
            public NativeArray<float3> worldPositions;
            public float cellSize;
            public float cellGap;
            public float3 origin;
            
            public void Execute(int index)
            {
                int2 gridPos = gridPositions[index];
                float totalSize = cellSize + cellGap;
                
                worldPositions[index] = origin + new float3(
                    gridPos.x * totalSize,
                    0,
                    gridPos.y * totalSize
                );
            }
        }
        
        private void OnDestroy()
        {
            if (_gridPositions.IsCreated) _gridPositions.Dispose();
            if (_worldPositions.IsCreated) _worldPositions.Dispose();
        }
    }
}
```

---

# 3. IMPLEMENT OBJECT POOLING (High Priority)

## Implementation Plan

### Phase 1: Core Pooling System (2 days)

```csharp
// File: /Assets/Code/Core/Pooling/ObjectPool.cs
namespace WallChess.Core.Pooling
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Generic object pool for any GameObject
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly Queue<T> _pool;
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly int _maxSize;
        private readonly Transform _poolParent;
        
        public int CountInactive => _pool.Count;
        public int CountActive { get; private set; }
        public int CountAll => CountInactive + CountActive;
        
        public ObjectPool(
            Func<T> createFunc,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int defaultCapacity = 10,
            int maxSize = 100)
        {
            _pool = new Queue<T>(defaultCapacity);
            _createFunc = createFunc;
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxSize = maxSize;
            
            // Create pool parent object
            GameObject poolParentGO = new GameObject($"Pool_{typeof(T).Name}");
            _poolParent = poolParentGO.transform;
            
            // Pre-warm pool
            for (int i = 0; i < defaultCapacity; i++)
            {
                T obj = _createFunc();
                obj.transform.SetParent(_poolParent);
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }
        
        public T Get()
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = _createFunc();
                obj.transform.SetParent(_poolParent);
            }
            
            obj.gameObject.SetActive(true);
            _onGet?.Invoke(obj);
            CountActive++;
            
            return obj;
        }
        
        public void Release(T obj)
        {
            if (obj == null) return;
            
            _onRelease?.Invoke(obj);
            obj.gameObject.SetActive(false);
            
            if (_pool.Count < _maxSize)
            {
                _pool.Enqueue(obj);
            }
            else
            {
                _onDestroy?.Invoke(obj);
                UnityEngine.Object.Destroy(obj.gameObject);
            }
            
            CountActive--;
        }
        
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T obj = _pool.Dequeue();
                _onDestroy?.Invoke(obj);
                UnityEngine.Object.Destroy(obj.gameObject);
            }
            
            CountActive = 0;
        }
    }
}
```

```csharp
// File: /Assets/Code/Core/Pooling/PoolManager.cs
namespace WallChess.Core.Pooling
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Central manager for all object pools
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager _instance;
        public static PoolManager Instance => _instance;
        
        [System.Serializable]
        public class PoolConfig
        {
            public string poolName;
            public GameObject prefab;
            public int defaultCapacity = 10;
            public int maxSize = 100;
        }
        
        [Header("Pool Configurations")]
        [SerializeField] private PoolConfig[] _poolConfigs;
        
        private Dictionary<string, object> _pools;
        
        // Specific pools for game objects
        private ObjectPool<TileController> _tilePool;
        private ObjectPool<WallController> _wallPool;
        private ObjectPool<EffectController> _effectPool;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializePools();
        }
        
        private void InitializePools()
        {
            _pools = new Dictionary<string, object>();
            
            // Initialize tile pool
            _tilePool = new ObjectPool<TileController>(
                createFunc: () => CreateTile(),
                onGet: (tile) => ResetTile(tile),
                onRelease: (tile) => CleanupTile(tile),
                defaultCapacity: 81, // 9x9 grid
                maxSize: 100
            );
            
            // Initialize wall pool
            _wallPool = new ObjectPool<WallController>(
                createFunc: () => CreateWall(),
                onGet: (wall) => ResetWall(wall),
                onRelease: (wall) => CleanupWall(wall),
                defaultCapacity: 20,
                maxSize: 50
            );
            
            // Initialize effect pool
            _effectPool = new ObjectPool<EffectController>(
                createFunc: () => CreateEffect(),
                onGet: (effect) => ResetEffect(effect),
                onRelease: (effect) => CleanupEffect(effect),
                defaultCapacity: 10,
                maxSize: 30
            );
        }
        
        // Tile pool methods
        private TileController CreateTile()
        {
            GameObject tilePrefab = Resources.Load<GameObject>("Prefabs/Tile");
            GameObject tileGO = Instantiate(tilePrefab);
            return tileGO.GetComponent<TileController>();
        }
        
        private void ResetTile(TileController tile)
        {
            tile.transform.localScale = Vector3.one;
            tile.ResetState();
        }
        
        private void CleanupTile(TileController tile)
        {
            // Clean up any references or events
        }
        
        // Wall pool methods
        private WallController CreateWall()
        {
            GameObject wallPrefab = Resources.Load<GameObject>("Prefabs/Wall");
            GameObject wallGO = Instantiate(wallPrefab);
            return wallGO.GetComponent<WallController>();
        }
        
        private void ResetWall(WallController wall)
        {
            wall.transform.rotation = Quaternion.identity;
            wall.ResetState();
        }
        
        private void CleanupWall(WallController wall)
        {
            // Clean up any references
        }
        
        // Effect pool methods
        private EffectController CreateEffect()
        {
            GameObject effectPrefab = Resources.Load<GameObject>("Prefabs/Effect");
            GameObject effectGO = Instantiate(effectPrefab);
            return effectGO.GetComponent<EffectController>();
        }
        
        private void ResetEffect(EffectController effect)
        {
            effect.StopAllParticles();
            effect.ResetState();
        }
        
        private void CleanupEffect(EffectController effect)
        {
            // Clean up particle systems
        }
        
        // Public access methods
        public TileController GetTile()
        {
            return _tilePool.Get();
        }
        
        public void ReleaseTile(TileController tile)
        {
            _tilePool.Release(tile);
        }
        
        public WallController GetWall()
        {
            return _wallPool.Get();
        }
        
        public void ReleaseWall(WallController wall)
        {
            _wallPool.Release(wall);
        }
        
        public EffectController GetEffect()
        {
            return _effectPool.Get();
        }
        
        public void ReleaseEffect(EffectController effect)
        {
            _effectPool.Release(effect);
        }
        
        // Performance monitoring
        public void LogPoolStatistics()
        {
            Debug.Log($"=== Pool Statistics ===");
            Debug.Log($"Tiles: Active={_tilePool.CountActive}, Pooled={_tilePool.CountInactive}");
            Debug.Log($"Walls: Active={_wallPool.CountActive}, Pooled={_wallPool.CountInactive}");
            Debug.Log($"Effects: Active={_effectPool.CountActive}, Pooled={_effectPool.CountInactive}");
        }
        
        private void OnDestroy()
        {
            _tilePool?.Clear();
            _wallPool?.Clear();
            _effectPool?.Clear();
        }
    }
}
```

### Phase 2: Integration with Existing Systems (2 days)

```csharp
// File: /Assets/Code/Core/Pooling/PooledGridBuilder.cs
namespace WallChess.Core.Pooling
{
    using UnityEngine;
    using WallChess.Grid;

    /// <summary>
    /// Grid builder that uses object pooling
    /// </summary>
    public class PooledGridBuilder : MonoBehaviour
    {
        private UnifiedGridSystem _gridSystem;
        private PoolManager _poolManager;
        
        [Header("Grid Settings")]
        [SerializeField] private int _gridWidth = 9;
        [SerializeField] private int _gridHeight = 9;
        
        private TileController[,] _activeTiles;
        
        private void Start()
        {
            _gridSystem = UnifiedGridSystem.Instance;
            _poolManager = PoolManager.Instance;
            
            BuildGrid();
        }
        
        private void BuildGrid()
        {
            _activeTiles = new TileController[_gridWidth, _gridHeight];
            
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    // Get tile from pool instead of instantiating
                    TileController tile = _poolManager.GetTile();
                    
                    // Position tile
                    Vector3 worldPos = _gridSystem.GridToWorld(x, y);
                    tile.transform.position = worldPos;
                    
                    // Configure tile
                    tile.SetGridPosition(new Vector2Int(x, y));
                    tile.SetColor(GetTileColor(x, y));
                    
                    _activeTiles[x, y] = tile;
                    _gridSystem.SetCellTile(new Vector2Int(x, y), tile.gameObject);
                }
            }
            
            Debug.Log($"Grid built using pooled tiles");
            _poolManager.LogPoolStatistics();
        }
        
        private Color GetTileColor(int x, int y)
        {
            // Alternating colors for chess pattern
            return ((x + y) % 2 == 0) ? Color.white : Color.black;
        }
        
        public void ClearGrid()
        {
            if (_activeTiles == null) return;
            
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (_activeTiles[x, y] != null)
                    {
                        // Return tile to pool instead of destroying
                        _poolManager.ReleaseTile(_activeTiles[x, y]);
                        _activeTiles[x, y] = null;
                    }
                }
            }
            
            Debug.Log("Grid cleared - tiles returned to pool");
            _poolManager.LogPoolStatistics();
        }
        
        private void OnDestroy()
        {
            ClearGrid();
        }
    }
}
```

---

# 4. EXTRACT GOD OBJECT (High Priority)

## Current State Analysis
`WallChessGameManager` has 1000+ lines handling:
- Game state
- Turn management
- Input handling
- Wall placement
- Pawn movement
- Win conditions
- UI updates

## Implementation Plan

### Phase 1: Create Specialized Managers (3 days)

```csharp
// File: /Assets/Code/Core/Managers/ITurnManager.cs
namespace WallChess.Core.Managers
{
    using System;
    using UnityEngine.Events;

    public interface ITurnManager
    {
        int CurrentPlayerIndex { get; }
        int TurnNumber { get; }
        bool IsTurnActive { get; }
        
        UnityEvent<int> OnTurnStarted { get; }
        UnityEvent<int> OnTurnEnded { get; }
        
        void StartTurn(int playerIndex);
        void EndTurn();
        void ForceEndTurn();
    }
}
```

```csharp
// File: /Assets/Code/Core/Managers/TurnManager.cs
namespace WallChess.Core.Managers
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    public class TurnManager : MonoBehaviour, ITurnManager
    {
        [Header("Turn Settings")]
        [SerializeField] private float _turnTimeLimit = 30f;
        [SerializeField] private int _maxPlayers = 4;
        
        private int _currentPlayerIndex;
        private int _turnNumber;
        private float _turnTimer;
        private bool _isTurnActive;
        
        public int CurrentPlayerIndex => _currentPlayerIndex;
        public int TurnNumber => _turnNumber;
        public bool IsTurnActive => _isTurnActive;
        
        public UnityEvent<int> OnTurnStarted { get; } = new UnityEvent<int>();
        public UnityEvent<int> OnTurnEnded { get; } = new UnityEvent<int>();
        public UnityEvent<float> OnTurnTimerUpdated { get; } = new UnityEvent<float>();
        
        private void Update()
        {
            if (_isTurnActive)
            {
                _turnTimer += Time.deltaTime;
                OnTurnTimerUpdated?.Invoke(_turnTimeLimit - _turnTimer);
                
                if (_turnTimer >= _turnTimeLimit)
                {
                    Debug.LogWarning($"Turn timeout for player {_currentPlayerIndex}");
                    ForceEndTurn();
                }
            }
        }
        
        public void StartTurn(int playerIndex)
        {
            if (_isTurnActive)
            {
                Debug.LogWarning("Cannot start turn - turn already active");
                return;
            }
            
            _currentPlayerIndex = playerIndex;
            _turnNumber++;
            _turnTimer = 0f;
            _isTurnActive = true;
            
            OnTurnStarted?.Invoke(playerIndex);
            Debug.Log($"Turn {_turnNumber} started for player {playerIndex}");
        }
        
        public void EndTurn()
        {
            if (!_isTurnActive)
            {
                Debug.LogWarning("Cannot end turn - no active turn");
                return;
            }
            
            _isTurnActive = false;
            OnTurnEnded?.Invoke(_currentPlayerIndex);
            
            // Calculate next player
            int nextPlayer = (_currentPlayerIndex + 1) % _maxPlayers;
            
            Debug.Log($"Turn {_turnNumber} ended. Next player: {nextPlayer}");
            
            // Auto-start next turn after delay
            Invoke(nameof(StartNextTurn), 0.5f);
        }
        
        private void StartNextTurn()
        {
            int nextPlayer = (_currentPlayerIndex + 1) % _maxPlayers;
            StartTurn(nextPlayer);
        }
        
        public void ForceEndTurn()
        {
            Debug.Log($"Force ending turn for player {_currentPlayerIndex}");
            EndTurn();
        }
    }
}
```

```csharp
// File: /Assets/Code/Core/Managers/InputManager.cs
namespace WallChess.Core.Managers
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.InputSystem;

    public class InputManager : MonoBehaviour
    {
        public enum InputMode
        {
            None,
            PawnSelection,
            PawnMovement,
            WallPlacement,
            MenuNavigation
        }
        
        [Header("Input Settings")]
        [SerializeField] private InputMode _currentMode = InputMode.None;
        [SerializeField] private LayerMask _interactableLayers;
        
        // Events
        public UnityEvent<Vector3> OnClickPosition { get; } = new UnityEvent<Vector3>();
        public UnityEvent<GameObject> OnObjectClicked { get; } = new UnityEvent<GameObject>();
        public UnityEvent<Vector3> OnDragStarted { get; } = new UnityEvent<Vector3>();
        public UnityEvent<Vector3> OnDragUpdated { get; } = new UnityEvent<Vector3>();
        public UnityEvent<Vector3> OnDragEnded { get; } = new UnityEvent<Vector3>();
        
        private Camera _mainCamera;
        private bool _isDragging;
        private Vector3 _dragStartPosition;
        
        private void Awake()
        {
            _mainCamera = Camera.main;
        }
        
        public void SetInputMode(InputMode mode)
        {
            if (_currentMode == mode) return;
            
            Debug.Log($"Input mode changed: {_currentMode} -> {mode}");
            _currentMode = mode;
            
            // Reset drag state on mode change
            if (_isDragging)
            {
                CancelDrag();
            }
        }
        
        private void Update()
        {
            if (_currentMode == InputMode.None) return;
            
            HandleMouseInput();
            HandleKeyboardInput();
        }
        
        private void HandleMouseInput()
        {
            // Left click
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
            
            // Drag handling
            if (Input.GetMouseButton(0) && !_isDragging && 
                Vector3.Distance(Input.mousePosition, _dragStartPosition) > 5f)
            {
                StartDrag();
            }
            
            if (_isDragging)
            {
                UpdateDrag();
            }
            
            if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                EndDrag();
            }
            
            // Right click for cancel
            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }
        }
        
        private void HandleClick()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, _interactableLayers))
            {
                OnObjectClicked?.Invoke(hit.collider.gameObject);
                OnClickPosition?.Invoke(hit.point);
                
                _dragStartPosition = Input.mousePosition;
            }
        }
        
        private void StartDrag()
        {
            if (_currentMode != InputMode.PawnMovement && 
                _currentMode != InputMode.WallPlacement)
                return;
                
            _isDragging = true;
            OnDragStarted?.Invoke(GetWorldPosition());
        }
        
        private void UpdateDrag()
        {
            OnDragUpdated?.Invoke(GetWorldPosition());
        }
        
        private void EndDrag()
        {
            _isDragging = false;
            OnDragEnded?.Invoke(GetWorldPosition());
        }
        
        private void CancelDrag()
        {
            _isDragging = false;
            // Trigger cancel event
        }
        
        private void HandleRightClick()
        {
            // Cancel current action
            switch (_currentMode)
            {
                case InputMode.PawnMovement:
                case InputMode.WallPlacement:
                    SetInputMode(InputMode.PawnSelection);
                    break;
            }
        }
        
        private void HandleKeyboardInput()
        {
            // ESC for menu
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetInputMode(InputMode.MenuNavigation);
            }
            
            // Tab for switching between pawns
            if (Input.GetKeyDown(KeyCode.Tab) && _currentMode == InputMode.PawnSelection)
            {
                // Trigger pawn switch event
            }
        }
        
        private Vector3 GetWorldPosition()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f))
            {
                return hit.point;
            }
            
            // Fallback to plane intersection
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (groundPlane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }
            
            return Vector3.zero;
        }
    }
}
```

```csharp
// File: /Assets/Code/Core/Managers/GameFlowManager.cs
namespace WallChess.Core.Managers
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using WallChess.Core.UnifiedStateSystem;

    /// <summary>
    /// Orchestrates high-level game flow between managers
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        [Header("Manager References")]
        [SerializeField] private UnifiedStateManager _stateManager;
        [SerializeField] private TurnManager _turnManager;
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private MovementManager _movementManager;
        [SerializeField] private WallPlacementManager _wallManager;
        
        [Header("Game Settings")]
        [SerializeField] private int _numberOfPlayers = 2;
        [SerializeField] private int _wallsPerPlayer = 9;
        
        // Events
        public UnityEvent<int> OnGameStarted { get; } = new UnityEvent<int>();
        public UnityEvent<int> OnGameEnded { get; } = new UnityEvent<int>();
        public UnityEvent<int> OnPlayerWon { get; } = new UnityEvent<int>();
        
        private void Start()
        {
            InitializeManagers();
            SubscribeToEvents();
        }
        
        private void InitializeManagers()
        {
            // Set up manager dependencies
            _movementManager.Initialize(_turnManager, _inputManager);
            _wallManager.Initialize(_turnManager, _inputManager);
            
            // Configure state manager
            _stateManager.TransitionTo<GameStartState>();
        }
        
        private void SubscribeToEvents()
        {
            // Turn events
            _turnManager.OnTurnStarted.AddListener(HandleTurnStarted);
            _turnManager.OnTurnEnded.AddListener(HandleTurnEnded);
            
            // Movement events
            _movementManager.OnPawnMoved.AddListener(HandlePawnMoved);
            _movementManager.OnPawnReachedGoal.AddListener(HandlePawnReachedGoal);
            
            // Wall events
            _wallManager.OnWallPlaced.AddListener(HandleWallPlaced);
            
            // State events
            _stateManager.OnStateTransition.AddListener(HandleStateTransition);
        }
        
        private void HandleTurnStarted(int playerIndex)
        {
            Debug.Log($"[GameFlow] Turn started for player {playerIndex}");
            
            // Enable appropriate input mode
            if (IsPlayerHuman(playerIndex))
            {
                _inputManager.SetInputMode(InputManager.InputMode.PawnSelection);
            }
            else
            {
                // Trigger AI
                TriggerAI(playerIndex);
            }
        }
        
        private void HandleTurnEnded(int playerIndex)
        {
            Debug.Log($"[GameFlow] Turn ended for player {playerIndex}");
            
            // Disable input
            _inputManager.SetInputMode(InputManager.InputMode.None);
            
            // Check win conditions
            if (CheckWinCondition(playerIndex))
            {
                EndGame(playerIndex);
            }
        }
        
        private void HandlePawnMoved(int playerIndex, Vector2Int from, Vector2Int to)
        {
            Debug.Log($"[GameFlow] Player {playerIndex} moved pawn from {from} to {to}");
            
            // Update game state
            _stateManager.TransitionTo<TurnValidationState>();
        }
        
        private void HandlePawnReachedGoal(int playerIndex)
        {
            Debug.Log($"[GameFlow] Player {playerIndex} reached goal!");
            OnPlayerWon?.Invoke(playerIndex);
            EndGame(playerIndex);
        }
        
        private void HandleWallPlaced(int playerIndex, Vector2Int position)
        {
            Debug.Log($"[GameFlow] Player {playerIndex} placed wall at {position}");
            
            // Update game state
            _stateManager.TransitionTo<TurnValidationState>();
        }
        
        private void HandleStateTransition(string from, string to)
        {
            Debug.Log($"[GameFlow] State transition: {from} -> {to}");
            
            // Update input mode based on state
            switch (to)
            {
                case "PlayerTurn":
                    _inputManager.SetInputMode(InputManager.InputMode.PawnSelection);
                    break;
                case "PawnMoving":
                    _inputManager.SetInputMode(InputManager.InputMode.PawnMovement);
                    break;
                case "WallPlacement":
                    _inputManager.SetInputMode(InputManager.InputMode.WallPlacement);
                    break;
                case "GameOver":
                    _inputManager.SetInputMode(InputManager.InputMode.None);
                    break;
            }
        }
        
        public void StartNewGame()
        {
            Debug.Log("[GameFlow] Starting new game");
            
            OnGameStarted?.Invoke(_numberOfPlayers);
            _stateManager.TransitionTo<GameStartState>();
            _turnManager.StartTurn(0);
        }
        
        private void EndGame(int winnerIndex)
        {
            Debug.Log($"[GameFlow] Game ended. Winner: Player {winnerIndex}");
            
            OnGameEnded?.Invoke(winnerIndex);
            _stateManager.TransitionTo<GameOverState>();
        }
        
        private bool IsPlayerHuman(int playerIndex)
        {
            // TODO: Implement player type checking
            return playerIndex == 0; // Assume first player is human for now
        }
        
        private void TriggerAI(int playerIndex)
        {
            // TODO: Trigger AI decision making
            Debug.Log($"[GameFlow] Triggering AI for player {playerIndex}");
        }
        
        private bool CheckWinCondition(int playerIndex)
        {
            // TODO: Implement win condition checking
            return false;
        }
    }
}
```

### Phase 2: Dependency Injection Setup (1 day)

```csharp
// File: /Assets/Code/Core/DI/ServiceLocator.cs
namespace WallChess.Core.DI
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Simple service locator for dependency injection
    /// </summary>
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator _instance;
        private Dictionary<Type, object> _services;
        
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("ServiceLocator");
                    _instance = go.AddComponent<ServiceLocator>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            _services = new Dictionary<Type, object>();
            
            RegisterServices();
        }
        
        private void RegisterServices()
        {
            // Auto-register all manager components
            RegisterService<ITurnManager>(GetComponent<TurnManager>());
            RegisterService<IInputManager>(GetComponent<InputManager>());
            RegisterService<IMovementManager>(GetComponent<MovementManager>());
            RegisterService<IWallManager>(GetComponent<WallPlacementManager>());
        }
        
        public void RegisterService<T>(T service) where T : class
        {
            Type type = typeof(T);
            
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"Service {type.Name} already registered");
                return;
            }
            
            _services[type] = service;
            Debug.Log($"Service registered: {type.Name}");
        }
        
        public T GetService<T>() where T : class
        {
            Type type = typeof(T);
            
            if (_services.TryGetValue(type, out object service))
            {
                return service as T;
            }
            
            Debug.LogError($"Service {type.Name} not found!");
            return null;
        }
        
        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
        
        public void UnregisterService<T>() where T : class
        {
            Type type = typeof(T);
            
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
                Debug.Log($"Service unregistered: {type.Name}");
            }
        }
    }
}
```

---

# 5. CLEAN CODEBASE (Immediate Priority)

## Implementation Plan

### Phase 1: Audit and Analysis (1 day)

```csharp
// File: /Assets/Editor/CodebaseAuditor.cs
namespace WallChess.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public class CodebaseAuditor : EditorWindow
    {
        private List<FileAuditResult> _auditResults = new List<FileAuditResult>();
        private Vector2 _scrollPosition;
        
        [System.Serializable]
        public class FileAuditResult
        {
            public string filePath;
            public string fileName;
            public long fileSize;
            public int lineCount;
            public bool hasReferences;
            public bool isObsolete;
            public bool isDuplicate;
            public List<string> issues = new List<string>();
            
            public float RiskScore
            {
                get
                {
                    float score = 0;
                    if (!hasReferences) score += 3;
                    if (isObsolete) score += 2;
                    if (isDuplicate) score += 2;
                    if (fileName.Contains("OLD") || fileName.Contains("Backup")) score += 1;
                    if (fileName.Contains("Test") || fileName.Contains("Demo")) score += 0.5f;
                    return score;
                }
            }
        }
        
        [MenuItem("WallChess/Tools/Codebase Auditor")]
        public static void ShowWindow()
        {
            GetWindow<CodebaseAuditor>("Codebase Auditor");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Codebase Audit Tool", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Run Full Audit"))
            {
                RunAudit();
            }
            
            if (GUILayout.Button("Export Audit Report"))
            {
                ExportReport();
            }
            
            EditorGUILayout.Space();
            
            if (_auditResults.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {_auditResults.Count} files to review");
                
                // Sort by risk score
                var sortedResults = _auditResults.OrderByDescending(r => r.RiskScore).ToList();
                
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                
                foreach (var result in sortedResults)
                {
                    DrawFileResult(result);
                }
                
                EditorGUILayout.EndScrollView();
            }
        }
        
        private void RunAudit()
        {
            _auditResults.Clear();
            
            string[] allScripts = Directory.GetFiles(
                Application.dataPath,
                "*.cs",
                SearchOption.AllDirectories
            );
            
            foreach (string scriptPath in allScripts)
            {
                AuditFile(scriptPath);
            }
            
            Debug.Log($"Audit complete. Found {_auditResults.Count} files with issues.");
        }
        
        private void AuditFile(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            
            var result = new FileAuditResult
            {
                filePath = filePath,
                fileName = fileInfo.Name,
                fileSize = fileInfo.Length,
                lineCount = File.ReadAllLines(filePath).Length
            };
            
            // Check for references
            result.hasReferences = CheckFileReferences(filePath);
            
            // Check for obsolete markers
            string content = File.ReadAllText(filePath);
            result.isObsolete = content.Contains("[Obsolete") || 
                               content.Contains("// OLD") ||
                               content.Contains("// DEPRECATED");
            
            // Check for duplicates
            result.isDuplicate = CheckForDuplicates(filePath);
            
            // Identify issues
            if (!result.hasReferences)
                result.issues.Add("No references found");
            
            if (result.isObsolete)
                result.issues.Add("Contains obsolete code");
            
            if (result.isDuplicate)
                result.issues.Add("Possible duplicate");
            
            if (fileInfo.Name.Contains("OLD") || fileInfo.Name.Contains("Backup"))
                result.issues.Add("Suspicious filename");
            
            if (result.lineCount > 500)
                result.issues.Add("Large file (>500 lines)");
            
            if (result.issues.Count > 0)
            {
                _auditResults.Add(result);
            }
        }
        
        private bool CheckFileReferences(string filePath)
        {
            // Convert to relative path
            string relativePath = filePath.Replace(Application.dataPath, "Assets");
            
            // Get asset
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(relativePath);
            if (asset == null) return false;
            
            // Find references
            string[] guids = AssetDatabase.FindAssets("t:Scene t:Prefab");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string[] dependencies = AssetDatabase.GetDependencies(path);
                
                if (dependencies.Contains(relativePath))
                    return true;
            }
            
            return false;
        }
        
        private bool CheckForDuplicates(string filePath)
        {
            // Simple duplicate detection based on file size and name similarity
            FileInfo fileInfo = new FileInfo(filePath);
            string baseName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            
            string directory = fileInfo.DirectoryName;
            var similarFiles = Directory.GetFiles(directory, $"*{baseName}*.cs");
            
            return similarFiles.Length > 1;
        }
        
        private void DrawFileResult(FileAuditResult result)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Color code by risk
            Color originalColor = GUI.color;
            if (result.RiskScore > 5) GUI.color = Color.red;
            else if (result.RiskScore > 3) GUI.color = Color.yellow;
            
            EditorGUILayout.LabelField($"{result.fileName} (Risk: {result.RiskScore:F1})");
            
            GUI.color = originalColor;
            
            EditorGUILayout.LabelField($"Path: {result.filePath}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Size: {result.fileSize} bytes | Lines: {result.lineCount}", EditorStyles.miniLabel);
            
            if (result.issues.Count > 0)
            {
                EditorGUILayout.LabelField("Issues:", EditorStyles.boldLabel);
                foreach (string issue in result.issues)
                {
                    EditorGUILayout.LabelField($"  • {issue}", EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Open", GUILayout.Width(60)))
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(result.filePath, 0);
            }
            
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog(
                    "Delete File",
                    $"Are you sure you want to delete {result.fileName}?",
                    "Delete",
                    "Cancel"))
                {
                    SafeDeleteFile(result.filePath);
                    _auditResults.Remove(result);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        private void SafeDeleteFile(string filePath)
        {
            // Create backup first
            string backupDir = Path.Combine(Application.dataPath, "../Backups/DeletedFiles");
            Directory.CreateDirectory(backupDir);
            
            string fileName = Path.GetFileName(filePath);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(backupDir, $"{timestamp}_{fileName}");
            
            File.Copy(filePath, backupPath);
            
            // Delete file and meta
            File.Delete(filePath);
            File.Delete(filePath + ".meta");
            
            AssetDatabase.Refresh();
            
            Debug.Log($"File deleted and backed up: {fileName}");
        }
        
        private void ExportReport()
        {
            string reportPath = EditorUtility.SaveFilePanel(
                "Save Audit Report",
                Application.dataPath,
                "CodebaseAudit_" + DateTime.Now.ToString("yyyyMMdd"),
                "txt"
            );
            
            if (string.IsNullOrEmpty(reportPath)) return;
            
            using (StreamWriter writer = new StreamWriter(reportPath))
            {
                writer.WriteLine("WALLCHESSQUIDOR CODEBASE AUDIT REPORT");
                writer.WriteLine($"Generated: {DateTime.Now}");
                writer.WriteLine($"Total Issues Found: {_auditResults.Count}");
                writer.WriteLine();
                
                var sortedResults = _auditResults.OrderByDescending(r => r.RiskScore).ToList();
                
                foreach (var result in sortedResults)
                {
                    writer.WriteLine($"File: {result.fileName}");
                    writer.WriteLine($"Path: {result.filePath}");
                    writer.WriteLine($"Risk Score: {result.RiskScore:F1}");
                    writer.WriteLine($"Size: {result.fileSize} bytes | Lines: {result.lineCount}");
                    writer.WriteLine("Issues:");
                    
                    foreach (string issue in result.issues)
                    {
                        writer.WriteLine($"  - {issue}");
                    }
                    
                    writer.WriteLine();
                }
            }
            
            Debug.Log($"Report exported to: {reportPath}");
        }
    }
}
```

### Phase 2: Safe Cleanup Procedures (1 day)

```bash
# Cleanup Script: cleanup_codebase.sh
#!/bin/bash

# Create backup
echo "Creating backup..."
BACKUP_DIR="../Backups/$(date +%Y%m%d_%H%M%S)"
mkdir -p $BACKUP_DIR
cp -r Assets $BACKUP_DIR/

# Remove obviously obsolete files
echo "Removing obsolete files..."
find Assets -name "*OLD*" -type f -delete
find Assets -name "*Backup*" -type f -delete
find Assets -name "*Copy*" -type f -delete

# Clean empty directories
echo "Cleaning empty directories..."
find Assets -type d -empty -delete

# Generate report
echo "Generating cleanup report..."
echo "Cleanup Report - $(date)" > cleanup_report.txt
echo "Backup created at: $BACKUP_DIR" >> cleanup_report.txt
echo "Files removed:" >> cleanup_report.txt
git status --porcelain | grep "^D" >> cleanup_report.txt

echo "Cleanup complete. Review cleanup_report.txt"
```

---

# 6. UNIFY INPUT SYSTEM (Medium Priority)

## Implementation Plan

### Phase 1: Input Abstraction Layer (2 days)

```csharp
// File: /Assets/Code/Input/UnifiedInput/IInputHandler.cs
namespace WallChess.Input
{
    using UnityEngine;
    using UnityEngine.Events;

    public interface IInputHandler
    {
        bool IsEnabled { get; set; }
        
        UnityEvent<Vector3> OnPointerDown { get; }
        UnityEvent<Vector3> OnPointerUp { get; }
        UnityEvent<Vector3> OnPointerMove { get; }
        UnityEvent<float> OnScroll { get; }
        UnityEvent<int> OnKeyPressed { get; }
        
        void ProcessInput();
    }
}
```

```csharp
// File: /Assets/Code/Input/UnifiedInput/UnifiedInputSystem.cs
namespace WallChess.Input
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.InputSystem;

    public class UnifiedInputSystem : MonoBehaviour
    {
        [System.Serializable]
        public class InputContext
        {
            public string name;
            public bool isActive;
            public int priority;
            public List<string> allowedActions;
        }
        
        [Header("Input Settings")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private List<InputContext> _contexts;
        
        private Dictionary<string, IInputHandler> _handlers;
        private InputContext _activeContext;
        
        // Unified events
        public UnityEvent<string, object> OnInputAction { get; } = new UnityEvent<string, object>();
        
        private void Awake()
        {
            _handlers = new Dictionary<string, IInputHandler>();
            InitializeInputActions();
        }
        
        private void InitializeInputActions()
        {
            // Register default handlers
            RegisterHandler("Mouse", new MouseInputHandler());
            RegisterHandler("Touch", new TouchInputHandler());
            RegisterHandler("Keyboard", new KeyboardInputHandler());
            RegisterHandler("Gamepad", new GamepadInputHandler());
        }
        
        public void RegisterHandler(string name, IInputHandler handler)
        {
            if (_handlers.ContainsKey(name))
            {
                Debug.LogWarning($"Handler {name} already registered");
                return;
            }
            
            _handlers[name] = handler;
            
            // Subscribe to handler events
            handler.OnPointerDown.AddListener(pos => HandlePointerDown(name, pos));
            handler.OnPointerUp.AddListener(pos => HandlePointerUp(name, pos));
            handler.OnPointerMove.AddListener(pos => HandlePointerMove(name, pos));
        }
        
        public void SetContext(string contextName)
        {
            var context = _contexts.Find(c => c.name == contextName);
            if (context == null)
            {
                Debug.LogError($"Context {contextName} not found");
                return;
            }
            
            _activeContext = context;
            Debug.Log($"Input context set to: {contextName}");
        }
        
        private void Update()
        {
            if (_activeContext == null || !_activeContext.isActive) return;
            
            // Process all active handlers
            foreach (var handler in _handlers.Values)
            {
                if (handler.IsEnabled)
                {
                    handler.ProcessInput();
                }
            }
        }
        
        private void HandlePointerDown(string source, Vector3 position)
        {
            if (!IsActionAllowed("PointerDown")) return;
            
            OnInputAction?.Invoke("PointerDown", new PointerEventData
            {
                source = source,
                position = position,
                timestamp = Time.time
            });
        }
        
        private void HandlePointerUp(string source, Vector3 position)
        {
            if (!IsActionAllowed("PointerUp")) return;
            
            OnInputAction?.Invoke("PointerUp", new PointerEventData
            {
                source = source,
                position = position,
                timestamp = Time.time
            });
        }
        
        private void HandlePointerMove(string source, Vector3 position)
        {
            if (!IsActionAllowed("PointerMove")) return;
            
            OnInputAction?.Invoke("PointerMove", new PointerEventData
            {
                source = source,
                position = position,
                timestamp = Time.time
            });
        }
        
        private bool IsActionAllowed(string action)
        {
            if (_activeContext == null) return false;
            return _activeContext.allowedActions.Contains(action);
        }
        
        [System.Serializable]
        public class PointerEventData
        {
            public string source;
            public Vector3 position;
            public float timestamp;
        }
    }
}
```

```csharp
// File: /Assets/Code/Input/UnifiedInput/InputHandlers/MouseInputHandler.cs
namespace WallChess.Input
{
    using UnityEngine;
    using UnityEngine.Events;

    public class MouseInputHandler : IInputHandler
    {
        public bool IsEnabled { get; set; } = true;
        
        public UnityEvent<Vector3> OnPointerDown { get; } = new UnityEvent<Vector3>();
        public UnityEvent<Vector3> OnPointerUp { get; } = new UnityEvent<Vector3>();
        public UnityEvent<Vector3> OnPointerMove { get; } = new UnityEvent<Vector3>();
        public UnityEvent<float> OnScroll { get; } = new UnityEvent<float>();
        public UnityEvent<int> OnKeyPressed { get; } = new UnityEvent<int>();
        
        private Camera _mainCamera;
        private Vector3 _lastMousePosition;
        
        public MouseInputHandler()
        {
            _mainCamera = Camera.main;
        }
        
        public void ProcessInput()
        {
            if (!IsEnabled) return;
            
            // Mouse button events
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                OnPointerDown?.Invoke(GetWorldPosition());
            }
            
            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                OnPointerUp?.Invoke(GetWorldPosition());
            }
            
            // Mouse movement
            if (UnityEngine.Input.mousePosition != _lastMousePosition)
            {
                _lastMousePosition = UnityEngine.Input.mousePosition;
                OnPointerMove?.Invoke(GetWorldPosition());
            }
            
            // Mouse scroll
            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                OnScroll?.Invoke(scroll);
            }
        }
        
        private Vector3 GetWorldPosition()
        {
            if (_mainCamera == null) return Vector3.zero;
            
            Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f))
            {
                return hit.point;
            }
            
            // Fallback to plane intersection
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (groundPlane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }
            
            return Vector3.zero;
        }
    }
}
```

---

# 7. DECOUPLE AI SYSTEM (Medium Priority)

## Implementation Plan

### Phase 1: AI Architecture Design (2 days)

```csharp
// File: /Assets/Code/AI/Core/IAIStrategy.cs
namespace WallChess.AI
{
    using System;
    using UnityEngine;

    public interface IAIStrategy
    {
        string Name { get; }
        float ThinkTime { get; set; }
        
        AIDecision MakeDecision(AIContext context);
        float EvaluatePosition(AIContext context);
    }
    
    [System.Serializable]
    public class AIContext
    {
        public int playerIndex;
        public Vector2Int currentPosition;
        public Vector2Int goalPosition;
        public int wallsRemaining;
        public float timeRemaining;
        public GridState gridState;
        public PlayerState[] allPlayers;
    }
    
    [System.Serializable]
    public class AIDecision
    {
        public enum DecisionType
        {
            MovePawn,
            PlaceWall,
            Pass
        }
        
        public DecisionType type;
        public Vector2Int targetPosition;
        public WallPlacement wallPlacement;
        public float confidence;
        public string reasoning;
    }
    
    [System.Serializable]
    public class GridState
    {
        public int width;
        public int height;
        public bool[,] occupied;
        public List<WallPlacement> walls;
    }
    
    [System.Serializable]
    public class PlayerState
    {
        public int playerIndex;
        public Vector2Int position;
        public int wallsRemaining;
        public bool isAI;
    }
    
    [System.Serializable]
    public class WallPlacement
    {
        public Vector2Int position;
        public WallOrientation orientation;
    }
}
```

```csharp
// File: /Assets/Code/AI/Core/AIManager.cs
namespace WallChess.AI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    public class AIManager : MonoBehaviour
    {
        [Header("AI Configuration")]
        [SerializeField] private AIProfile[] _aiProfiles;
        [SerializeField] private float _defaultThinkTime = 1.5f;
        
        private Dictionary<int, IAIStrategy> _playerStrategies;
        private Coroutine _thinkingCoroutine;
        
        public UnityEvent<int, AIDecision> OnAIDecisionMade { get; } = new UnityEvent<int, AIDecision>();
        public UnityEvent<int> OnAIThinkingStarted { get; } = new UnityEvent<int>();
        public UnityEvent<int> OnAIThinkingCompleted { get; } = new UnityEvent<int>();
        
        [System.Serializable]
        public class AIProfile
        {
            public string name;
            public AIType type;
            public DifficultyLevel difficulty;
            public float thinkTimeMultiplier = 1f;
            public AnimationCurve decisionCurve;
        }
        
        public enum AIType
        {
            Minimax,
            AlphaBeta,
            MonteCarlo,
            Neural,
            Hybrid
        }
        
        public enum DifficultyLevel
        {
            Easy,
            Medium,
            Hard,
            Expert
        }
        
        private void Awake()
        {
            _playerStrategies = new Dictionary<int, IAIStrategy>();
        }
        
        public void AssignAIToPlayer(int playerIndex, string profileName)
        {
            var profile = Array.Find(_aiProfiles, p => p.name == profileName);
            if (profile == null)
            {
                Debug.LogError($"AI Profile {profileName} not found");
                return;
            }
            
            IAIStrategy strategy = CreateStrategy(profile);
            _playerStrategies[playerIndex] = strategy;
            
            Debug.Log($"Assigned {strategy.Name} AI to player {playerIndex}");
        }
        
        private IAIStrategy CreateStrategy(AIProfile profile)
        {
            switch (profile.type)
            {
                case AIType.Minimax:
                    return new MinimaxStrategy(profile.difficulty);
                    
                case AIType.AlphaBeta:
                    return new AlphaBetaStrategy(profile.difficulty);
                    
                case AIType.MonteCarlo:
                    return new MonteCarloStrategy(profile.difficulty);
                    
                case AIType.Neural:
                    return new NeuralNetworkStrategy(profile.difficulty);
                    
                case AIType.Hybrid:
                    return new HybridStrategy(profile.difficulty);
                    
                default:
                    return new MinimaxStrategy(DifficultyLevel.Medium);
            }
        }
        
        public void RequestAIDecision(int playerIndex, AIContext context)
        {
            if (!_playerStrategies.ContainsKey(playerIndex))
            {
                Debug.LogError($"No AI assigned to player {playerIndex}");
                return;
            }
            
            if (_thinkingCoroutine != null)
            {
                StopCoroutine(_thinkingCoroutine);
            }
            
            _thinkingCoroutine = StartCoroutine(ProcessAIDecision(playerIndex, context));
        }
        
        private IEnumerator ProcessAIDecision(int playerIndex, AIContext context)
        {
            OnAIThinkingStarted?.Invoke(playerIndex);
            
            IAIStrategy strategy = _playerStrategies[playerIndex];
            float thinkTime = strategy.ThinkTime;
            
            // Start decision calculation in background
            AIDecision decision = null;
            bool decisionReady = false;
            
            // Run AI calculation asynchronously
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    decision = strategy.MakeDecision(context);
                    decisionReady = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"AI Error: {e.Message}");
                    decision = new AIDecision
                    {
                        type = AIDecision.DecisionType.Pass,
                        confidence = 0,
                        reasoning = "Error occurred"
                    };
                    decisionReady = true;
                }
            });
            
            // Wait for minimum think time
            float elapsed = 0;
            while (elapsed < thinkTime || !decisionReady)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            OnAIThinkingCompleted?.Invoke(playerIndex);
            OnAIDecisionMade?.Invoke(playerIndex, decision);
            
            _thinkingCoroutine = null;
        }
        
        public float EvaluateCurrentPosition(int playerIndex, AIContext context)
        {
            if (!_playerStrategies.ContainsKey(playerIndex))
                return 0;
                
            return _playerStrategies[playerIndex].EvaluatePosition(context);
        }
    }
}
```

```csharp
// File: /Assets/Code/AI/Strategies/MinimaxStrategy.cs
namespace WallChess.AI
{
    using System.Collections.Generic;
    using UnityEngine;

    public class MinimaxStrategy : IAIStrategy
    {
        private readonly AIManager.DifficultyLevel _difficulty;
        private readonly int _maxDepth;
        
        public string Name => $"Minimax ({_difficulty})";
        public float ThinkTime { get; set; }
        
        public MinimaxStrategy(AIManager.DifficultyLevel difficulty)
        {
            _difficulty = difficulty;
            
            // Set depth based on difficulty
            switch (difficulty)
            {
                case AIManager.DifficultyLevel.Easy:
                    _maxDepth = 2;
                    ThinkTime = 0.5f;
                    break;
                case AIManager.DifficultyLevel.Medium:
                    _maxDepth = 4;
                    ThinkTime = 1.0f;
                    break;
                case AIManager.DifficultyLevel.Hard:
                    _maxDepth = 6;
                    ThinkTime = 2.0f;
                    break;
                case AIManager.DifficultyLevel.Expert:
                    _maxDepth = 8;
                    ThinkTime = 3.0f;
                    break;
            }
        }
        
        public AIDecision MakeDecision(AIContext context)
        {
            // Calculate best move using minimax
            var possibleMoves = GeneratePossibleMoves(context);
            
            if (possibleMoves.Count == 0)
            {
                return new AIDecision
                {
                    type = AIDecision.DecisionType.Pass,
                    confidence = 0,
                    reasoning = "No valid moves available"
                };
            }
            
            float bestScore = float.MinValue;
            AIDecision bestDecision = null;
            
            foreach (var move in possibleMoves)
            {
                float score = Minimax(context, move, _maxDepth, false);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDecision = move;
                }
            }
            
            bestDecision.confidence = Mathf.Clamp01((bestScore + 100) / 200);
            return bestDecision;
        }
        
        private float Minimax(AIContext context, AIDecision move, int depth, bool isMaximizing)
        {
            if (depth == 0 || IsTerminalState(context))
            {
                return EvaluatePosition(context);
            }
            
            if (isMaximizing)
            {
                float maxEval = float.MinValue;
                var moves = GeneratePossibleMoves(context);
                
                foreach (var childMove in moves)
                {
                    AIContext newContext = ApplyMove(context, childMove);
                    float eval = Minimax(newContext, childMove, depth - 1, false);
                    maxEval = Mathf.Max(maxEval, eval);
                }
                
                return maxEval;
            }
            else
            {
                float minEval = float.MaxValue;
                var moves = GeneratePossibleMoves(context);
                
                foreach (var childMove in moves)
                {
                    AIContext newContext = ApplyMove(context, childMove);
                    float eval = Minimax(newContext, childMove, depth - 1, true);
                    minEval = Mathf.Min(minEval, eval);
                }
                
                return minEval;
            }
        }
        
        public float EvaluatePosition(AIContext context)
        {
            float score = 0;
            
            // Distance to goal (negative is better)
            float distanceToGoal = Vector2Int.Distance(
                context.currentPosition,
                context.goalPosition
            );
            score -= distanceToGoal * 10;
            
            // Wall advantage
            score += context.wallsRemaining * 5;
            
            // Position control (center is better)
            float centerDistance = Vector2Int.Distance(
                context.currentPosition,
                new Vector2Int(context.gridState.width / 2, context.gridState.height / 2)
            );
            score -= centerDistance * 2;
            
            // Mobility (number of possible moves)
            int mobility = CalculateMobility(context);
            score += mobility * 3;
            
            return score;
        }
        
        private List<AIDecision> GeneratePossibleMoves(AIContext context)
        {
            List<AIDecision> moves = new List<AIDecision>();
            
            // Generate pawn moves
            var validPawnMoves = GetValidPawnMoves(context);
            foreach (var move in validPawnMoves)
            {
                moves.Add(new AIDecision
                {
                    type = AIDecision.DecisionType.MovePawn,
                    targetPosition = move,
                    reasoning = "Valid pawn move"
                });
            }
            
            // Generate wall placements (if walls remaining)
            if (context.wallsRemaining > 0)
            {
                var validWallPlacements = GetValidWallPlacements(context);
                foreach (var wall in validWallPlacements)
                {
                    moves.Add(new AIDecision
                    {
                        type = AIDecision.DecisionType.PlaceWall,
                        wallPlacement = wall,
                        reasoning = "Valid wall placement"
                    });
                }
            }
            
            return moves;
        }
        
        private List<Vector2Int> GetValidPawnMoves(AIContext context)
        {
            // TODO: Implement based on game rules
            List<Vector2Int> moves = new List<Vector2Int>();
            
            Vector2Int[] directions = {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };
            
            foreach (var dir in directions)
            {
                Vector2Int newPos = context.currentPosition + dir;
                if (IsValidPosition(newPos, context.gridState))
                {
                    moves.Add(newPos);
                }
            }
            
            return moves;
        }
        
        private List<WallPlacement> GetValidWallPlacements(AIContext context)
        {
            // TODO: Implement wall placement validation
            return new List<WallPlacement>();
        }
        
        private bool IsValidPosition(Vector2Int pos, GridState gridState)
        {
            return pos.x >= 0 && pos.x < gridState.width &&
                   pos.y >= 0 && pos.y < gridState.height &&
                   !gridState.occupied[pos.x, pos.y];
        }
        
        private bool IsTerminalState(AIContext context)
        {
            return context.currentPosition == context.goalPosition;
        }
        
        private AIContext ApplyMove(AIContext context, AIDecision move)
        {
            // Create a deep copy of context and apply the move
            // TODO: Implement context cloning and move application
            return context;
        }
        
        private int CalculateMobility(AIContext context)
        {
            return GetValidPawnMoves(context).Count;
        }
    }
}
```

---

## Testing Strategy for All Refactors

```csharp
// File: /Assets/Tests/Integration/RefactoringTests.cs
namespace WallChess.Tests.Integration
{
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using System.Collections;

    public class RefactoringTests
    {
        [Test]
        public void StateManagement_SingleSourceOfTruth()
        {
            // Verify only one state manager exists
            var stateManagers = Object.FindObjectsOfType<UnifiedStateManager>();
            Assert.AreEqual(1, stateManagers.Length);
        }
        
        [Test]
        public void GridSystem_ConsistentCoordinates()
        {
            var grid = UnifiedGridSystem.Instance;
            Vector2Int testPos = new Vector2Int(4, 4);
            Vector3 worldPos = grid.GridToWorld(testPos);
            Vector2Int backToGrid = grid.WorldToGrid(worldPos);
            
            Assert.AreEqual(testPos, backToGrid);
        }
        
        [UnityTest]
        public IEnumerator ObjectPooling_ReuseObjects()
        {
            var poolManager = PoolManager.Instance;
            
            var tile1 = poolManager.GetTile();
            int instanceId1 = tile1.GetInstanceID();
            
            poolManager.ReleaseTile(tile1);
            yield return null;
            
            var tile2 = poolManager.GetTile();
            int instanceId2 = tile2.GetInstanceID();
            
            Assert.AreEqual(instanceId1, instanceId2, "Object should be reused from pool");
        }
        
        [Test]
        public void Managers_ProperSeparation()
        {
            // Verify each manager has single responsibility
            var turnManager = Object.FindObjectOfType<TurnManager>();
            var inputManager = Object.FindObjectOfType<InputManager>();
            
            Assert.IsNotNull(turnManager);
            Assert.IsNotNull(inputManager);
            
            // Verify no cross-dependencies
            var turnManagerType = turnManager.GetType();
            var fields = turnManagerType.GetFields(
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance
            );
            
            foreach (var field in fields)
            {
                Assert.IsFalse(
                    field.FieldType == typeof(InputManager),
                    "TurnManager should not directly reference InputManager"
                );
            }
        }
    }
}
```

---

## Performance Benchmarks

```csharp
// File: /Assets/Tests/Performance/PerformanceBenchmarks.cs
namespace WallChess.Tests.Performance
{
    using Unity.PerformanceTesting;
    using UnityEngine;
    using NUnit.Framework;

    public class PerformanceBenchmarks
    {
        [Test, Performance]
        public void Benchmark_GridConversion()
        {
            Measure.Method(() =>
            {
                var grid = UnifiedGridSystem.Instance;
                for (int i = 0; i < 1000; i++)
                {
                    Vector2Int gridPos = new Vector2Int(
                        Random.Range(0, 9),
                        Random.Range(0, 9)
                    );
                    Vector3 worldPos = grid.GridToWorld(gridPos);
                    grid.WorldToGrid(worldPos);
                }
            })
            .WarmupCount(10)
            .MeasurementCount(100)
            .Run();
        }
        
        [Test, Performance]
        public void Benchmark_ObjectPooling()
        {
            Measure.Method(() =>
            {
                var poolManager = PoolManager.Instance;
                for (int i = 0; i < 100; i++)
                {
                    var tile = poolManager.GetTile();
                    poolManager.ReleaseTile(tile);
                }
            })
            .WarmupCount(5)
            .MeasurementCount(50)
            .Run();
        }
        
        [Test, Performance]
        public void Benchmark_StateTransitions()
        {
            Measure.Method(() =>
            {
                var stateManager = Object.FindObjectOfType<UnifiedStateManager>();
                stateManager.TransitionTo<PlayerTurnState>();
                stateManager.TransitionTo<PawnMovingState>();
                stateManager.TransitionTo<TurnValidationState>();
            })
            .WarmupCount(5)
            .MeasurementCount(50)
            .Run();
        }
    }
}
```

---

## Implementation Timeline

### Week 1: Critical & Immediate
- Day 1-2: Clean Codebase (Priority 5)
- Day 3-5: Consolidate State Management (Priority 1)

### Week 2: High Priority Systems
- Day 6-7: Simplify Grid System (Priority 2)
- Day 8-9: Implement Object Pooling (Priority 3)
- Day 10: Extract God Object - Phase 1 (Priority 4)

### Week 3: Complete High Priority & Start Medium
- Day 11-12: Extract God Object - Phase 2 (Priority 4)
- Day 13-14: Unify Input System (Priority 6)
- Day 15: Decouple AI System - Phase 1 (Priority 7)

### Week 4: Finalization
- Day 16-17: Decouple AI System - Phase 2 (Priority 7)
- Day 18-19: Integration testing
- Day 20: Performance optimization and documentation

## Success Criteria

### Per Refactor:
1. **State Management**: Single source of truth, no race conditions, all tests pass
2. **Grid System**: One coordinate system, consistent conversions, improved performance
3. **Object Pooling**: 50%+ reduction in GC allocations, stable frame rate
4. **God Object**: No class > 300 lines, clear separation of concerns
5. **Clean Codebase**: No unused files, clear organization, comprehensive documentation
6. **Input System**: Platform-agnostic input, easy to extend, consistent behavior
7. **AI System**: Modular AI strategies, easy difficulty adjustment, testable

### Overall Project:
- Build time reduced by 30%+
- Runtime performance improved by 40%+
- Code coverage > 70%
- No critical bugs introduced
- Clear documentation for all systems

## Rollback Procedures

For each refactor phase:

```bash
# Before starting any refactor
git checkout -b refactor/[refactor-name]
git add .
git commit -m "Checkpoint before [refactor-name]"

# If refactor fails
git stash  # Save any valuable changes
git checkout main
git branch -D refactor/[refactor-name]

# If partially successful
git add [successful-files]
git commit -m "Partial implementation of [refactor-name]"
git checkout main
git cherry-pick [commit-hash]
```

## Risk Mitigation

1. **Always work in feature branches**
2. **Create automated tests before refactoring**
3. **Document assumptions and decisions**
4. **Regular code reviews (even if solo)**
5. **Performance profiling at each milestone**
6. **Maintain backward compatibility during migration**
7. **Keep old systems functional until new ones are verified**

---

This comprehensive guide provides everything needed to systematically refactor the WallChessQuidor codebase. Each section includes concrete code examples, step-by-step instructions, and validation criteria to ensure successful implementation.