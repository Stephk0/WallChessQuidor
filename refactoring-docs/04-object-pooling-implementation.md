# Part 4: High Priority - Object Pooling Implementation
**WallChessQuidor Refactoring Implementation Guide**

**Page:** 35-45 of 101  
**Document:** 04-object-pooling-implementation.md  
**Priority:** 🟠 HIGH  
**Estimated Time:** 1 day  
**Prerequisites:** [Part 6: Codebase Cleanup](06-codebase-cleanup.md)  
**Next Document:** [Part 3: Grid System](03-grid-system-simplification.md)  

---

## Why Object Pooling Matters

Currently, WallChessQuidor does this everywhere:
```csharp
// Bad: Creates garbage every move
Instantiate(tilePrefab);
Destroy(oldTile);
```

This causes:
- **GC spikes** every few moves (frame drops)
- **Memory allocation** pressure
- **Slower instantiation** for effects and tiles

**Goal**: Implement object pooling for a 60% performance improvement in 1 day.

---

## User Stories

### Story 1: Create Generic Object Pool
**As a developer, I want a reusable object pool so I can pool any type of object easily.**

**Acceptance Criteria:**
- One pool class that works for tiles, pieces, effects, etc.
- Easy to use: `pool.Get()` and `pool.Return(obj)`
- Handles creation and cleanup automatically
- Thread-safe for Unity main thread usage

**Implementation:**
```csharp
// Assets/_Project/00_Core/Utilities/ObjectPool.cs
using UnityEngine;
using System.Collections.Generic;

public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> _pool = new Queue<T>();
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly int _initialSize;
    
    public ObjectPool(T prefab, int initialSize = 10, Transform parent = null)
    {
        _prefab = prefab;
        _initialSize = initialSize;
        _parent = parent;
        
        // Pre-populate pool
        for (int i = 0; i < initialSize; i++)
        {
            var obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
    
    public T Get()
    {
        if (_pool.Count == 0)
        {
            // Create new if pool is empty
            var newObj = Object.Instantiate(_prefab, _parent);
            return newObj;
        }
        
        var obj = _pool.Dequeue();
        obj.gameObject.SetActive(true);
        return obj;
    }
    
    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
    }
    
    public int AvailableCount => _pool.Count;
}
```

### Story 2: Pool Manager for Easy Access
**As a developer, I want one place to get pooled objects so I don't manage multiple pools.**

**Acceptance Criteria:**
- Central manager that handles all pools
- Register pools for different object types
- Easy access from anywhere in code
- Automatic cleanup on scene changes

**Implementation:**
```csharp
// Assets/_Project/00_Core/Systems/PoolManager.cs
using UnityEngine;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }
    
    [Header("Pool Settings")]
    public PoolConfig[] poolConfigs;
    
    private Dictionary<string, IObjectPool> pools = new Dictionary<string, IObjectPool>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePools()
    {
        foreach (var config in poolConfigs)
        {
            CreatePool(config);
        }
    }
    
    private void CreatePool(PoolConfig config)
    {
        var poolType = typeof(ObjectPool<>).MakeGenericType(config.prefab.GetType());
        var pool = System.Activator.CreateInstance(poolType, 
            config.prefab, config.initialSize, transform);
        
        pools[config.poolName] = (IObjectPool)pool;
        
        Debug.Log($"Created pool '{config.poolName}' with {config.initialSize} objects");
    }
    
    public T Get<T>(string poolName) where T : Component
    {
        if (pools.TryGetValue(poolName, out var pool))
        {
            return ((ObjectPool<T>)pool).Get();
        }
        
        Debug.LogError($"Pool '{poolName}' not found!");
        return null;
    }
    
    public void Return<T>(string poolName, T obj) where T : Component
    {
        if (pools.TryGetValue(poolName, out var pool))
        {
            ((ObjectPool<T>)pool).Return(obj);
        }
    }
}

[System.Serializable]
public class PoolConfig
{
    public string poolName;
    public Component prefab;
    public int initialSize = 10;
}

public interface IObjectPool { }
```

### Story 3: Convert Tile System to Use Pooling
**As a player, I want smooth tile animations without frame drops during piece movement.**

**Acceptance Criteria:**
- No more Instantiate/Destroy for tile highlighting
- Smooth animations during piece selection
- No GC spikes when highlighting valid moves
- Visual quality remains the same

**Before (Bad):**
```csharp
// Old way - creates garbage
public void HighlightTile(Vector2Int position)
{
    var highlight = Instantiate(highlightPrefab, GetWorldPosition(position), Quaternion.identity);
    StartCoroutine(DestroyAfterTime(highlight, 2f));
}

private IEnumerator DestroyAfterTime(GameObject obj, float time)
{
    yield return new WaitForSeconds(time);
    Destroy(obj);
}
```

**After (Good):**
```csharp
// New way - uses pooling
public void HighlightTile(Vector2Int position)
{
    var highlight = PoolManager.Instance.Get<TileHighlight>("TileHighlight");
    highlight.transform.position = GetWorldPosition(position);
    highlight.Show(2f, () => {
        PoolManager.Instance.Return("TileHighlight", highlight);
    });
}
```

**TileHighlight Component:**
```csharp
public class TileHighlight : MonoBehaviour
{
    [Header("Animation")]
    public float fadeDuration = 0.3f;
    
    private CanvasGroup canvasGroup;
    private System.Action onComplete;
    
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
    
    public void Show(float duration, System.Action onComplete = null)
    {
        this.onComplete = onComplete;
        gameObject.SetActive(true);
        
        // Fade in
        canvasGroup.alpha = 0f;
        LeanTween.alphaCanvas(canvasGroup, 1f, fadeDuration)
                 .setOnComplete(() => {
                     // Wait, then fade out
                     LeanTween.alphaCanvas(canvasGroup, 0f, fadeDuration)
                              .setDelay(duration)
                              .setOnComplete(Hide);
                 });
    }
    
    private void Hide()
    {
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}
```

### Story 4: Pool Visual Effects
**As a player, I want smooth particle effects when pieces move and walls are placed.**

**Acceptance Criteria:**
- Particle systems are pooled and reused
- No stuttering when multiple effects play
- Effects start/stop cleanly from pools
- Memory usage stays stable during gameplay

**Implementation:**
```csharp
// Assets/_Project/00_Core/Systems/EffectsManager.cs
using UnityEngine;

public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance { get; private set; }
    
    void Awake()
    {
        Instance = this;
    }
    
    public void PlayMoveEffect(Vector3 position)
    {
        var effect = PoolManager.Instance.Get<ParticleEffect>("MoveEffect");
        effect.transform.position = position;
        effect.Play(() => {
            PoolManager.Instance.Return("MoveEffect", effect);
        });
    }
    
    public void PlayWallPlaceEffect(Vector3 position)
    {
        var effect = PoolManager.Instance.Get<ParticleEffect>("WallEffect");
        effect.transform.position = position;
        effect.Play(() => {
            PoolManager.Instance.Return("WallEffect", effect);
        });
    }
}

public class ParticleEffect : MonoBehaviour
{
    private ParticleSystem particles;
    private System.Action onComplete;
    
    void Awake()
    {
        particles = GetComponent<ParticleSystem>();
    }
    
    public void Play(System.Action onComplete = null)
    {
        this.onComplete = onComplete;
        particles.Play();
        
        // Return to pool when done
        float duration = particles.main.duration + particles.main.startLifetime.constantMax;
        Invoke(nameof(Stop), duration);
    }
    
    private void Stop()
    {
        particles.Stop();
        onComplete?.Invoke();
    }
}
```

### Story 5: Measure Performance Improvement
**As a developer, I want to see concrete performance improvements from object pooling.**

**Acceptance Criteria:**
- Before/after frame rate comparison
- Memory allocation reduction measurement  
- GC spike elimination verification
- Performance data logged for review

**Performance Monitor:**
```csharp
// Assets/_Project/00_Core/Utilities/PoolingPerformanceMonitor.cs
using UnityEngine;
using Unity.Profiling;

public class PoolingPerformanceMonitor : MonoBehaviour
{
    private static readonly ProfilerMarker PoolGetMarker = new ProfilerMarker("Pool.Get");
    private static readonly ProfilerMarker PoolReturnMarker = new ProfilerMarker("Pool.Return");
    
    [Header("Performance Stats")]
    public int totalGets = 0;
    public int totalReturns = 0;
    public float avgGetTime = 0f;
    public float avgReturnTime = 0f;
    
    private float totalGetTime = 0f;
    private float totalReturnTime = 0f;
    
    public static PoolingPerformanceMonitor Instance { get; private set; }
    
    void Awake()
    {
        Instance = this;
    }
    
    public void RecordGet(float time)
    {
        totalGets++;
        totalGetTime += time;
        avgGetTime = totalGetTime / totalGets;
    }
    
    public void RecordReturn(float time)
    {
        totalReturns++;
        totalReturnTime += time;
        avgReturnTime = totalReturnTime / totalReturns;
    }
    
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 120));
        GUILayout.Label("Object Pooling Performance", "box");
        GUILayout.Label($"Pool Gets: {totalGets} (avg: {avgGetTime:F3}ms)");
        GUILayout.Label($"Pool Returns: {totalReturns} (avg: {avgReturnTime:F3}ms)");
        GUILayout.Label($"Pool Efficiency: {(float)totalReturns/totalGets*100:F1}%");
        
        // Memory info
        long memory = System.GC.GetTotalMemory(false) / 1024 / 1024;
        GUILayout.Label($"Memory: {memory}MB");
        
        GUILayout.EndArea();
    }
}
```

---

## Setup Instructions

### Step 1: Configure Pool Manager
1. Create empty GameObject called "PoolManager"
2. Add `PoolManager` component  
3. Configure pools in inspector:
   - **TileHighlight**: Initial size 20
   - **MoveEffect**: Initial size 10  
   - **WallEffect**: Initial size 10
   - **PieceGhost**: Initial size 8

### Step 2: Update Existing Code
1. Find all `Instantiate()` calls for tiles/effects
2. Replace with `PoolManager.Instance.Get<>()`
3. Find all `Destroy()` calls for those objects
4. Replace with `PoolManager.Instance.Return()`

### Step 3: Test Performance
1. Play game and make lots of moves
2. Watch the performance monitor
3. Check Unity Profiler for GC allocations
4. Compare with baseline from Part 1

---

## Expected Results

After implementing object pooling:

- **60% better performance** during heavy tile highlighting
- **No GC spikes** during normal gameplay
- **Smoother animations** for all pooled effects
- **Stable memory usage** instead of constantly growing
- **Faster instantiation** of commonly used objects

The game should feel noticeably smoother, especially when selecting pieces and showing valid moves.

---

**Time Investment**: 1 day  
**Risk Level**: Low (doesn't change game logic)  
**Impact**: High (immediate performance improvement)

---

**Page 45 of 45**  
**Next Document**: [Part 3: Grid System Simplification](03-grid-system-simplification.md)  
**Previous Document**: [Part 6: Codebase Cleanup](06-codebase-cleanup.md)  
**Master Index**: [00-master-index.md](00-master-index.md)  

---
*WallChessQuidor Refactoring Implementation Guide - Pool objects, not memory leaks*