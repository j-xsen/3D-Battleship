using UnityEngine;

using System.Collections.Generic;


public class CombatBoardState
{
    private readonly int _sizeWidth;
    private readonly int _sizeHeight;
    private readonly int _sizeDepth;

    // authoritative host-side combat data
    private readonly ShipRecord[,,] _shipGrid;
    private readonly bool[,,] _attackedCells;

    public CombatBoardState(int sizeWidth, int sizeHeight, int sizeDepth)
    {
        _sizeWidth = sizeWidth;
        _sizeHeight = sizeHeight;
        _sizeDepth = sizeDepth;

        _shipGrid = new ShipRecord[sizeWidth, sizeHeight, sizeDepth];
        _attackedCells = new bool[sizeWidth, sizeHeight, sizeDepth];
    }

    public void RegisterShipData(int shipType, int length, List<Vector3Int> occupiedCells)
    {
        // create one shared ShipRecord for the whole ship
        ShipRecord record = new ShipRecord();
        record.shipType = shipType;
        record.length = length;
        record.occupiedCells = new List<Vector3Int>(occupiedCells);

        // IMPORTANT:
        // your ShipRecord.hitCells appears to be a HashSet<Vector3Int>,
        // not a List<Vector3Int>
        record.hitCells = new HashSet<Vector3Int>();

        foreach (Vector3Int cell in occupiedCells)
        {
            if (!IsInBounds(cell))
            {
                Debug.LogError($"CombatBoardState: ship cell out of bounds: {cell}");
                return;
            }

            if (_shipGrid[cell.x, cell.y, cell.z] != null)
            {
                Debug.LogError($"CombatBoardState: cell already occupied: {cell}");
                return;
            }

            // every occupied cell points to the same ship record
            _shipGrid[cell.x, cell.y, cell.z] = record;
            Debug.Log($"CombatBoardState: stored ship type {shipType} in cell {cell}");
        }

        Debug.Log($"CombatBoardState: registered ship type {shipType} with {occupiedCells.Count} cells");
    }

    public ShipRecord GetShipAtCell(Vector3Int cell)
    {
        if (!IsInBounds(cell)) return null;
        return _shipGrid[cell.x, cell.y, cell.z];
    }

    public AttackResult RegisterAttack(Vector3Int cell)
    {
        if (!IsInBounds(cell))
        {
            Debug.LogError($"CombatBoardState: attack out of bounds at {cell}");
            return AttackResult.Miss;
        }

        // already attacked
        if (_attackedCells[cell.x, cell.y, cell.z])
        {
            Debug.Log($"{cell} already attacked");
            return AttackResult.Miss;
        }

        _attackedCells[cell.x, cell.y, cell.z] = true;

        ShipRecord ship = _shipGrid[cell.x, cell.y, cell.z];

        // miss
        if (ship == null)
        {
            Debug.Log($"? Miss at {cell}");
            return AttackResult.Miss;
        }

        // hit
        ship.hitCells.Add(cell);
        Debug.Log($"? Hit at {cell} (ship type {ship.shipType})");

        // destroyed
        if (ship.hitCells.Count >= ship.length)
        {
            Debug.Log($"?? Ship DESTROYED! Type {ship.shipType}");
            return AttackResult.Destroyed;
        }

        return AttackResult.Hit;
    }

    public bool WasCellAttacked(Vector3Int cell)
    {
        if (!IsInBounds(cell)) return false;
        return _attackedCells[cell.x, cell.y, cell.z];
    }

    public bool AllShipsDestroyed()
    {
        HashSet<ShipRecord> seenShips = new HashSet<ShipRecord>();

        for (int x = 0; x < _sizeWidth; x++)
        {
            for (int y = 0; y < _sizeHeight; y++)
            {
                for (int z = 0; z < _sizeDepth; z++)
                {
                    ShipRecord ship = _shipGrid[x, y, z];
                    if (ship == null) continue;

                    seenShips.Add(ship);
                }
            }
        }

        if (seenShips.Count == 0)
        {
            return false;
        }

        foreach (ShipRecord ship in seenShips)
        {
            if (ship.hitCells.Count < ship.length)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsInBounds(Vector3Int cell)
    {
        return cell.x >= 0 && cell.x < _sizeWidth &&
               cell.y >= 0 && cell.y < _sizeHeight &&
               cell.z >= 0 && cell.z < _sizeDepth;
    }
}

