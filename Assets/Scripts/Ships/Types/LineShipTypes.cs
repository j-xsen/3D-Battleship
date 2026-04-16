using System.Collections.Generic;
using UnityEngine;

namespace Ships.Types
{
    public class LineShipTypes : ShipTypeGroup<LineShipTypes.Types>
    {
        // basic straight line ships
        public enum Types
        {
            Two,
            Three,
            Four,
            Five
        }

        public override int MinShip => (int)Types.Two;
        protected override int MaxShip => (int)Types.Five;

        // TODO : server gets settings?
        public override Dictionary<int, int> Rations => new() { //lowering for testing, feel free to change back 
            [(int)Types.Two] = 1,
            [(int)Types.Three] = 0,
            [(int)Types.Four] = 0,
            [(int)Types.Five] = 0
        };
    }
}
