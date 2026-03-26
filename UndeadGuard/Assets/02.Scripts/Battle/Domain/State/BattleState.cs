using System.Collections.Generic;

/// <summary>
/// 현재 전투 전체 상태를 관리하는 중심 클래스.
/// 살아있는 유닛, 소멸한 유닛, 핵, 플레이어 자원을 함께 보관한다.
/// </summary>
public sealed class BattleState
{
    private readonly Dictionary<int, BattleUnit> aliveUnits = new Dictionary<int, BattleUnit>();
    private readonly Dictionary<int, DeadUnitRecord> deadUnits = new Dictionary<int, DeadUnitRecord>();

    public BattleGrid Grid { get; }
    public BattleCore Core { get; }
    public PlayerResourceState PlayerResources { get; }

    public BattleState(BattleGrid grid, BattleCore core, PlayerResourceState playerResources)
    {
        Grid = grid;
        Core = core;
        PlayerResources = playerResources;
    }

    public IEnumerable<BattleUnit> GetAliveUnits()
    {
        foreach (KeyValuePair<int, BattleUnit> pair in aliveUnits)
        {
            yield return pair.Value;
        }
    }

    public IEnumerable<BattleUnit> GetAliveUnits(TeamType team)
    {
        foreach (KeyValuePair<int, BattleUnit> pair in aliveUnits)
        {
            if (pair.Value.Team == team)
            {
                yield return pair.Value;
            }
        }
    }

    public IEnumerable<DeadUnitRecord> GetDeadUnits()
    {
        foreach (KeyValuePair<int, DeadUnitRecord> pair in deadUnits)
        {
            yield return pair.Value;
        }
    }

    public bool TryAddUnit(BattleUnit unit)
    {
        if (aliveUnits.ContainsKey(unit.Id))
        {
            return false;
        }

        if (!Grid.IsInside(unit.Position))
        {
            return false;
        }

        CellData cell = Grid.GetCell(unit.Position);

        if (!cell.IsWalkable || cell.IsOccupied)
        {
            return false;
        }

        aliveUnits.Add(unit.Id, unit);
        cell.SetOccupant(unit.Id);
        return true;
    }

    public bool TryGetUnit(int unitId, out BattleUnit unit)
    {
        return aliveUnits.TryGetValue(unitId, out unit);
    }

    public bool TryGetDeadUnit(int unitId, out DeadUnitRecord deadUnit)
    {
        return deadUnits.TryGetValue(unitId, out deadUnit);
    }

    public bool TryGetAliveUnitAtPosition(GridPosition position, out BattleUnit unit)
    {
        unit = null;

        if (!Grid.IsInside(position))
        {
            return false;
        }

        CellData cell = Grid.GetCell(position);

        if (!cell.IsOccupied)
        {
            return false;
        }

        return aliveUnits.TryGetValue(cell.OccupantUnitId, out unit);
    }

    public bool IsCoreAtPosition(GridPosition position)
    {
        return Core.Position == position;
    }

    public bool TryKillUnit(int unitId, int resurrectCost)
    {
        if (!aliveUnits.TryGetValue(unitId, out BattleUnit unit))
        {
            return false;
        }

        GridPosition deathPosition = unit.Position;
        CellData deathCell = Grid.GetCell(deathPosition);

        if (deathCell.IsOccupied && deathCell.OccupantUnitId == unitId)
        {
            deathCell.ClearOccupant();
        }

        aliveUnits.Remove(unitId);
        unit.MarkDead();

        if (unit.Team == TeamType.Player)
        {
            deadUnits[unitId] = new DeadUnitRecord(unit, deathPosition, resurrectCost);
        }

        return true;
    }

    public bool TryResurrectUnit(int unitId, GridPosition targetPosition, int reviveHp)
    {
        if (!deadUnits.TryGetValue(unitId, out DeadUnitRecord record))
        {
            return false;
        }

        if (!Grid.IsInside(targetPosition))
        {
            return false;
        }

        CellData cell = Grid.GetCell(targetPosition);

        if (!cell.IsWalkable || cell.IsOccupied)
        {
            return false;
        }

        BattleUnit unit = record.Unit;
        unit.Revive(targetPosition, reviveHp);

        deadUnits.Remove(unitId);
        aliveUnits.Add(unitId, unit);
        cell.SetOccupant(unit.Id);

        return true;
    }
}