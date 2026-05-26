using UnityEngine;
using UnityEngine.InputSystem;

// Preparation-stage field input controller.
// Handles undead unit drag placement inside the player spawn zone.
public class PreparationFieldInputController : FieldInputControllerBase
{
    protected override StageType ActiveStage => StageType.Preparation;
    private BattleInputGuard inputGuard;
    private bool isDraggingSelection;

    // Original unit position at drag start.
    private Vector2Int dragOriginPos;

    // Last valid grid position while dragging.
    private Vector2Int lastValidGridPos;

    protected override void Awake()
    {
        base.Awake();
        inputGuard = BattleInputGuard.Instance;
    }

    private void OnEnable()
    {
        SetFieldInputEnabled(true);
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isDraggingSelection = false;

            if (!CanStartDragFromPointerDown())
                return;

            isDraggingSelection = TryStartDrag();
        }
        else if (Mouse.current.leftButton.isPressed && isDraggingSelection && selectedUnit != null)
            UpdateDrag();
        else if (Mouse.current.leftButton.wasReleasedThisFrame && isDraggingSelection && selectedUnit != null)
        {
            FinalizeDrag();
            isDraggingSelection = false;
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDraggingSelection = false;
        }
    }

    private bool CanStartDragFromPointerDown()
    {
        if (!IsFieldInputEnabled())
            return false;

        // UI click should not deselect units or start field drag in preparation stage.
        if (inputGuard != null && inputGuard.ConsumeSkipNextWorldClick())
            return false;

        if (inputGuard != null && inputGuard.IsPointerBlockedByUI())
            return false;

        return true;
    }

    // Start drag only when clicking a living undead unit.
    // Non-unit clicks are ignored to avoid UI clicks collapsing current selection.
    private bool TryStartDrag()
    {
        if (!TryGetHit(out var hit))
        {
            return false;
        }

        var unit = hit.collider.GetComponentInParent<UnitBase>();
        if (unit == null)
            return false;

        if (unit.Team != TeamType.Undead || unit.IsDead)
        {
            ClearSelection();
            return false;
        }

        SelectUnit(unit);
        dragOriginPos = unit.GridPosition;
        lastValidGridPos = dragOriginPos;

        ShowValidPositions();
        return true;
    }

    // While dragging, move unit only to valid player spawn cells.
    private void UpdateDrag()
    {
        if (!TryGetHit(out var hit)) return;

        var gridPos = GridManager.Instance.WorldToGrid(hit.point);

        if (gridPos == lastValidGridPos) return;

        var cell = GridManager.Instance.MapDefinition.GetCell(gridPos.x, gridPos.y);
        if (cell.spawnZone != SpawnZoneType.PlayerSpawn) return;
        if (IsOccupiedByOtherUnit(gridPos)) return;
        if (TutorialManager.Instance != null && !TutorialManager.Instance.CanRelocateUndeadTo(gridPos)) return;

        MoveUnitTo(gridPos);
        lastValidGridPos = gridPos;
    }

    // Finalize drag and publish relocate event when position changed.
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

    // Highlight all player spawn cells as movable area.
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
