using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static UnityEngine.UI.Image;

public class selection : MonoBehaviour
{
    private Vector2 pos;
    private Vector2 change;
    private Vector2 speed;
    private float leftcap = 40;
    private float rightcap = 320;
    private Vector3 origin;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        origin = new Vector3(4, (float)3.5, (float)3.5);
        gameObject.transform.position = new Vector3(4, (float)3.5, -9);
    }


    // Update is called once per frame
    void Update()
    {
        //reset mouse starting position
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            pos = Mouse.current.position.ReadValue();
            //Debug.Log("pos: " + pos);
        }
        //check for mouse movement 
        else if (Mouse.current.leftButton.isPressed)
        {
            change = Mouse.current.position.ReadValue();
            //Debug.Log("cha: " + pos);
            speed = new Vector2((pos.x - change.x), (pos.y - change.y));
            
            if (((transform.rotation.eulerAngles.y > 180) && (transform.rotation.eulerAngles.y <= rightcap) && (speed.x >= 0)) || ((transform.rotation.eulerAngles.y < 180) && (transform.rotation.eulerAngles.y >= leftcap) && (speed.x <= 0)))
            {
                speed.x = 0;
            }
            else
            {
                transform.RotateAround(origin, Vector3.up, -(speed.x / 500));
            }
            if (((transform.rotation.eulerAngles.x > 180) && (transform.rotation.eulerAngles.x <= rightcap) && (speed.y <= 0)) || ((transform.rotation.eulerAngles.x < 180) && (transform.rotation.eulerAngles.x >= leftcap) && (speed.y >= 0)))
            {
                speed.y = 0;
            }
            else
            {
                transform.RotateAround(origin, Vector3.right, (speed.y / 500));
            }

            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0);
            //Debug.Log("max: " + transform.rotation.eulerAngles + "y: " + transform.rotation.eulerAngles.y);
        }
    }
}