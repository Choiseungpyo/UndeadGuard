using System;
using System.Collections.Generic;

/// <summary>
/// 전투에서 실제 행동 실행을 담당하는 서비스.
/// 이동, 기본 공격, 코어 및 구조물 공격, 부활, 대기 같은 전투 규칙을 처리한다.
/// </summary>
public sealed class BattleActionService
{
    private readonly TurnManager turnManager;

    public event Action<int, IReadOnlyList<GridPosition>> UnitMoved;
    public event Action<int, int, int> UnitAttackedUnit;
    public event Action<int, StructureType, GridPosition, int> UnitAttackedStructure;
    public event Action<int, int> UnitAttackedCore;
    public event Action<StructureType, GridPosition> StructureDestroyed;
    public event Action<int> UnitDied;
    public event Action<int, GridPosition> UnitResurrected;
    public event Action<int> UnitWaited;

    public BattleActionService(TurnManager turnManager)
    {
        this.turnManager = turnManager;
    }

    public bool TryMove(
        BattleState state,
        int unitId,
        GridPosition targetPosition,
        HashSet<GridPosition> allowedPositions,
        IReadOnlyList<GridPosition> path)
    {
        if (!state.TryGetUnit(unitId, out BattleUnit unit))
        {
            return false;
        }

        if (unit.Team != turnManager.CurrentTurn)
        {
            return false;
        }

        if (!unit.CanMove())
        {
            return false;
        }

        if (allowedPositions == null || !allowedPositions.Contains(targetPosition))
        {
            return false;
        }

        if (path == null || path.Count == 0)
        {
            return false;
        }

        if (path[path.Count - 1] != targetPosition)
        {
            return false;
        }

        if (!state.Grid.IsInside(targetPosition))
        {
            return false;
        }

        if (state.HasBlockingStructureAtPosition(targetPosition))
        {
            return false;
        }

        CellData targetCell = state.Grid.GetCell(targetPosition);

        if (!targetCell.IsWalkable || targetCell.IsOccupied)
        {
            return false;
        }

        GridPosition from = unit.Position;
        CellData fromCell = state.Grid.GetCell(from);

        fromCell.ClearOccupant();
        targetCell.SetOccupant(unit.Id);

        unit.FaceTo(targetPosition);
        unit.SetPosition(targetPosition);
        unit.MarkMoved();

        UnitMoved?.Invoke(unit.Id, path);
        return true;
    }

    public bool TryBasicAttackUnit(BattleState state, int attackerId, int targetId)
    {
        if (!state.TryGetUnit(attackerId, out BattleUnit attacker))
        {
            return false;
        }

        if (!state.TryGetUnit(targetId, out BattleUnit target))
        {
            return false;
        }

        if (attacker.Team != turnManager.CurrentTurn)
        {
            return false;
        }

        if (attacker.Team == target.Team)
        {
            return false;
        }

        if (!attacker.CanAct() || !target.IsAlive)
        {
            return false;
        }

        int distance = attacker.Position.ManhattanDistance(target.Position);
        if (distance > attacker.AttackRange)
        {
            return false;
        }

        attacker.FaceTo(target.Position);

        int finalDamage = attacker.PhysicalAttack - target.DefensePower;
        if (finalDamage < 1)
        {
            finalDamage = 1;
        }

        target.TakeDamage(finalDamage);
        attacker.MarkActed(UnitActionType.BasicAttack);

        UnitAttackedUnit?.Invoke(attackerId, targetId, finalDamage);

        if (target.Hp <= 0)
        {
            bool killed = state.TryKillUnit(targetId, resurrectCost: 3);

            if (killed)
            {
                if (target.Team == TeamType.Enemy)
                {
                    state.PlayerResources.AddDarkEnergy(1);
                }

                UnitDied?.Invoke(targetId);
            }
        }

        return true;
    }

    public bool TryAttackStructure(BattleState state, int attackerId, GridPosition targetPosition)
    {
        if (!state.TryGetUnit(attackerId, out BattleUnit attacker))
        {
            return false;
        }

        if (attacker.Team != turnManager.CurrentTurn)
        {
            return false;
        }

        if (!attacker.CanAct())
        {
            return false;
        }

        if (!state.TryGetStructureAtPosition(targetPosition, out BattleStructure structure))
        {
            return false;
        }

        if (structure.IsDestroyed)
        {
            return false;
        }

        int distance = attacker.Position.ManhattanDistance(targetPosition);
        if (distance > attacker.AttackRange)
        {
            return false;
        }

        attacker.FaceTo(targetPosition);

        int damage = attacker.PhysicalAttack;
        if (damage < 1)
        {
            damage = 1;
        }

        structure.TakeDamage(damage);
        attacker.MarkActed(UnitActionType.BasicAttack);

        UnitAttackedStructure?.Invoke(attackerId, structure.StructureType, targetPosition, damage);

        if (structure.StructureType == StructureType.Core)
        {
            UnitAttackedCore?.Invoke(attackerId, damage);
        }

        if (structure.IsDestroyed)
        {
            StructureDestroyed?.Invoke(structure.StructureType, targetPosition);
        }

        return true;
    }

    public bool TryAttackCore(BattleState state, int attackerId)
    {
        if (state.Core == null)
        {
            return false;
        }

        return TryAttackStructure(state, attackerId, state.Core.Position);
    }

    public bool TryWait(BattleState state, int unitId)
    {
        if (!state.TryGetUnit(unitId, out BattleUnit unit))
        {
            return false;
        }

        if (unit.Team != turnManager.CurrentTurn)
        {
            return false;
        }

        if (!unit.CanAct())
        {
            return false;
        }

        unit.MarkActed(UnitActionType.Wait);
        UnitWaited?.Invoke(unitId);
        return true;
    }

    public bool TryResurrect(BattleState state, int deadUnitId, GridPosition targetPosition)
    {
        if (turnManager.CurrentTurn != TeamType.Player)
        {
            return false;
        }

        if (!state.HasActiveRevivalAltar())
        {
            return false;
        }

        if (!state.TryGetDeadUnit(deadUnitId, out DeadUnitRecord record))
        {
            return false;
        }

        if (!state.PlayerResources.TrySpendDarkEnergy(record.ResurrectCost))
        {
            return false;
        }

        bool resurrected = state.TryResurrectUnit(deadUnitId, targetPosition, reviveHp: 5);

        if (!resurrected)
        {
            state.PlayerResources.AddDarkEnergy(record.ResurrectCost);
            return false;
        }

        UnitResurrected?.Invoke(deadUnitId, targetPosition);
        return true;
    }

    public bool TryGrantExtraAction(BattleState state, int unitId, int darkEnergyCost)
    {
        if (turnManager.CurrentTurn != TeamType.Player)
        {
            return false;
        }

        if (!state.TryGetUnit(unitId, out BattleUnit unit))
        {
            return false;
        }

        if (unit.Team != TeamType.Player)
        {
            return false;
        }

        if (!state.PlayerResources.TrySpendDarkEnergy(darkEnergyCost))
        {
            return false;
        }

        unit.RestoreAction();
        return true;
    }
}