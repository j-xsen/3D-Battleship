using System;
using System.Collections.Generic;
using Network;
using Ships.Types;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class ShipPlacementUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDoc; // uidoc where container is created
        [SerializeField] private VisualTreeAsset shipPlaceButton; // prefab place button
        
        private ShipTypeManager _typeManager;
        private Dictionary<int, Button> _buttons;
        private Dictionary<int, Label> _labels;

        private ShipManager _sm;

        private void Awake()
        {
            // init empty dictionaries
            _buttons = new Dictionary<int, Button>();
            _labels = new Dictionary<int, Label>();
            
            // find type manager
            _typeManager = GetComponentInParent<ShipTypeManager>();
            if(!_typeManager) Debug.LogError("No ShipTypeManager with ShipPlacementUI");
            
            // button container
            VisualElement container = new()
            {
                pickingMode = PickingMode.Ignore, // ignore mouse clicks
                style =
                {
                    position = Position.Absolute,
                    top = 0,
                    bottom = 0,
                    right = 0,
                    justifyContent = Justify.Center,
                    alignItems = Align.FlexEnd,
                    paddingRight = 32
                }
            };
            uiDoc.rootVisualElement.Add(container);
            
            // define ShipManager
            _sm = GetComponentInParent<ShipManager>();
            // if (_sm)
            // {
            //     _sm.GetNetwork().OnStateChanged += OnStateChanged;                
            // }
            // else
            // {
            //     Debug.LogError("No ShipManager with ShipPlacementUI");
            // }
            
            // create ui for each ShipType
            foreach ((int shipType, int count) in _typeManager.Rations())
            {
                int capturedType = shipType;
                // create button
                VisualElement shipButtonElement = shipPlaceButton.Instantiate();
                
                // text
                Label shipSelectLabel = shipButtonElement.Q<Label>("Remaining");
                shipSelectLabel.text = count.ToString();
                _labels[shipType] = shipSelectLabel;
                
                Button selectButton = shipButtonElement.Q<Button>("Select"); // actual button
                selectButton.clicked += () => _sm.SelectShip(capturedType);
                // set height
                selectButton.style.height = 50 + (25 * shipType);
                _buttons[shipType] = selectButton;

                // shipButtonElement.pickingMode = PickingMode.Ignore;
                shipButtonElement.name = shipType.ToString();
                container.Add(shipButtonElement);
            }
        }

        public void Start()
        {
            SessionManager network = _sm?.GetNetwork();
            if (network)
            {
                network.OnStateChanged += OnStateChanged;
            }
            else
            {
                Debug.LogError("No sessionmanager found from shipmanager");
            }
        }
        
        public void UpdateButtons()
        {
            foreach ((int shipType, Button _) in _buttons)
            {
                int remaining = _sm.Remaining(shipType);
                _buttons[shipType].SetEnabled(remaining > 0);
                _labels[shipType].text = remaining.ToString();
            }
        }

        private void OnStateChanged(string state)
        {
            if (state == "AtWar")
            {
                uiDoc.rootVisualElement.Clear();
            }
        }

        private void OnDestroy()
        {
            if (_sm)
            {
                _sm.GetNetwork().OnStateChanged -= OnStateChanged;
            }
        }
    }
}