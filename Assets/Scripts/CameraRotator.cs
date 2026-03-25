using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotator : MonoBehaviour
{
    [SerializeField] private SpaceBuilder spaceBuilder;
    private Vector2 _pos;
    private Vector2 _change;
    private Vector2 _speed;
    private const float LeftCap = 40;
    private const float RightCap = 320;
    
    void Update()
    {
        //reset mouse starting position
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _pos = Mouse.current.position.ReadValue();
            //Debug.Log("pos: " + pos);
        }
        //check for mouse movement 
        else if (Mouse.current.leftButton.isPressed)
        {
            _change = Mouse.current.position.ReadValue();
            //Debug.Log("cha: " + pos);
            _speed = new Vector2((_pos.x - _change.x), (_pos.y - _change.y));

            if ((transform.rotation.eulerAngles.y is > 180 and <= RightCap &&
                 (_speed.x >= 0)) || (transform.rotation.eulerAngles.y is < 180 and >= LeftCap && (_speed.x <= 0)))
            {
                _speed.x = 0;
            }
            else
            {
                transform.RotateAround(spaceBuilder.GetOrigin(), transform.up, -(_speed.x / 500));
            }

            if ((transform.rotation.eulerAngles.x is > 180 and <= RightCap &&
                 (_speed.y <= 0)) || (transform.rotation.eulerAngles.x is < 180 and >= LeftCap && (_speed.y >= 0)))
            {
                _speed.y = 0;
            }
            else
            {
                transform.RotateAround(spaceBuilder.GetOrigin(), transform.right, (_speed.y / 500));
            }

            transform.rotation =
                Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0);
            //Debug.Log("max: " + transform.rotation.eulerAngles + "y: " + transform.rotation.eulerAngles.y);
        }
    }
}