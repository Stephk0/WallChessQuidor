using UnityEngine;

namespace WallChess
{
    /// <summary>
    /// Simple curve-based dangle animation for pawns when dragged.
    /// Clean and straightforward approach using only animation curves.
    /// </summary>
    public class SimplePawnDangleAnimation : MonoBehaviour
    {
        [Header("Transform References")]
        [SerializeField] private Transform visualTransform;
        
        [Header("Animation Settings")]
        [SerializeField] private AnimationCurve dangleRotationCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.25f, 15f, 0f, 0f),
            new Keyframe(0.5f, 0f, 0f, 0f),
            new Keyframe(0.75f, -15f, 0f, 0f),
            new Keyframe(1f, 0f, 0f, 0f)
        );
        
        [SerializeField] private float animationSpeed = 2f;
        [SerializeField] private float maxRotationAngle = 15f;
        [SerializeField] private bool loopAnimation = true;
        [SerializeField] private float curveIntensityMultiplier = 1f;
        
        [Header("Transition")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        // Runtime variables
        private bool isDangling = false;
        private bool isTransitioning = false;
        private bool isFadingIn = false;
        private float animationTime = 0f;
        private float transitionTime = 0f;
        private Vector3 originalRotation;
        private float currentIntensity = 0f;
        
        private void Awake()
        {
            ValidateComponents();
            InitializeAnimation();
        }
        
        private void ValidateComponents()
        {
            if (visualTransform == null)
            {
                // Try to find a visual transform in children
                Transform visual = transform.Find("Visual") ?? transform.Find("Model") ?? transform.Find("Mesh");
                if (visual == null && transform.childCount > 0)
                {
                    // Use the first child if no specifically named visual found
                    visualTransform = transform.GetChild(0);
                }
                else
                {
                    visualTransform = visual;
                }
                
                if (visualTransform == null)
                {
                    Debug.LogWarning($"No visual transform found for {name}. Using self as visual transform.");
                    visualTransform = transform; // Fallback to self
                }
            }
        }
        
        private void InitializeAnimation()
        {
            if (visualTransform != null)
            {
                originalRotation = visualTransform.localEulerAngles;
            }
        }
        
        private void Update()
        {
            if (isDangling || isTransitioning)
            {
                UpdateAnimation();
            }
        }
        
        private void UpdateAnimation()
        {
            if (isTransitioning)
            {
                UpdateTransition();
            }
            
            if (isDangling || currentIntensity > 0f)
            {
                UpdateDangleRotation();
            }
        }
        
        private void UpdateTransition()
        {
            transitionTime += Time.deltaTime;
            float transitionDuration = isFadingIn ? fadeInDuration : fadeOutDuration;
            float normalizedTime = Mathf.Clamp01(transitionTime / transitionDuration);
            float curveValue = transitionCurve.Evaluate(normalizedTime);
            
            if (isFadingIn)
            {
                // Fading in
                currentIntensity = curveValue;
            }
            else
            {
                // Fading out
                currentIntensity = 1f - curveValue;
            }
            
            // Check if transition is complete
            if (normalizedTime >= 1f)
            {
                isTransitioning = false;
                
                if (!isFadingIn)
                {
                    // Finished fading out, reset rotation
                    if (visualTransform != null)
                    {
                        visualTransform.localRotation = Quaternion.Euler(originalRotation);
                    }
                    currentIntensity = 0f;
                }
                else
                {
                    // Finished fading in
                    currentIntensity = 1f;
                }
            }
        }
        
        private void UpdateDangleRotation()
        {
            if (visualTransform == null) return;
            
            // Update animation time
            if (isDangling)
            {
                animationTime += Time.deltaTime * animationSpeed;
                
                if (loopAnimation && animationTime >= 1f)
                {
                    animationTime = animationTime % 1f; // Loop the animation
                }
            }
            
            // Sample the curve
            float curveValue = dangleRotationCurve.Evaluate(animationTime);
            
            // Apply curve intensity multiplier, max rotation, and transition intensity
            float finalRotation = curveValue * curveIntensityMultiplier * maxRotationAngle * currentIntensity;
            
            // Apply rotation
            Vector3 targetRotation = originalRotation + new Vector3(0f, 0f, finalRotation);
            visualTransform.localRotation = Quaternion.Euler(targetRotation);
        }
        
        /// <summary>
        /// Start the dangling animation
        /// </summary>
        public void StartDangling()
        {
            if (isDangling) return;
            
            isDangling = true;
            isTransitioning = true;
            isFadingIn = true;
            transitionTime = 0f;
            animationTime = 0f;
            
            Debug.Log($"Started simple dangle animation for {name}");
        }
        
        /// <summary>
        /// Stop the dangling animation
        /// </summary>
        public void StopDangling()
        {
            if (!isDangling) return;
            
            isDangling = false;
            isTransitioning = true;
            isFadingIn = false;
            transitionTime = 0f;
            
            Debug.Log($"Stopped simple dangle animation for {name}");
        }
        
        /// <summary>
        /// Check if currently dangling
        /// </summary>
        public bool IsDangling => isDangling;
        
        /// <summary>
        /// Reset the animation to initial state
        /// </summary>
        public void ResetAnimation()
        {
            isDangling = false;
            isTransitioning = false;
            animationTime = 0f;
            transitionTime = 0f;
            currentIntensity = 0f;
            
            if (visualTransform != null)
            {
                visualTransform.localRotation = Quaternion.Euler(originalRotation);
            }
        }
        
        /// <summary>
        /// Set custom animation curve at runtime
        /// </summary>
        public void SetDangleRotationCurve(AnimationCurve newCurve)
        {
            if (newCurve != null)
            {
                dangleRotationCurve = newCurve;
            }
        }
        
        /// <summary>
        /// Set the curve intensity multiplier at runtime
        /// </summary>
        public void SetCurveIntensityMultiplier(float multiplier)
        {
            curveIntensityMultiplier = Mathf.Max(0f, multiplier);
        }
        
        /// <summary>
        /// Get the current curve intensity multiplier
        /// </summary>
        public float GetCurveIntensityMultiplier()
        {
            return curveIntensityMultiplier;
        }
        
        #region Editor Utilities
        #if UNITY_EDITOR
        [ContextMenu("Test Start Dangle")]
        private void TestStartDangle()
        {
            StartDangling();
        }
        
        [ContextMenu("Test Stop Dangle")]
        private void TestStopDangle()
        {
            StopDangling();
        }
        
        [ContextMenu("Reset Animation")]
        private void TestResetAnimation()
        {
            ResetAnimation();
        }
        
        [ContextMenu("Test Intensity 0.5x")]
        private void TestHalfIntensity()
        {
            SetCurveIntensityMultiplier(0.5f);
            Debug.Log($"Set curve intensity to 0.5x for {name}");
        }
        
        [ContextMenu("Test Intensity 2x")]
        private void TestDoubleIntensity()
        {
            SetCurveIntensityMultiplier(2f);
            Debug.Log($"Set curve intensity to 2x for {name}");
        }
        
        [ContextMenu("Reset Intensity 1x")]
        private void TestResetIntensity()
        {
            SetCurveIntensityMultiplier(1f);
            Debug.Log($"Reset curve intensity to 1x for {name}");
        }
        
        [ContextMenu("Create Default Curve")]
        private void CreateDefaultCurve()
        {
            dangleRotationCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.25f, 1f, 0f, 0f),
                new Keyframe(0.5f, 0f, 0f, 0f),
                new Keyframe(0.75f, -1f, 0f, 0f),
                new Keyframe(1f, 0f, 0f, 0f)
            );
        }
        
        private void OnValidate()
        {
            // Clamp values to reasonable ranges
            animationSpeed = Mathf.Max(0.1f, animationSpeed);
            maxRotationAngle = Mathf.Clamp(maxRotationAngle, 0f, 90f);
            fadeInDuration = Mathf.Max(0.01f, fadeInDuration);
            fadeOutDuration = Mathf.Max(0.01f, fadeOutDuration);
            curveIntensityMultiplier = Mathf.Max(0f, curveIntensityMultiplier);
        }
        #endif
        #endregion
    }
}