using System.Collections.Generic;
using UnityEngine;

public abstract class UnitActionBase : MonoBehaviour, IUnitAction
{
    [SerializeField] private string actionId;
    [SerializeField] private string displayName;
    [SerializeField] private string description;

    protected UnitBase owner;

    public string ActionId => ResolveActionId();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ActionId : displayName;
    public string Description => description;

    protected virtual void Awake()
    {
        owner = GetComponent<UnitBase>();
    }

    public virtual bool CanUse()
    {
        if (owner == null) return false;
        if (owner.IsDead) return false;
        if (owner.HasActed) return false;
        return true;
    }

    public virtual IReadOnlyCollection<Vector2Int> GetTargetTiles()
    {
        return GetResolvedActionTargetTiles();
    }

    public virtual bool CanTarget(Vector2Int targetPosition)
    {
        return GetResolvedActionTargetTiles().Contains(targetPosition)
            && GetLiveEnemyAtTile(targetPosition) != null;
    }

    public virtual UnitBase GetPrimaryTarget(Vector2Int targetPosition)
    {
        return GetLiveEnemyAtTile(targetPosition);
    }

    public virtual List<Transform> GetCameraTargets(Vector2Int targetPosition)
    {
        List<Transform> transforms = new List<Transform>();
        UnitBase target = GetPrimaryTarget(targetPosition);
        if (target != null)
            transforms.Add(target.transform);

        return transforms;
    }

    protected string ResolveActionId()
    {
        if (!string.IsNullOrWhiteSpace(actionId))
            return actionId.Trim();

        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim();

        if (AttackPatternResolver.TryGetFirstNonBasicActionId(owner, out string fromDatabase))
            return fromDatabase;

        return GetType().Name;
    }

    protected void PlayActionParticle(Vector2Int targetPosition)
    {
        if (owner == null)
            return;

        AttackEffectService.Play(owner, targetPosition, ResolveActionId());
    }

    protected void FaceOwnerToward(Vector2Int targetPosition)
    {
        if (owner == null)
            return;

        owner.FaceToward(targetPosition);
    }

    protected HashSet<Vector2Int> GetResolvedActionTargetTiles()
    {
        HashSet<Vector2Int> tileSet = new HashSet<Vector2Int>();
        if (owner == null)
            return tileSet;

        int fallbackRange = owner.Stats != null ? owner.Stats.AttackRange : 0;
        List<Vector2Int> rawTiles = AttackPatternResolver.GetTargetTiles(owner, ResolveActionId(), fallbackRange);

        for (int i = 0; i < rawTiles.Count; i++)
        {
            Vector2Int tile = rawTiles[i];
            if (!IsActionTileEffectable(tile))
                continue;

            tileSet.Add(tile);
        }

        return tileSet;
    }

    protected List<UnitBase> GetLiveEnemiesInTiles(HashSet<Vector2Int> tileSet)
    {
        List<UnitBase> enemies = new List<UnitBase>();
        if (tileSet == null || tileSet.Count == 0 || UnitRegistry.Instance == null)
            return enemies;

        IReadOnlyList<UnitBase> allUnits = UnitRegistry.Instance.GetAllUnits();
        for (int i = 0; i < allUnits.Count; i++)
        {
            UnitBase unit = allUnits[i];
            if (unit == null || unit.IsDead)
                continue;

            if (unit.Team != TeamType.Enemy)
                continue;

            if (!tileSet.Contains(unit.GridPosition))
                continue;

            enemies.Add(unit);
        }

        return enemies;
    }

    protected void LimitToResolvedActionTargetTiles(HashSet<Vector2Int> tileSet)
    {
        if (tileSet == null || tileSet.Count == 0)
            return;

        tileSet.IntersectWith(GetResolvedActionTargetTiles());
    }

    protected UnitBase GetLiveEnemyAtTile(Vector2Int targetPosition)
    {
        if (UnitRegistry.Instance == null)
            return null;

        IReadOnlyList<UnitBase> allUnits = UnitRegistry.Instance.GetAllUnits();
        for (int i = 0; i < allUnits.Count; i++)
        {
            UnitBase unit = allUnits[i];
            if (unit == null || unit.IsDead)
                continue;

            if (unit.Team != TeamType.Enemy)
                continue;

            if (unit.GridPosition == targetPosition)
                return unit;
        }

        return null;
    }

    protected void PublishUnitAttacked(UnitBase target, int damage)
    {
        EventBus.Instance.Publish(new UnitAttackedEvent
        {
            Attacker = owner,
            Target = target,
            Damage = damage
        });
    }

    public static bool IsActionTileEffectable(Vector2Int tile)
    {
        GridManager grid = GridManager.Instance;
        if (grid == null || !grid.IsInBounds(tile))
            return false;

        MapCellData cell = grid.MapDefinition.GetCell(tile.x, tile.y);
        if (cell.objectType == StructureType.Wall || cell.objectType == StructureType.Core)
            return false;

        return true;
    }

    public abstract void Execute(Vector2Int targetPosition);
}
