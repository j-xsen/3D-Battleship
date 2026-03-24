using Ships;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShipManager : MonoBehaviour
{
    [SerializeField] private LineShipView[] lineShipPrefabs;

    [Header("Ghost Materials")]
    [SerializeField] private Material ghostValid;
    [SerializeField] private Material ghostInvalid;
    [Header("buttons")]
    [SerializeField] private Button buttonOne;
    [SerializeField] private Button buttonTwo;
    [SerializeField] private Button buttonThree;
    [SerializeField] private Button buttonFour;
    [Header("cam")]
    [SerializeField] private GameObject cam;

    private TMP_Text[] texts;
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


    //for setting collision detections
    private Vector3 bcollider;
    private Vector3 ccollider;
    //check how many total are left
    private int _shipcount = 0;
    //prevent overlap
    public LayerMask overlap;
    //making a number to add onto names
    private int _index = 0;
    private void Start()
    {
        hover.current.Clicked += PlaceShip;
        hover.current.Shipclick += Redo;
        texts = new TMP_Text[] { buttonOne.gameObject.GetComponentInChildren<TMP_Text>(), buttonTwo.gameObject.GetComponentInChildren<TMP_Text>(), buttonThree.gameObject.GetComponentInChildren<TMP_Text>(), buttonFour.gameObject.GetComponentInChildren<TMP_Text>() };
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
            [ShipTypes.Two] = 4,
            [ShipTypes.Three] = 3,
            [ShipTypes.Four] = 2,
            [ShipTypes.Five] = 1
        };

        //set up starting UI with number of available ships
        texts[0].text = (_shipRations[ShipTypes.Two]) + "";
        texts[1].text = (_shipRations[ShipTypes.Three]) + "";
        texts[2].text = (_shipRations[ShipTypes.Four]) + "";
        texts[3].text = (_shipRations[ShipTypes.Five]) + "";


        // init ship objects dict with null values
        _shipObjects = new Dictionary<ShipTypes, List<ShipView>>();
        foreach ((ShipTypes shipType, int count) in _shipRations)
        {
            List<ShipView> objects = new(count);
            for (int i = 0; i < count; i++)
            {
                objects.Add(null);
                _shipcount = _shipcount + 1;
            }
            _shipObjects[shipType] = objects;
        }
        Debug.Log("shipcount: " + _shipcount);

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

        //set up UI Buttons to have listeners
        buttonOne.onClick.AddListener(() =>
        {
            ChosenShip(0);
            ReinstantiateGhost();
        });
        buttonTwo.onClick.AddListener(() =>
        {
            ChosenShip(1);
            ReinstantiateGhost();
        });
        buttonThree.onClick.AddListener(() =>
        {
            ChosenShip(2);
            ReinstantiateGhost();
        });
        buttonFour.onClick.AddListener(() =>
        {
            ChosenShip(3);
            ReinstantiateGhost();
        });

    }

    public void Protect()
    {
        for (int i = 0; i < _shipObjects.Count; i++)
        {
            ChosenShip(i);
            foreach (var ship in _shipObjects[_selectedShip])
            {
                ship.transform.parent = null;
                DontDestroyOnLoad(ship);
            }
        }
    }

    private void Redo(GameObject Ship)
    {
        //Debug.Log("got this far");
        ChosenShip(Ship.GetComponent<LineShipView>().shipLength - 2);
        //Debug.Log(_shipObjects[_selectedShip]);
        //_shipObjects[_selectedShip].RemoveAt(Ship.GetComponent<LineShipView>().index);
        for (int i = 0; i < _shipObjects[_selectedShip].Count; i++)
        {
            /*
            Debug.Log("found ship name: " + Ship.name);
            Debug.Log("listed ship name: " + _shipObjects[_selectedShip][i].gameObject.name);
            */
            if (_shipObjects[_selectedShip][i].gameObject.name == Ship.name)
            {
                _shipObjects[_selectedShip].RemoveAt(i);
                Destroy(Ship);
                texts[(int)_selectedShip].text = SelectedRemaining() + "";
                return;
            }
            //Debug.Log(_shipObjects[_selectedShip]);
        }
        Debug.Log("couldn't find ship");
    }
    private void OnDestroy()
    {
        // unload input function
        if (_spaceBuilder) _spaceBuilder.OnCursorMoved -= HandleCursorMoved;
        if (_placeShip != null) _placeShip.performed -= _onPlaceShip;
        if (_cycleShip != null) _cycleShip.performed -= _onCycleShip;
        if (_rotateShipRight != null) _rotateShipRight.performed -= _onRotateShip;
        if (_rotateShipLeft != null) _rotateShipLeft.performed -= _onRotateShip;
        hover.current.Clicked -= PlaceShip;
        hover.current.Shipclick -= Redo;
    }

    private int SelectedRemaining()
    {
        // returns the number of selected ships remaining
        int startingRation = _shipRations[_selectedShip];
        int numberPlaced = _shipObjects[_selectedShip].Count(ship => ship);
        if (startingRation - numberPlaced == 0)
        {
            texts[(int)_selectedShip].gameObject.transform.parent.GetComponent<Button>().interactable = false;
        }
        else
        {
            texts[(int)_selectedShip].gameObject.transform.parent.GetComponent<Button>().interactable = true;
        }
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

    private void ChosenShip(int clicked)
    {
        switch (clicked)
        {
            case 0:
                {
                    _selectedShip = ShipTypes.Two;
                    break;
                }
            case 1:
                {
                    _selectedShip = ShipTypes.Three;
                    break;
                }
            case 2:
                {
                    _selectedShip = ShipTypes.Four;
                    break;
                }
            case 3:
                {
                    _selectedShip = ShipTypes.Five;
                    break;
                }
        }
        //ReinstantiateGhost();
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

        AxisObject savedAxes = _ghost ? _ghost.GetAxes() : Axes.X;

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
        // places a ship where the ghost ship is

        if (!GhostValid()) return; // ghost ship required
       // Debug.Log("Valid");

        if (SelectedRemaining() == 0) return; // check if ship available.
                                              //  Debug.Log("Not Enough");
        int len = _ghost.GetComponent<LineShipView>().shipLength;
        ccollider = _ghost.transform.position;

        //determine the direction for the collider check
        if ( _ghost.transform.rotation.eulerAngles.y == 90)
        {
            bcollider = new Vector3(1, 1, len);
            ccollider.z = ccollider.z + (float)((len - 1) * .5);
          //  Debug.Log("up " + _ghost.transform.rotation.eulerAngles.y);
        }
        else if ( _ghost.transform.rotation.eulerAngles.z == 90)
        {
            bcollider = new Vector3(1, len, 1);
            ccollider.y = ccollider.y + (float)((len - 1) * .5);
           // Debug.Log("right" + _ghost.transform.rotation.eulerAngles.z);
        }
        else
        {
            bcollider = new Vector3(len, 1, 1);
            ccollider.x = ccollider.x + (float)((len - 1) * .5);
           // Debug.Log("normal z: " + _ghost.transform.rotation.eulerAngles.z + " y: " + _ghost.transform.rotation.eulerAngles.y);
        }

        //the overlap box needs to have dimensions half that of the original, or it will be too large
        Vector3 correct = bcollider / 3;
        Collider[] hit = Physics.OverlapBox(ccollider, correct, Quaternion.identity, overlap);
        foreach (Collider found in hit)
        {
            Debug.Log("colliders: " + found);
        }

        //if there are colliders of ships already there, abort
        if (hit.Length > 0)
        {
            return;
        }

        if (_shipRations[_selectedShip] - SelectedRemaining() >= _shipObjects[_selectedShip].Count)
        {
            _shipObjects[_selectedShip].Add(
              Instantiate(ObjectFromSelected(),
                  _ghost.transform.position,
                  _ghost.transform.rotation,
                  transform));
        }
        else
        {
            // get the dict of ship objects for the selected ship
            // then get the index of the maximum ship ration - remaining amount
            // then set that to a prefab placed at the ghost's location
            Debug.Log((_shipRations[_selectedShip] - SelectedRemaining()));
            _shipObjects[_selectedShip][_shipRations[_selectedShip] - SelectedRemaining()] =
                Instantiate(ObjectFromSelected(),
                    _ghost.transform.position,
                    _ghost.transform.rotation,
                    transform);
        }
        //Debug.Log("index 2: " + (_shipRations[_selectedShip] - SelectedRemaining()));
        Debug.Log("number placed: " + ((_shipRations[_selectedShip] - SelectedRemaining()) - 1));
        GameObject collider_object = _shipObjects[_selectedShip][(_shipRations[_selectedShip] - SelectedRemaining()) - 1].gameObject;
        collider_object.layer = LayerMask.NameToLayer("Ship");
        collider_object.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Ship");
        collider_object.name = collider_object.name + " " + _index;
        _index = _index + 1;
      //  Debug.Log("listed ship name: " + _shipObjects[_selectedShip][(_shipRations[_selectedShip] - SelectedRemaining()) - 1].gameObject.name);


        //  _shipObjects[_selectedShip][(_shipRations[_selectedShip] - SelectedRemaining()) - 1].gameObject.GetComponent<LineShipView>().index = (_shipRations[_selectedShip] - SelectedRemaining()) - 1;

        //update UI
        texts[(int)_selectedShip].text = SelectedRemaining() + "";

        HandleCursorMoved(); // checks if out of ships
    }

    private ShipView ObjectFromSelected()
    {
        // Above function but does selected ship
        return lineShipPrefabs[(int)_selectedShip];
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // Check that it is being run in Play Mode, so it doesn't try to draw this in Editor mode
        if (Application.isPlaying)
            // Draw a cube where the OverlapBox is (positioned where your GameObject is as well as a size)
            Gizmos.DrawWireCube(ccollider, bcollider);
    }
}