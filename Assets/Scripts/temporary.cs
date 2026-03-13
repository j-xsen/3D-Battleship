using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class temporary : MonoBehaviour
{
    public Button demo;
    public Button pausedemo;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        demo.onClick.AddListener(() => SceneManager.LoadScene("grid_screen"));

        pausedemo.onClick.AddListener(() => SceneManager.LoadScene("Game"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
