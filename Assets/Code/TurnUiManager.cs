using UnityEngine;
using WallChess.Pawn; // for TurnEventBroadcaster + TurnChangeEventArgs

public class TurnUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject playerOnePanel;
    [SerializeField] private GameObject playerTwoPanel;

    private void OnEnable()
    {
        TurnEventBroadcaster.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        TurnEventBroadcaster.OnTurnChanged -= HandleTurnChanged;
    }

    private void HandleTurnChanged(TurnChangeEventArgs args)
    {
        // Example: assuming index 0 = Player One, index 1 = Player Two
        if (args.newPlayerIndex == 0)
        {
            playerOnePanel.SetActive(true);
            playerTwoPanel.SetActive(false);
        }
        else if (args.newPlayerIndex == 1)
        {
            playerOnePanel.SetActive(false);
            playerTwoPanel.SetActive(true);
        }
    }
}
