using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class PlayButton : MonoBehaviour
    {
        public string sceneName;

        public void Press()
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }
}
