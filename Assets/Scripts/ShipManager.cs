using System;
using System.Collections.Generic;
using System.Linq;
using Ships;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipManager : MonoBehaviour
{
    [SerializeField] private LineShipView[] lineShipPrefabs;

    [Header("Ghost Materials")]
    [SerializeField] private Material ghostValid;
    [SerializeField] private Material ghostInvalid;

    private enum ShipTypes
    {
        Two,
        Three,
        Four,
        Five
    }

    // these store the smallest and biggest ship from ShipTypes
    private const ShipTypes MinShip = ShipTypes.Two;
    private const ShipTypes MaxShip = ShipTypes.Five;

    private ShipView _ghost;

    private ShipTypes _selectedShip; // the current "ghost" ship
    private bool _placing; // this is a config bool to toggle updating the ghost
    private SpaceBuilder _spaceBuilder;
    private Dictionary<ShipTypes, int> _shipRations; // currently defined in Start(), is the number of ships allowed
    private Dictionary<ShipTypes, List<ShipView>> _shipObjects; // a list of all ship game objects to keep count

    // inputs
    private InputAction _placeShip;
    private Action<InputAction.CallbackContext> _onPlaceShip;
    private InputAction _cycleShip;
    private Action<InputAction.CallbackContext> _onCycleShip;
    private InputAction _rotateShipRight;
    private Action<InputAction.CallbackContext> _onRotateShip;
    private InputAction _rotateShipLeft;

    private void Start()
    {
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

        // TODO: Get game settings from server?
        
        // maximum number
        _shipRations = new Dictionary<ShipTypes, int>
        {
            [ShipTypes.Two] = 1,
            [ShipTypes.Three] = 1,
            [ShipTypes.Four] = 1,
            [ShipTypes.Five] = 1
        };
        // init ship objects dict with null values
        _shipObjects = new Dictionary<ShipTypes, List<ShipView>>();
        foreach ((ShipTypes shipType, int count) in _shipRations)
        {
            List<ShipView> objects = new(count);
            for (int i = 0; i < count; i++)
            {
                objects.Add(null);
            }

            _shipObjects[shipType] = objects;
        }

        _placing = true; // config
        _selectedShip = ShipTypes.Two; // defaults ghost ship to the two wide ship

        // used to access config (i.e. field size) & cursor event
        _spaceBuilder = GetComponent<SpaceBuilder>();
        if (!_spaceBuilder)
        {
            Debug.LogError("No SpaceBuilder found on ShipManager!");
            return;
        }

        _spaceBuilder.OnCursorMoved += HandleCursorMoved; // gets called every time the cursor moves
        HandleCursorMoved(); // creates ghost on start
    }

    private void OnDestroy()
    {
        // unload input function
        if (_spaceBuilder) _spaceBuilder.OnCursorMoved -= HandleCursorMoved;
        _placeShip.performed -= _onPlaceShip;
        _cycleShip.performed -= _onCycleShip;
        _rotateShipRight.performed -= _onRotateShip;
        _rotateShipLeft.performed -= _onRotateShip;
    }

    private int SelectedRemaining()
    {
        // returns the number of selected ships remaining
        int startingRation = _shipRations[_selectedShip];
        int numberPlaced = _shipObjects[_selectedShip].Count(ship => ship);
        return startingRation - numberPlaced;
    }

    private bool GhostValid()
    {
        // TODO - don't allow placing on top of another ship
        if (SelectedRemaining() <= 0) return false; // check if ship size available
        return _ghost.HasValidPlacement(_spaceBuilder.GetSize());
    }

    private Vector3 TransformedPoint()
    {
        // gets the world positioning of the cursor for placement alignment
        return _spaceBuilder.transform.TransformPoint(_spaceBuilder.GetCursorLocation());
    }

    private Material GetMaterial()
    {
        return GhostValid() ? ghostValid : ghostInvalid;
    }

    private void RotateShip()
    {
        // alters the rotation axis
        if (!_ghost) return;
        _ghost.Rotate();
        _ghost.SetMaterial(GetMaterial());
    }

    private void CycleShip()
    {
        // called to cycle through ship types
        _selectedShip += 1;
        if (_selectedShip.CompareTo(MaxShip) > 0) _selectedShip = MinShip; // reset to min ship when bigger than max
        ReinstantiateGhost();
    }

    private void ReinstantiateGhost()
    {
        // destroys the ghost object and recreates it
        
        // saves transform info of ghost
        Vector3 newPos = _ghost ? _ghost.transform.position : TransformedPoint();
        Quaternion newRot = _ghost ? _ghost.transform.rotation : Quaternion.identity;

        AxisObject savedAxes =_ghost ? _ghost.GetAxes() : Axes.X;
        
        if (_ghost) Destroy(_ghost.gameObject);
        
        // remakes ghost with the transform info & parents to self
        _ghost = Instantiate(ObjectFromSelected(), newPos, newRot, transform);
        _ghost.SetAxes(savedAxes);
        _ghost.MoveShip(newPos, _spaceBuilder.GetCursorLocation());
        _ghost.SetMaterial(GetMaterial());
    }

    private void HandleCursorMoved()
    {
        if (!_placing) return;

        if (_ghost)
        {
            // ghost exists, just update pos and material
            _ghost.MoveShip(TransformedPoint(), _spaceBuilder.GetCursorLocation());
            _ghost.SetMaterial(GetMaterial());
        }
        else
        {
            // ghost doesn't exist, create
            ReinstantiateGhost();
        }
    }

    private void PlaceShip()
    {
        // places a ship where the ghost ship is
        
        if (!GhostValid()) return; // ghost ship required

        if (SelectedRemaining() == 0) return; // check if ship available.

        // get the dict of ship objects for the selected ship
        // then get the index of the maximum ship ration - remaining amount
        // then set that to a prefab placed at the ghost's location
        _shipObjects[_selectedShip][_shipRations[_selectedShip]-SelectedRemaining()] =
            Instantiate(ObjectFromSelected(),
                _ghost.transform.position,
                _ghost.transform.rotation,
                transform);

        HandleCursorMoved(); // checks if out of ships
    }

    private ShipView ObjectFromSelected()
    {
        // Above function but does selected ship
        return lineShipPrefabs[(int)_selectedShip];
    }
}