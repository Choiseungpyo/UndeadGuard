using UnityEngine;
using UnityEngine.InputSystem;

public class BattleInputController : BaseInputController
{
    protected override StageType ActiveStage => StageType.Battle;

    private bool isUnitMoving;
    private bool isSkillMode;

    protected override void Awake()
    {
        base.Awake();

        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Subscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
        EventBus.Instance.Subscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Subscribe<AttackModeRequestedEvent>(OnAttackModeRequested);
        EventBus.Instance.Subscribe<SkillModeRequestedEvent>(OnSkillModeRequested);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Unsubscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
        EventBus.Instance.Unsubscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Unsubscribe<AttackModeRequestedEvent>(OnAttackModeRequested);
        EventBus.Instance.Unsubscribe<SkillModeRequestedEvent>(OnSkillModeRequested);
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        enabled = true;
        isInputEnabled = e.CurrentTurn == TurnType.Player;

        if (!isInputEnabled)
        {
            isSkillMode = false;
            DeselectUnit();
        }
    }

    private void OnUnitMoveStarted(UnitMoveStartedEvent e) => isUnitMoving = true;
    private void OnUnitMoveFinished(UnitMoveFinishedEvent e) => isUnitMoving = false;

    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
        isSkillMode = false;
    }

    private void OnAttackModeRequested(AttackModeRequestedEvent e)
    {
        isSkillMode = false;
    }

    private void OnSkillModeRequested(SkillModeRequestedEvent e)
    {
        isSkillMode = selectedUnit != null;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasReleasedThisFrame)
            return;

        if (!isInputEnabled)
            return;

        if (isUnitMoving)
            return;

        if (UIWorldInputGuard.ConsumeSkipNextWorldClick())
            return;

        HandleWorldClick();
    }

    private void HandleWorldClick()
    {
        if (isSkillMode && selectedUnit != null)
        {
            if (TryGetGroundHit(out var skillGroundHit))
            {
                var skillGridPos = GridManager.Instance.WorldToGrid(skillGroundHit.point);
                EventBus.Instance.Publish(new TileClickedEvent { GridPosition = skillGridPos });
            }

            return;
        }

        if (!TryGetHit(out var hit))
            return;

        var unit = hit.collider.GetComponentInParent<UnitBase>();

        if (unit != null && unit.Team == TeamType.Enemy && !unit.IsDead && selectedUnit != null)
        {
            EventBus.Instance.Publish(new AttackRequestedEvent { Target = unit });
            return;
        }

        if (unit != null && unit.Team == TeamType.Enemy)
            return;

        if (unit != null && unit.Team == TeamType.Undead && !unit.IsDead)
        {
            isSkillMode = false;
            SelectUnit(unit);
            return;
        }

        if (selectedUnit != null)
        {
            if (TryGetGroundHit(out var groundHit))
            {
                var gridPos = GridManager.Instance.WorldToGrid(groundHit.point);
                EventBus.Instance.Publish(new TileClickedEvent { GridPosition = gridPos });
            }
            else
            {
                DeselectUnit();
            }
        }
    }
}