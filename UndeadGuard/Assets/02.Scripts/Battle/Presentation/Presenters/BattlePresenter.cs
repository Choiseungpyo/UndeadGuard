using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 로직 이벤트를 받아 화면 요소를 갱신하는 프레젠터.
/// 유닛 선택, 이동 가능 칸, 경로 표시, 실제 액터 이동을 뷰 계층에 반영한다.
/// </summary>
public sealed class BattlePresenter : MonoBehaviour
{
    [SerializeField] private GridView gridView;
    [SerializeField] private GridCoordinateMapper coordinateMapper;

    private readonly Dictionary<int, UnitActor> actorByUnitId = new Dictionary<int, UnitActor>();

    private BattleController controller;
    private UnitActor currentSelectedActor;

    public void Initialize(BattleController controller, List<UnitActor> actors)
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
        if (controller == null)
        {
            return;
        }

        controller.NotifyUnitMoveFinished(unitId);
    }
}