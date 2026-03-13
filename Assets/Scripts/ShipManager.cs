using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipManager : MonoBehaviour
{
    [Header("Ship Prefabs")]
    [SerializeField] private GameObject prefabTwo;
    [SerializeField] private GameObject prefabThree;
    [SerializeField] private GameObject prefabFour;
    [SerializeField] private GameObject prefabFive;

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

    private enum Axis
    {
        X,
        Y,
        Z
    }

    // these just auto select the smallest and biggest ship from ShipTypes
    private static readonly ShipTypes MinShip = Enum.GetValues(typeof(ShipTypes)).Cast<ShipTypes>().Min();
    private static readonly ShipTypes MaxShip = Enum.GetValues(typeof(ShipTypes)).Cast<ShipTypes>().Max();

    private ShipTypes _selectedShip; // the current "ghost" ship
    private bool _placing; // this is a config bool to toggle updating the ghost
    private SpaceBuilder _spaceBuilder;
    private GameObject _ghost; // the transparent to-be-placed ship
    private Renderer _ghostRenderer; // the ship's renderer - used to change ghost materials
    private Axis _ghostAxis = Axis.X; // the long axis of the ship
    private Dictionary<ShipTypes, int> _shipRations; // currently defined in Start(), is the number of ships allowed
    private Dictionary<ShipTypes, List<GameObject>> _shipObjects; // a list of all ship game objects to keep count

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
        _shipObjects = new Dictionary<ShipTypes, List<GameObject>>();
        foreach ((ShipTypes shipType, int count) in _shipRations)
        {
            List<GameObject> objects = new(count);
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

    private int SelectedLength()
    {
        return _selectedShip switch
        {
            ShipTypes.Two => 2,
            ShipTypes.Three => 3,
            ShipTypes.Four => 4,
            ShipTypes.Five => 5,
            _ => throw new ArgumentOutOfRangeException(),
        };
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
        Vector3 curPoint = _spaceBuilder.GetCursorLocation();
        // define bounds
        return _ghostAxis switch
        {
            Axis.X => !(curPoint.x + SelectedLength() > _spaceBuilder.GetSize()),
            Axis.Z => curPoint.z - SelectedLength() >= -1,
            Axis.Y => !(curPoint.y + SelectedLength() > _spaceBuilder.GetSize()),
            _ => throw new ArgumentOutOfRangeException()
        };
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
        Axis axis = _ghostAxis switch
        {
            Axis.X => Axis.Z,
            Axis.Z => Axis.Y,
            Axis.Y => Axis.X,
            _ => throw new ArgumentOutOfRangeException()
        };
        switch (axis)
        {
            case Axis.X:
                _ghost.transform.Rotate(Vector3.forward, 270);
                break;
            case Axis.Z:
                _ghost.transform.Rotate(Vector3.up, 90);
                break;
            case Axis.Y:
                _ghost.transform.Rotate(Vector3.up, 270); // undoes Z
                _ghost.transform.Rotate(Vector3.forward, 90);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _ghostAxis = axis;
        _ghostRenderer.material = GetMaterial();
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

        if (_ghost) Destroy(_ghost);
        if (_ghostRenderer) Destroy(_ghostRenderer);
        
        // remakes ghost with the transform info & parents to self
        _ghost = Instantiate(ObjectFromSelected(), newPos, newRot, transform);
        _ghostRenderer = _ghost.GetComponentInChildren<Renderer>();
        _ghostRenderer.material = GetMaterial();
    }

    private void HandleCursorMoved()
    {
        if (!_placing) return;

        Vector3 worldPos = TransformedPoint();

        if (_ghost)
        {
            // ghost exists, just update pos and material
            _ghost.transform.position = worldPos;
            _ghostRenderer.material = GhostValid() ? ghostValid : ghostInvalid;
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

    private GameObject ObjectFromType(ShipTypes type)
    {
        // returns prefab from ShipType
        return type switch
        {
            ShipTypes.Two => prefabTwo,
            ShipTypes.Three => prefabThree,
            ShipTypes.Four => prefabFour,
            ShipTypes.Five => prefabFive,
            _ => prefabTwo
        };
    }

    private GameObject ObjectFromSelected()
    {
        // Above function but does selected ship
        return ObjectFromType(_selectedShip);
    }
}