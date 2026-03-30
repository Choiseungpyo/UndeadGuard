using System.Collections.Generic;
using UnityEngine;

public sealed class BattlePresenter : MonoBehaviour
{
    [SerializeField] private GridView gridView;
    [SerializeField] private GridCoordinateMapper coordinateMapper;

    private readonly Dictionary<int, UnitActor> actorByUnitId = new Dictionary<int, UnitActor>();

    private PlayerCommandController controller;
    private IMoveCompletionReceiver moveCompletionReceiver;
    private UnitActor currentSelectedActor;

    public void Initialize(
        PlayerCommandController controller,
        List<UnitActor> actors)
    {
        this.controller = controller;

        actorByUnitId.Clear();
        currentSelectedActor = null;

        for (int i = 0; i < actors.Count; i++)
        {
            UnitActor actor = actors[i];

            if (actor == null)
            {
                continue;
            }

            actorByUnitId[actor.UnitId] = actor;
            actor.MoveFinished += HandleActorMoveFinished;
        }

        controller.UnitSelected += HandleUnitSelected;
        controller.SelectionCleared += HandleSelectionCleared;
        controller.MoveRangeChanged += HandleMoveRangeChanged;
        controller.PathPreviewChanged += HandlePathPreviewChanged;
        controller.UnitMoveRequested += HandleUnitMoveRequested;
    }

    public void SetMoveCompletionReceiver(IMoveCompletionReceiver receiver)
    {
        moveCompletionReceiver = receiver;
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.UnitSelected -= HandleUnitSelected;
            controller.SelectionCleared -= HandleSelectionCleared;
            controller.MoveRangeChanged -= HandleMoveRangeChanged;
            controller.PathPreviewChanged -= HandlePathPreviewChanged;
            controller.UnitMoveRequested -= HandleUnitMoveRequested;
        }

        foreach (UnitActor actor in actorByUnitId.Values)
        {
            if (actor != null)
            {
                actor.MoveFinished -= HandleActorMoveFinished;
            }
        }
    }

    private void HandleUnitSelected(int unitId, GridPosition _)
    {
        if (!actorByUnitId.TryGetValue(unitId, out UnitActor actor))
        {
            return;
        }

        if (currentSelectedActor == actor)
        {
            currentSelectedActor.SetSelected(true);
            return;
        }

        if (currentSelectedActor != null)
        {
            currentSelectedActor.SetSelected(false);
        }

        currentSelectedActor = actor;
        currentSelectedActor.SetSelected(true);
    }

    private void HandleSelectionCleared()
    {
        if (currentSelectedActor != null)
        {
            currentSelectedActor.SetSelected(false);
            currentSelectedActor = null;
        }

        gridView.ClearMoveRange();
        gridView.ClearPathPreview();
    }

    private void HandleMoveRangeChanged(IReadOnlyCollection<GridPosition> positions)
    {
        gridView.ShowMoveRange(positions);
    }

    private void HandlePathPreviewChanged(IReadOnlyList<GridPosition> path)
    {
        gridView.ShowPathPreview(path);
    }

    private void HandleUnitMoveRequested(int unitId, IReadOnlyList<GridPosition> path)
    {
        if (!actorByUnitId.TryGetValue(unitId, out UnitActor actor))
        {
            return;
        }

        actor.PlayMove(path, coordinateMapper);
    }

    private void HandleActorMoveFinished(int unitId)
    {
        moveCompletionReceiver?.NotifyMoveFinished(unitId);
    }
}