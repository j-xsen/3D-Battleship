using System;

namespace Ships
{
    public class LineShip : Ship
    {
        public override bool HasValidPlacement(int fieldSize)
        {
            return GetAxis() switch
            {
                Axis.X => !(X + _length > fieldSize),
                Axis.Z => Z - _length >= -1,
                Axis.Y => !(Y + _length > fieldSize),
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