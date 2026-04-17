using System;
using System.Threading.Tasks;
using Network;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private ShipManager shipManager;
    [SerializeField] private SpaceBuilder board;

    private SessionManager network;
    private bool isMyTurn;
    private bool shotQueued;
    
    // inputs
    private InputAction _placeShip;
    private Action<InputAction.CallbackContext> _onPlaceShip;

    private void Awake()
    {
        network = FindFirstObjectByType<SessionManager>();
        if (!network)
        {
            Debug.LogError("CombatManager could not find SessionManager");
            return;
        }

        network.OnMyTurn += OnMyTurn;
        network.OnTheirTurn += OnTheirTurn;
        network.OnStateChanged += OnStateChange;

        enabled = false;
        
        _placeShip = InputSystem.actions.FindAction("SpaceField/ShipPlace");
        _onPlaceShip = _ => SendShot();
        if (_placeShip != null) _placeShip.performed += _onPlaceShip;
    }

    private void OnDestroy()
    {
        if (network)
        {
            network.OnMyTurn -= OnMyTurn;
            network.OnTheirTurn -= OnTheirTurn;
            network.OnStateChanged -= OnStateChange;
        }

        if (HoverActions.current)
        {
            HoverActions.current.CombatClicked -= SendShot;
        }
    }

    private void Update()
    {
        // only allow firing during my turn while combat mode is active
        if (!enabled || !isMyTurn || network == null || board == null) return;

        // use Enter / Numpad Enter as the fire button
        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            SendShot();
        }
    }

    private void OnStateChange(string state)
    {
        if (state == "AtWar")
        {
            enabled = true;

            if (HoverActions.current)
            {
                HoverActions.current.currentMode = HoverActions.InputMode.Combat;
                HoverActions.current.CombatClicked += SendShot;
            }
        }
        else
        {
            enabled = false;

            if (HoverActions.current)
            {
                HoverActions.current.CombatClicked -= SendShot;
            }
        }
    }

    private void OnMyTurn()
    {
        Debug.Log("My turn!");
        isMyTurn = true;
        shotQueued = false;

        if (shipManager != null)
        {
            shipManager.SetActiveBoard(false);
        }

        if (board != null)
        {
            board.SetCursorVisible(true);
            board.SetActiveBoard(true);
        }
    }

    private void OnTheirTurn()
    {
        Debug.Log("Their turn!");
        isMyTurn = false;
        shotQueued = false;

        if (shipManager != null)
        {
            shipManager.SetActiveBoard(false);
        }

        if (board != null)
        {
            board.SetCursorVisible(false);
            board.SetActiveBoard(false);
        }
    }

    private void SendShot()
    {
        if (!isMyTurn || network == null || board == null || shotQueued) return;

        Vector3 cursor = board.GetCursorLocation();
        Vector3Int shotCell = new Vector3Int(
            Mathf.RoundToInt(cursor.x),
            Mathf.RoundToInt(cursor.y),
            Mathf.RoundToInt(cursor.z)
        );

        _ = SendShotAsync(shotCell);
    }

    private async Task SendShotAsync(Vector3Int cell)
    {
        if (network == null || board == null) return;

        Debug.Log($"Sending shot at {cell}");

        // prevent double-fire while waiting for turn to switch
        shotQueued = true;
        isMyTurn = false;

        board.SetActiveBoard(false);
        board.SetCursorVisible(false);

        await network.SetShotTarget(cell);
    }
}