using UnityEngine;
using UnityEngine.UI;

namespace WallChess.UI
{
    /// <summary>
    /// Helper script to properly setup PieDial components in the scene
    /// Ensures all required references are assigned and positioned correctly
    /// </summary>
    [RequireComponent(typeof(PieDial))]
    public class PieDialSetup : MonoBehaviour
    {
        [Header("Auto-Setup Options")]
        [SerializeField] private bool autoSetupOnAwake = true;
        [SerializeField] private bool createMissingComponents = true;
        
        [Header("Visual Settings")]
        [SerializeField] private Color confirmDotColor = Color.green;
        [SerializeField] private Color compassColor = Color.white;
        [SerializeField] private float dotSize = 20f;
        [SerializeField] private float compassSize = 100f;
        
        private PieDial pieDial;
        
        private void Awake()
        {
            pieDial = GetComponent<PieDial>();
            
            if (autoSetupOnAwake)
            {
                SetupPieDialComponents();
            }
        }
        
        [ContextMenu("Setup PieDial Components")]
        public void SetupPieDialComponents()
        {
            if (pieDial == null)
            {
                pieDial = GetComponent<PieDial>();
                if (pieDial == null)
                {
                    Debug.LogError("PieDial component not found!", this);
                    return;
                }
            }
            
            // Get or create the base rect (clickable area)
            RectTransform baseRect = GetComponent<RectTransform>();
            
            // Setup compass (directional indicator)
            RectTransform compassRect = CreateOrFindChild("Compass");
            SetupCompass(compassRect);
            
            // Setup confirm dot (draggable center point)
            RectTransform dotRect = CreateOrFindChild("ConfirmDot");
            SetupConfirmDot(dotRect);
            
            // Assign references via reflection since fields might be private
            var pieDialType = typeof(PieDial);
            var baseRectField = pieDialType.GetField("baseRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var compassRectField = pieDialType.GetField("compassRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dotRectField = pieDialType.GetField("dotRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (baseRectField != null) baseRectField.SetValue(pieDial, baseRect);
            if (compassRectField != null) compassRectField.SetValue(pieDial, compassRect);
            if (dotRectField != null) dotRectField.SetValue(pieDial, dotRect);
            
            Debug.Log("PieDial setup completed successfully!", this);
        }
        
        private RectTransform CreateOrFindChild(string childName)
        {
            Transform existing = transform.Find(childName);
            if (existing != null)
            {
                return existing.GetComponent<RectTransform>();
            }
            
            if (!createMissingComponents)
            {
                Debug.LogWarning($"Child '{childName}' not found and createMissingComponents is false", this);
                return null;
            }
            
            // Create new child
            GameObject child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            
            RectTransform rectTransform = child.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.one * 0.5f;
            rectTransform.anchorMax = Vector2.one * 0.5f;
            rectTransform.anchoredPosition = Vector2.zero;
            
            return rectTransform;
        }
        
        private void SetupCompass(RectTransform compassRect)
        {
            if (compassRect == null) return;
            
            // Setup compass visual
            Image compassImage = compassRect.GetComponent<Image>();
            if (compassImage == null)
            {
                compassImage = compassRect.gameObject.AddComponent<Image>();
            }
            
            compassImage.color = compassColor;
            compassRect.sizeDelta = Vector2.one * compassSize;
            
            // Initially hidden
            compassRect.gameObject.SetActive(false);
        }
        
        private void SetupConfirmDot(RectTransform dotRect)
        {
            if (dotRect == null) return;
            
            // Setup confirm dot visual
            Image dotImage = dotRect.GetComponent<Image>();
            if (dotImage == null)
            {
                dotImage = dotRect.gameObject.AddComponent<Image>();
            }
            
            dotImage.color = confirmDotColor;
            dotRect.sizeDelta = Vector2.one * dotSize;
            
            // Make it circular
            if (dotImage.sprite == null)
            {
                // Create a simple circular sprite
                dotImage.sprite = CreateCircleSprite();
            }
            
            // Initially hidden
            dotRect.gameObject.SetActive(false);
        }
        
        private Sprite CreateCircleSprite()
        {
            // Create a simple white circle texture
            int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            
            Vector2 center = Vector2.one * (size * 0.5f);
            float radius = size * 0.4f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);
                    
                    if (distance <= radius)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f);
        }
        
        [ContextMenu("Test PieDial")]
        public void TestPieDial()
        {
            if (pieDial == null)
            {
                Debug.LogError("PieDial not found!", this);
                return;
            }
            
            // Simulate a direction confirmation
            Vector2 testDirection = Vector2.up;
            Debug.Log($"Testing PieDial with direction: {testDirection}");
            
            if (pieDial.OnDirectionConfirmed != null)
            {
                pieDial.OnDirectionConfirmed.Invoke(testDirection);
            }
        }
    }
}
