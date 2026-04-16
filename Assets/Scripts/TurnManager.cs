using System.Threading.Tasks;
using Network;
using UnityEngine;
using UnityEngine.Serialization;

public class TurnManager : MonoBehaviour
{
    [FormerlySerializedAs("player1Board")]
    [Header("Player 1")] [SerializeField] private SpaceBuilder myBoard;
    [SerializeField] private ShipManager player1ShipManager;
    
    private bool isMyTurn;

    private SessionManager _sm;

    private void Awake()
    {
        if (HoverActions.current)
        {
            HoverActions.current.currentMode = HoverActions.InputMode.Placement;
            HoverActions.current.CombatClicked += HandleCombatClick;
        }

        _sm = FindFirstObjectByType<SessionManager>();
        if (!_sm)
        {
            Debug.LogError("TurnManager could not find SessionManager");
        }
        _sm.OnMyTurn += SetMyTurn;
        _sm.OnTheirTurn += SetTheirTurn;
    }

    private void OnDestroy()
    {
        if (HoverActions.current)
        {
            HoverActions.current.CombatClicked -= HandleCombatClick;
        }

        if (_sm) return;
        _sm.OnMyTurn -= SetMyTurn;
        _sm.OnTheirTurn -= SetTheirTurn;
    }

    public void ConfirmPlacement()
    {
        Debug.Log("Checking Player placement");

        if (!player1ShipManager.AllShipsPlaced())
        {
            Debug.Log("Player must place all ships before locking in.");
            return;
        }

        player1ShipManager.LockPlacement();
        _ = ConfirmPlacementAsync();
    }
    
    private async Task ConfirmPlacementAsync()
    {
        await _sm.SendReadyAsync(true);
    }

    private void SetMyTurn()
    {
        SetCombatTurn(true);
    }

    private void SetTheirTurn()
    {
        SetCombatTurn(false);
    }


    private void SetCombatTurn(bool myTurn)
    {
        // placement stays locked
        player1ShipManager.SetActiveBoard(myTurn);
        myBoard.SetCursorVisible(myTurn);
        myBoard.SetActiveBoard(myTurn);
    }

    private void HandleCombatClick()
    {
        return;
        // if (player1Turn)
        // {
        //     // Vector3 target = player2Board.GetCursorLocation();
        //     // Vector3Int cell = new Vector3Int(
        //     //     Mathf.RoundToInt(target.x),
        //     //     Mathf.RoundToInt(target.y),
        //     //     Mathf.RoundToInt(target.z)
        //     // );
        //     //
        //     // Debug.Log($"Player 1 attacks {cell}");
        //     // player2Board.RegisterAttack(cell);
        //
        //     // later:
        //     // SwitchCombatTurn();
        // }
        // else
        // {
        //     Vector3 target = player1Board.GetCursorLocation();
        //     Vector3Int cell = new Vector3Int(
        //         Mathf.RoundToInt(target.x),
        //         Mathf.RoundToInt(target.y),
        //         Mathf.RoundToInt(target.z)
        //     );
        //
        //     Debug.Log($"Player 2 attacks {cell}");
        //     player1Board.RegisterAttack(cell);

            // later:
            // SwitchCombatTurn();
        // }
    }
}