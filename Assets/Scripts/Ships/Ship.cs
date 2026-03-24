using System;
using UnityEngine;

namespace Ships
{
    public static class Axes
    {
        public static readonly AxisObject X = new X();
        public static readonly AxisObject Y = new Y();
        public static readonly AxisObject Z = new Z();
    }
    
    public abstract class Ship
    {
        public abstract bool HasValidPlacement(Vector3 size);
        public void Rotate(Transform transform)
        {
            _axes.TransformFrom(transform);
            _axes = GetNextAxes();
            _axes.TransformTo(transform);
        }
        
        private AxisObject _axes = Axes.X;

        public void SetAxes(AxisObject ax)
        {
            _axes = ax;
        }

        protected float X = 0f;
        protected float Y = 0f;
        protected float Z = 0f;

        public void SetPosition(Vector3 newPos)
        {
            X = newPos.x;
            Y = newPos.y;
            Z = newPos.z;
        }

        protected Axis GetAxis()
        {
            return _axes.GetAxis();
        }

        public AxisObject GetAxes()
        {
            return _axes;
        }

        private AxisObject GetNextAxes()
        {
            // find next axis based of what the current axis is
            return _axes.GetAxis() switch
            {
                Axis.X => Axes.Z,
                Axis.Z => Axes.Y,
                Axis.Y => Axes.X,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}