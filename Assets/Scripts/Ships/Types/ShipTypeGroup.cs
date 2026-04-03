using System;
using System.Collections.Generic;

namespace Ships.Types
{
    public abstract class ShipTypeGroup
    {
        public abstract Dictionary<int, int> Rations { get; }
        public abstract int MinShip { get; } // first ship in cycle
        protected abstract int MaxShip { get; } // last ship in cycle

        // returns next shipType to show in cycle
        public int CycleShip(int selected)
        {
            selected += 1;
            return selected > MaxShip ? MinShip : selected;
        }
    }
    
    // this makes ShipTypeGroup require a defining of an Enum
    public abstract class ShipTypeGroup<T> : ShipTypeGroup where T : Enum
    {
        
    }
}