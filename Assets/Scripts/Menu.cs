using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
