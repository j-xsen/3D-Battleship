using System;
using Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class ReadyButton : MonoBehaviour
    {
        private Button button;
        // public event Action ready;
        //  public static ReadyButton current;
        public bool ready = false;
        [SerializeField] private Button one;
        [SerializeField] private Button two;
        [SerializeField] private Button three;
        [SerializeField] private Button four;

        private SessionManager _network;
        private ShipManager _shipManager;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            button = GetComponent<Button>();
            button.interactable = false;
            // current = this;
        }

        private void Awake()
        {
            // find network
            _network = GameObject.FindWithTag("NetworkManager")?.GetComponent<SessionManager>();
            if (!_network) Debug.LogError("Unable to find NetworkManager!");
            
            // find ship manager
            _shipManager = GameObject.Find("Player_1_Grid")?.GetComponent<ShipManager>();
            if(!_shipManager) Debug.LogError("No ShipManager with ShipPlacementUI");
        }

        // Update is called once per frame
        private void Update()
        {
            if (!button.interactable & _shipManager.AllShipsPlaced())
            {
                EnableReady();
            }
        }

        public void Ready()
        {
            if (_network)
            {
                _ = _network.SendReadyAsync(true);
            }
            else
            {
                Debug.LogError("No Network to send Ready to!");
            }
        }

        public void EnableReady()
        {
            button.interactable = true;
        }

        public void DisableReady()
        {
            button.interactable = false;
        }

    }
}
