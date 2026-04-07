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

    private void Start()
    {
        

        if (HoverActions.current != null)
        {
            HoverActions.current.currentMode = HoverActions.InputMode.Placement;
            HoverActions.current.CombatClicked += HandleCombatClick;
        }

        SetTurn(true);
    }

    private void OnDestroy()
    {
        if (HoverActions.current != null)
        {
            HoverActions.current.CombatClicked -= HandleCombatClick;
        }
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

        player1Board.SetCursorVisible(isPlayer1Turn);
        player2Board.SetCursorVisible(!isPlayer1Turn);
    }


    private void SetCombatTurn(bool isPlayer1Turn)
    {
        player1Turn = isPlayer1Turn;

        // placement stays locked
        player1ShipManager.SetActiveBoard(false);
        player2ShipManager.SetActiveBoard(false);

        if (player1Turn)
        {
            // Player 1 attacks Player 2
            player1Board.SetCursorVisible(false);
            player2Board.SetCursorVisible(true);//cursor visible while player 1 is attacking player 2 

            player2Camera.gameObject.SetActive(true);//looking at player 2's grid to attack 
            player1Camera.gameObject.SetActive(false);

            player1Board.SetActiveBoard(false);
            player2Board.SetActiveBoard(true);
        }
        else
        {
            // Player 2 attacks Player 1
            player1Board.SetCursorVisible(true);
            player2Board.SetCursorVisible(false);

            player1Camera.gameObject.SetActive(true);
            player2Camera.gameObject.SetActive(false);

            player1Board.SetActiveBoard(true);
            player2Board.SetActiveBoard(false);
        }
    }
    private void CombatPhase()
    {
        if (HoverActions.current != null)
        {
            HoverActions.current.currentMode = HoverActions.InputMode.Combat;
        }

        SetCombatTurn(true);
        Debug.Log("Combat phase started. Player 1 is attacking Player 2.");
    }


    private void HandleCombatClick()
    {
        if (player1Turn)
        {
            Vector3 target = player2Board.GetCursorLocation();
            Vector3Int cell = new Vector3Int(
                Mathf.RoundToInt(target.x),
                Mathf.RoundToInt(target.y),
                Mathf.RoundToInt(target.z)
            );

            Debug.Log($"Player 1 attacks {cell}");
            player2Board.RegisterAttack(cell);

            // later:
            // SwitchCombatTurn();
        }
        else
        {
            Vector3 target = player1Board.GetCursorLocation();
            Vector3Int cell = new Vector3Int(
                Mathf.RoundToInt(target.x),
                Mathf.RoundToInt(target.y),
                Mathf.RoundToInt(target.z)
            );

            Debug.Log($"Player 2 attacks {cell}");
            player1Board.RegisterAttack(cell);

            // later:
            // SwitchCombatTurn();
        }
    }
}