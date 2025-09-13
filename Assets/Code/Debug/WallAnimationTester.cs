using UnityEngine;
using WallChess.Grid;

namespace WallChess
{
    /// <summary>
    /// Debug component to test wall animation system integration.
    /// Provides runtime controls for testing animation features.
    /// </summary>
    public class WallAnimationTester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WallManager wallManager;
        [SerializeField] private WallAnimationHandler animationHandler;
        
        [Header("Test Settings")]
        [SerializeField] private GameObject testWallPrefab;
        [SerializeField] private Vector3 testPosition = Vector3.zero;
        [SerializeField] private GridSystem.Orientation testOrientation = GridSystem.Orientation.Horizontal;
        
        [Header("Animation Test Controls")]
        [SerializeField] private bool testRotation = false;
        [SerializeField] private bool testTranslation = false;
        [SerializeField] private bool testSlide = false;
        
        private GameObject currentTestWall;
        
        void Start()
        {
            // Auto-find references if not set
            if (wallManager == null)
                wallManager = FindObjectOfType<WallManager>();
                
            if (animationHandler == null && wallManager != null)
                animationHandler = wallManager.GetComponent<WallAnimationHandler>();
                
            if (animationHandler == null)
                Debug.LogWarning("WallAnimationTester: Animation handler not found!");
        }
        
        void Update()
        {
            // Keyboard shortcuts for testing
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
                TestRotationAnimation();
                
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
                TestTranslationAnimation();
                
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3))
                TestSlideAnimation();
                
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4))
                TestAllAnimations();
                
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5))
                ToggleAnimations();
                
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha0))
                CleanupTestWall();
        }
        
        [ContextMenu("Test/Rotation Animation")]
        public void TestRotationAnimation()
        {
            if (!EnsureTestWall()) return;
            
            // Toggle orientation
            testOrientation = testOrientation == GridSystem.Orientation.Horizontal 
                ? GridSystem.Orientation.Vertical 
                : GridSystem.Orientation.Horizontal;
                
            Debug.Log($"Testing rotation animation to {testOrientation}");
            
            if (wallManager != null)
            {
                wallManager.ApplySmoothRotation(currentTestWall, testOrientation);
            }
            else if (animationHandler != null)
            {
                animationHandler.ApplySmoothRotation(currentTestWall, testOrientation);
            }
        }
        
        [ContextMenu("Test/Translation Animation")]
        public void TestTranslationAnimation()
        {
            if (!EnsureTestWall()) return;
            
            Vector3 newPosition = testPosition + Random.insideUnitSphere * 2f;
            newPosition.z = 0; // Keep on placement plane
            
            Debug.Log($"Testing translation animation to {newPosition}");
            
            if (wallManager != null)
            {
                wallManager.ApplySmoothTranslationOnPlace(currentTestWall, newPosition);
            }
            else if (animationHandler != null)
            {
                animationHandler.ApplySmoothTranslationOnPlace(currentTestWall, newPosition);
            }
            
            testPosition = newPosition;
        }
        
        [ContextMenu("Test/Slide Animation")]
        public void TestSlideAnimation()
        {
            if (!EnsureTestWall()) return;
            
            Vector3 slideTarget = testPosition + new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
            
            Debug.Log($"Testing slide animation to {slideTarget}");
            
            if (wallManager != null)
            {
                wallManager.ApplySlideTranslation(currentTestWall, slideTarget);
            }
            else if (animationHandler != null)
            {
                animationHandler.ApplySlideTranslation(currentTestWall, slideTarget);
            }
            
            testPosition = slideTarget;
        }
        
        [ContextMenu("Test/All Animations Sequential")]
        public void TestAllAnimations()
        {
            StartCoroutine(TestAllAnimationsSequence());
        }
        
        private System.Collections.IEnumerator TestAllAnimationsSequence()
        {
            Debug.Log("Starting animation test sequence...");
            
            // Test translation
            TestTranslationAnimation();
            yield return new WaitForSeconds(0.5f);
            
            // Test rotation
            TestRotationAnimation();
            yield return new WaitForSeconds(0.5f);
            
            // Test slide
            TestSlideAnimation();
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log("Animation test sequence complete!");
        }
        
        [ContextMenu("Test/Toggle Animations")]
        public void ToggleAnimations()
        {
            if (animationHandler != null)
            {
                bool currentState = animationHandler.IsRotationLerpEnabled();
                animationHandler.SetAnimationsEnabled(!currentState);
                Debug.Log($"Animations {(!currentState ? "ENABLED" : "DISABLED")}");
            }
        }
        
        [ContextMenu("Test/Create Test Wall")]
        public void CreateTestWall()
        {
            CleanupTestWall();
            
            GameObject prefab = testWallPrefab;
            if (prefab == null && wallManager != null)
            {
                prefab = wallManager.GetRandomWallPrefab();
            }
            
            if (prefab == null)
            {
                // Fallback to primitive
                currentTestWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                currentTestWall.name = "TestWall_Primitive";
            }
            else
            {
                currentTestWall = Instantiate(prefab);
                currentTestWall.name = "TestWall_Prefab";
            }
            
            currentTestWall.transform.position = testPosition;
            
            if (wallManager != null)
            {
                currentTestWall.transform.rotation = wallManager.GetWallRotation(testOrientation);
                currentTestWall.transform.localScale = wallManager.GetWallScale(testOrientation);
            }
            
            Debug.Log($"Created test wall at {testPosition}");
        }
        
        [ContextMenu("Test/Cleanup Test Wall")]
        public void CleanupTestWall()
        {
            if (currentTestWall != null)
            {
                DestroyImmediate(currentTestWall);
                currentTestWall = null;
                Debug.Log("Cleaned up test wall");
            }
        }
        
        private bool EnsureTestWall()
        {
            if (currentTestWall == null)
            {
                CreateTestWall();
            }
            
            return currentTestWall != null;
        }
        
        [ContextMenu("Debug/Show Animation Stats")]
        public void ShowAnimationStats()
        {
            if (animationHandler != null)
            {
                Debug.Log(animationHandler.GetAnimationStats());
            }
            else
            {
                Debug.Log("No animation handler found!");
            }
        }
        
        [ContextMenu("Debug/Configure Fast Animations")]
        public void ConfigureFastAnimations()
        {
            if (animationHandler != null)
            {
                animationHandler.ConfigureRotation(true, 0.1f, AnimationCurve.Linear(0, 0, 1, 1));
                animationHandler.ConfigureTranslation(true, 0.1f, 0.2f, AnimationCurve.Linear(0, 0, 1, 1));
                animationHandler.ConfigureSlide(true, 0.05f, AnimationCurve.Linear(0, 0, 1, 1));
                Debug.Log("Configured fast animations");
            }
        }
        
        [ContextMenu("Debug/Configure Slow Animations")]
        public void ConfigureSlowAnimations()
        {
            if (animationHandler != null)
            {
                animationHandler.ConfigureRotation(true, 1f, AnimationCurve.EaseInOut(0, 0, 1, 1));
                animationHandler.ConfigureTranslation(true, 1f, 1f, AnimationCurve.EaseInOut(0, 0, 1, 1));
                animationHandler.ConfigureSlide(true, 0.5f, AnimationCurve.EaseInOut(0, 0, 1, 1));
                Debug.Log("Configured slow animations");
            }
        }
        
        void OnGUI()
        {
            if (!Application.isEditor) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            
            GUILayout.Label("Wall Animation Tester", GUI.skin.box);
            GUILayout.Space(5);
            
            GUILayout.Label("Keyboard Controls:");
            GUILayout.Label("1 - Test Rotation");
            GUILayout.Label("2 - Test Translation");
            GUILayout.Label("3 - Test Slide");
            GUILayout.Label("4 - Test All");
            GUILayout.Label("5 - Toggle Animations");
            GUILayout.Label("0 - Cleanup Test Wall");
            
            GUILayout.Space(10);
            
            if (animationHandler != null)
            {
                GUILayout.Label($"Animations: {(animationHandler.IsRotationLerpEnabled() ? "ON" : "OFF")}");
                GUILayout.Label(animationHandler.GetAnimationStats());
            }
            else
            {
                GUILayout.Label("Animation Handler not found!");
            }
            
            GUILayout.EndArea();
        }
    }
}