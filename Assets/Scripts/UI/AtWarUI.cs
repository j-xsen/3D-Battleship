using System;
using System.Collections.Generic;
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
        private SpaceBuilder _spaceBuilder;
        private ShipManager _shipManager;
        private Label _currentLabel;
        private RadioButtonGroup _radios;

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

            _spaceBuilder = FindFirstObjectByType<SpaceBuilder>();
            _shipManager = FindFirstObjectByType<ShipManager>();
        }

        private void ShowTheirShips()
        {
            Debug.Log("Showing their ships...");
            
            if(_shipManager) _shipManager.HideAll();
            
            CombatBoardState theirBoard = _network.GetTheirBoardState();

            if (theirBoard == null)
            {
                Debug.LogError("Unable to find their board!");
                return;
            }
            
            List<string> attackedCells = theirBoard.GetAttackedCells();

            if (attackedCells == null) return;
            
            foreach (string attacked in theirBoard.GetAttackedCells())
            {
                Vector3Int attackedCell = theirBoard.VectorFromString(attacked);
                AttackResult result = theirBoard.ResultFromString(attacked);
                if (result == AttackResult.INVALID) continue;
                _spaceBuilder.VisualizeCell(attackedCell, result);
            }
        }

        private void ShowMyShips()
        {
            Debug.Log("Showing my ships...");
            if(_shipManager) _shipManager.ShowAll();

            CombatBoardState myBoard = _network.GetMyBoardState();
            if (myBoard == null) return;

            foreach (string attacked in myBoard.GetAttackedCells())
            {
                Vector3Int attackedCell = myBoard.VectorFromString(attacked);
                AttackResult result = myBoard.ResultFromString(attacked);
                if (result == AttackResult.INVALID) continue;
                _spaceBuilder.VisualizeCell(attackedCell, result);
            }
        }

        private void OnStateChange(string state)
        {
            if (state != "AtWar") return;
            
            // loading into war, open ui
            atWarUI.CloneTree(uiDoc.rootVisualElement);
            _currentLabel = uiDoc.rootVisualElement.Q<Label>("CurrentPlayer");
            _radios = uiDoc.rootVisualElement.Q<RadioButtonGroup>("Radios");
            _radios.RegisterValueChangedCallback(RadioUpdated);
            
            // clear all cells
            _spaceBuilder.ClearBoard();
        }
        
        private void RadioUpdated(ChangeEvent<int> evt)
        {
            // clear board
            _spaceBuilder.ClearBoard();
            if (evt.newValue == 0)
            {
                // show my ships
                ShowTheirShips();
            }
            else
            {
                ShowMyShips();
            }
        }

        private void OnDestroy()
        {
            if (!_network) return;
            _network.OnMyTurn -= OnMyTurn;
            _network.OnTheirTurn -= OnTheirTurn;
            _network.OnStateChanged -= OnStateChange;

            _radios?.UnregisterValueChangedCallback(RadioUpdated);
        }

        private void OnMyTurn()
        {
            if (_currentLabel == null) return;
            _currentLabel.text = "YOU!";
            _radios.value = 0;
        }

        private void OnTheirTurn()
        {
            if (_currentLabel == null) return;
            _currentLabel.text = "THEM";
            _radios.value = 1;
        }
    }
}