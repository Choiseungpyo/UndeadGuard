using UnityEngine;
using UnityEngine.InputSystem;

// 준비 단계의 입력과 동작을 담당한다
// 언데드 유닛을 좌클릭 드래그로 PlayerSpawn 구역 내 원하는 위치로 이동시킨다
// 드래그 시작 시 이동 가능한 구역을 하이라이트로 표시한다
public class PreparationInputController : BaseInputController
{
    protected override StageType ActiveStage => StageType.Preparation;

    // 드래그 중인 유닛의 원래 위치 (놓기 취소 시 복원)
    private Vector2Int dragOriginPos;

    // 드래그 중 마지막으로 유효했던 그리드 위치
    private Vector2Int lastValidGridPos;

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryStartDrag();
        else if (Mouse.current.leftButton.isPressed && selectedUnit != null)
            UpdateDrag();
        else if (Mouse.current.leftButton.wasReleasedThisFrame && selectedUnit != null)
            FinalizeDrag();
    }

    // 클릭한 대상이 살아있는 언데드 유닛이면 드래그를 시작한다
    // 유닛이 아닌 곳을 클릭하면 현재 선택을 해제한다
    private void TryStartDrag()
    {
        if (!TryGetHit(out var hit))
        {
            ClearSelection();
            return;
        }

        var unit = hit.collider.GetComponentInParent<UnitBase>();
        if (unit == null || unit.Team != TeamType.Undead || unit.IsDead)
        {
            ClearSelection();
            return;
        }

        SelectUnit(unit);
        dragOriginPos = unit.GridPosition;
        lastValidGridPos = dragOriginPos;

        ShowValidPositions();
    }

    // 드래그 중 마우스 위치의 그리드 셀로 유닛을 실시간으로 이동시킨다
    private void UpdateDrag()
    {
        if (!TryGetHit(out var hit)) return;

        var gridPos = GridManager.Instance.WorldToGrid(hit.point);

        if (gridPos == lastValidGridPos) return;

        var cell = GridManager.Instance.MapDefinition.GetCell(gridPos.x, gridPos.y);
        if (cell.spawnZone != SpawnZoneType.PlayerSpawn) return;
        if (IsOccupiedByOtherUnit(gridPos)) return;

        MoveUnitTo(gridPos);
        lastValidGridPos = gridPos;
    }

    // 마우스를 놓으면 현재 위치에 확정한다
    // 원래 위치와 다를 때만 이벤트를 발행한다
    // 드래그 완료 후에도 유닛 선택 상태를 유지한다
    private void FinalizeDrag()
    {
        GridHighlighter.Instance.ClearMovable();

        if (lastValidGridPos != dragOriginPos)
        {
            EventBus.Instance.Publish(new UndeadRelocatedEvent
            {
                Unit = selectedUnit,
                From = dragOriginPos,
                To = lastValidGridPos
            });
        }
    }

    // PlayerSpawn 구역 전체를 이동 가능 하이라이트로 표시한다
    private void ShowValidPositions()
    {
        var positions = GridManager.Instance.MapDefinition.GetSpawnZonePositions(SpawnZoneType.PlayerSpawn);
        GridHighlighter.Instance.ShowMovable(positions);
    }

    private void MoveUnitTo(Vector2Int gridPos)
    {
        var worldPos = GridManager.Instance.GridToWorld(gridPos);
        selectedUnit.SetGridPosition(gridPos);
        selectedUnit.transform.position = new Vector3(worldPos.x, selectedUnit.transform.position.y, worldPos.z);
    }

    // 선택 해제 및 하이라이트를 정리한다
    private void ClearSelection()
    {
        if (selectedUnit == null) return;
        GridHighlighter.Instance.ClearMovable();
        DeselectUnit();
    }

    private bool IsOccupiedByOtherUnit(Vector2Int targetPos)
    {
        foreach (UnitBase u in UnitRegistry.Instance.GetAllUnits())
        {
            if (u == selectedUnit || u.IsDead) continue;
            if (u.GridPosition == targetPos) return true;
        }
        return false;
    }

}
