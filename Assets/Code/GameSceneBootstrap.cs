using UnityEngine;
using WallChess.Core;
using WallChess.Core.Config;
using WallChess.AI;
using WallChess.Input;
using WallChess.View;
using WallChess.View.Board;
using WallChess.View.UI;

namespace WallChess
{
    /// <summary>
    /// Bootstrap component that initializes and wires up all game systems.
    /// Attach to a root GameObject and assign references in the inspector.
    /// </summary>
    public class GameSceneBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BoardPreset defaultGamePreset;
        [SerializeField] private ViewConfig viewConfig;

        [Header("Core Systems")]
        [SerializeField] private GameController gameController;
        [SerializeField] private AIController aiController;
        [SerializeField] private InputRouter inputRouter;

        [Header("View Systems")]
        [SerializeField] private GameViewBinder viewBinder;
        [SerializeField] private BoardView boardView;
        [SerializeField] private GameUIView gameUI;

        [Header("Auto Start")]
        [SerializeField] private bool autoStartGame = true;

        private void Start()
        {
            ValidateReferences();

            if (autoStartGame && defaultGamePreset != null)
            {
                StartGame();
            }
        }

        private void ValidateReferences()
        {
            if (gameController == null)
                Debug.LogError("GameSceneBootstrap: GameController not assigned!");
            if (aiController == null)
                Debug.LogWarning("GameSceneBootstrap: AIController not assigned - AI players won't work");
            if (inputRouter == null)
                Debug.LogWarning("GameSceneBootstrap: InputRouter not assigned - input won't work");
            if (viewBinder == null)
                Debug.LogWarning("GameSceneBootstrap: GameViewBinder not assigned - visuals won't update");
            if (defaultGamePreset == null)
                Debug.LogWarning("GameSceneBootstrap: No default game preset assigned");
        }

        [ContextMenu("Start Game")]
        public void StartGame()
        {
            if (gameController == null || defaultGamePreset == null)
            {
                Debug.LogError("Cannot start game: missing GameController or preset");
                return;
            }

            gameController.Initialize(defaultGamePreset);
            gameController.StartGame();
        }

        [ContextMenu("Create Scene Hierarchy")]
        public void CreateSceneHierarchy()
        {
            #if UNITY_EDITOR
            // Create root structure
            CreateChildIfMissing("Views");
            CreateChildIfMissing("Input");
            CreateChildIfMissing("AI");
            CreateChildIfMissing("UI");

            var views = transform.Find("Views");
            if (views != null)
            {
                CreateChildIfMissing("BoardView", views);
                CreateChildIfMissing("PawnContainer", views);
                CreateChildIfMissing("WallContainer", views);
                CreateChildIfMissing("HighlightContainer", views);
            }

            Debug.Log("Scene hierarchy created. Add components manually in Editor.");
            #endif
        }

        private void CreateChildIfMissing(string name, Transform parent = null)
        {
            var p = parent ?? transform;
            if (p.Find(name) == null)
            {
                var child = new GameObject(name);
                child.transform.SetParent(p);
                child.transform.localPosition = Vector3.zero;
            }
        }
    }
}
