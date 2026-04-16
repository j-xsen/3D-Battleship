using UnityEngine;

namespace Ships
{
    public abstract class ShipView : MonoBehaviour
    {
        // base class for ship prefabs
        protected Ship Ship;
        private Renderer _renderer;

        protected void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
        }

        public bool HasValidPlacement(Vector3 size)
        {
            return Ship.HasValidPlacement(size);
        }

        public void Rotate()
        {
            Ship.Rotate(transform);
        }

        public void SetMaterial(Material newMat)
        {
            _renderer.material = newMat;
        }

        public void MoveShip(Vector3 worldPos, Vector3 gamePos)
        {
            Ship.SetPosition(gamePos);
            transform.position = worldPos;
        }

        public AxisObject GetAxes()
        {
            // Axis Object with Transforms
            return Ship.GetAxes();
        }

        public void SetAxis(AxisObject axis)
        {
            // sets axis object
            Ship.SetAxis(axis);
        }

        public Ship GetShip()
        {
            return Ship;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }
    }
}