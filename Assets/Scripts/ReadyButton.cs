using JetBrains.Annotations;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ReadyButton : MonoBehaviour
{
    private Button button;
    // public event Action ready;
    //  public static ReadyButton current;
    public bool ready = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button = GetComponent<Button>();
        button.interactable = false;
       // current = this;
    }

    // Update is called once per frame
    
    public void ReadyTrigger()
    {
        button.interactable = true;
    }

    public void TakeBack()
    {
        button.interactable = false;
    }

}
