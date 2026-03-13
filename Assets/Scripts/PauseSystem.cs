using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseSystem : MonoBehaviour
{
    public Button continueButton;
    public Button menuButton;
    public Button quitButton;

    void Start()
    {
        continueButton.onClick.AddListener(() => gameObject.SetActive(false));
        menuButton.onClick.AddListener(() => SceneManager.LoadScene("Main Menu"));
        quitButton.onClick.AddListener(() => Application.Quit());
    }
}

