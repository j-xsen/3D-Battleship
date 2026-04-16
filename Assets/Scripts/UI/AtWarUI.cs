using System;
using Network;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class AtWarUI: MonoBehaviour
    {
        [SerializeField] private UIDocument uiDoc;
        [SerializeField] private VisualTreeAsset atWarUI;

        private SessionManager _network;
        private Label _currentLabel;

        private void Start()
        {
            _network = FindFirstObjectByType<SessionManager>();
            if (!_network)
            {
                Debug.LogError("SessionManager not found by AtWarUI!");
                return;
            }

            _network.OnMyTurn += OnMyTurn;
            _network.OnTheirTurn += OnTheirTurn;
            _network.OnStateChanged += OnStateChange;
        }

        private void OnStateChange(string state)
        {
            if (state != "AtWar") return;
            
            // loading into war, open ui
            atWarUI.CloneTree(uiDoc.rootVisualElement);
            _currentLabel = uiDoc.rootVisualElement.Q<Label>("CurrentPlayer");
        }

        private void OnDestroy()
        {
            if (!_network) return;
            _network.OnMyTurn -= OnMyTurn;
            _network.OnTheirTurn -= OnTheirTurn;
            _network.OnStateChanged -= OnStateChange;
        }

        private void OnMyTurn()
        {
            if (_currentLabel == null) return;
            _currentLabel.text = "YOU!";
        }

        private void OnTheirTurn()
        {
            if (_currentLabel == null) return;
            _currentLabel.text = "THEM";
        }
    }
}