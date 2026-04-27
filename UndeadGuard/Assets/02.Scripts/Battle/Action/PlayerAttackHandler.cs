using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackHandler : MonoBehaviour
{
    [SerializeField] private float postAttackFlowDelay = 0.2f;

    private UnitBase selectedUnit;
    private bool isResolvingAttack;
    private readonly List<UnitBase> attackableTargets = new List<UnitBase>();
    private readonly HashSet<Vector2Int> attackableTileSet = new HashSet<Vector2Int>();

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Subscribe<AttackRequestedEvent>(OnAttackRequested);
        EventBus.Instance.Subscribe<AttackModeRequestedEvent>(OnAttackModeRequested);
        EventBus.Instance.Subscribe<SkillModeRequestedEvent>(OnSkillModeRequested);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Unsubscribe<AttackRequestedEvent>(OnAttackRequested);
        EventBus.Instance.Unsubscribe<AttackModeRequestedEvent>(OnAttackModeRequested);
        EventBus.Instance.Unsubscribe<SkillModeRequestedEvent>(OnSkillModeRequested);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void OnUnitSelected(UnitSelectedEvent e)
    {
        if (GameStageController.Instance.CurrentStage != StageType.Battle) return;
        if (e.Unit.Team != TeamType.Undead) return;

        attackableTargets.Clear();
        attackableTileSet.Clear();
        GridHighlighter.Instance.ClearAttackable();

        selectedUnit = e.Unit;
    }

    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
        attackableTargets.Clear();
        attackableTileSet.Clear();
        GridHighlighter.Instance.ClearAttackable();
    }

    private void OnAttackModeRequested(AttackModeRequestedEvent e)
    {
        if (GameStageController.Instance.CurrentStage != StageType.Battle) return;
        RefreshAttackHighlight();
    }

    private void OnSkillModeRequested(SkillModeRequestedEvent e)
    {
        attackableTargets.Clear();
        attackableTileSet.Clear();
        GridHighlighter.Instance.ClearAttackable();
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (e.CurrentTurn != TurnType.Enemy)
            return;

        isResolvingAttack = false;
        selectedUnit = null;
        attackableTargets.Clear();
        attackableTileSet.Clear();
        GridHighlighter.Instance.ClearAttackable();
    }

    private void OnAttackRequested(AttackRequestedEvent e)
    {
        if (selectedUnit == null) return;
        if (selectedUnit.HasActed) return;
        if (!attackableTargets.Contains(e.Target)) return;
        if (isResolvingAttack) return;

        StartCoroutine(ResolveAttackFlow(selectedUnit, e.Target));
    }

    private void RefreshAttackHighlight()
    {
        attackableTargets.Clear();
        attackableTileSet.Clear();
        GridHighlighter.Instance.ClearAttackable();

        if (selectedUnit == null || selectedUnit.HasActed)
            return;

        List<Vector2Int> rawTiles = AttackPatternResolver.GetTargetTiles(
            selectedUnit,
            AttackActionIds.BasicAttack,
            selectedUnit.Stats.AttackRange);

        for (int i = 0; i < rawTiles.Count; i++)
        {
            Vector2Int tile = rawTiles[i];
            if (!IsAttackTileDisplayable(tile))
                continue;

            attackableTileSet.Add(tile);
        }

        foreach (UnitBase unit in UnitRegistry.Instance.GetAllUnits())
        {
            if (unit.Team != TeamType.Enemy || unit.IsDead)
                continue;

            if (attackableTileSet.Contains(unit.GridPosition))
            {
                attackableTargets.Add(unit);
            }
        }

        GridHighlighter.Instance.ShowAttackable(new List<Vector2Int>(attackableTileSet));
    }

    private static bool IsAttackTileDisplayable(Vector2Int tile)
    {
        GridManager grid = GridManager.Instance;
        if (grid == null || !grid.IsInBounds(tile))
            return false;

        MapCellData cell = grid.MapDefinition.GetCell(tile.x, tile.y);
        if (cell.objectType == StructureType.Wall || cell.objectType == StructureType.Core)
            return false;

        return true;
    }

    private System.Collections.IEnumerator ResolveAttackFlow(UnitBase attacker, UnitBase target)
    {
        isResolvingAttack = true;

        attacker.PerformAttack(target);
        ApplyPatternDamageToAdditionalTargets(attacker, target);

        float delay = Mathf.Max(0f, postAttackFlowDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (attacker != null && !attacker.IsDead)
            attacker.MarkAsActed();

        GridHighlighter.Instance.ClearAttackable();
        attackableTargets.Clear();
        attackableTileSet.Clear();

        if (attacker != null && attacker.HasMoved)
            EventBus.Instance.Publish(new UnitDeselectedEvent());

        isResolvingAttack = false;
    }

    private void ApplyPatternDamageToAdditionalTargets(UnitBase attacker, UnitBase primaryTarget)
    {
        if (attacker == null || UnitRegistry.Instance == null)
            return;

        int damage = attacker.Stats != null ? attacker.Stats.PhysicalAttack : 0;
        if (damage <= 0)
            return;

        IReadOnlyList<UnitBase> allUnits = UnitRegistry.Instance.GetAllUnits();
        for (int i = 0; i < allUnits.Count; i++)
        {
            UnitBase unit = allUnits[i];
            if (unit == null || unit == primaryTarget)
                continue;

            if (unit.Team != TeamType.Enemy || unit.IsDead)
                continue;

            if (!attackableTileSet.Contains(unit.GridPosition))
                continue;

            unit.TakeDamage(damage);
            EventBus.Instance.Publish(new UnitAttackedEvent
            {
                Attacker = attacker,
                Target = unit,
                Damage = damage
            });
        }
    }
}
