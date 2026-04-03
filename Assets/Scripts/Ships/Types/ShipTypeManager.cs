using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ships.Types
{
    public class ShipTypeManager : MonoBehaviour
    {
        // Holds the different ShipTypeGroups and provides an interface to interact with them through
        
        // Each ship type will have to have its prefabs defined here
        [SerializeField] private ShipView[] lineShipPrefabs;
        
        // switching the Active group changes the ships available
        private ShipTypeGroup Active { get; set; }

        private void Awake()
        {
            // default to LineShips
            Active = new LineShipTypes();
        }

        public ShipView GetPrefab(int selectedShip)
        {
            return lineShipPrefabs[selectedShip];
        }

        public int Rations(int shipType)
        {
            // get a shipType's allowed count
            return Active.Rations[shipType];
        }

        public Dictionary<int, int> Rations()
        {
            // returns a dictionary of <int shipType, int maxAllowed>
            return Active.Rations;
        }

        public int MinShip()
        {
            // Returns smallest ship (used for initialization)
            return Active.MinShip;
        }

        public int CycleShip(int selected)
        {
            return Active.CycleShip(selected);
        }
    }
}