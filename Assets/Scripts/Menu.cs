using UnityEngine;

public class Menu : MonoBehaviour
{
    public GameObject start;

    public void Quit()
    {
        Application.Quit();
    }

    public void Begin()
    {
        start.SetActive(false);
    }
}
