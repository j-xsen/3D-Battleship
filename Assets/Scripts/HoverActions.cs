using System;
using UnityEngine;


public class HoverActions : MonoBehaviour
{
    public static HoverActions current;

    private void Awake()
    {
        current = this;
    }

    public event Action<int, int, int> UpdatePosition;

    public void OnUpdatePosition(GameObject rec)
    {
        UpdatePosition?.Invoke((int)rec.gameObject.transform.position.x, (int)rec.gameObject.transform.position.y,
            (int)rec.gameObject.transform.position.z);
    }

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