using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class hover : MonoBehaviour
{

    public static hover current;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        current = this;
      //  Debug.Log("Hover Awake called: current set to " + current);
    }
    public event Action<int, int, int> Pos;
    public void Recpos(GameObject rec)
    {
        if (Pos != null)
        {
            Pos?.Invoke((int)rec.gameObject.transform.position.x, (int)rec.gameObject.transform.position.y, (int)rec.gameObject.transform.position.z);
        }
    }
    public event Action Clicked;
    public void Recclick()
    {
        if (Clicked != null)
        {
            Clicked?.Invoke();
        }
    }

    public event Action<GameObject> Shipclick;
    public void ReShip(GameObject gameObject)
    {
        if (Shipclick != null)
        {
            Shipclick?.Invoke(gameObject);
        }
    }
    // Update is called once per frame
    void Update()
    {

    }
}
