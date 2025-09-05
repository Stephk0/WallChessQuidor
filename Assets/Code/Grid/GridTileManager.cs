using UnityEngine;
using System.Collections.Generic;

namespace WallChess.Grid
{
    public class GridTileManager
    {
        private readonly GridCoordinateConverter _coordinateConverter;
        private readonly Transform _parentTransform;
        
        private GameObject[,] _gridTiles;
        private bool[,] _tileOccupied;
        
        private readonly GameObject[] _lightTilePrefabs;
        private readonly GameObject[] _darkTilePrefabs;
        private readonly float _tileSize;

        public System.Action<Vector2Int, bool> OnTileOccupancyChanged;

        public GridTileManager(GridCoordinateConverter coordinateConverter, 
            Transform parentTransform, float tileSize, 
            GameObject[] lightTilePrefabs = null, GameObject[] darkTilePrefabs = null)
        {
            _coordinateConverter = coordinateConverter;
            _parentTransform = parentTransform;
            _tileSize = tileSize;
            _lightTilePrefabs = lightTilePrefabs ?? new GameObject[0];
            _darkTilePrefabs = darkTilePrefabs ?? new GameObject[0];
            
            InitializeArrays();
        }

        private void InitializeArrays()
        {
            int gridSize = _coordinateConverter.GetGridSize();
            _gridTiles = new GameObject[gridSize, gridSize];
            _tileOccupied = new bool[gridSize, gridSize];
            
            for (int x = 0; x < gridSize; x++)
                for (int y = 0; y < gridSize; y++)
                    _tileOccupied[x, y] = false;
        }

        public void CreateAllTiles()
        {
            int gridSize = _coordinateConverter.GetGridSize();
            
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    CreateTileAt(new Vector2Int(x, y));
                }
            }
        }

        private void CreateTileAt(Vector2Int gridPos)
        {
            Vector3 worldPosition = _coordinateConverter.GridToWorldPosition(gridPos);
            
            bool isLightTile = (gridPos.x + gridPos.y) % 2 == 0;
            GameObject[] prefabArray = isLightTile ? _lightTilePrefabs : _darkTilePrefabs;
            
            GameObject tilePrefab = null;
            if (prefabArray != null && prefabArray.Length > 0)
            {
                // Use random prefab from the appropriate array for variety
                int randomIndex = Random.Range(0, prefabArray.Length);
                tilePrefab = prefabArray[randomIndex];
            }
            
            GameObject tile;
            if (tilePrefab != null)
            {
                tile = Object.Instantiate(tilePrefab, worldPosition, Quaternion.identity, _parentTransform);
            }
            else
            {
                // Fallback to primitive quad if no prefabs provided
                tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.transform.position = worldPosition;
                tile.transform.parent = _parentTransform;
                
                // Apply fallback material with checkerboard pattern
                Renderer tileRenderer = tile.GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    Material defaultMat = new Material(Shader.Find("Sprites/Default"));
                    defaultMat.color = isLightTile ? Color.white : Color.gray;
                    tileRenderer.sharedMaterial = defaultMat; // Use sharedMaterial to avoid leaks
                }
            }
            
            tile.name = $"Tile_{gridPos.x}_{gridPos.y}_{(isLightTile ? "Light" : "Dark")}";
            tile.transform.localScale = Vector3.one * _tileSize;
            
            // Remove collider if it exists (we handle input differently)
            Collider tileCollider = tile.GetComponent<Collider>();
            if (tileCollider != null)
                Object.DestroyImmediate(tileCollider);
            
            _gridTiles[gridPos.x, gridPos.y] = tile;
        }

        public bool IsTileOccupied(Vector2Int gridPos)
        {
            if (!_coordinateConverter.IsValidGridPosition(gridPos)) return true;
            return _tileOccupied[gridPos.x, gridPos.y];
        }

        public void SetTileOccupied(Vector2Int gridPos, bool occupied)
        {
            if (!_coordinateConverter.IsValidGridPosition(gridPos)) return;
            
            _tileOccupied[gridPos.x, gridPos.y] = occupied;
            OnTileOccupancyChanged?.Invoke(gridPos, occupied);
        }

        public GameObject GetTile(Vector2Int gridPos)
        {
            if (!_coordinateConverter.IsValidGridPosition(gridPos)) return null;
            return _gridTiles[gridPos.x, gridPos.y];
        }

        public Vector3 GetTileCenter(Vector2Int gridPos)
        {
            if (!_coordinateConverter.IsValidGridPosition(gridPos)) return Vector3.zero;
            return _gridTiles[gridPos.x, gridPos.y].transform.position;
        }

        public List<Vector2Int> GetValidAdjacentPositions(Vector2Int currentPos)
        {
            List<Vector2Int> validPositions = new List<Vector2Int>();
            
            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int newPos = currentPos + direction;
                
                if (_coordinateConverter.IsValidGridPosition(newPos) && !IsTileOccupied(newPos))
                {
                    validPositions.Add(newPos);
                }
            }

            return validPositions;
        }

        public void ClearOccupancy()
        {
            int gridSize = _coordinateConverter.GetGridSize();
            for (int x = 0; x < gridSize; x++)
                for (int y = 0; y < gridSize; y++)
                    _tileOccupied[x, y] = false;
        }

        public void DestroyAllTiles()
        {
            if (_gridTiles != null)
            {
                int sizeX = _gridTiles.GetLength(0);
                int sizeY = _gridTiles.GetLength(1);
                
                for (int x = 0; x < sizeX; x++)
                {
                    for (int y = 0; y < sizeY; y++)
                    {
                        if (_gridTiles[x, y] != null)
                            Object.DestroyImmediate(_gridTiles[x, y]);
                    }
                }
            }
        }

        /// <summary>
        /// Gets count of available light tile prefabs
        /// </summary>
        public int GetLightTilePrefabCount() => _lightTilePrefabs?.Length ?? 0;

        /// <summary>
        /// Gets count of available dark tile prefabs  
        /// </summary>
        public int GetDarkTilePrefabCount() => _darkTilePrefabs?.Length ?? 0;

        /// <summary>
        /// Gets a specific light tile prefab by index
        /// </summary>
        public GameObject GetLightTilePrefab(int index)
        {
            if (_lightTilePrefabs == null || index < 0 || index >= _lightTilePrefabs.Length)
                return null;
            return _lightTilePrefabs[index];
        }

        /// <summary>
        /// Gets a specific dark tile prefab by index
        /// </summary>
        public GameObject GetDarkTilePrefab(int index)
        {
            if (_darkTilePrefabs == null || index < 0 || index >= _darkTilePrefabs.Length)
                return null;
            return _darkTilePrefabs[index];
        }
    }
}