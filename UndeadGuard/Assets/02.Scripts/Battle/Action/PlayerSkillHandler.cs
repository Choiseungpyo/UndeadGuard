using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillHandler : MonoBehaviour
{
    private UndeadUnit selectedUnit;
    private bool isSkillMode;

    private readonly HashSet<Vector2Int> skillTileSet = new HashSet<Vector2Int>();

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Subscribe<AttackModeRequestedEvent>(OnAttackModeRequested);
        EventBus.Instance.Subscribe<SkillModeRequestedEvent>(OnSkillModeRequested);
        EventBus.Instance.Subscribe<TileClickedEvent>(OnTileClicked);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Unsubscribe<AttackModeRequestedEvent>(OnAttackModeRequested);
        EventBus.Instance.Unsubscribe<SkillModeRequestedEvent>(OnSkillModeRequested);
        EventBus.Instance.Unsubscribe<TileClickedEvent>(OnTileClicked);

        ExitSkillMode();
    }

    private void OnUnitSelected(UnitSelectedEvent e)
    {
        ExitSkillMode();
        selectedUnit = e.Unit as UndeadUnit;
    }

    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
        ExitSkillMode();
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (e.CurrentTurn == TurnType.Enemy)
        {
            selectedUnit = null;
            ExitSkillMode();
        }
    }

    private void OnAttackModeRequested(AttackModeRequestedEvent e)
    {
        ExitSkillMode();
    }

    private void OnSkillModeRequested(SkillModeRequestedEvent e)
    {
        if (GameStageController.Instance.CurrentStage != StageType.Battle)
            return;

        if (selectedUnit == null || selectedUnit.IsDead || selectedUnit.HasActed)
            return;

        ISkill skill = selectedUnit.GetSkill();
        if (skill == null)
            return;

        EnterSkillMode();
    }

    private void OnTileClicked(TileClickedEvent e)
    {
        if (!isSkillMode)
            return;

        if (selectedUnit == null || selectedUnit.IsDead)
        {
            ExitSkillMode();
            return;
        }

        if (!skillTileSet.Contains(e.GridPosition))
            return;

        if (!HasLiveEnemyAt(e.GridPosition))
            return;

        selectedUnit.UseSkill(e.GridPosition);
        ExitSkillMode();

        if (selectedUnit.HasMoved)
            EventBus.Instance.Publish(new UnitDeselectedEvent());
    }

    private void EnterSkillMode()
    {
        ExitSkillMode();

        isSkillMode = true;

        ISkill skill = selectedUnit.GetSkill();
        string actionId = ResolveSkillActionId(selectedUnit, skill);

        List<Vector2Int> tiles = AttackPatternResolver.GetTargetTiles(
            selectedUnit,
            actionId,
            selectedUnit.Stats.AttackRange);

        for (int i = 0; i < tiles.Count; i++)
        {
            Vector2Int tile = tiles[i];
            if (!GridManager.Instance.IsInBounds(tile))
                continue;

            skillTileSet.Add(tile);
        }

        GridHighlighter.Instance.ClearAttackable();
        GridHighlighter.Instance.ClearMovable();
        GridHighlighter.Instance.ClearPath();
        GridHighlighter.Instance.ShowSkillRange(new List<Vector2Int>(skillTileSet));
    }

    private void ExitSkillMode()
    {
        isSkillMode = false;
        skillTileSet.Clear();
        GridHighlighter.Instance.ClearSkillRange();
    }

    private static bool HasLiveEnemyAt(Vector2Int gridPosition)
    {
        if (UnitRegistry.Instance == null)
            return false;

        IReadOnlyList<UnitBase> units = UnitRegistry.Instance.GetAllUnits();
        for (int i = 0; i < units.Count; i++)
        {
            UnitBase unit = units[i];
            if (unit == null || unit.IsDead)
                continue;

            if (unit.Team != TeamType.Enemy)
                continue;

            if (unit.GridPosition == gridPosition)
                return true;
        }

        return false;
    }

    private static string ResolveSkillActionId(UndeadUnit unit, ISkill skill)
    {
        if (AttackPatternResolver.TryGetFirstNonBasicActionId(unit, out string fromDatabase))
            return fromDatabase;

        if (skill != null && !string.IsNullOrWhiteSpace(skill.SkillName))
            return skill.SkillName.Trim();

        return "Skill";
    }
}
