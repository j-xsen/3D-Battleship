using UnityEngine;
using UnityEngine.EventSystems;


public class HoverMouseControls : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
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
        Debug.Log("Mouse entered: " + gameObject.transform.position);
        if (gameObject.layer != 3)
        {
            HoverActions.current.OnUpdatePosition(this.gameObject);
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
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (gameObject.layer == 3)
        {
            HoverActions.current.OnShipClicked(this.gameObject);
            //        Debug.Log("SHIP");
        }
        if (HoverActions.current.currentMode == HoverActions.InputMode.Placement)//ship placement click
        {
            HoverActions.current.OnClicked();
        }
        else if (HoverActions.current.currentMode == HoverActions.InputMode.Combat) //combat shooting click 
        {
            HoverActions.current.OnCombatClicked();
        }
    }
}
