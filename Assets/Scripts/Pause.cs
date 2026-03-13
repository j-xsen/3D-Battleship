using UnityEngine;
using UnityEngine.InputSystem;
public class Pause : MonoBehaviour
{
    public GameObject pause;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pause.SetActive(false);
    }

    // Update is called once per frame
    public void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("pressed escape");
            pause.SetActive(true);
        }
    }
}
