using System;

/// <summary>
/// 전투 중 존재하는 구조물의 런타임 상태를 관리하는 클래스.
/// 코어, 벽, 부활 제단 등 맵 구조물의 체력과 기능 상태를 담당한다.
/// </summary>
public sealed class BattleStructure
{
    public StructureType StructureType { get; }
    public GridPosition Position { get; }

    public int MaxHp { get; }
    public int CurrentHp { get; private set; }

    public bool BlocksMovement { get; }
    public bool EnablesResurrection { get; }

    public bool IsDestroyed => CurrentHp <= 0;
    public bool IsBlockingActive => !IsDestroyed && BlocksMovement;
    public bool IsResurrectionActive => !IsDestroyed && EnablesResurrection;

    public BattleStructure(StructureDefinition definition, GridPosition position)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        StructureType = definition.StructureType;
        Position = position;
        MaxHp = definition.MaxHp;
        CurrentHp = definition.MaxHp;
        BlocksMovement = definition.BlocksMovement;
        EnablesResurrection = definition.EnablesResurrection;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDestroyed)
        {
            return;
        }

        CurrentHp -= amount;

        if (CurrentHp < 0)
        {
            CurrentHp = 0;
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDestroyed)
        {
            return;
        }

        CurrentHp += amount;

        if (CurrentHp > MaxHp)
        {
            CurrentHp = MaxHp;
        }
    }
}