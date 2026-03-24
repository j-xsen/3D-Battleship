using UnityEngine;

public class NewScene : MonoBehaviour
{
    private GameObject[] list;
    private bool switching = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject SpaceBuilder = GameObject.FindGameObjectWithTag("Builder");
        SpaceBuilder.GetComponent<ShipManager>().enabled = false;
        list = GameObject.FindGameObjectsWithTag("Ship");
    }
    public void ReadyAttack()
    {
        if (switching)
        {
            foreach (GameObject obj in list)
            {
                obj.SetActive(false);
            }
            switching = false;
        }
        else
        {
            foreach (GameObject obj in list)
            {
                obj.SetActive(true);
            }
            switching = true;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
