using UnityEngine;

namespace Ships
{
    public enum Axis
    {
        X,
        Y,
        Z
    }
    
    public abstract class AxisObject
    {
        private readonly Axis _axis;

        protected AxisObject(Axis axis)
        {
            _axis = axis;
        }

        public Axis GetAxis()
        {
            return _axis;
        }
        
        public abstract void TransformTo(Transform ship);     // Adjusts Transform to this axis
        public abstract void TransformFrom(Transform ship);   // Adjusts Transform back to rest
    }

    // x is the default axis
    public class X : AxisObject
    {
        public X() : base(Axis.X) {}
        public override void TransformTo(Transform ship) {}

        public override void TransformFrom(Transform ship) {}
    }

    public class Y : AxisObject
    {
        public Y() : base(Axis.Y) {}
        public override void TransformTo(Transform ship)
        {
            ship.Rotate(Vector3.forward, 90);
        }

        public override void TransformFrom(Transform ship)
        {
            ship.Rotate(Vector3.forward, 270);
        }
    }

    public class Z : AxisObject
    {
        public Z() : base(Axis.Z) {}
        public override void TransformTo(Transform ship)
        {
            ship.Rotate(Vector3.up, 90);
        }

        public override void TransformFrom(Transform ship)
        {
            ship.Rotate(Vector3.up, 270);
        }
    }
}