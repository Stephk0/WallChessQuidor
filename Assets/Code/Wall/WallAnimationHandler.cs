using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WallChess.Grid;

namespace WallChess
{
    /// <summary>
    /// Modular animation handler for wall placement visuals.
    /// Provides smooth rotation, translation, and slide effects without disrupting core logic.
    /// Designed to be completely optional and configurable via Inspector.
    /// </summary>
    public class WallAnimationHandler : MonoBehaviour
    {
        #region Animation Settings
        [Header("Animation Toggles")]
        [SerializeField] private bool enableAnimations = true;
        [Tooltip("Master toggle for all animations")]
        
        [SerializeField] private bool enableRotationLerp = true;
        [Tooltip("Smooth rotation when walls change orientation")]
        
        [SerializeField] private bool enableTranslationOnPlace = true;
        [Tooltip("Z-axis emergence animation when walls are placed")]
        
        [SerializeField] private bool enableSlideTranslation = true;
        [Tooltip("Smooth sliding when dragging walls along gaps")]
        
        [Header("Rotation Animation")]
        [SerializeField] private float rotationDuration = 0.3f;
        [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Translation On Place Animation")]
        [SerializeField] private float translationDuration = 0.25f;
        [SerializeField] private float translationZOffset = 0.5f;
        [Tooltip("How far behind the wall starts before emerging")]
        [SerializeField] private AnimationCurve translationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Slide Translation Animation")]
        [SerializeField] private float slideDuration = 0.15f;
        [Tooltip("Duration for drag-and-drop sliding")]
        [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Performance")]
        [SerializeField] private bool bypassInDebugMode = true;
        [Tooltip("Skip animations when in debug/development mode")]
        [SerializeField] private int maxConcurrentAnimations = 10;
        [Tooltip("Limit concurrent animations for performance")]
        #endregion
        
        #region Private Fields
        // Track active animations per GameObject to prevent conflicts
        private Dictionary<GameObject, Coroutine> activeRotations = new Dictionary<GameObject, Coroutine>();
        private Dictionary<GameObject, Coroutine> activeTranslations = new Dictionary<GameObject, Coroutine>();
        private Dictionary<GameObject, Coroutine> activeSlides = new Dictionary<GameObject, Coroutine>();
        
        // Reference to WallManager for configuration checks
        private WallManager wallManager;
        
        // Performance tracking
        private int currentAnimationCount = 0;
        #endregion
        
        #region Initialization
        /// <summary>
        /// Initialize the animation handler with a reference to the WallManager
        /// </summary>
        public void Initialize(WallManager manager)
        {
            wallManager = manager;
            Debug.Log($"WallAnimationHandler initialized - Animations: {enableAnimations}");
        }
        #endregion
        
        #region Public API - Animation Triggers
        /// <summary>
        /// Apply smooth rotation animation to a wall GameObject
        /// </summary>
        public void ApplySmoothRotation(GameObject wallObject, GridSystem.Orientation targetOrientation)
        {
            if (!CanAnimate(wallObject)) 
            {
                ApplyImmediateRotation(wallObject, targetOrientation);
                return;
            }
            
            // Stop any existing rotation animation on this object
            StopAnimation(wallObject, activeRotations);
            
            // Start new rotation animation
            Quaternion targetRotation = wallManager.GetWallRotation(targetOrientation);
            Coroutine rotationCoroutine = StartCoroutine(RotationLerpCoroutine(wallObject, targetRotation));
            activeRotations[wallObject] = rotationCoroutine;
        }
        
        /// <summary>
        /// Apply smooth translation animation when wall is placed (emergence effect)
        /// </summary>
        public void ApplySmoothTranslationOnPlace(GameObject wallObject, Vector3 targetPosition)
        {
            if (!CanAnimate(wallObject))
            {
                wallObject.transform.position = targetPosition;
                return;
            }
            
            // Stop any existing translation animation on this object
            StopAnimation(wallObject, activeTranslations);
            
            // Set initial position behind the target
            Vector3 startPosition = targetPosition - Vector3.forward * translationZOffset;
            wallObject.transform.position = startPosition;
            
            // Start translation animation
            Coroutine translationCoroutine = StartCoroutine(TranslationLerpCoroutine(wallObject, targetPosition));
            activeTranslations[wallObject] = translationCoroutine;
        }
        
        /// <summary>
        /// Apply smooth slide animation when dragging wall along gaps
        /// </summary>
        public void ApplySlideTranslation(GameObject wallObject, Vector3 targetPosition)
        {
            if (!CanAnimate(wallObject))
            {
                wallObject.transform.position = targetPosition;
                return;
            }
            
            // Stop any existing slide animation on this object
            StopAnimation(wallObject, activeSlides);
            
            // Start slide animation from current position
            Coroutine slideCoroutine = StartCoroutine(SlideLerpCoroutine(wallObject, targetPosition));
            activeSlides[wallObject] = slideCoroutine;
        }
        #endregion
        
        #region Animation Coroutines
        private IEnumerator RotationLerpCoroutine(GameObject target, Quaternion targetRotation)
        {
            if (target == null) yield break;
            
            currentAnimationCount++;
            Quaternion startRotation = target.transform.rotation;
            float elapsed = 0f;
            
            while (elapsed < rotationDuration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / rotationDuration;
                float curveValue = rotationCurve.Evaluate(t);
                
                target.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curveValue);
                yield return null;
            }
            
            // Ensure final rotation is set
            if (target != null)
            {
                target.transform.rotation = targetRotation;
            }
            
            // Clean up tracking
            if (activeRotations.ContainsKey(target))
            {
                activeRotations.Remove(target);
            }
            currentAnimationCount--;
        }
        
        private IEnumerator TranslationLerpCoroutine(GameObject target, Vector3 targetPosition)
        {
            if (target == null) yield break;
            
            currentAnimationCount++;
            Vector3 startPosition = target.transform.position;
            float elapsed = 0f;
            
            while (elapsed < translationDuration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / translationDuration;
                float curveValue = translationCurve.Evaluate(t);
                
                target.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
                yield return null;
            }
            
            // Ensure final position is set
            if (target != null)
            {
                target.transform.position = targetPosition;
            }
            
            // Clean up tracking
            if (activeTranslations.ContainsKey(target))
            {
                activeTranslations.Remove(target);
            }
            currentAnimationCount--;
        }
        
        private IEnumerator SlideLerpCoroutine(GameObject target, Vector3 targetPosition)
        {
            if (target == null) yield break;
            
            currentAnimationCount++;
            Vector3 startPosition = target.transform.position;
            float elapsed = 0f;
            
            while (elapsed < slideDuration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideDuration;
                float curveValue = slideCurve.Evaluate(t);
                
                target.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
                yield return null;
            }
            
            // Ensure final position is set
            if (target != null)
            {
                target.transform.position = targetPosition;
            }
            
            // Clean up tracking
            if (activeSlides.ContainsKey(target))
            {
                activeSlides.Remove(target);
            }
            currentAnimationCount--;
        }
        #endregion
        
        #region Helper Methods
        /// <summary>
        /// Check if animations should be applied
        /// </summary>
        private bool CanAnimate(GameObject target)
        {
            if (!enableAnimations) return false;
            if (target == null) return false;
            if (bypassInDebugMode && wallManager != null && wallManager.IsDebugMode()) return false;
            if (currentAnimationCount >= maxConcurrentAnimations) return false;
            
            return true;
        }
        
        /// <summary>
        /// Stop an active animation and remove from tracking
        /// </summary>
        private void StopAnimation(GameObject target, Dictionary<GameObject, Coroutine> activeAnimations)
        {
            if (activeAnimations.ContainsKey(target))
            {
                if (activeAnimations[target] != null)
                {
                    StopCoroutine(activeAnimations[target]);
                }
                activeAnimations.Remove(target);
            }
        }
        
        /// <summary>
        /// Apply rotation immediately without animation
        /// </summary>
        private void ApplyImmediateRotation(GameObject target, GridSystem.Orientation orientation)
        {
            if (target != null && wallManager != null)
            {
                target.transform.rotation = wallManager.GetWallRotation(orientation);
            }
        }
        
        /// <summary>
        /// Stop all active animations
        /// </summary>
        public void StopAllAnimations()
        {
            // Stop all rotation animations
            foreach (var kvp in activeRotations)
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
            }
            activeRotations.Clear();
            
            // Stop all translation animations
            foreach (var kvp in activeTranslations)
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
            }
            activeTranslations.Clear();
            
            // Stop all slide animations
            foreach (var kvp in activeSlides)
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
            }
            activeSlides.Clear();
            
            currentAnimationCount = 0;
        }
        #endregion
        
        #region Configuration API
        /// <summary>
        /// Check if rotation animation is enabled
        /// </summary>
        public bool IsRotationLerpEnabled() => enableAnimations && enableRotationLerp;
        
        /// <summary>
        /// Check if translation on place animation is enabled
        /// </summary>
        public bool IsTranslationOnPlaceEnabled() => enableAnimations && enableTranslationOnPlace;
        
        /// <summary>
        /// Check if slide translation animation is enabled
        /// </summary>
        public bool IsSlideTranslationEnabled() => enableAnimations && enableSlideTranslation;
        
        /// <summary>
        /// Get rotation animation duration for backwards compatibility
        /// </summary>
        public float GetRotationLerpDuration() => rotationDuration;
        
        /// <summary>
        /// Get translation animation duration for backwards compatibility
        /// </summary>
        public float GetTranslationOnPlaceDuration() => translationDuration;
        
        /// <summary>
        /// Get slide animation duration for backwards compatibility
        /// </summary>
        public float GetSlideTranslationDuration() => slideDuration;
        
        /// <summary>
        /// Set animation enabled state at runtime
        /// </summary>
        public void SetAnimationsEnabled(bool enabled)
        {
            enableAnimations = enabled;
            if (!enabled)
            {
                StopAllAnimations();
            }
        }
        
        /// <summary>
        /// Configure rotation animation settings
        /// </summary>
        public void ConfigureRotation(bool enabled, float duration, AnimationCurve curve)
        {
            enableRotationLerp = enabled;
            rotationDuration = duration;
            if (curve != null) rotationCurve = curve;
        }
        
        /// <summary>
        /// Configure translation animation settings
        /// </summary>
        public void ConfigureTranslation(bool enabled, float duration, float zOffset, AnimationCurve curve)
        {
            enableTranslationOnPlace = enabled;
            translationDuration = duration;
            translationZOffset = zOffset;
            if (curve != null) translationCurve = curve;
        }
        
        /// <summary>
        /// Configure slide animation settings
        /// </summary>
        public void ConfigureSlide(bool enabled, float duration, AnimationCurve curve)
        {
            enableSlideTranslation = enabled;
            slideDuration = duration;
            if (curve != null) slideCurve = curve;
        }
        #endregion
        
        #region Cleanup
        void OnDestroy()
        {
            StopAllAnimations();
        }
        #endregion
        
        #region Debug
        /// <summary>
        /// Get current animation statistics for debugging
        /// </summary>
        public string GetAnimationStats()
        {
            return $"Active Animations: {currentAnimationCount}/{maxConcurrentAnimations}\n" +
                   $"Rotations: {activeRotations.Count}\n" +
                   $"Translations: {activeTranslations.Count}\n" +
                   $"Slides: {activeSlides.Count}";
        }
        
        [ContextMenu("Debug/Toggle All Animations")]
        private void DebugToggleAnimations()
        {
            SetAnimationsEnabled(!enableAnimations);
            Debug.Log($"Animations {(enableAnimations ? "ENABLED" : "DISABLED")}");
        }
        
        [ContextMenu("Debug/Show Animation Stats")]
        private void DebugShowStats()
        {
            Debug.Log(GetAnimationStats());
        }
        #endregion
    }
}