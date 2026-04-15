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
            // TODO - readd this, gives error rn
            //might be better to create an event trigger but this is functional
            // if (button.interactable == false & (one.interactable == false & two.interactable == false & three.interactable == false & four.interactable == false))
            // {
            //     ReadyTrigger();
            // }
            /*else if (button.interactable != false)
        {
            TakeBack();
        }*/
            if (!button.interactable & _shipManager.AllShipsPlaced())
            {
                EnableReady();
            }
        }

        public void Ready()
        {
            // SceneManager.LoadScene("GamePlay");
            if (_network)
            {
                _ = _network.SendReady(true);
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
