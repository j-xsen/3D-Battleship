using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SpaceBuilder : MonoBehaviour
{
    public event Action OnCursorMoved;
    
    [SerializeField] private GameObject spacePrefab; // Prefab of empty space cube
    [SerializeField] private int sizeWidth; // Size of map
    [SerializeField] private int sizeHeight; // Size of map
    [SerializeField] private int sizeDepth; // Size of map
    [SerializeField] private Material defaultMat; // Default material for prefab
    [SerializeField] private Material selectMat; // Material upon cursor selected
    [SerializeField] private bool showCursor; // enable/disable showing the cursor

    public Vector3 boardOffset; //used to put grids apart and for camera offsetting 

    [SerializeField] private bool isActiveBoard = true;
    
    private Renderer[,,] _renderers; // saves on expensive GetComponent calls
    private int _selectedX;
    private int _selectedY;
    private int _selectedZ;

    private enum CellVisualState//for combat cell hits 
    {
        Normal,
        Miss,
        Hit
    }

    private CellVisualState[,,] _cellVisualStates;

    [SerializeField] private Material missMat;
    [SerializeField] private Material hitMat;


    //grid data for combat 
    //ship locations 
    private ShipRecord[,,] _myShips;
    private ShipRecord[,,] _theirShips;
    //cell shot grid 
    private bool[,,] _myAttacked;
    private bool[,,] _theirAttacked;

    private InputAction _selectUp;
    private InputAction _selectDown;
    private InputAction _selectRight;
    private InputAction _selectLeft;
    private InputAction _selectForward;
    private InputAction _selectBack;
    private InputAction _rotateMapRight;
    private InputAction _rotateMapLeft;
    private Action<InputAction.CallbackContext> _upCtx;
    private Action<InputAction.CallbackContext> _downCtx;
    private Action<InputAction.CallbackContext> _leftCtx;
    private Action<InputAction.CallbackContext> _rightCtx;
    private Action<InputAction.CallbackContext> _forwardCtx;
    private Action<InputAction.CallbackContext> _backCtx;
    private int row;
    
    private Vector3 _origin;
  
    public void SetActiveBoard(bool active)
    {
        isActiveBoard = active; 
    }

    public Vector3 GetCursorLocation()
    {
        return new Vector3(_selectedX, _selectedY, _selectedZ);
    }

    public Vector3 GetSize()
    {
        return new Vector3(sizeWidth, sizeHeight, sizeDepth);
    }
    
    private void Start()
    {
        // DontDestroyOnLoad(this);
        if (HoverActions.current)
        {
            HoverActions.current.UpdatePosition += UpdateSelectedMos;
        }
        else
        {
            Debug.LogWarning("hover.current is null. Make sure the hover script has been initialized first.");
        }
        // input listeners
        _selectUp = InputSystem.actions.FindAction("SelectUp");
        _selectDown = InputSystem.actions.FindAction("SelectDown");
        _selectRight = InputSystem.actions.FindAction("SelectRight");
        _selectLeft = InputSystem.actions.FindAction("SelectLeft");
        _selectForward = InputSystem.actions.FindAction("SelectForward");
        _selectBack = InputSystem.actions.FindAction("SelectBack");
        _rotateMapRight = InputSystem.actions.FindAction("SpaceField/MapRotateRight");
        _rotateMapLeft = InputSystem.actions.FindAction("SpaceField/MapRotateLeft");
        //
        

        _upCtx = _ => UpdateSelected(0, 1, 0);
        _downCtx = _ => UpdateSelected(0, -1, 0);
        _rightCtx = _ => UpdateSelected(1, 0, 0);
        _leftCtx = _ => UpdateSelected(-1, 0, 0);
        _forwardCtx = _ => UpdateSelected(0, 0, 1);
        _backCtx = _ => UpdateSelected(0, 0, -1);

        _selectUp.performed += _upCtx;
        _selectDown.performed += _downCtx;
        _selectRight.performed += _rightCtx;
        _selectLeft.performed += _leftCtx;
        _selectForward.performed += _forwardCtx;
        _selectBack.performed += _backCtx;
        
        // make an array of renderers so we don't keep calling GetComponent
        _renderers = new Renderer[sizeWidth, sizeHeight, sizeDepth];
        // attacking data 
        _myShips = new ShipRecord[sizeWidth, sizeHeight, sizeDepth];
        _theirShips = new ShipRecord[sizeWidth, sizeHeight, sizeDepth];
        _myAttacked = new bool[sizeWidth, sizeHeight, sizeDepth];
        _theirAttacked = new bool[sizeWidth, sizeHeight, sizeDepth];

        _cellVisualStates = new CellVisualState[sizeWidth, sizeHeight, sizeDepth];

        GenerateField();
        // init to 0,0,0
        UpdateSelected(0,0,0);
    }

    private void OnDestroy()
    {
        if (_selectUp != null) _selectUp.performed -= _upCtx;
        if (_selectDown != null) _selectDown.performed -= _downCtx;
        if (_selectRight != null) _selectRight.performed -= _rightCtx;
        if (_selectLeft != null) _selectLeft.performed -= _leftCtx;
        if (_selectForward != null) _selectForward.performed -= _forwardCtx;
        if (_selectBack != null) _selectBack.performed -= _backCtx;
        HoverActions.current.UpdatePosition -= UpdateSelected;
    }

    private void Update()
    {
        if (!isActiveBoard) return;//checks if the current board i.e player 1 is currently active, if not, it returns 

        if (Mouse.current.scroll.ReadValue().y < 0)
        {
            MoveRow(true);
        }
        else if(Mouse.current.scroll.ReadValue().y > 0)
        {
            MoveRow(false);
        }
        // these are in update so you can hold them
        if (_rotateMapLeft.IsPressed()) transform.RotateAround(_origin, Vector3.up, 2f);
        if (_rotateMapRight.IsPressed()) transform.RotateAround(_origin, Vector3.up, -2f);
    }
    private void MoveRow(bool further)
    {
        if ((further) & (row < (sizeDepth-1)))
        {
            for (int i = 0; i < sizeWidth; i++)
            {
                for (int j = 0; j < sizeHeight; j++)
                {
                    //_renderers[i, j, row].GetComponent<BoxCollider>().enabled = false;
                    _renderers[i,j,row].gameObject.layer = LayerMask.NameToLayer("Blocked");
                }
            }
            if (row != (sizeDepth - 1))
            {
                row += 1;
            }
        }
        else if ((!further) & (row >= 0))
        {

            for (int i = 0; i < sizeWidth; i++)
            {
                for (int j = 0; j < sizeHeight; j++)
                {
                    //_renderers[i, j, row].GetComponent<BoxCollider>().enabled = true;
                    _renderers[i, j, row].gameObject.layer = LayerMask.NameToLayer("Default");
                }
            }
            if (row != 0)
            {
                row -= 1;
            }
        }
    }

    private void UpdateSelectedMos(int x, int y, int z)
    {
        if (!isActiveBoard) return;//checks if the current board i.e player 1 is currently active, if not, it returns 

        // validate
        // TODO: notify of error?
        int newX = x;
        int newY = y;
        int newZ = z;

        if (0 > newX || 0 > newY || 0 > newZ) return;
        if (sizeWidth <= newX || sizeHeight <= newY || sizeDepth <= newZ) return;

        if (showCursor) ApplyCellVisual(_selectedX, _selectedY, _selectedZ, false);

        _selectedX = newX;
        _selectedY = newY;
        _selectedZ = newZ;

        if (showCursor) ApplyCellVisual(_selectedX, _selectedY, _selectedZ, true);
        // this alerts all the listeners
        OnCursorMoved?.Invoke();
    }

    private void UpdateSelected(int x, int y, int z)
    {
        if (!isActiveBoard) return;

        int newX = x + _selectedX;
        int newY = y + _selectedY;
        int newZ = z + _selectedZ;

        if (0 > newX || 0 > newY || 0 > newZ) return;
        if (sizeWidth <= newX || sizeHeight <= newY || sizeDepth <= newZ) return;

        if (showCursor) ApplyCellVisual(_selectedX, _selectedY, _selectedZ, false);

        _selectedX = newX;
        _selectedY = newY;
        _selectedZ = newZ;

        if (showCursor) ApplyCellVisual(_selectedX, _selectedY, _selectedZ, true);

        OnCursorMoved?.Invoke();
    }


    private void GenerateField()
    {
        // create field
        for (int x = 0; x < sizeWidth; x++)
        {
            for (int y = 0; y < sizeHeight; y++)
            {
                for (int z = 0; z < sizeDepth; z++)
                {
                    GameObject newSpace = Instantiate(spacePrefab, new Vector3(x,y,z) + boardOffset, Quaternion.identity, transform);
                    //newSpace.transform.parent = null;
                    //DontDestroyOnLoad(newSpace);
                    newSpace.name = string.Concat(x, y, z);
                    _renderers[x, y, z] = newSpace.GetComponent<Renderer>();
                }
            }
        }
        
        float posX = (sizeWidth - 1) / 2f;
        float posY = (sizeHeight - 1) / 2f;
        float posZ = (sizeDepth - 1) / 2f;
        _origin = new Vector3(posX, posY, posZ) + boardOffset; // origin for rotation
    }

    public Vector3 GetOrigin()
    {
        return _origin;
    }


    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Multiplayer methods i.e data 


    


    

    public void BoardData()
    {
        //list of ships on the grid (positions)

        //attacked cells 



    }

    private void Initialize_Board_Data()
    {
        //creates arrays and clears any ship list 
    }

    public void Register_ship_data(int shipType, int length, List<Vector3Int> occupiedCells)
    {
        //when ShipManager places a ship, call board data and mark all occupied cells 


        ShipRecord record = new ShipRecord();
        record.shipType = shipType;
        record.length = length;
        record.OccupiedCells = new List<Vector3Int>(occupiedCells);
        record.HitCells = new List<Vector3Int>();

        foreach (Vector3Int cell in occupiedCells)
        {
            // checks if cell is in bounds or is already occupied 
            if (cell.x < 0 || cell.x >= sizeWidth ||
                cell.y < 0 || cell.y >= sizeHeight ||
                cell.z < 0 || cell.z >= sizeDepth)
            {
                Debug.LogError($"Ship cell out of bounds: {cell}");
                return;
            }

            if (_myShips[cell.x, cell.y, cell.z] != null)
            {
                Debug.LogError($"Cell already occupied: {cell}");
                return;
            }
            /////
            ///

            _myShips[cell.x, cell.y, cell.z] = record;// ship assignment 

            Debug.Log($"Stored ship in grid cell {cell}");

        }
        Debug.Log($"Registered ship type {shipType} with {occupiedCells.Count} cells");
    }

    public ShipRecord GetShipAtCell(Vector3Int cell)//for multiplayer 
    {
        return _myShips[cell.x, cell.y, cell.z];
    }


    public void DebugCell(Vector3Int cell)
    {
    Debug.Log(_myShips[cell.x, cell.y, cell.z] == null
        ? $"Cell {cell} is empty"
        : $"Cell {cell} contains ship type {_myShips[cell.x, cell.y, cell.z].shipType}");
    }



    private bool IsInBounds(Vector3Int cell)
    {
        return cell.x >= 0 && cell.x < sizeWidth &&
               cell.y >= 0 && cell.y < sizeHeight &&
               cell.z >= 0 && cell.z < sizeDepth;
    }
    

    public void SetCursorVisible(bool visible)
    {
        if (showCursor && !visible)
        {
            ApplyCellVisual(_selectedX, _selectedY, _selectedZ, false);
        }

        showCursor = visible;

        if (showCursor)
        {
            ApplyCellVisual(_selectedX, _selectedY, _selectedZ, true);
        }
    }

    public GameObject GetCellObject(Vector3Int cell)
    {
        // bounds check
        if (cell.x < 0 || cell.x >= sizeWidth ||
            cell.y < 0 || cell.y >= sizeHeight ||
            cell.z < 0 || cell.z >= sizeDepth)
        {
            Debug.LogError($"GetCellObject out of bounds: {cell}");
            return null;
        }

        Renderer rend = _renderers[cell.x, cell.y, cell.z];
        if (rend == null)
        {
            Debug.LogError($"No renderer found for cell {cell}");
            return null;
        }

        return rend.gameObject;
    }

    public void MarkMiss(Vector3Int cell)
    {
        if (cell.x < 0 || cell.x >= sizeWidth ||
            cell.y < 0 || cell.y >= sizeHeight ||
            cell.z < 0 || cell.z >= sizeDepth)
        {
            return;
        }

        _cellVisualStates[cell.x, cell.y, cell.z] = CellVisualState.Miss;
        ApplyCellVisual(cell.x, cell.y, cell.z, false);

        Debug.Log($"VISUAL: Miss at {cell}");
    }

    public void MarkHit(Vector3Int cell)
    {
        if (cell.x < 0 || cell.x >= sizeWidth ||
            cell.y < 0 || cell.y >= sizeHeight ||
            cell.z < 0 || cell.z >= sizeDepth)
        {
            return;
        }

        _cellVisualStates[cell.x, cell.y, cell.z] = CellVisualState.Hit;
        ApplyCellVisual(cell.x, cell.y, cell.z, false);

        Debug.Log($"VISUAL: Hit at {cell}");
    }

    public void ClearBoard()
    {
        for (int x = 0; x < sizeWidth; x++)
        for (int y = 0; y < sizeHeight; y++)
        for (int z = 0; z < sizeDepth; z++)
            _cellVisualStates[x, y, z] = CellVisualState.Normal;

        foreach (Renderer renderer in _renderers)
            renderer.material = defaultMat;
    }

    public void VisualizeCell(Vector3Int cell, AttackResult status)
    {
        switch (status)
        {
            case AttackResult.Hit:
            case AttackResult.Destroyed:
                MarkHit(cell);
                break;
            case AttackResult.Miss:
                MarkMiss(cell);
                break;
        }
    }

    private void ApplyCellVisual(int x, int y, int z, bool selected)
    {
        Renderer rend = _renderers[x, y, z];
        if (rend == null) return;

        if (selected && showCursor)
        {
            rend.material = selectMat;
            return;
        }

        switch (_cellVisualStates[x, y, z])
        {
            case CellVisualState.Miss:
                rend.material = missMat;
                break;
            case CellVisualState.Hit:
                rend.material = hitMat;
                break;
            default:
                rend.material = defaultMat;
                break;
        }
    }



}
