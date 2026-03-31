using UnityEngine;
using UnityEngine.InputSystem;

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


    private void Start()
    {
        SetTurn(true);
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

            //CombatPhase(); 
        }
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

    }
}