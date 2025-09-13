using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace WallChess.Core.Pooling
{
    /// <summary>
    /// Centralized object pooling system for performance optimization.
    /// Reduces GC pressure by reusing GameObjects instead of instantiating/destroying.
    /// Part of Phase 4: Object Pooling & Performance (MVP Implementation Guide)
    /// Expected performance improvement: 60%+ FPS gain
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        #region Singleton
        private static PoolManager _instance;
        public static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PoolManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PoolManager");
                        _instance = go.AddComponent<PoolManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion
        
        #region Types
        /// <summary>
        /// Pool configuration for different object types
        /// </summary>
        [System.Serializable]
        public class PoolConfig
        {
            public string poolKey;
            public GameObject prefab;
            public int initialSize = 10;
            public int maxSize = 100;
            public bool expandable = true;
            public bool reparentOnReturn = true;
            
            public PoolConfig(string key, GameObject prefab, int size)
            {
                this.poolKey = key;
                this.prefab = prefab;
                this.initialSize = size;
                this.maxSize = size * 2;
            }
        }
        
        /// <summary>
        /// Statistics for pool performance monitoring
        /// </summary>
        public class PoolStatistics
        {
            public int totalCreated;
            public int currentActive;
            public int currentPooled;
            public int peakActive;
            public int getCount;
            public int returnCount;
            public int expandCount;
            
            public float UtilizationRate => totalCreated > 0 ? (float)peakActive / totalCreated : 0f;
            public float RecycleRate => getCount > 0 ? (float)returnCount / getCount : 0f;
        }
        #endregion
        
        #region Configuration
        [Header("Pool Configurations")]
        [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();
        
        [Header("Performance Settings")]
        [SerializeField] private bool warmPools = true;
        [SerializeField] private bool useContainerPerPool = true;
        [SerializeField] private int prewarmBatchSize = 5;
        
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private bool trackStatistics = true;
        #endregion
        
        #region State
        private Dictionary<string, IObjectPool> pools = new Dictionary<string, IObjectPool>();
        private Dictionary<string, PoolStatistics> statistics = new Dictionary<string, PoolStatistics>();
        private Transform poolContainer;
        #endregion
        
        #region Unity Lifecycle
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeContainer();
            InitializeDefaultPools();
            
            if (warmPools)
            {
                PrewarmAllPools();
            }
        }
        
        void OnDestroy()
        {
            if (_instance == this)
            {
                ClearAllPools();
                _instance = null;
            }
        }
        #endregion
        
        #region Initialization
        private void InitializeContainer()
        {
            poolContainer = transform;
        }
        
        private void InitializeDefaultPools()
        {
            // Default pool configurations for WallChessQuidor
            if (poolConfigs.Count == 0)
            {
                // Add default configurations
                AddDefaultPoolConfig("Tile", 81);      // 9x9 grid
                AddDefaultPoolConfig("Wall", 20);      // Max 18 walls in play
                AddDefaultPoolConfig("Highlight", 10); // Move highlights
                AddDefaultPoolConfig("Particle", 20);  // Effects
            }
            
            // Create pools from configurations
            foreach (var config in poolConfigs)
            {
                if (config.prefab != null)
                {
                    CreatePool(config);
                }
            }
        }
        
        private void AddDefaultPoolConfig(string key, int size)
        {
            // Try to find prefab in Resources
            GameObject prefab = Resources.Load<GameObject>($"Prefabs/{key}Prefab");
            if (prefab != null)
            {
                poolConfigs.Add(new PoolConfig(key, prefab, size));
            }
        }
        
        private void PrewarmAllPools()
        {
            StartCoroutine(PrewarmPoolsCoroutine());
        }
        
        private System.Collections.IEnumerator PrewarmPoolsCoroutine()
        {
            foreach (var pool in pools.Values)
            {
                pool.Prewarm(prewarmBatchSize);
                yield return null; // Spread prewarming across frames
            }
            
            if (enableDebugLogging)
            {
                Debug.Log($"[PoolManager] Prewarmed {pools.Count} pools");
            }
        }
        #endregion
        
        #region Public API
        /// <summary>
        /// Create a new object pool
        /// </summary>
        public bool CreatePool(PoolConfig config)
        {
            if (pools.ContainsKey(config.poolKey))
            {
                Debug.LogWarning($"[PoolManager] Pool '{config.poolKey}' already exists");
                return false;
            }
            
            Transform container = useContainerPerPool ? 
                CreatePoolContainer(config.poolKey) : poolContainer;
            
            var pool = new ObjectPool(config, container);
            pools[config.poolKey] = pool;
            
            if (trackStatistics)
            {
                statistics[config.poolKey] = new PoolStatistics();
            }
            
            if (enableDebugLogging)
            {
                Debug.Log($"[PoolManager] Created pool '{config.poolKey}' with size {config.initialSize}");
            }
            
            return true;
        }
        
        /// <summary>
        /// Create a pool with a specific prefab
        /// </summary>
        public bool CreatePool(string key, GameObject prefab, int initialSize = 10, int maxSize = 100)
        {
            var config = new PoolConfig(key, prefab, initialSize)
            {
                maxSize = maxSize,
                expandable = true
            };
            
            return CreatePool(config);
        }
        
        /// <summary>
        /// Get an object from a pool
        /// </summary>
        public T Get<T>(string poolKey) where T : Component
        {
            if (!pools.TryGetValue(poolKey, out IObjectPool pool))
            {
                Debug.LogError($"[PoolManager] Pool '{poolKey}' not found");
                return null;
            }
            
            var obj = pool.Get();
            
            if (trackStatistics && statistics.TryGetValue(poolKey, out var stats))
            {
                stats.getCount++;
                stats.currentActive++;
                stats.currentPooled--;
                stats.peakActive = Mathf.Max(stats.peakActive, stats.currentActive);
            }
            
            return obj?.GetComponent<T>();
        }
        
        /// <summary>
        /// Get a GameObject from a pool
        /// </summary>
        public GameObject GetGameObject(string poolKey)
        {
            if (!pools.TryGetValue(poolKey, out IObjectPool pool))
            {
                Debug.LogError($"[PoolManager] Pool '{poolKey}' not found");
                return null;
            }
            
            var obj = pool.Get();
            
            if (trackStatistics && statistics.TryGetValue(poolKey, out var stats))
            {
                stats.getCount++;
                stats.currentActive++;
                stats.currentPooled--;
                stats.peakActive = Mathf.Max(stats.peakActive, stats.currentActive);
            }
            
            return obj;
        }
        
        /// <summary>
        /// Return an object to its pool
        /// </summary>
        public bool Return(string poolKey, GameObject obj)
        {
            if (obj == null) return false;
            
            if (!pools.TryGetValue(poolKey, out IObjectPool pool))
            {
                Debug.LogWarning($"[PoolManager] Pool '{poolKey}' not found, destroying object");
                Destroy(obj);
                return false;
            }
            
            pool.Return(obj);
            
            if (trackStatistics && statistics.TryGetValue(poolKey, out var stats))
            {
                stats.returnCount++;
                stats.currentActive--;
                stats.currentPooled++;
            }
            
            return true;
        }
        
        /// <summary>
        /// Return a component's GameObject to its pool
        /// </summary>
        public bool Return<T>(string poolKey, T component) where T : Component
        {
            return component != null ? Return(poolKey, component.gameObject) : false;
        }
        
        /// <summary>
        /// Clear a specific pool
        /// </summary>
        public void ClearPool(string poolKey)
        {
            if (pools.TryGetValue(poolKey, out IObjectPool pool))
            {
                pool.Clear();
                
                if (trackStatistics && statistics.ContainsKey(poolKey))
                {
                    statistics[poolKey] = new PoolStatistics();
                }
            }
        }
        
        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in pools.Values)
            {
                pool.Clear();
            }
            
            if (trackStatistics)
            {
                statistics.Clear();
            }
        }
        
        /// <summary>
        /// Get statistics for a pool
        /// </summary>
        public PoolStatistics GetStatistics(string poolKey)
        {
            return statistics.TryGetValue(poolKey, out var stats) ? stats : null;
        }
        
        /// <summary>
        /// Get all pool keys
        /// </summary>
        public List<string> GetPoolKeys()
        {
            return pools.Keys.ToList();
        }
        
        /// <summary>
        /// Check if a pool exists
        /// </summary>
        public bool HasPool(string poolKey)
        {
            return pools.ContainsKey(poolKey);
        }
        #endregion
        
        #region Private Methods
        private Transform CreatePoolContainer(string poolKey)
        {
            GameObject container = new GameObject($"Pool_{poolKey}");
            container.transform.SetParent(poolContainer);
            return container.transform;
        }
        #endregion
        
        #region Object Pool Implementation
        private interface IObjectPool
        {
            GameObject Get();
            void Return(GameObject obj);
            void Clear();
            void Prewarm(int batchSize);
        }
        
        private class ObjectPool : IObjectPool
        {
            private readonly Queue<GameObject> pool = new Queue<GameObject>();
            private readonly PoolConfig config;
            private readonly Transform container;
            private int totalCreated = 0;
            
            public ObjectPool(PoolConfig config, Transform container)
            {
                this.config = config;
                this.container = container;
                
                // Create initial objects
                for (int i = 0; i < config.initialSize; i++)
                {
                    CreateNewObject();
                }
            }
            
            public GameObject Get()
            {
                GameObject obj = null;
                
                if (pool.Count > 0)
                {
                    obj = pool.Dequeue();
                }
                else if (config.expandable && totalCreated < config.maxSize)
                {
                    obj = CreateNewObject();
                }
                else if (config.expandable)
                {
                    Debug.LogWarning($"[Pool] Pool '{config.poolKey}' reached max size ({config.maxSize})");
                    obj = Instantiate(config.prefab);
                }
                
                if (obj != null)
                {
                    obj.SetActive(true);
                    obj.transform.SetParent(null);
                    
                    // Reset poolable component if exists
                    var poolable = obj.GetComponent<IPoolable>();
                    poolable?.OnGetFromPool();
                }
                
                return obj;
            }
            
            public void Return(GameObject obj)
            {
                if (obj == null) return;
                
                // Reset poolable component if exists
                var poolable = obj.GetComponent<IPoolable>();
                poolable?.OnReturnToPool();
                
                // Reset transform
                obj.SetActive(false);
                if (config.reparentOnReturn)
                {
                    obj.transform.SetParent(container);
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                    obj.transform.localScale = Vector3.one;
                }
                
                pool.Enqueue(obj);
            }
            
            public void Clear()
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                totalCreated = 0;
            }
            
            public void Prewarm(int batchSize)
            {
                int toCreate = Mathf.Min(batchSize, config.initialSize - pool.Count);
                for (int i = 0; i < toCreate; i++)
                {
                    if (totalCreated < config.maxSize)
                    {
                        CreateNewObject();
                    }
                }
            }
            
            private GameObject CreateNewObject()
            {
                var obj = Instantiate(config.prefab, container);
                obj.SetActive(false);
                obj.name = $"{config.prefab.name}_{totalCreated:000}";
                
                pool.Enqueue(obj);
                totalCreated++;
                
                return obj;
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Interface for objects that need to be reset when pooled
    /// </summary>
    public interface IPoolable
    {
        void OnGetFromPool();
        void OnReturnToPool();
    }
    
    /// <summary>
    /// Helper component for automatic pool return
    /// </summary>
    public class PooledObject : MonoBehaviour, IPoolable
    {
        [SerializeField] private string poolKey;
        [SerializeField] private float autoReturnDelay = 0f;
        
        private Coroutine autoReturnCoroutine;
        
        public string PoolKey
        {
            get => poolKey;
            set => poolKey = value;
        }
        
        public void OnGetFromPool()
        {
            if (autoReturnDelay > 0)
            {
                autoReturnCoroutine = StartCoroutine(AutoReturnCoroutine());
            }
        }
        
        public void OnReturnToPool()
        {
            if (autoReturnCoroutine != null)
            {
                StopCoroutine(autoReturnCoroutine);
                autoReturnCoroutine = null;
            }
        }
        
        public void ReturnToPool()
        {
            PoolManager.Instance.Return(poolKey, gameObject);
        }
        
        private System.Collections.IEnumerator AutoReturnCoroutine()
        {
            yield return new WaitForSeconds(autoReturnDelay);
            ReturnToPool();
        }
        
        void OnDisable()
        {
            OnReturnToPool();
        }
    }
}