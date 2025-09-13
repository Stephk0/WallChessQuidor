using UnityEngine;
using UnityEngine.UI;

namespace WallChess.Grid
{
    public class GridUIManager
    {
        private readonly GridCoordinateConverter _coordinateConverter;
        private readonly Transform _parentTransform;
        
        private GameObject[,] _tileLabels;
        private Canvas _uiCanvas;
        private GameObject _canvasObject;
        
        private readonly bool _showTileLabels;
        private readonly Font _labelFont;
        private readonly int _labelFontSize;
        private readonly Color _labelColor;

        public GridUIManager(GridCoordinateConverter coordinateConverter, 
            Transform parentTransform, bool showTileLabels = true, 
            Font labelFont = null, int labelFontSize = 12, Color labelColor = default)
        {
            _coordinateConverter = coordinateConverter;
            _parentTransform = parentTransform;
            _showTileLabels = showTileLabels;
            _labelFont = labelFont;
            _labelFontSize = labelFontSize;
            _labelColor = labelColor == default ? Color.black : labelColor;
            
            InitializeArrays();
            CreateUICanvas();
        }

        private void InitializeArrays()
        {
            int gridSize = _coordinateConverter.GetGridSize();
            _tileLabels = new GameObject[gridSize, gridSize];
        }

        private void CreateUICanvas()
        {
            _uiCanvas = Object.FindFirstObjectByType<Canvas>();
            
            if (_uiCanvas == null)
            {
                _canvasObject = new GameObject("GridCanvas");
                _canvasObject.transform.parent = _parentTransform;
                
                _uiCanvas = _canvasObject.AddComponent<Canvas>();
                _uiCanvas.renderMode = RenderMode.WorldSpace;
                _uiCanvas.worldCamera = Camera.main;
                
                var canvasScaler = _canvasObject.AddComponent<CanvasScaler>();
                canvasScaler.dynamicPixelsPerUnit = 100f;
                
                _canvasObject.AddComponent<GraphicRaycaster>();
                
                _canvasObject.transform.position = new Vector3(0, 0, -0.1f);
                _canvasObject.transform.localScale = Vector3.one * 0.01f;
            }
        }

        public void CreateAllTileLabels()
        {
            if (!_showTileLabels || _uiCanvas == null) return;
            
            int gridSize = _coordinateConverter.GetGridSize();
            
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector3 position = _coordinateConverter.GridToWorldPosition(new Vector2Int(x, y));
                    CreateTileLabel(x, y, position);
                }
            }
        }

        private void CreateTileLabel(int x, int y, Vector3 tilePosition)
        {
            GameObject textObj = new GameObject($"Label_{x}_{y}");
            textObj.transform.SetParent(_uiCanvas.transform);
            
            Text textComponent = textObj.AddComponent<Text>();
            textComponent.text = $"{x},{y}";
            textComponent.font = _labelFont != null ? _labelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = _labelFontSize;
            textComponent.color = _labelColor;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            RectTransform rectTransform = textComponent.GetComponent<RectTransform>();
            
            Vector3 canvasPos = tilePosition;
            canvasPos.z = -0.05f;
            
            Vector3 localPos = _uiCanvas.transform.InverseTransformPoint(canvasPos);
            rectTransform.localPosition = localPos;
            
            rectTransform.sizeDelta = new Vector2(100, 50);
            
            _tileLabels[x, y] = textObj;
        }

        public void ToggleTileLabels(bool show)
        {
            if (_tileLabels != null)
            {
                int gridSize = _coordinateConverter.GetGridSize();
                for (int x = 0; x < gridSize; x++)
                {
                    for (int y = 0; y < gridSize; y++)
                    {
                        if (_tileLabels[x, y] != null)
                        {
                            _tileLabels[x, y].SetActive(show);
                        }
                    }
                }
            }
        }

        public void UpdateTileLabel(int x, int y, string newText)
        {
            int gridSize = _coordinateConverter.GetGridSize();
            if (_tileLabels != null && x >= 0 && x < gridSize && y >= 0 && y < gridSize)
            {
                if (_tileLabels[x, y] != null)
                {
                    var textComponent = _tileLabels[x, y].GetComponent<Text>();
                    if (textComponent != null)
                    {
                        textComponent.text = newText;
                    }
                }
            }
        }

        public void DestroyAllLabels()
        {
            if (_tileLabels != null)
            {
                int sizeX = _tileLabels.GetLength(0);
                int sizeY = _tileLabels.GetLength(1);
                
                for (int x = 0; x < sizeX; x++)
                {
                    for (int y = 0; y < sizeY; y++)
                    {
                        if (_tileLabels[x, y] != null)
                            Object.DestroyImmediate(_tileLabels[x, y]);
                    }
                }
            }

            if (_canvasObject != null)
            {
                Object.DestroyImmediate(_canvasObject);
                _canvasObject = null;
                _uiCanvas = null;
            }
        }
    }
}