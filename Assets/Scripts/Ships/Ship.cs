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
        // this is used to calculate valid placement
        public abstract bool HasValidPlacement(Vector3 size);
        
        public void Rotate(Transform transform)
        {
            // transforms back to rest
            _axis.TransformFrom(transform);
            // gets next axis
            _axis = GetNextAxes();
            // transforms to axis
            _axis.TransformTo(transform);
        }
        
        public void SetAxis(AxisObject ax)
        {
            _axis = ax;
        }
        
        public void SetPosition(Vector3 newPos)
        {
            X = newPos.x;
            Y = newPos.y;
            Z = newPos.z;
        }
        
        public AxisObject GetAxes()
        {
            return _axis;
        }
        
        protected float X;
        protected float Y;
        protected float Z;

        protected Axis GetAxis()
        {
            return _axis.GetAxis();
        }
        
        private AxisObject _axis = Axes.X;

        private AxisObject GetNextAxes()
        {
            // find next axis based of what the current axis is
            return _axis.GetAxis() switch
            {
                Axis.X => Axes.Z,
                Axis.Z => Axes.Y,
                Axis.Y => Axes.X,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}