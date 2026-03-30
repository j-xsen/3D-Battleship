using Ships;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Ghost Materials")] [SerializeField]
    private Material ghostValid;
    [SerializeField] private Material ghostInvalid;
    
    [Header("cam")]
    [SerializeField] private GameObject cam;

    [SerializeField] private bool isActiveBoard = true; //checks if this board is active 

    // private TMP_Text[] texts;

    private ShipView _ghost;
    private ShipTypeManager _shipTypeManager;

    private int _selectedShip; // the current "ghost" ship
    private bool _placing; // this is a config bool to toggle updating the ghost
    private ShipPlacementUI _placementUI;
    private Dictionary<int, List<ShipView>> _shipObjects; // a list of all ship game objects to keep count

    // inputs
    private InputAction _placeShip;
    private Action<InputAction.CallbackContext> _onPlaceShip;
    private InputAction _cycleShip;
    private Action<InputAction.CallbackContext> _onCycleShip;
    private InputAction _rotateShipRight;
    private Action<InputAction.CallbackContext> _onRotateShip;
    private InputAction _rotateShipLeft;

    //for setting collision detections
    private Vector3 _bCollider;
    private Vector3 _cCollider;
    //prevent overlap
    public LayerMask overlap;
    //making a number to add onto names
    private int _index;
    
    // network
    private SessionManager _network;

    private void Start()
    {
        // find network
        _network = GameObject.FindWithTag("NetworkManager").GetComponent<SessionManager>();
        if(!_network) Debug.LogError("Unable to find NetworkManager!");
        
        // hover events
        HoverActions.current.Clicked += PlaceShip;
        HoverActions.current.ShipClicked += Redo;

        // input setup
        _placeShip = InputSystem.actions.FindAction("SpaceField/ShipPlace");
        _onPlaceShip = _ => PlaceShip();
        _placeShip.performed += _onPlaceShip;
        _cycleShip = InputSystem.actions.FindAction("SpaceField/ShipCycle");
        _onCycleShip = _ => CycleShip();
        _cycleShip.performed += _onCycleShip;
        _rotateShipRight = InputSystem.actions.FindAction("SpaceField/ShipRotateRight");
        _onRotateShip = _ => RotateShip();
        _rotateShipRight.performed += _onRotateShip;
        _rotateShipLeft = InputSystem.actions.FindAction("SpaceField/ShipRotateLeft");
        _rotateShipLeft.performed += _onRotateShip;

        // get ShipTypeManager, which should be attached with this object
        _shipTypeManager = GetComponentInParent<ShipTypeManager>();
        if (!_shipTypeManager) Debug.LogError("No ShipTypeManager found with ShipManager");

        // get PlacementUI, which should be attached with this object
        _placementUI = GetComponentInParent<ShipPlacementUI>();
        if (!_placementUI) Debug.LogError("No ShipPlacementUI found with ShipManager");

        // init ship objects dict with null values
        _shipObjects = new Dictionary<int, List<ShipView>>();
        foreach ((int shipType, int count) in _shipTypeManager.Rations())
        {
            List<ShipView> objects = new(count);
            for (int i = 0; i < count; i++)
            {
                objects.Add(null);
            }
            _shipObjects[shipType] = objects;
        }

        _placing = true; // config
        _selectedShip = _shipTypeManager.MinShip(); // defaults ghost ship to the smallest ship

        // used to access config (i.e. field size) & cursor event
        //_spaceBuilder = GetComponent<SpaceBuilder>();
        if (!spaceBuilder)
        {
            Debug.LogError("No SpaceBuilder found on ShipManager!");
            return;
        }

        spaceBuilder.OnCursorMoved += HandleCursorMoved; // gets called every time the cursor moves
        HandleCursorMoved(); // creates ghost on start
    }

    private void Redo(GameObject ship)
    {
        //Debug.Log("got this far");
        SelectShip(ship.GetComponent<LineShipView>().shipLength - 2);
        //Debug.Log(_shipObjects[_selectedShip]);
        //_shipObjects[_selectedShip].RemoveAt(Ship.GetComponent<LineShipView>().index);
        for (int i = 0; i < _shipObjects[_selectedShip].Count; i++)
        {
            /*
            Debug.Log("found ship name: " + Ship.name);
            Debug.Log("listed ship name: " + _shipObjects[_selectedShip][i].gameObject.name);
            */
            if (_shipObjects[_selectedShip][i].gameObject.name != ship.name) continue;
            _shipObjects[_selectedShip][i] = null;
            Destroy(ship);
            // texts[(int)_selectedShip].text = SelectedRemaining() + "";
            _placementUI.UpdateButtons();
            return;
            //Debug.Log(_shipObjects[_selectedShip]);
        }

        Debug.Log("couldn't find ship");
    }

    private void OnDestroy()
    {
        // unload input function
        if (spaceBuilder) spaceBuilder.OnCursorMoved -= HandleCursorMoved;
        if (_placeShip != null) _placeShip.performed -= _onPlaceShip;
        if (_cycleShip != null) _cycleShip.performed -= _onCycleShip;
        if (_rotateShipRight != null) _rotateShipRight.performed -= _onRotateShip;
        if (_rotateShipLeft != null) _rotateShipLeft.performed -= _onRotateShip;
        HoverActions.current.Clicked -= PlaceShip;
        HoverActions.current.ShipClicked -= Redo;
    }

    private int SelectedRemaining()
    {
        // returns the number of selected ships remaining
        int startingRation = _shipTypeManager.Rations(_selectedShip);
        int numberPlaced = ShipsPlaced(_selectedShip);
        return startingRation - numberPlaced;
    }

    private bool GhostValid()
    {
        // TODO - don't allow placing on top of another ship
        return SelectedRemaining() > 0 && // check if ship size available
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
        if (!isActiveBoard) return;

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
        return _shipObjects[shipType].Count(ship => ship);
    }

    private void CycleShip()
    {
        if (!isActiveBoard) return;

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
        _ghost.SetAxes(savedAxes);
        _ghost.MoveShip(newPos, spaceBuilder.GetCursorLocation());
        _ghost.SetMaterial(GetMaterial());
    }

    private void HandleCursorMoved()
    {
        if (!isActiveBoard) return;

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

    private bool free = true;

    private void Update()
    {
        if ((Mouse.current.rightButton.isPressed) & (free))
        {
            RotateShip();
            free = false;
            StartCoroutine(Delay(0.5f));
        }

        if (Mouse.current.scroll.ReadValue().y != 0)
        {
            ReinstantiateGhost();
        }
    }

    public void ChangeMode()
    {
        PhysicsRaycaster mode = cam.GetComponent<PhysicsRaycaster>();
        //check if default layer is active
        if ((mode.eventMask & (1 << 0)) != 0)
        {
            //set all layers to mask
            mode.eventMask = ~0 & ~(LayerMask.GetMask("Default")) & ~(LayerMask.GetMask("Blocked"));
            _ghost.GetComponentInChildren<MeshRenderer>().enabled = false;
            HandleCursorMoved();
        }
        else
        {
            mode.eventMask = ~0 & ~(LayerMask.GetMask("Ship")) & ~(LayerMask.GetMask("Blocked"));
            _ghost.GetComponentInChildren<MeshRenderer>().enabled = true;
        }
    }

    IEnumerator Delay(float wait)
    {
        yield return new WaitForSeconds(wait);
        free = true;
    }

    private void PlaceShip()
    {
        if (!isActiveBoard) return;

        // places a ship where the ghost ship is

        if (!GhostValid()) return; // ghost ship required

        if (SelectedRemaining() == 0) return; // check if ship available.

        int len = _ghost.GetComponent<LineShipView>().shipLength;
        _cCollider = _ghost.transform.position;

        //determine the direction for the collider check
        if (Mathf.Approximately(_ghost.transform.rotation.eulerAngles.y, 90))
        {
            _bCollider = new Vector3(1, 1, len);
            _cCollider.z += (float)((len - 1) * .5);
            //  Debug.Log("up " + _ghost.transform.rotation.eulerAngles.y);
        }
        else if (Mathf.Approximately(_ghost.transform.rotation.eulerAngles.z, 90))
        {
            _bCollider = new Vector3(1, len, 1);
            _cCollider.y += (float)((len - 1) * .5);
            // Debug.Log("right" + _ghost.transform.rotation.eulerAngles.z);
        }
        else
        {
            _bCollider = new Vector3(len, 1, 1);
            _cCollider.x += (float)((len - 1) * .5);
            // Debug.Log("normal z: " + _ghost.transform.rotation.eulerAngles.z + " y: " + _ghost.transform.rotation.eulerAngles.y);
        }

        //the overlap box needs to have dimensions half that of the original, or it will be too large
        Vector3 correct = _bCollider / 3;
        Collider[] hit = Physics.OverlapBox(_cCollider, correct, Quaternion.identity, overlap);
        foreach (Collider found in hit)
        {
            Debug.Log("colliders: " + found);
        }

        //if there are colliders of ships already there, abort
        if (hit.Length > 0)
        {
            return;
        }

        // get the dict of ship objects for the selected ship
        // then get the index of the maximum ship ration - remaining amount
        // then set that to a prefab placed at the ghost's location
        int index = _shipTypeManager.Rations(_selectedShip) - SelectedRemaining();
        _shipObjects[_selectedShip][index] =
            Instantiate(ObjectFromSelected(),
                _ghost.transform.position,
                _ghost.transform.rotation,
                transform);

        //Debug.Log("index 2: " + (_shipRations[_selectedShip] - SelectedRemaining()));
        Debug.Log("number placed: " + index);
        GameObject colliderObject =
            _shipObjects[_selectedShip][index].gameObject;
        colliderObject.layer = LayerMask.NameToLayer("Ship");
        colliderObject.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Ship");
        colliderObject.name = colliderObject.name + " " + _index;
        _index += 1;
        //  Debug.Log("listed ship name: " + _shipObjects[_selectedShip][(_shipRations[_selectedShip] - SelectedRemaining()) - 1].gameObject.name);
        
        _network.PlaceShip(_selectedShip, ShipsPlaced(_selectedShip), _ghost);
        //  _shipObjects[_selectedShip][(_shipRations[_selectedShip] - SelectedRemaining()) - 1].gameObject.GetComponent<LineShipView>().index = (_shipRations[_selectedShip] - SelectedRemaining()) - 1;

        //update UI
        // texts[(int)_selectedShip].text = SelectedRemaining() + "";
        _placementUI.UpdateButtons();

        HandleCursorMoved(); // checks if out of ships
    }

    private ShipView ObjectFromSelected()
    {
        // Above function but does selected ship
        return _shipTypeManager.GetPrefab(_selectedShip);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // Check that it is being run in Play Mode, so it doesn't try to draw this in Editor mode
        if (Application.isPlaying)
            // Draw a cube where the OverlapBox is (positioned where your GameObject is as well as a size)
            Gizmos.DrawWireCube(_cCollider, _bCollider);
    }


    public void SetActiveBoard(bool active)
    {
        isActiveBoard = active;
    }
}