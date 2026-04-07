using Ships;
using System;
using System.Collections;
using System.Collections.Generic;
using Network;
using Ships.Types;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class ShipManager : MonoBehaviour
{
    [FormerlySerializedAs("_spaceBuilder")]
    [SerializeField] private SpaceBuilder spaceBuilder;

    [Header("Ghost Materials")]
    [SerializeField] private Material ghostValid;
    [SerializeField] private Material ghostInvalid;

    [Header("Camera")] [SerializeField] private GameObject cam;

    [SerializeField] private bool placementLocked; // bool check to lock placement when all ships are placed
    [SerializeField] private bool isActiveBoard = true; // checks if this board is active

    private ShipView _ghost; // ghost ship game object
    private ShipTypeManager _shipTypeManager;

    private int _selectedShip; // the current "ghost" ship
    private bool _placing; // this is a config bool to toggle updating the ghost
    private ShipPlacementUI _placementUI; // placement UI
    private Dictionary<int, List<ShipView>> _shipObjects; // a dictionary of a list of all ship game objects

    // inputs
    private InputAction _placeShip;
    private Action<InputAction.CallbackContext> _onPlaceShip;
    private InputAction _cycleShip;
    private Action<InputAction.CallbackContext> _onCycleShip;
    private InputAction _rotateShipRight;
    private Action<InputAction.CallbackContext> _onRotateShip;
    private InputAction _rotateShipLeft;

    // for setting collision detections
    private Vector3 _bCollider;
    private Vector3 _cCollider;

    // prevent overlap
    public LayerMask overlap;

    // making a number to add onto names
    private int _index;

    // network
    private SessionManager _network;

    private bool free = true;

    private void Start()
    {
        //find network
        //network = GameObject.FindWithTag("NetworkManager")?.GetComponent<SessionManager>();
        // if (!_network) Debug.LogError("Unable to find NetworkManager!");

        // get ship type manager
        _shipTypeManager = GetComponent<ShipTypeManager>();
        if (!_shipTypeManager) Debug.LogError("No ShipTypeManager found on ShipManager");
        
        // get PlacementUI, which should be attached with this object
        _placementUI = GetComponentInParent<ShipPlacementUI>();
        if (!_placementUI) Debug.LogError("No ShipPlacementUI found with ShipManager");
        
        if (!spaceBuilder)
        {
            Debug.LogError("No SpaceBuilder found on ShipManager!");
            return;
        }

        // hover events
        HoverActions.current.Clicked += PlaceShip;
        HoverActions.current.ShipClicked += Redo;

        // input setup
        _placeShip = InputSystem.actions.FindAction("SpaceField/ShipPlace");
        _onPlaceShip = _ => PlaceShip();
        if (_placeShip != null) _placeShip.performed += _onPlaceShip;

        _cycleShip = InputSystem.actions.FindAction("SpaceField/ShipCycle");
        _onCycleShip = _ => CycleShip();
        if (_cycleShip != null) _cycleShip.performed += _onCycleShip;

        _rotateShipRight = InputSystem.actions.FindAction("SpaceField/ShipRotateRight");
        _onRotateShip = _ => RotateShip();
        if (_rotateShipRight != null) _rotateShipRight.performed += _onRotateShip;

        _rotateShipLeft = InputSystem.actions.FindAction("SpaceField/ShipRotateLeft");
        if (_rotateShipLeft != null) _rotateShipLeft.performed += _onRotateShip;

        // init ship objects dict with empty lists
        _shipObjects = new Dictionary<int, List<ShipView>>();
        foreach (int shipType in _shipTypeManager.Rations().Keys)
        {
            _shipObjects[shipType] = new List<ShipView>();
        }

        _placing = true; // config
        _selectedShip = _shipTypeManager.MinShip(); // defaults ghost ship to the smallest ship

        spaceBuilder.OnCursorMoved += HandleCursorMoved; // gets called every time the cursor moves
        HandleCursorMoved(); // creates ghost on start
    }
    
    private void Update()
    {
        if ((Mouse.current.rightButton.isPressed) & free)
        {
            RotateShip();
            free = false;
            StartCoroutine(Delay(0.5f));
        }

        if (CanEditShips() && Mouse.current.scroll.ReadValue().y != 0)
        {
            ReinstantiateGhost();
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        // Check that it is being run in Play Mode, so it doesn't try to draw this in Editor mode
        if (Application.isPlaying)
            // Draw a cube where the OverlapBox is
            Gizmos.DrawWireCube(_cCollider, _bCollider);
    }
    
    private void OnDestroy()
    {
        // unload input functions
        if (spaceBuilder) spaceBuilder.OnCursorMoved -= HandleCursorMoved;
        if (_placeShip != null) _placeShip.performed -= _onPlaceShip;
        if (_cycleShip != null) _cycleShip.performed -= _onCycleShip;
        if (_rotateShipRight != null) _rotateShipRight.performed -= _onRotateShip;
        if (_rotateShipLeft != null) _rotateShipLeft.performed -= _onRotateShip;

        if (!HoverActions.current) return;

        HoverActions.current.Clicked -= PlaceShip;
        HoverActions.current.ShipClicked -= Redo;
    }

    private void Redo(GameObject ship)
    {
        if (!CanEditShips()) return; // prevents removal after player has locked ships

        ShipView shipView = ship.GetComponent<ShipView>();

        if (shipView && _shipObjects[_selectedShip].Remove(shipView))
        {
            Destroy(ship);
            _placementUI.UpdateButtons();
            return;
        }

        Debug.Log("couldn't find ship");
    }
    
    private bool CanEditShips() // extra check
    {
        return isActiveBoard && !placementLocked;
    }

    public int Remaining(int shipType)
    {
        // returns number of shipTypes remaining
        int startingRation = _shipTypeManager.Rations(shipType);
        int numberPlaced = ShipsPlaced(shipType);

        return startingRation - numberPlaced;
    }

    private int SelectedRemaining()
    {
        // returns the number of selected ships remaining
        return Remaining(_selectedShip);
    }

    private bool GhostValid()
    {
        // TODO - don't allow placing on top of another ship
        return SelectedRemaining() > 0 &&
               _ghost &&
               _ghost.HasValidPlacement(spaceBuilder.GetSize());
    }

    private Vector3 TransformedPoint()
    {
        // gets the world positioning of the cursor for placement alignment
        return spaceBuilder.transform.TransformPoint(spaceBuilder.GetCursorLocation());
    }

    private Material GetMaterial()
    {
        return GhostValid() ? ghostValid : ghostInvalid;
    }

    private void RotateShip()
    {
        if (!CanEditShips()) return;

        // alters the rotation axis
        if (!_ghost) return;
        _ghost.Rotate();
        _ghost.SetMaterial(GetMaterial());
    }

    public void SelectShip(int clicked)
    {
        Debug.Log($"SelectShip called with {clicked}");
        _selectedShip = clicked;
        ReinstantiateGhost();
    }

    public int ShipsPlaced(int shipType)
    {
        return _shipObjects[shipType].Count;
    }

    private void CycleShip()
    {
        if (!CanEditShips()) return;

        // called to cycle through ship types
        _selectedShip = _shipTypeManager.CycleShip(_selectedShip);
        ReinstantiateGhost();
    }

    private void ReinstantiateGhost()
    {
        // destroys the ghost object and recreates it

        // saves transform info of ghost
        Vector3 newPos = _ghost ? _ghost.transform.position : TransformedPoint();
        Quaternion newRot = _ghost ? _ghost.transform.rotation : Quaternion.identity;

        AxisObject savedAxes = _ghost ? _ghost.GetAxes() : Axes.X;

        if (_ghost) Destroy(_ghost.gameObject);

        // remakes ghost with the transform info & parents to self
        _ghost = Instantiate(ObjectFromSelected(), newPos, newRot, transform);
        _ghost.SetAxis(savedAxes);
        _ghost.MoveShip(newPos, spaceBuilder.GetCursorLocation());
        _ghost.SetMaterial(GetMaterial());
    }

    private void HandleCursorMoved()
    {
        if (!CanEditShips()) return;
        if (!_placing) return;

        if (_ghost)
        {
            // ghost exists, just update pos and material
            _ghost.MoveShip(TransformedPoint(), spaceBuilder.GetCursorLocation());
            _ghost.SetMaterial(GetMaterial());
        }
        else
        {
            // ghost doesn't exist, create
            ReinstantiateGhost();
        }
    }

    public void ChangeMode()
    {
        PhysicsRaycaster mode = cam.GetComponent<PhysicsRaycaster>();

        // check if default layer is active
        if ((mode.eventMask & (1 << 0)) != 0)
        {
            // set all layers to mask
            mode.eventMask = ~0 & ~(LayerMask.GetMask("Default")) & ~(LayerMask.GetMask("Blocked"));
            if (_ghost)
                _ghost.GetComponentInChildren<MeshRenderer>().enabled = false;
            HandleCursorMoved();
        }
        else
        {
            mode.eventMask = ~0 & ~(LayerMask.GetMask("Ship")) & ~(LayerMask.GetMask("Blocked"));
            if (_ghost) _ghost.GetComponentInChildren<MeshRenderer>().enabled = true;
        }
    }

    private IEnumerator Delay(float wait)
    {
        yield return new WaitForSeconds(wait);
        free = true;
    }

    private void PlaceShip()
    {
        if (!CanEditShips()) return; // checks if valid to place ships
        if (!GhostValid()) return; // ghost ship required
        if (SelectedRemaining() == 0) return; // check if ship available

        int len = _ghost.GetComponent<LineShipView>().shipLength;
        _cCollider = _ghost.transform.position;

        // determine the direction for the collider check
        if (Mathf.Approximately(_ghost.transform.rotation.eulerAngles.y, 90))
        {
            _bCollider = new Vector3(1, 1, len);
            _cCollider.z += (len - 1) * 0.5f;
        }
        else if (Mathf.Approximately(_ghost.transform.rotation.eulerAngles.z, 90))
        {
            _bCollider = new Vector3(1, len, 1);
            _cCollider.y += (len - 1) * 0.5f;
        }
        else
        {
            _bCollider = new Vector3(len, 1, 1);
            _cCollider.x += (len - 1) * 0.5f;
        }

        // the overlap box needs to have dimensions half that of the original, or it will be too large
        Vector3 correct = _bCollider / 3f;
        Collider[] hit = { };
        int _ = Physics.OverlapBoxNonAlloc(_cCollider, correct, hit, Quaternion.identity, overlap);

        // if there are colliders of ships already there, abort
        if (hit.Length > 0)
        {
            return;
        }

        ShipView newShip = Instantiate(
            ObjectFromSelected(),
            _ghost.transform.position,
            _ghost.transform.rotation,
            transform
        );

        _shipObjects[_selectedShip].Add(newShip);//placing it, grab data here

        //////////////////////////////
        // Grabbing Ship Location, length, direction 

        Vector3 cursor = spaceBuilder.GetCursorLocation();
        //convert to Vector3Int
        Vector3Int startCell = new Vector3Int(
            Mathf.RoundToInt(cursor.x),
            Mathf.RoundToInt(cursor.y),
            Mathf.RoundToInt(cursor.z)
        );

        Vector3Int direction;

        if (Mathf.Approximately(_ghost.transform.rotation.eulerAngles.y, 90))
        {
            direction = new Vector3Int(0, 0, 1);
        }
        else if (Mathf.Approximately(_ghost.transform.rotation.eulerAngles.z, 90))
        {
            direction = new Vector3Int(0, 1, 0);
        }
        else
        {
            direction = new Vector3Int(1, 0, 0);
        }

        //////////////
        List<Vector3Int> occupiedCells = GetOccupiedCells(startCell, direction, len);// grabs ships starting cell, direction, and length 

        foreach (Vector3Int cell in occupiedCells)
        {
            Debug.Log($"Ship occupies cell: {cell}");
        }

        spaceBuilder.Register_ship_data(_selectedShip, len, occupiedCells);


        GameObject colliderObject = newShip.gameObject;
        colliderObject.layer = LayerMask.NameToLayer("Ship");

        if (colliderObject.transform.childCount > 0)
            colliderObject.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Ship");

        colliderObject.name = colliderObject.name + " " + _index;
        _index++;

        // update UI
        _placementUI.UpdateButtons();

        HandleCursorMoved(); // checks if out of ships
    }

    private ShipView ObjectFromSelected()
    {
        // Gets the prefab / game object for the selected ship
        return _shipTypeManager.GetPrefab(_selectedShip);
    }

    public void SetActiveBoard(bool active)
    {
        isActiveBoard = active;
    }

    public bool AllShipsPlaced()
    {
        // iterates through the dict and compares how many ships are allowed to how many are placed
        foreach ((int shipType, int _) in _shipTypeManager.Rations())
        {
            if (Remaining(shipType) != 0) return false;
        }

        return true;
    }

    public void LockPlacement()
    {
        // changes bool type to true and checks if a ghost object is still present and if so destroys it
        placementLocked = true;

        if (!_ghost) return;
        Destroy(_ghost.gameObject);
        _ghost = null;
    }

    public void TryLockPlacement()
    {
        // holder function that checks if all ships are placed for that player then activates lock
        if (!AllShipsPlaced())
        {
            Debug.Log("You must place all ships before locking.");
            return;
        }

        LockPlacement();
    }

    List<Vector3Int> GetOccupiedCells(Vector3Int startcell, Vector3Int direction, int len)
    {
        List<Vector3Int> cells = new List<Vector3Int>();

        for (int i = 0; i < len; i++)
        {
            cells.Add(startcell + direction * i);
        }

        return cells;
    }
}