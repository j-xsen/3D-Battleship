using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class hovercube : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    public void OnPointerExit(PointerEventData eventData)
    {
        if (gameObject.layer == 3)
        {
            gameObject.transform.GetChild(0).GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        //  Debug.Log("Mouse entered: " + gameObject.transform.position);
        if (gameObject.layer != 3)
        {
            hover.current.Recpos(this.gameObject);
        }
        else
        {
            //change color
            gameObject.transform.GetChild(0).GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            gameObject.transform.GetChild(0).GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.blue * 2f);
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
      //      Debug.Log("layer:" + gameObject.layer);
            if (gameObject.layer == 3)
            {
                hover.current.ReShip(this.gameObject);
        //        Debug.Log("SHIP");
            }
            //Debug.Log("Clicked");
            else
            {
                hover.current.Recclick();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
