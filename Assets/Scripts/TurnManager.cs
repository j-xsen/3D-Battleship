using System.Threading.Tasks;
using Network;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Player 1")]
    [SerializeField] private SpaceBuilder player1Board;
    [SerializeField] private ShipManager player1ShipManager;
    [SerializeField] private Camera player1Camera;

    [Header("Player 2")]
    [SerializeField] private SpaceBuilder player2Board;
    [SerializeField] private ShipManager player2ShipManager;
    [SerializeField] private Camera player2Camera;

    private bool player1Turn = true;

    private SessionManager _sm;

    private void Start()
    {
        // get session manager
        _sm = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<SessionManager>();
        if (!_sm) Debug.LogError("Unable to find session manager!");
        
        SetTurn(true);
    }

    private async Task ConfirmPlacementAsync()
    {
        await _sm.SendReady(true);
    }

    public void ConfirmPlacement()
    {
        Debug.Log(player1Turn ? "Checking Player 1 placement" : "Checking Player 2 placement");

        if (player1Turn)
        {
            if (!player1ShipManager.AllShipsPlaced())
            {
                Debug.Log("Player 1 must place all ships before locking in.");
                return;
            }

            player1ShipManager.LockPlacement();
            player1Turn = false;
            SetTurn(false);
            _ = ConfirmPlacementAsync();
        }
        else
        {
            if (!player2ShipManager.AllShipsPlaced())
            {
                Debug.Log("Player 2 must place all ships before locking in.");
                return;
            }

            player2ShipManager.LockPlacement();
            Debug.Log("Both players locked in. Start battle phase here.");

            CombatPhase();
        }
    }

    public void SwitchTurn()
    {
        player1Turn = !player1Turn;
        SetTurn(player1Turn);
    }

    private void SetTurn(bool isPlayer1Turn)
    {
        player1Board.SetActiveBoard(isPlayer1Turn);
        player1ShipManager.SetActiveBoard(isPlayer1Turn);
        player1Camera.gameObject.SetActive(isPlayer1Turn);

        player2Board.SetActiveBoard(!isPlayer1Turn);
        player2ShipManager.SetActiveBoard(!isPlayer1Turn);
        player2Camera.gameObject.SetActive(!isPlayer1Turn);
    }

    private void CombatPhase()
    {
        player1Board.SetActiveBoard(false);
        player1ShipManager.SetActiveBoard(false);
        player1Camera.gameObject.SetActive(false);

        player2Board.SetActiveBoard(false);
        player2ShipManager.SetActiveBoard(false);
        player2Camera.gameObject.SetActive(false);
    }
}