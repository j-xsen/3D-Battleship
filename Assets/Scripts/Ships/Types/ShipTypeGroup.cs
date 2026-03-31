using System;
using System.Collections.Generic;

namespace Ships.Types
{
    public abstract class ShipTypeGroup
    {
        public abstract Dictionary<int, int> Rations { get; }
        public abstract int MinShip { get; }
        protected abstract int MaxShip { get; }

        public int CycleShip(int selected)
        {
            selected += 1;
            return selected > MaxShip ? MinShip : selected;
        }
    }
    public abstract class ShipTypeGroup<T> : ShipTypeGroup where T : Enum
    {
        
    }
}