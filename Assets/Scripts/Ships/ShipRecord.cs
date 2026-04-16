using UnityEngine;
using System.Collections.Generic;


public class ShipRecord
{
    public int shipType;
    public int length;

    public List<Vector3Int> OccupiedCells = new ();
    public List<Vector3Int> HitCells = new ();
    // public HashSet<Vector3Int> hitCells = new HashSet<Vector3Int>();

    public bool OccupiesCell(Vector3Int cell)
    {
        return OccupiedCells.Contains(cell);
    }

    public int Health()
    {
        return length - HitCells.Count;
    }
}
