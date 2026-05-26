using System.Collections.Generic;
using UnityEngine;

public sealed class DefaultUnitAction : IUnitAction
{
    private readonly UnitBase owner;

    public DefaultUnitAction(UnitBase owner)
    {
        this.owner = owner;
    }

    public string ActionId => UnitActionIds.DefaultAction;
    public string DisplayName => "Action";
    public string Description => "Default action";

    public bool CanUse()
    {
        if (owner == null) return false;
        if (owner.IsDead) return false;
        if (owner.HasActed) return false;
        return true;
    }

    public IReadOnlyCollection<Vector2Int> GetTargetTiles()
    {
        return GetResolvedTargetTiles();
    }

    public bool CanTarget(Vector2Int targetPosition)
    {
        return GetResolvedTargetTiles().Contains(targetPosition)
            && GetPrimaryTarget(targetPosition) != null;
    }

    public UnitBase GetPrimaryTarget(Vector2Int targetPosition)
    {
        return FindLiveEnemyAt(targetPosition);
    }

    public List<Transform> GetCameraTargets(Vector2Int targetPosition)
    {
        List<Transform> targets = new List<Transform>();
        UnitBase primaryTarget = GetPrimaryTarget(targetPosition);
        if (primaryTarget != null)
            targets.Add(primaryTarget.transform);

        if (UnitRegistry.Instance == null)
            return targets;

        HashSet<Vector2Int> targetTiles = GetResolvedTargetTiles();
        IReadOnlyList<UnitBase> allUnits = UnitRegistry.Instance.GetAllUnits();
        for (int i = 0; i < allUnits.Count; i++)
        {
            UnitBase unit = allUnits[i];
            if (unit == null || unit == primaryTarget || unit.IsDead)
                continue;

            if (unit.Team != TeamType.Enemy)
                continue;

            if (!targetTiles.Contains(unit.GridPosition))
                continue;

            targets.Add(unit.transform);
        }

        return targets;
    }

    public void Execute(Vector2Int targetPosition)
    {
        if (!CanUse())
            return;

        UnitBase primaryTarget = GetPrimaryTarget(targetPosition);
        if (primaryTarget == null)
            return;

        owner.PerformAttack(primaryTarget);
        ApplyPatternDamageToAdditionalTargets(primaryTarget);
        owner.MarkAsActed();
    }

    private HashSet<Vector2Int> GetResolvedTargetTiles()
    {
        HashSet<Vector2Int> tileSet = new HashSet<Vector2Int>();
        if (owner == null)
            return tileSet;

        int fallbackRange = owner.Stats != null ? owner.Stats.AttackRange : 0;
        List<Vector2Int> rawTiles = AttackPatternResolver.GetTargetTiles(owner, ActionId, fallbackRange);
        for (int i = 0; i < rawTiles.Count; i++)
        {
            Vector2Int tile = rawTiles[i];
            if (!UnitActionBase.IsActionTileEffectable(tile))
                continue;

            tileSet.Add(tile);
        }

        return tileSet;
    }

    private void ApplyPatternDamageToAdditionalTargets(UnitBase primaryTarget)
    {
        if (owner == null || UnitRegistry.Instance == null)
            return;

        int damage = owner.Stats != null ? owner.Stats.PhysicalAttack : 0;
        if (damage <= 0)
            return;

        HashSet<Vector2Int> targetTiles = GetResolvedTargetTiles();
        IReadOnlyList<UnitBase> allUnits = UnitRegistry.Instance.GetAllUnits();
        for (int i = 0; i < allUnits.Count; i++)
        {
            UnitBase unit = allUnits[i];
            if (unit == null || unit == primaryTarget)
                continue;

            if (unit.Team != TeamType.Enemy || unit.IsDead)
                continue;

            if (!targetTiles.Contains(unit.GridPosition))
                continue;

            unit.TakeDamage(damage);
            EventBus.Instance.Publish(new UnitAttackedEvent
            {
                Attacker = owner,
                Target = unit,
                Damage = damage
            });
        }
    }

    private static UnitBase FindLiveEnemyAt(Vector2Int gridPosition)
    {
        if (UnitRegistry.Instance == null)
            return null;

        IReadOnlyList<UnitBase> units = UnitRegistry.Instance.GetAllUnits();
        for (int i = 0; i < units.Count; i++)
        {
            UnitBase unit = units[i];
            if (unit == null || unit.IsDead)
                continue;

            if (unit.Team != TeamType.Enemy)
                continue;

            if (unit.GridPosition == gridPosition)
                return unit;
        }

        return null;
    }
}
