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
        public override Dictionary<int, int> Rations => new() {
            [(int)Types.Two] = 4,
            [(int)Types.Three] = 3,
            [(int)Types.Four] = 2,
            [(int)Types.Five] = 1
        };
    }
}
