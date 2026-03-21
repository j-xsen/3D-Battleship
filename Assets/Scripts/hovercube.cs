using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class hovercube : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log("Mouse entered: " + gameObject.transform.position);
        hover.current.Recpos(this.gameObject);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log("Clicked");
            hover.current.Recclick();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
