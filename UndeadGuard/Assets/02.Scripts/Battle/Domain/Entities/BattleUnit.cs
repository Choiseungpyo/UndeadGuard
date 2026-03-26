using System;

/// <summary>
/// 전투에 참여하는 유닛 1개의 상태를 표현하는 클래스.
/// 위치, 방향, 체력, 공격력, 이동력, 턴 행동 상태를 관리한다.
/// </summary>
public sealed class BattleUnit
{
    public int Id { get; }
    public TeamType Team { get; }

    public GridPosition Position { get; private set; }
    public Direction Facing { get; private set; }

    public int MaxHp { get; }
    public int Hp { get; private set; }

    public int PhysicalAttack { get; }
    public int MagicAttack { get; }
    public int DefensePower { get; }
    public int AttackRange { get; }
    public int MoveRange { get; }

    public bool HasMovedThisTurn { get; private set; }
    public bool HasActedThisTurn { get; private set; }
    public bool IsAlive { get; private set; }

    public UnitActionType LastActionType { get; private set; }

    public BattleUnit(
        int id,
        TeamType team,
        GridPosition startPosition,
        Direction facing,
        int maxHp,
        int physicalAttack,
        int magicAttack,
        int defensePower,
        int attackRange,
        int moveRange)
    {
        Id = id;
        Team = team;
        Position = startPosition;
        Facing = facing;

        MaxHp = maxHp;
        Hp = maxHp;

        PhysicalAttack = physicalAttack;
        MagicAttack = magicAttack;
        DefensePower = defensePower;
        AttackRange = attackRange;
        MoveRange = moveRange;

        HasMovedThisTurn = false;
        HasActedThisTurn = false;
        IsAlive = true;
        LastActionType = UnitActionType.None;
    }

    public bool CanMove()
    {
        return IsAlive && !HasMovedThisTurn;
    }

    public bool CanAct()
    {
        return IsAlive && !HasActedThisTurn;
    }

    public void SetPosition(GridPosition position)
    {
        Position = position;
    }

    public void SetFacing(Direction direction)
    {
        Facing = direction;
    }

    public void FaceTo(GridPosition targetPosition)
    {
        int dx = targetPosition.x - Position.x;
        int dz = targetPosition.z - Position.z;

        if (Math.Abs(dx) > Math.Abs(dz))
        {
            Facing = dx > 0 ? Direction.East : Direction.West;
        }
        else if (dz != 0)
        {
            Facing = dz > 0 ? Direction.North : Direction.South;
        }
    }

    public void MarkMoved()
    {
        HasMovedThisTurn = true;
    }

    public void MarkActed(UnitActionType actionType)
    {
        HasActedThisTurn = true;
        LastActionType = actionType;
    }

    public void RestoreAction()
    {
        HasActedThisTurn = false;
        LastActionType = UnitActionType.None;
    }

    public void ResetTurnFlags()
    {
        HasMovedThisTurn = false;
        HasActedThisTurn = false;
        LastActionType = UnitActionType.None;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            damage = 1;
        }

        Hp -= damage;

        if (Hp < 0)
        {
            Hp = 0;
        }
    }

    public void MarkDead()
    {
        IsAlive = false;
        Hp = 0;
        HasMovedThisTurn = false;
        HasActedThisTurn = false;
        LastActionType = UnitActionType.None;
    }

    public void Revive(GridPosition position, int hp)
    {
        IsAlive = true;
        Position = position;
        Hp = hp > MaxHp ? MaxHp : hp;
        HasMovedThisTurn = false;
        HasActedThisTurn = false;
        LastActionType = UnitActionType.None;
    }
}