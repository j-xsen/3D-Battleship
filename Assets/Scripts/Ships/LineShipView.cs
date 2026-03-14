using System;
using UnityEngine;

namespace Ships
{
    public class LineShipView : ShipView
    {
        [SerializeField] public int shipLength;
        
        private void Awake()
        {
            _ship = new LineShip((int)shipLength);
        }
    }
}