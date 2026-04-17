using System;
using UnityEngine;

using System.Collections.Generic;
using System.Linq;


public class CombatBoardState
{
    private readonly int _sizeWidth;
    private readonly int _sizeHeight;
    private readonly int _sizeDepth;

    // authoritative host-side combat data
    // private readonly ShipRecord[,,] _shipGrid;
    // private readonly bool[,,] _attackedCells;
    private List<ShipRecord> _ships;
    private List<string> _attacked;


    public CombatBoardState(int sizeWidth, int sizeHeight, int sizeDepth)
    {
        _sizeWidth = sizeWidth;
        _sizeHeight = sizeHeight;
        _sizeDepth = sizeDepth;

        // _shipGrid = new ShipRecord[sizeWidth, sizeHeight, sizeDepth];
        // _attackedCells = new bool[sizeWidth, sizeHeight, sizeDepth];
        _ships = new List<ShipRecord>();
        _attacked = new List<string>();
    }

    public void RegisterShipData(int shipType, int length, List<Vector3Int> occupiedCells)
    {
        // create one shared ShipRecord for the whole ship
        ShipRecord record = new ShipRecord
        {
            shipType = shipType,
            length = length,
            OccupiedCells = new List<Vector3Int>(occupiedCells),
        };

        foreach (Vector3Int cell in occupiedCells)
        {
            if (!IsInBounds(cell))
            {
                Debug.LogError($"CombatBoardState: ship cell out of bounds: {cell}");
                return;
            }

            foreach (ShipRecord ship in _ships)
            {
                if (!ship.OccupiesCell(cell)) continue;
                
                Debug.LogError($"CombatBoardState: cell already occupied: {cell}");
                return;
            }

            // every occupied cell points to the same ship record
            // _shipGrid[cell.x, cell.y, cell.z] = record;
            Debug.Log($"CombatBoardState: stored ship type {shipType} in cell {cell}");
        }

        _ships.Add(record);
        Debug.Log($"CombatBoardState: registered ship type {shipType} with {occupiedCells.Count} cells");
    }

    public ShipRecord GetShipAtCell(Vector3Int cell)
    {
        if (!IsInBounds(cell)) return null;

        foreach (ShipRecord ship in _ships)
        {
            if (ship.OccupiesCell(cell)) return ship;
        }

        return null;
    }

    public AttackResult RegisterAttack(Vector3Int cell)
    {
        if (!IsInBounds(cell))
        {
            Debug.LogError($"CombatBoardState: attack out of bounds at {cell}");
            return AttackResult.Miss;
        }

        // already attacked
        foreach (string attackedCell in _attacked)
        {
            Vector3Int stringToVector = VectorFromString(attackedCell);
            
            if (stringToVector != cell) continue;
            Debug.LogError("Already attacked this cell!");
            return AttackResult.AlreadyAttacked;
        }

        ShipRecord ship = GetShipAtCell(cell);

        string vectorToString = $"{cell.x},{cell.y},{cell.z};";
        
        if (ship == null)
        {
            vectorToString += AttackResult.Miss;
            _attacked.Add(vectorToString);
            return AttackResult.Miss;
        }
        
        // hit
        ship.HitCells.Add(cell);
        Debug.Log($"? Hit at {cell} (ship type {ship.shipType})");
        
        if (ship.Health() == 0)
        {
            vectorToString += AttackResult.Destroyed;
            _attacked.Add(vectorToString);
            return AttackResult.Destroyed;
        }

        vectorToString += AttackResult.Hit;
        
        _attacked.Add(vectorToString);

        return AttackResult.Hit;
    }

    public Vector3Int VectorFromString(string cell)
    {
        string[] presplit = cell.Split(";");
        string[] split = presplit[0].Split(",");
        return new Vector3Int(Convert.ToInt32(split[0]),
            Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
    }

    public AttackResult ResultFromString(string cell)
    {
        string[] presplit = cell.Split(";");
        if (Enum.TryParse(presplit[1], out AttackResult result)) return result;
        return AttackResult.INVALID;
    }

    public bool WasCellAttacked(Vector3Int cell)
    {
        if (!IsInBounds(cell)) return false;

        foreach (string attacked in _attacked)
        {
            Vector3Int fromString = VectorFromString(attacked);
            if (cell == fromString) return true;
        }

        return false;
    }

    public bool AllShipsDestroyed()
    {
        foreach (ShipRecord ship in _ships)
        {
            if (ship.HitCells.Count < ship.length) return false;
        }

        return true;
    }

    private bool IsInBounds(Vector3Int cell)
    {
        return cell.x >= 0 && cell.x < _sizeWidth &&
               cell.y >= 0 && cell.y < _sizeHeight &&
               cell.z >= 0 && cell.z < _sizeDepth;
    }

    public void RecordAttackResult(Vector3Int cell, AttackResult result)
    {
        string entry = $"{cell.x},{cell.y},{cell.z};{result}";
        if (!_attacked.Contains(entry))
            _attacked.Add(entry);
    }

    public List<string> GetAttackedCells()
    {
        return _attacked;
    }

    public List<ShipRecord> GetShips()
    {
        return _ships;
    }
}

