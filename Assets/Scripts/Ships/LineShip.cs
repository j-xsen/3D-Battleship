using System;
using UnityEngine;

namespace Ships
{
    public class LineShip : Ship
    {
        public override bool HasValidPlacement(Vector3 size)
        {
            return GetAxis() switch
            {
                Axis.X => !(X + _length > size.x),
                Axis.Z => Z - _length >= -1,
                Axis.Y => !(Y + _length > size.y),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public LineShip(int length)
        {
            _length = length;
        }

        private readonly int _length;

    }
}