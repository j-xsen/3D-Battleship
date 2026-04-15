using System;
using Network;
using UnityEngine;

namespace UI
{
    public class ReadyUI : MonoBehaviour
    {
        private SessionManager _network;

        private void Start()
        {
            _network = FindFirstObjectByType<SessionManager>();
            if (_network)
            {
                _network.OnStateChanged += OnStateChanged;
            }
            else
            {
                Debug.LogError("Unable to find SessionManager from ReadyUI");
            }
        }

        private void OnStateChanged(string state)
        {
            if(state=="AtWar") gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_network) _network.OnStateChanged -= OnStateChanged;
        }
    }
}