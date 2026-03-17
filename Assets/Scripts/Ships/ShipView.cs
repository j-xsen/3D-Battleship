using UnityEngine;

namespace Ships
{
    public abstract class ShipView : MonoBehaviour
    {
        protected Ship _ship;

        public bool HasValidPlacement(int fieldSize)
        {
            return _ship.HasValidPlacement(fieldSize);
        }

        public void Rotate()
        {
            _ship.Rotate(transform);
        }

        public void SetMaterial(Material newMat)
        {
            GetComponentInChildren<Renderer>().material = newMat;
        }

        public void MoveShip(Vector3 worldPos, Vector3 gamePos)
        {
            _ship.SetPosition(gamePos);
            transform.position = worldPos;
        }

        public AxisObject GetAxes()
        {
            return _ship.GetAxes();
        }

        public void SetAxes(AxisObject axes)
        {
            _ship.SetAxes(axes);
        }
    }
}