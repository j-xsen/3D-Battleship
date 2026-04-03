using UnityEngine;

namespace Ships
{
    public class LineShipView : ShipView
    {
        // line ship prefabs
        [SerializeField] public int shipLength;
        public int shipHealth;
       // public int index;
        private new void Awake()
        {
            base.Awake();
            Ship = new LineShip((int)shipLength);
            shipHealth = shipLength;
        }
    }
}
