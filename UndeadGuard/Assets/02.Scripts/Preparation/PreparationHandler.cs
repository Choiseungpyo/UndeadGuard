using UnityEngine;
using UnityEngine.InputSystem;

// 준비 단계의 입력과 동작을 담당한다
// 언데드 유닛 선택 및 PlayerSpawn 구역 내 순간이동, 오브젝트 정보 표시를 처리한다
public class PreparationHandler : MonoBehaviour
{
    // 준비 단계 활성화 여부
    private bool isActive;

    // 현재 선택된 언데드 유닛
    private UnitBase selectedUnit;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
    }

    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        isActive = (e.CurrentPhase == PhaseType.Preparation);

        if (!isActive)
            DeselectUnit();
    }

    private void Update()
    {
        if (!isActive) return;
        if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleLeftClick();
    }

    private void HandleLeftClick()
    {
        var mousePos = Mouse.current.position.ReadValue();
        var ray = Camera.main.ScreenPointToRay(mousePos);
        if (!Physics.Raycast(ray, out var hit)) return;

        var clickedUnit = hit.collider.GetComponentInParent<UnitBase>();

        // 살아있는 언데드 유닛 클릭 → 선택 및 정보 UI 활성화
        if (clickedUnit != null && clickedUnit.Team == TeamType.Undead && !clickedUnit.IsDead)
        {
            SelectUnit(clickedUnit);
            return;
        }

        // 언데드가 선택된 상태에서 타일 클릭 → 순간이동 시도
        if (selectedUnit != null)
        {
            var gridPos = GridManager.Instance.WorldToGrid(hit.point);
            TryRelocate(gridPos);
            return;
        }

        // 유닛이 없는 곳 클릭 → 구조물 클릭 여부 확인
        var clickedGridPos = GridManager.Instance.WorldToGrid(hit.point);
        var cell = GridManager.Instance.MapDefinition.GetCell(clickedGridPos.x, clickedGridPos.y);
        if (cell.objectType != StructureType.None)
        {
            EventBus.Instance.Publish(new ObjectSelectedEvent
            {
                ObjectType = cell.objectType,
                GridPosition = clickedGridPos
            });
            return;
        }

        // 그 외 클릭 → 선택 해제
        DeselectUnit();
    }

    // 선택된 언데드를 targetPos로 순간이동시킨다
    // PlayerSpawn 구역이 아니거나 다른 유닛이 점유 중이면 취소한다
    private void TryRelocate(Vector2Int targetPos)
    {
        var cell = GridManager.Instance.MapDefinition.GetCell(targetPos.x, targetPos.y);

        if (cell.spawnZone != SpawnZoneType.PlayerSpawn)
        {
            DeselectUnit();
            return;
        }

        if (IsOccupiedByOtherUnit(targetPos))
            return;

        var from = selectedUnit.GridPosition;
        var worldPos = GridManager.Instance.GridToWorld(targetPos);

        selectedUnit.SetGridPosition(targetPos);
        selectedUnit.transform.position = new Vector3(
            worldPos.x,
            selectedUnit.transform.position.y,
            worldPos.z);

        EventBus.Instance.Publish(new UndeadRelocatedEvent
        {
            Unit = selectedUnit,
            From = from,
            To = targetPos
        });

        DeselectUnit();
    }

    // targetPos에 선택된 유닛 외의 다른 살아있는 유닛이 있는지 확인한다
    private bool IsOccupiedByOtherUnit(Vector2Int targetPos)
    {
        foreach (UnitBase u in UnitRegistry.Instance.GetAllUnits())
        {
            if (u == selectedUnit || u.IsDead) continue;
            if (u.GridPosition == targetPos) return true;
        }
        return false;
    }

    private void SelectUnit(UnitBase unit)
    {
        selectedUnit = unit;
        EventBus.Instance.Publish(new UnitSelectedEvent { Unit = unit });
    }

    private void DeselectUnit()
    {
        if (selectedUnit == null) return;
        selectedUnit = null;
        EventBus.Instance.Publish(new UnitDeselectedEvent());
    }
}
