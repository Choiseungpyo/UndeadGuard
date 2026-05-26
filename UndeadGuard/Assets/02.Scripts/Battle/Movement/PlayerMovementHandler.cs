using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementHandler : MonoBehaviour
{
    private List<Vector2Int> movablePositions = new List<Vector2Int>();
    private UnitBase selectedUnit;
    private bool isMoving;
    private Vector2Int lastHoveredPos = new Vector2Int(-999, -999);

    private void Awake()
    {
        enabled = false;

        EventBus.Instance.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Subscribe<TileClickedEvent>(OnTileClicked);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Subscribe<ActionModeRequestedEvent>(OnActionModeRequested);
    }

    private void OnDestroy()
    {
        EventBus.Instance.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Unsubscribe<TileClickedEvent>(OnTileClicked);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Instance.Unsubscribe<ActionModeRequestedEvent>(OnActionModeRequested);
    }

    private void Update()
    {
        HandlePathPreview();
    }

    private void HandlePathPreview()
    {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        var groundPlane = new UnityEngine.Plane(Vector3.up, Vector3.zero);
        if (!groundPlane.Raycast(ray, out float dist)) return;

        var worldPoint = ray.GetPoint(dist);
        var gridPos = GridManager.Instance.WorldToGrid(worldPoint);

        if (gridPos == lastHoveredPos) return;

        lastHoveredPos = gridPos;

        if (movablePositions.Contains(gridPos))
        {
            var path = Pathfinder.FindPath(selectedUnit.GridPosition, gridPos, GridManager.Instance, selectedUnit);
            GridHighlighter.Instance.ShowPath(path);
        }
        else
        {
            GridHighlighter.Instance.ClearPath();
        }
    }

    private void OnActionModeRequested(ActionModeRequestedEvent e)
    {
        if (selectedUnit == null) return;
        if (isMoving) return;

        GridHighlighter.Instance.ClearMovable();
        GridHighlighter.Instance.ClearPath();
        movablePositions.Clear();

        enabled = false;
    }

    private void OnUnitSelected(UnitSelectedEvent e)
    {
        GameStageController stageController = GameStageController.Instance;
        if (stageController == null) return;
        if (stageController.CurrentStage != StageType.Battle) return;
        if (e.Unit.Team != TeamType.Undead) return;

        selectedUnit = e.Unit;
        lastHoveredPos = new Vector2Int(-999, -999);

        if (selectedUnit.HasMoved)
        {
            GridHighlighter.Instance.ClearMovable();
            GridHighlighter.Instance.ClearPath();
            movablePositions.Clear();
            enabled = false;
            return;
        }

        movablePositions = MovementRangeCalculator.Calculate(
            selectedUnit.GridPosition,
            selectedUnit.Stats.MoveRange,
            GridManager.Instance,
            selectedUnit);

        GridHighlighter.Instance.ShowMovable(movablePositions);
        enabled = true;
    }

    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
        movablePositions.Clear();
        GridHighlighter.Instance.ClearMovable();
        GridHighlighter.Instance.ClearPath();
        enabled = false;
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (e.CurrentTurn == TurnType.Enemy)
        {
            movablePositions.Clear();
            GridHighlighter.Instance.ClearMovable();
            GridHighlighter.Instance.ClearPath();
            enabled = false;
        }
    }

    private void OnTileClicked(TileClickedEvent e)
    {
        if (selectedUnit == null) return;
        if (isMoving) return;

        if (!movablePositions.Contains(e.GridPosition))
        {
            if (movablePositions.Count > 0)
                EventBus.Instance.Publish(new UnitDeselectedEvent());
            return;
        }

        if (TutorialManager.Instance != null && !TutorialManager.Instance.CanMoveTo(e.GridPosition))
            return;

        if (selectedUnit.HasMoved) return;

        var path = Pathfinder.FindPath(selectedUnit.GridPosition, e.GridPosition, GridManager.Instance, selectedUnit);
        if (path.Count == 0) return;

        GridHighlighter.Instance.ClearMovable();
        GridHighlighter.Instance.ClearPath();
        movablePositions.Clear();
        enabled = false;

        StartCoroutine(ExecuteMove(selectedUnit, path));
    }

    private IEnumerator ExecuteMove(UnitBase unit, List<Vector2Int> path)
    {
        isMoving = true;
        yield return StartCoroutine(unit.MoveAlongPath(path));
        isMoving = false;
    }
}
