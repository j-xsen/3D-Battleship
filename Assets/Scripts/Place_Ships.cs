using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static logic;

public class Place_Ships : MonoBehaviour
{
    [Header("Buttons")]
    public Button one;
    public Button two;
    public Button three;   
    public Button four;
    public Button five;

   // private GameObject current;
    private struct place
    {
        private int count;
        private int max;
    }
    //private fleet create_fleet()
   // {
  //      logic.player 
  //  }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
  //      one.onClick.AddListener(() => current = get);

    }
    void OnMouseEnter()
    {
        Debug.Log("Entered: " + gameObject.name);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
