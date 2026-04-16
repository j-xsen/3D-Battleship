using System;
using Network;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CombatManager : MonoBehaviour
{

    // [SerializeField] private bool player1_turn = true;

    //whos turn it is for combat 
    // private void player_turn()
    // {
    //     if(!player1_turn)
    //     {
    //         //TurnManager.SwitchTurn();
    //     }
//    }

    private SessionManager _network;
    private ShipManager _shipManager;
    private SpaceBuilder _board;

    private InputAction _shootAtAction;
    private Action<InputAction.CallbackContext> _shootAtActionCtx;

    private void Awake()
    {
        _network = FindFirstObjectByType<SessionManager>();
        _network.OnMyTurn += OnMyTurn;
        _network.OnTheirTurn += OnTheirTurn;
        _network.OnStateChanged += OnStateChange;

        _shipManager = FindFirstObjectByType<ShipManager>();
        _board = FindFirstObjectByType<SpaceBuilder>();
        
        _shootAtAction = InputSystem.actions.FindAction("ShipPlace");
        _shootAtActionCtx = _ => SendShot();
        _shootAtAction.performed += _shootAtActionCtx;
    }

    private void OnDestroy()
    {
        if (_network)
        {
            _network.OnMyTurn -= OnMyTurn;
            _network.OnTheirTurn -= OnTheirTurn;
            _network.OnStateChanged -= OnStateChange;
        }

        if (_shootAtAction != null)
        {
            _shootAtAction.performed -= _shootAtActionCtx;
        }
    }

    private void OnStateChange(string state)
    {
        if (state == "AtWar")
        {
            enabled = true;
        }
    }

    private void OnMyTurn()
    {
        Debug.LogError("My turn!");
        _shipManager.SetActiveBoard(true);
        _board.SetCursorVisible(true);
        _board.SetActiveBoard(true);
        HoverActions.current.Clicked += SendShot;
    }

    private void OnTheirTurn()
    {
        Debug.LogError("Their turn!");
        _shipManager.SetActiveBoard(false);
        _board.SetCursorVisible(false);
        _board.SetActiveBoard(false);
        HoverActions.current.Clicked -= SendShot;
    }

    private void SendShot()
    {
        Vector3 shotLoc = _board.GetCursorLocation();
        Debug.Log($"Shooting at {shotLoc}");
        _ = _network.SetShotTarget(shotLoc);
    }



    //sending shot request 



    //resolving hit/miss

    //sink detection 

    //combat phase transistions 

    //win con 







}
