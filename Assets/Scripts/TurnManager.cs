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

    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            SwitchTurn();
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
}