using UnityEngine;

namespace WallChess.Data
{
    /// <summary>
    /// Debug and development configuration
    /// </summary>
    [CreateAssetMenu(fileName = "DebugSettings", menuName = "Wall Chess/Debug Settings")]
    public class DebugSettings : ScriptableObject
    {
        [Header("Debug Configuration")]
        [Tooltip("When enabled, allows any pawn to be moved regardless of turn")]
        public bool debugMode = false;
        
        [Tooltip("Enable detailed logging for game events")]
        public bool enableVerboseLogging = true;
        
        [Tooltip("Show debug UI elements in game")]
        public bool showDebugUI = false;
        
        [Tooltip("Skip animations for faster testing")]
        public bool skipAnimations = false;

        [Header("Development Tools")]
        [Tooltip("Enable developer hotkeys")]
        public bool enableDevHotkeys = true;
        
        [Tooltip("Enable context menu items")]
        public bool enableContextMenus = true;
    }
}