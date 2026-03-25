using UnityEngine;

namespace Ships
{
    public abstract class ShipView : MonoBehaviour
    {
        protected Ship _ship;
        private Renderer _renderer;

        protected void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
        }

        public bool HasValidPlacement(Vector3 size)
        {
            return _ship.HasValidPlacement(size);
        }

        public void Rotate()
        {
            _ship.Rotate(transform);
        }

        public void SetMaterial(Material newMat)
        {
            _renderer.material = newMat;
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