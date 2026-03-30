using System.Collections.Generic;
using Ships.Types;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class ShipPlacementUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDoc;
        [SerializeField] private VisualTreeAsset shipPlaceButton;
        
        private ShipTypeManager _typeManager;
        private Dictionary<int, Button> _buttons;
        private Dictionary<int, Label> _labels;

        private ShipManager _sm;

        private void Awake()
        {
            _buttons = new Dictionary<int, Button>();
            _labels = new Dictionary<int, Label>();
            _typeManager = GetComponentInParent<ShipTypeManager>();
            if(!_typeManager) Debug.LogError("No ShipTypeManager with ShipPlacementUI");
            // button container
            VisualElement container = new()
            {
                pickingMode = PickingMode.Ignore,
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
            if(!_sm) Debug.LogError("No ShipManager with ShipPlacementUI");
            
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
        
        public void UpdateButtons()
        {
            foreach ((int shipType, Button _) in _buttons)
            {
                int startingRation = _typeManager.Rations(shipType);
                int numberPlaced = _sm.ShipsPlaced(shipType);
                int remaining = startingRation - numberPlaced;
                _buttons[shipType].SetEnabled(remaining > 0);
                _labels[shipType].text = remaining.ToString();
            }
        }
    }
}