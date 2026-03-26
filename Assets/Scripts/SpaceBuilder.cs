using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SpaceBuilder : MonoBehaviour
{
    public event Action OnCursorMoved;
    
    [SerializeField] private GameObject spacePrefab; // Prefab of empty space cube
    [SerializeField] private int size; // Size of map
    [SerializeField] private Material defaultMat; // Default material for prefab
    [SerializeField] private Material selectMat; // Material upon cursor selected
    [SerializeField] private bool showCursor; // enable/disable showing the cursor

    public Vector3 boardOffset;//used to put grids apart and for camera offsetting 

    [SerializeField] private bool isActiveBoard = true;



    private Renderer[,,] _renderers; // saves on expensive GetComponent calls
    private int _selectedX;
    private int _selectedY;
    private int _selectedZ;

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
    private int row = 0;


    public void SetActiveBoard(bool active)
    {
        isActiveBoard = active; 
    }


    private Vector3 _origin;
  //  private hover hovers;

    public Vector3 GetCursorLocation()
    {
        return new Vector3(_selectedX, _selectedY, _selectedZ);
    }

    public int GetSize()
    {
        return size;
    }
    
    private void Start()
    {
        DontDestroyOnLoad(this);
        //    hovers = GetComponent<hover>();
        if (hover.current != null)
        {
            hover.current.Pos += UpdateSelectedMos;
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
        _renderers = new Renderer[size, size, size];
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
        hover.current.Pos -= UpdateSelected;
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
        if ((further) & (row < (size -1)))
        {
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    //_renderers[i, j, row].GetComponent<BoxCollider>().enabled = false;
                    _renderers[i,j,row].gameObject.layer = LayerMask.NameToLayer("Blocked");
                }
            }
            if (row != (size - 1))
            {
                row = row + 1;
 //               Debug.Log(row);
            }
        }
        else if ((!further) & (row >= 0))
        {

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    //_renderers[i, j, row].GetComponent<BoxCollider>().enabled = true;
                    _renderers[i, j, row].gameObject.layer = LayerMask.NameToLayer("Default");
                }
            }
            if (row != 0)
            {
                row = row - 1;
  //              Debug.Log(row);
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
        if (size <= newX || size <= newY || size <= newZ) return;

        if (showCursor) _renderers[_selectedX, _selectedY, _selectedZ].material = defaultMat;

        _selectedX = newX;
        _selectedY = newY;
        _selectedZ = newZ;

        if (showCursor) _renderers[_selectedX, _selectedY, _selectedZ].material = selectMat;
        // this alerts all the listeners
        OnCursorMoved?.Invoke();
    }

    private void UpdateSelected(int x, int y, int z)
    {
        if (!isActiveBoard) return;//checks if the current board i.e player 1 is currently active, if not, it returns 

        // validate
        // TODO: notify of error?
        int newX = x + _selectedX;
        int newY = y + _selectedY;
        int newZ = z + _selectedZ;

        if (0 > newX || 0 > newY || 0 > newZ) return;
        if (size <= newX || size <= newY || size <= newZ) return;
        
        if (showCursor) _renderers[_selectedX, _selectedY, _selectedZ].material = defaultMat;

        _selectedX = newX;
        _selectedY = newY;
        _selectedZ = newZ;

        if (showCursor) _renderers[_selectedX, _selectedY, _selectedZ].material = selectMat;

        // this alerts all the listeners
        OnCursorMoved?.Invoke();
    }


    private void GenerateField()
    {
        // create field
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    GameObject newSpace = Instantiate(spacePrefab, new Vector3(x,y,z) + boardOffset, Quaternion.identity, this.transform);
                    //newSpace.transform.parent = null;
                    //DontDestroyOnLoad(newSpace);
                    newSpace.name = string.Concat(x, y, z);
                    _renderers[x, y, z] = newSpace.GetComponent<Renderer>();
                }
            }
        }
        
        float pos = (size - 1) / 2f;
        _origin = new Vector3(pos, pos, pos) + boardOffset; // origin for rotation
        
        /* move camera to center field
        Camera mainCamera = Camera.main;
        if (!mainCamera)
        {
            Debug.LogError("No main camera");
            return;
        }

        mainCamera.transform.position = new Vector3(pos, pos, -size) + boardOffset;*/
    }
}
