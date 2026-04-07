using System;
using UnityEngine;


public class HoverActions : MonoBehaviour
{
    public static HoverActions current;


    public enum InputMode
    {
        Placement,
        Combat
    }

    public InputMode currentMode = InputMode.Placement;// defaults input mode to placement for ship placing 

    private void Awake()
    {
        current = this;
    }

    public event Action<int, int, int> UpdatePosition;

    public void OnUpdatePosition(GameObject rec)
    {
        //UpdatePosition?.Invoke((int)rec.gameObject.transform.position.x, (int)rec.gameObject.transform.position.y,
        //(int)rec.gameObject.transform.position.z);

        
        
        Transform board = rec.transform.parent;// to allow for cursor to be sued for both grids 
        Vector3 localPos = board.InverseTransformPoint(rec.transform.position);

        UpdatePosition?.Invoke(
            Mathf.RoundToInt(localPos.x),
            Mathf.RoundToInt(localPos.y),
            Mathf.RoundToInt(localPos.z)
        );
        
    }

    public void OnCombatClicked() // a click event for combat so as not to mess with the click for shipManager 
    {
        CombatClicked?.Invoke();
    }

    public event Action CombatClicked;

    public event Action Clicked;

    public void OnClicked()
    {
        Clicked?.Invoke();
    }

    public event Action<GameObject> ShipClicked;

    public void OnShipClicked(GameObject go)
    {
        ShipClicked?.Invoke(go);
    }
}