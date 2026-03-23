using System;
using UnityEngine;

namespace Ships
{
    public class LineShipView : ShipView
    {
        [SerializeField] public int shipLength;
        public int shipHealth;
       // public int index;
        private void Awake()
        {
            _ship = new LineShip((int)shipLength);
            shipHealth = shipLength;
        }
    }
}