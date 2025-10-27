using UnityEngine;
using UnityEditor;
using WallChess.Gameplay.Pawns;

namespace WallChess.Editor
{
    /// <summary>
    /// Editor utility to add missing components to WallChessGameManager
    /// </summary>
    public static class ComponentSetupUtility
    {
        [MenuItem("WallChess/Setup Missing Components")]
        public static void SetupMissingComponents()
        {
            GameObject gameManager = GameObject.Find("WallChessGameManager");
            if (gameManager == null)
            {
                Debug.LogError("WallChessGameManager GameObject not found in scene!");
                return;
            }

            bool componentsAdded = false;

            // Add PawnManager if missing
            if (gameManager.GetComponent<PawnManager>() == null)
            {
                Undo.AddComponent<PawnManager>(gameManager);
                Debug.Log("Added PawnManager component");
                componentsAdded = true;
            }

            // Add HighlightManager if missing  
            if (gameManager.GetComponent<HighlightManager>() == null)
            {
                Undo.AddComponent<HighlightManager>(gameManager);
                Debug.Log("Added HighlightManager component");
                componentsAdded = true;
            }

            // Add PawnController if missing
            if (gameManager.GetComponent<PawnController>() == null)
            {
                Undo.AddComponent<PawnController>(gameManager);
                Debug.Log("Added PawnController component");
                componentsAdded = true;
            }

            if (componentsAdded)
            {
                EditorUtility.SetDirty(gameManager);
                Debug.Log("Missing components added successfully! Scene marked as dirty.");
            }
            else
            {
                Debug.Log("All required components are already present.");
            }
        }
    }
}
