using System.Threading.Tasks;
using Network;
using UnityEngine;
using UnityEngine.Serialization;



//checks if player is done placing ships
//locks placement and ready up 

public class TurnManager : MonoBehaviour
{
    [FormerlySerializedAs("player1Board")]
    [Header("Player Board")]
    [SerializeField] private SpaceBuilder myBoard;

    [SerializeField] private ShipManager myShipManager;

    private SessionManager _sm;

    private void Awake()
    {
        if (HoverActions.current)
        {
            HoverActions.current.currentMode = HoverActions.InputMode.Placement;
        }

        _sm = FindFirstObjectByType<SessionManager>();
        if (!_sm)
        {
            Debug.LogError("TurnManager could not find SessionManager");
        }
    }

    public void ConfirmPlacement()
    {
        Debug.Log("Checking player placement");

        if (!myShipManager.AllShipsPlaced())
        {
            Debug.Log("Player must place all ships before locking in.");
            return;
        }

        myShipManager.LockPlacement();
        myBoard.SetCursorVisible(false);
        myBoard.SetActiveBoard(false);

        _ = ConfirmPlacementAsync();
    }

    private async Task ConfirmPlacementAsync()
    {
        if (_sm == null) return;
        await _sm.SendReadyAsync(true);
    }
}