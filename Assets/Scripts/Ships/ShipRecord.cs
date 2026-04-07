using UnityEngine;
using System.Collections.Generic;


public class ShipRecord
{
    public int shipType;
    public int length;

    public List<Vector3Int> occupiedCells = new List<Vector3Int>();
    public HashSet<Vector3Int> hitCells = new HashSet<Vector3Int>();
}
