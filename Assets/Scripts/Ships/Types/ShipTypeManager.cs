using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ships.Types
{
    public class ShipTypeManager : MonoBehaviour
    {
        [SerializeField] private ShipView[] lineShipPrefabs;
        private ShipTypeGroup Active { get; set; }

        private void Awake()
        {
            Active = new LineShipTypes();
        }

        public ShipView GetPrefab(int selectedShip)
        {
            return lineShipPrefabs[selectedShip];
        }

        public int Rations(int shipType)
        {
            return Active.Rations[shipType];
        }

        public Dictionary<int, int> Rations()
        {
            return Active.Rations;
        }

        public int MinShip()
        {
            return Active.MinShip;
        }

        public int CycleShip(int selected)
        {
            return Active.CycleShip(selected);
        }
    }
}