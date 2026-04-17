using Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class EndScreen : MonoBehaviour
    {
        public Button mainMenuButton;

        private SessionManager _network;

        private void Awake()
        {
            _network = GameObject.FindWithTag("NetworkManager")?.GetComponent<SessionManager>();
            if (!_network) Debug.LogError("EndScreen: Unable to find NetworkManager!");
        }

        private void Start()
        {
            mainMenuButton.onClick.AddListener(OnMainMenuPressed);
        }

        private void OnMainMenuPressed()
        {
            if (_network)
            {
                _ = _network.LeaveSessionAsync();
            }

            SceneManager.LoadScene("Main Menu_network");
        }
    }
}
