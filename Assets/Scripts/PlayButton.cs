using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButton : MonoBehaviour
{
    public string sceneName;

    public void Press()
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }
}
