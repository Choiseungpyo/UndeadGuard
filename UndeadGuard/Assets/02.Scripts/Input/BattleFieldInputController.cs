using UnityEngine;
using UnityEngine.InputSystem;

public class BattleFieldInputController : FieldInputControllerBase
{
    protected override StageType ActiveStage => StageType.Battle;

    private BattleInputGuard inputGuard;
    private bool isUnitMoving;
    private bool isActionMode;

    protected override void Awake()
    {
        base.Awake();
        inputGuard = BattleInputGuard.Instance;

        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Subscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
        EventBus.Instance.Subscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Subscribe<ActionModeRequestedEvent>(OnActionModeRequested);

        UpdateSystemLock();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Unsubscribe<UnitMoveStartedEvent>(OnUnitMoveStarted);
        EventBus.Instance.Unsubscribe<UnitMoveFinishedEvent>(OnUnitMoveFinished);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Unsubscribe<ActionModeRequestedEvent>(OnActionModeRequested);

        if (inputGuard != null)
            inputGuard.SetSystemLock(this, false);
    }

    private void OnDisable()
    {
        if (inputGuard != null)
            inputGuard.SetSystemLock(this, false);
    }

    private void Update()
    {
        if (!CanProcessBattleFieldClick())
            return;

        HandleWorldClick();
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        enabled = true;
        SetFieldInputEnabled(e.CurrentTurn == TurnType.Player);
        UpdateSystemLock();

        if (!IsFieldInputEnabled())
        {
            isActionMode = false;
            DeselectUnit();
        }
    }

    private void OnUnitMoveStarted(UnitMoveStartedEvent e)
    {
        isUnitMoving = true;
        UpdateSystemLock();
    }

    private void OnUnitMoveFinished(UnitMoveFinishedEvent e)
    {
        isUnitMoving = false;
        UpdateSystemLock();
    }

    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
        isActionMode = false;
    }

    private void OnActionModeRequested(ActionModeRequestedEvent e)
    {
        isActionMode = selectedUnit != null;
    }

    private void UpdateSystemLock()
    {
        if (inputGuard == null)
            return;

        bool locked = !IsFieldInputEnabled() || isUnitMoving;
        inputGuard.SetSystemLock(this, locked);
    }

    private bool CanProcessBattleFieldClick()
    {
        if (!IsFieldInputEnabled())
            return false;

        if (Mouse.current == null)
            return false;

        if (!Mouse.current.leftButton.wasReleasedThisFrame)
            return false;

        if (inputGuard == null)
            return false;

        return inputGuard.TryConsumeWorldClickPermission();
    }

    private void HandleWorldClick()
    {
        if (isActionMode && selectedUnit != null)
        {
            if (TryGetHit(out var actionHit))
            {
                var targetUnit = actionHit.collider.GetComponentInParent<UnitBase>();
                if (targetUnit != null && targetUnit.Team == TeamType.Enemy && !targetUnit.IsDead)
                {
                    EventBus.Instance.Publish(new TileClickedEvent { GridPosition = targetUnit.GridPosition });
                    return;
                }
            }

            if (TryGetGroundHit(out var actionGroundHit))
            {
                var actionGridPos = GridManager.Instance.WorldToGrid(actionGroundHit.point);
                EventBus.Instance.Publish(new TileClickedEvent { GridPosition = actionGridPos });
            }

            return;
        }

        if (!TryGetHit(out var hit))
            return;

        var unit = hit.collider.GetComponentInParent<UnitBase>();

        if (unit != null && unit.Team == TeamType.Enemy)
            return;

        if (unit != null && unit.Team == TeamType.Undead && !unit.IsDead)
        {
            isActionMode = false;
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
