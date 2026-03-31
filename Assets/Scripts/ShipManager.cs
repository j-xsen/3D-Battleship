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
using UnityEngine.UIElements;

public class ShipManager : MonoBehaviour
{
    [FormerlySerializedAs("_spaceBuilder")]
    [SerializeField] private SpaceBuilder spaceBuilder;

    [Header("Ghost Materials")]
    [SerializeField] private Material ghostValid;
    [SerializeField] private Material ghostInvalid;

    [Header("cam")]
    [SerializeField] private GameObject cam;

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDoc;
    [SerializeField] private VisualTreeAsset shipPlaceButton;

    [SerializeField] private bool placementLocked = false; // bool check to lock placement when all ships are placed
    [SerializeField] private bool isActiveBoard = true; // checks if this board is active

<<<<<<< HEAD
    private bool CanEditShips() // extra check
    {
        return isActiveBoard && !placementLocked;
    }

=======
    private TMP_Text[] texts;
>>>>>>> parent of 89d099d (Added Turn Locking so that once player 1 has placed all ships they can press ready and player 2 can place their ships which after can lead to combat phase)
    private enum ShipTypes
    {
        Two = 0,
        Three = 1,
        Four = 2,
        Five = 3
    }

    private ShipView _ghost;
    private ShipTypeManager _shipTypeManager;

    private int _selectedShip; // the current "ghost" ship
    private bool _placing; // this is a config bool to toggle updating the ghost

    // currently defined in Start(), is the number of ships allowed
    private Dictionary<int, int> _shipRations;

    // ship button UI
    private Dictionary<int, Button> _shipButtons;
    private Dictionary<int, Label> _shipText;

    // a list of all ship game objects to keep count
    private Dictionary<int, List<ShipView>> _shipObjects;

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

<<<<<<< HEAD
    // prevent overlap
=======
    //for setting collision detections
    private Vector3 bcollider;
    private Vector3 ccollider;
    //check how many total are left
    private int _shipcount = 0;
    //prevent overlap
>>>>>>> parent of 89d099d (Added Turn Locking so that once player 1 has placed all ships they can press ready and player 2 can place their ships which after can lead to combat phase)
    public LayerMask overlap;

    // making a number to add onto names
    private int _index;

    // network
    private SessionManager _network;

    private bool free = true;

    private void Start()
    {
        // find network
        //_network = GameObject.FindWithTag("NetworkManager")?.GetComponent<SessionManager>();
        //if (!_network) Debug.LogError("Unable to find NetworkManager!");

        // get ship type manager
        _shipTypeManager = GetComponent<ShipTypeManager>();
        if (!_shipTypeManager) Debug.LogError("No ShipTypeManager found on ShipManager");

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

        // maximum number
        _shipRations = new Dictionary<int, int>
        {
<<<<<<< HEAD
            [(int)ShipTypes.Two] = 1,
            [(int)ShipTypes.Three] = 1,
            [(int)ShipTypes.Four] = 1,
            [(int)ShipTypes.Five] = 1
        };

        // init ship objects dict as empty dynamic lists
        _shipObjects = new Dictionary<int, List<ShipView>>();
        _shipButtons = new Dictionary<int, Button>();
        _shipText = new Dictionary<int, Label>();

        foreach (int shipType in _shipRations.Keys)
=======
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
>>>>>>> parent of 89d099d (Added Turn Locking so that once player 1 has placed all ships they can press ready and player 2 can place their ships which after can lead to combat phase)
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

        // create button ui
        BuildShipButtons();

        _placing = true; // config
        _selectedShip = _shipTypeManager != null ? _shipTypeManager.MinShip() : 0; // defaults ghost ship to the smallest ship

        if (!spaceBuilder)
        {
            Debug.LogError("No SpaceBuilder found on ShipManager!");
            return;
        }

        spaceBuilder.OnCursorMoved += HandleCursorMoved; // gets called every time the cursor moves
        HandleCursorMoved(); // creates ghost on start

        UpdateButtons();
    }

<<<<<<< HEAD
    private void BuildShipButtons()
    {
        if (uiDoc == null || shipPlaceButton == null)
        {
            Debug.LogWarning("UI Toolkit references missing on ShipManager.");
            return;
        }

        VisualElement root = uiDoc.rootVisualElement;
        if (root == null) return;

        VisualElement container = new VisualElement
        {
            pickingMode = PickingMode.Ignore
        };

        container.style.position = Position.Absolute;
        container.style.top = 0;
        container.style.bottom = 0;
        container.style.right = 0;
        container.style.justifyContent = Justify.Center;
        container.style.alignItems = Align.FlexEnd;
        container.style.paddingRight = 32;

        root.Add(container);

        foreach (KeyValuePair<int, int> pair in _shipRations)
        {
            int shipType = pair.Key;
            int count = pair.Value;

            VisualElement shipButtonElement = shipPlaceButton.Instantiate();
            shipButtonElement.pickingMode = PickingMode.Ignore;
            shipButtonElement.name = ((ShipTypes)shipType).ToString();

            Label shipSelectLabel = shipButtonElement.Q<Label>("Remaining");
            if (shipSelectLabel != null)
            {
                shipSelectLabel.text = count.ToString();
                _shipText[shipType] = shipSelectLabel;
            }

            Button selectButton = shipButtonElement.Q<Button>("Select");
            if (selectButton != null)
            {
                int localShipType = shipType;
                selectButton.clicked += () => SelectShip(localShipType);
                selectButton.style.height = 50 + (25 * shipType);
                _shipButtons[shipType] = selectButton;
            }

            container.Add(shipButtonElement);
        }
    }

    private void Redo(GameObject ship)
    {
        if (!CanEditShips()) return; // prevents removal after player has locked ships

        ShipView shipView = ship.GetComponent<ShipView>();

        if (shipView != null && _shipObjects[_selectedShip].Remove(shipView))
        {
            Destroy(ship);
            UpdateButtons();
            return;
=======
   /* public void Protect()
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
    }*/

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
>>>>>>> parent of 89d099d (Added Turn Locking so that once player 1 has placed all ships they can press ready and player 2 can place their ships which after can lead to combat phase)
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

        if (HoverActions.current != null)
        {
            HoverActions.current.Clicked -= PlaceShip;
            HoverActions.current.ShipClicked -= Redo;
        }
    }

    private void UpdateButtons()
    {
        foreach (KeyValuePair<int, Button> pair in _shipButtons)
        {
            int shipType = pair.Key;
            int startingRation = _shipRations[shipType];
            int numberPlaced = _shipObjects[shipType].Count;
            int remaining = startingRation - numberPlaced;

            pair.Value.SetEnabled(remaining > 0);

            if (_shipText.TryGetValue(shipType, out Label label))
            {
                label.text = remaining.ToString();
            }
        }
    }

    private int SelectedRemaining()
    {
        // returns the number of selected ships remaining
        int startingRation = _shipRations[_selectedShip];
<<<<<<< HEAD
        int numberPlaced = _shipObjects[_selectedShip].Count;
=======
        int numberPlaced = _shipObjects[_selectedShip].Count(ship => ship);
        if (startingRation - numberPlaced == 0)
        {
            texts[(int)_selectedShip].gameObject.transform.parent.GetComponent<Button>().interactable = false;
        }
        else
        {
            texts[(int)_selectedShip].gameObject.transform.parent.GetComponent<Button>().interactable = true;
        }
>>>>>>> parent of 89d099d (Added Turn Locking so that once player 1 has placed all ships they can press ready and player 2 can place their ships which after can lead to combat phase)
        return startingRation - numberPlaced;
    }

    private bool GhostValid()
    {
        // TODO - don't allow placing on top of another ship
        return SelectedRemaining() > 0 && // check if ship size available
               _ghost != null &&
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
        return _shipObjects[shipType].Count;
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
<<<<<<< HEAD
        if (!CanEditShips()) return;
=======
        if (!isActiveBoard) return;

>>>>>>> parent of 89d099d (Added Turn Locking so that once player 1 has placed all ships they can press ready and player 2 can place their ships which after can lead to combat phase)
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
<<<<<<< HEAD

=======
    private bool free = true;
>>>>>>> parent of 89d099d (Added Turn Locking so that once player 1 has placed all ships they can press ready and player 2 can place their ships which after can lead to combat phase)
    private void Update()
    {
        if ((Mouse.current.rightButton.isPressed) & free)
        {
            RotateShip();
            free = false;
            StartCoroutine(Delay(0.5f));
        }
<<<<<<< HEAD

        if (CanEditShips() && Mouse.current.scroll.ReadValue().y != 0)
=======
        if (Mouse.current.scroll.ReadValue().y != 0)
>>>>>>> parent of 89d099d (Added Turn Locking so that once player 1 has placed all ships they can press ready and player 2 can place their ships which after can lead to combat phase)
        {
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
            if (_ghost != null)
                _ghost.GetComponentInChildren<MeshRenderer>().enabled = false;
            HandleCursorMoved();
        }
        else
        {
            mode.eventMask = ~0 & ~(LayerMask.GetMask("Ship")) & ~(LayerMask.GetMask("Blocked"));
            if (_ghost != null)
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
<<<<<<< HEAD
        if (!CanEditShips()) return; // checks if valid to place ships
=======
        if (!isActiveBoard) return;

        // places a ship where the ghost ship is

>>>>>>> parent of 89d099d (Added Turn Locking so that once player 1 has placed all ships they can press ready and player 2 can place their ships which after can lead to combat phase)
        if (!GhostValid()) return; // ghost ship required
        if (SelectedRemaining() == 0) return; // check if ship available

        int len = _ghost.GetComponent<LineShipView>().shipLength;
        _cCollider = _ghost.transform.position;

        // determine the direction for the collider check
        if (Mathf.Approximately(_ghost.transform.rotation.eulerAngles.y, 90))
        {
            _bCollider = new Vector3(1, 1, len);
            _cCollider.z += (float)((len - 1) * .5);
        }
        else if (Mathf.Approximately(_ghost.transform.rotation.eulerAngles.z, 90))
        {
            _bCollider = new Vector3(1, len, 1);
            _cCollider.y += (float)((len - 1) * .5);
        }
        else
        {
            _bCollider = new Vector3(len, 1, 1);
            _cCollider.x += (float)((len - 1) * .5);
        }

        // the overlap box needs to have dimensions half that of the original, or it will be too large
        Vector3 correct = _bCollider / 3f;
        Collider[] hit = Physics.OverlapBox(_cCollider, correct, Quaternion.identity, overlap);

        // if there are colliders of ships already there, abort
        if (hit.Length > 0)
        {
            return;
        }
<<<<<<< HEAD

        ShipView newShip = Instantiate(
            ObjectFromSelected(),
            _ghost.transform.position,
            _ghost.transform.rotation,
            transform
        );

        _shipObjects[_selectedShip].Add(newShip);

        GameObject colliderObject = newShip.gameObject;
        colliderObject.layer = LayerMask.NameToLayer("Ship");

        if (colliderObject.transform.childCount > 0)
            colliderObject.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Ship");

        colliderObject.name = colliderObject.name + " " + _index;
        _index++;

        // send placement to server
        if (_network != null)
        {
            _network.PlaceShip(_selectedShip, ShipsPlaced(_selectedShip), newShip);
        }
=======

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
>>>>>>> parent of 89d099d (Added Turn Locking so that once player 1 has placed all ships they can press ready and player 2 can place their ships which after can lead to combat phase)

        // update UI
        UpdateButtons();

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
            // Draw a cube where the OverlapBox is
            Gizmos.DrawWireCube(_cCollider, _bCollider);
    }

    public void SetActiveBoard(bool active)
    {
        isActiveBoard = active;
    }
<<<<<<< HEAD

    public bool AllShipsPlaced() // iterates through the dict and compares how many ships are allowed to how many are placed
    {
        foreach (KeyValuePair<int, int> pair in _shipRations)
        {
            int shipType = pair.Key;
            int allowed = pair.Value;
            int placed = _shipObjects[shipType].Count;

            Debug.Log($"{gameObject.name} | {(ShipTypes)shipType}: placed {placed} / required {allowed}");

            if (placed < allowed)
                return false;
        }

        return true;
    }

    public void LockPlacement() // changes bool type to true and checks if a ghost object is still present and if so destroys it
    {
        placementLocked = true;

        if (_ghost != null)
        {
            Destroy(_ghost.gameObject);
            _ghost = null;
        }
    }

    public void TryLockPlacement() // holder function that checks if all ships are placed for that player then activates lock
    {
        if (!AllShipsPlaced())
        {
            Debug.Log("You must place all ships before locking.");
            return;
        }

        LockPlacement();
    }
=======
>>>>>>> parent of 89d099d (Added Turn Locking so that once player 1 has placed all ships they can press ready and player 2 can place their ships which after can lead to combat phase)
}