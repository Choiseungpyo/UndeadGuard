using System.Collections.Generic;
using UnityEngine;

public class PlayerActionHandler : MonoBehaviour
{
    [SerializeField] private ActionCameraDirector actionCameraDirector;
    [SerializeField] private float postActionFlowDelay = 0.2f;

    private BattleInputGuard inputGuard;
    private UnitBase selectedUnit;
    private IUnitAction selectedAction;
    private bool isResolvingAction;
    private readonly HashSet<Vector2Int> targetTileSet = new HashSet<Vector2Int>();

    private void Awake()
    {
        ResolveActionCameraDirectorIfNeeded();
        ResolveInputGuardIfNeeded();
    }

    private void OnEnable()
    {
        ResolveActionCameraDirectorIfNeeded();
        ResolveInputGuardIfNeeded();

        EventBus.Instance.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Subscribe<ActionModeRequestedEvent>(OnActionModeRequested);
        EventBus.Instance.Subscribe<TileClickedEvent>(OnTileClicked);
    }

    private void OnDisable()
    {
        SetActionCameraLock(false);
        isResolvingAction = false;

        EventBus.Instance.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Unsubscribe<ActionModeRequestedEvent>(OnActionModeRequested);
        EventBus.Instance.Unsubscribe<TileClickedEvent>(OnTileClicked);

        ClearActionMode();
    }

    private void OnUnitSelected(UnitSelectedEvent e)
    {
        if (!IsActionPreviewStage()) return;
        if (e.Unit.Team != TeamType.Undead) return;

        selectedUnit = e.Unit;
        ClearActionMode();
    }

    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
        ClearActionMode();
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (e.CurrentTurn != TurnType.Enemy)
            return;

        isResolvingAction = false;
        selectedUnit = null;
        ClearActionMode();
    }

    private void OnActionModeRequested(ActionModeRequestedEvent e)
    {
        if (!IsActionPreviewStage())
            return;

        UnitBase actionUnit = e.Unit != null ? e.Unit : selectedUnit;
        if (actionUnit == null || actionUnit.Team != TeamType.Undead || actionUnit.IsDead)
            return;

        if (e.Action == null || !e.Action.CanUse())
            return;

        selectedUnit = actionUnit;
        selectedAction = e.Action;
        RefreshActionHighlight();
    }

    private void OnTileClicked(TileClickedEvent e)
    {
        if (selectedUnit == null || selectedAction == null)
            return;

        if (selectedUnit.IsDead || selectedUnit.HasActed)
        {
            ClearActionMode();
            return;
        }

        if (!targetTileSet.Contains(e.GridPosition))
            return;

        if (!selectedAction.CanTarget(e.GridPosition))
            return;

        UnitBase target = selectedAction.GetPrimaryTarget(e.GridPosition);
        if (target == null)
            return;

        if (TutorialManager.Instance != null && !TutorialManager.Instance.CanAttackTarget(target))
            return;

        if (isResolvingAction)
            return;

        StartCoroutine(ResolveActionFlow(selectedUnit, selectedAction, e.GridPosition, target));
    }

    private void RefreshActionHighlight()
    {
        targetTileSet.Clear();
        GridHighlighter.Instance.ClearActionRange();
        GridHighlighter.Instance.ClearMovable();
        GridHighlighter.Instance.ClearPath();

        if (selectedUnit == null || selectedAction == null || !selectedAction.CanUse())
            return;

        IReadOnlyCollection<Vector2Int> targetTiles = selectedAction.GetTargetTiles();
        foreach (Vector2Int tile in targetTiles)
            targetTileSet.Add(tile);

        GridHighlighter.Instance.ShowActionRange(new List<Vector2Int>(targetTileSet));
    }

    private void ClearActionMode()
    {
        selectedAction = null;
        targetTileSet.Clear();
        GridHighlighter.Instance.ClearActionRange();
    }

    private static bool IsActionPreviewStage()
    {
        if (GameStageController.Instance == null)
            return false;

        StageType stage = GameStageController.Instance.CurrentStage;
        return stage == StageType.Battle || stage == StageType.Preparation;
    }

    private System.Collections.IEnumerator ResolveActionFlow(
        UnitBase actor,
        IUnitAction action,
        Vector2Int targetPosition,
        UnitBase primaryTarget)
    {
        isResolvingAction = true;
        SetActionCameraLock(true);

        try
        {
            ResolveActionCameraDirectorIfNeeded();

            if (actionCameraDirector != null && actor != null && primaryTarget != null)
            {
                List<Transform> affectedTargetTransforms = action.GetCameraTargets(targetPosition);
                yield return RunCameraRoutineSafely(
                    actionCameraDirector.PlayPlayerAttackCamera(actor.transform, primaryTarget.transform, affectedTargetTransforms),
                    "PlayPlayerActionCamera");
            }

            action.Execute(targetPosition);
            EventBus.Instance.Publish(new AttackCompletedEvent
            {
                Attacker = actor,
                Target = primaryTarget,
                TargetPosition = targetPosition
            });

            if (actor != null && !actor.IsDead && !actor.HasActed)
                actor.MarkAsActed();

            float delay = Mathf.Max(0f, postActionFlowDelay);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (actionCameraDirector != null)
            {
                yield return RunCameraRoutineSafely(
                    actionCameraDirector.HoldBeforeReturn(),
                    "HoldBeforeReturn");

                yield return RunCameraRoutineSafely(
                    actionCameraDirector.ReturnToSavedCamera(),
                    "ReturnToSavedCamera");
            }

            ClearActionMode();

            if (actor != null && actor.HasMoved)
                EventBus.Instance.Publish(new UnitDeselectedEvent());
        }
        finally
        {
            SetActionCameraLock(false);
            EventBus.Instance.Publish(new ActionCameraFlowFinishedEvent());
            isResolvingAction = false;
        }
    }

    private void ResolveActionCameraDirectorIfNeeded()
    {
        if (actionCameraDirector != null)
            return;

        actionCameraDirector = ActionCameraDirector.Instance;
    }

    private void ResolveInputGuardIfNeeded()
    {
        if (inputGuard != null)
            return;

        inputGuard = BattleInputGuard.Instance;
    }

    private void SetActionCameraLock(bool active)
    {
        ResolveInputGuardIfNeeded();
        if (inputGuard != null)
            inputGuard.SetActionCameraActive(active);
    }

    private System.Collections.IEnumerator RunCameraRoutineSafely(System.Collections.IEnumerator routine, string routineName)
    {
        if (routine == null)
            yield break;

        while (true)
        {
            object current;
            bool movedNext;
            try
            {
                movedNext = routine.MoveNext();
                if (!movedNext)
                    yield break;

                current = routine.Current;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlayerActionHandler] {routineName} failed: {ex.Message}");
                yield break;
            }

            yield return current;
        }
    }
}
