using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 유닛 선택 시 이동 가능 범위 표시, 경로 미리보기, 순차 이동을 담당한다
public class MovementHandler : MonoBehaviour
{
    // 타일 하나를 이동하는 데 걸리는 시간 (초)
    [SerializeField] private float tileMoveDuration = 0.15f;

    // 현재 이동 가능한 타일 좌표 목록
    private List<Vector2Int> movablePositions = new List<Vector2Int>();

    // 현재 선택된 유닛
    private UnitBase selectedUnit;

    // 순차 이동이 진행 중인지 여부
    private bool isMoving;

    // 마지막으로 호버된 그리드 위치 (불필요한 경로 재계산 방지)
    private Vector2Int lastHoveredPos = new Vector2Int(-999, -999);

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Subscribe<TileClickedEvent>(OnTileClicked);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Instance.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
        EventBus.Instance.Unsubscribe<TileClickedEvent>(OnTileClicked);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void Update()
    {
        // 유닛이 선택되어 있고 이동 중이 아닐 때만 경로 미리보기를 갱신한다
        if (selectedUnit == null || isMoving || selectedUnit.HasMoved) return;

        HandlePathPreview();
    }

    // 마우스 위치에 따라 경로 미리보기를 갱신한다
    private void HandlePathPreview()
    {
        var mousePos = Mouse.current.position.ReadValue();
        var ray = Camera.main.ScreenPointToRay(mousePos);

        if (!Physics.Raycast(ray, out var hit)) return;

        var gridPos = GridManager.Instance.WorldToGrid(hit.point);

        // 이전 프레임과 같은 칸이면 재계산하지 않는다
        if (gridPos == lastHoveredPos) return;

        lastHoveredPos = gridPos;

        if (movablePositions.Contains(gridPos))
        {
            // A* 경로를 계산하여 경로 미리보기를 표시한다
            var path = Pathfinder.FindPath(selectedUnit.GridPosition, gridPos, GridManager.Instance);
            GridHighlighter.Instance.ShowPath(path);
        }
        else
        {
            GridHighlighter.Instance.ClearPath();
        }
    }

    // 유닛이 선택되면 이동 범위를 계산하고 하이라이트를 표시한다
    // 전투 단계가 아닌 경우 이동 범위를 표시하지 않는다
    private void OnUnitSelected(UnitSelectedEvent e)
    {
        if (GamePhaseController.Instance.CurrentPhase != PhaseType.Battle) return;

        selectedUnit = e.Unit;
        lastHoveredPos = new Vector2Int(-999, -999);

        if (selectedUnit.HasMoved)
        {
            GridHighlighter.Instance.ClearMovable();
            GridHighlighter.Instance.ClearPath();
            movablePositions.Clear();
            return;
        }

        movablePositions = MovementRangeCalculator.Calculate(
            selectedUnit.GridPosition,
            selectedUnit.Stats.MoveRange,
            GridManager.Instance);

        GridHighlighter.Instance.ShowMovable(movablePositions);
    }

    // 유닛 선택이 해제되면 이동 관련 하이라이트를 제거한다
    private void OnUnitDeselected(UnitDeselectedEvent e)
    {
        selectedUnit = null;
        movablePositions.Clear();
        GridHighlighter.Instance.ClearMovable();
        GridHighlighter.Instance.ClearPath();
    }

    // 적 턴으로 전환되면 하이라이트를 모두 제거한다
    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (e.CurrentTurn == TurnType.Enemy)
        {
            movablePositions.Clear();
            GridHighlighter.Instance.ClearMovable();
            GridHighlighter.Instance.ClearPath();
        }
    }

    // 타일을 클릭했을 때 이동 가능한 칸이면 순차 이동을 시작한다
    private void OnTileClicked(TileClickedEvent e)
    {
        if (selectedUnit == null) return;
        if (isMoving) return;
        if (selectedUnit.HasMoved) return;
        if (!movablePositions.Contains(e.GridPosition)) return;

        var path = Pathfinder.FindPath(selectedUnit.GridPosition, e.GridPosition, GridManager.Instance);
        if (path.Count == 0) return;

        StartCoroutine(MoveAlongPath(selectedUnit, path));
    }

    // 경로를 따라 타일 하나씩 순차적으로 이동하는 코루틴
    private IEnumerator MoveAlongPath(UnitBase unit, List<Vector2Int> path)
    {
        isMoving = true;
        var from = unit.GridPosition;

        // 이동 시작 시 이동 범위 하이라이트를 제거한다
        GridHighlighter.Instance.ClearMovable();
        GridHighlighter.Instance.ClearPath();
        movablePositions.Clear();

        EventBus.Instance.Publish(new UnitMoveStartedEvent { Unit = unit });
        unit.UnitAnimator?.SetWalking(true);

        // path[0]은 현재 위치이므로 path[1]부터 순서대로 이동한다
        for (int i = 1; i < path.Count; i++)
        {
            var targetPos = path[i];
            var startWorldPos = unit.transform.position;
            var gridCenter = GridManager.Instance.GridToWorld(targetPos);
            // Y축은 유닛의 현재 높이를 유지한다
            var endWorldPos = new Vector3(gridCenter.x, unit.transform.position.y, gridCenter.z);

            float elapsed = 0f;
            while (elapsed < tileMoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tileMoveDuration);
                unit.transform.position = Vector3.Lerp(startWorldPos, endWorldPos, t);
                yield return null;
            }

            // 마지막 위치를 정확히 맞춘다
            unit.transform.position = endWorldPos;
            unit.SetGridPosition(targetPos);
        }

        unit.UnitAnimator?.SetWalking(false);
        unit.MarkAsMoved();
        isMoving = false;

        EventBus.Instance.Publish(new UnitMovedEvent
        {
            Unit = unit,
            From = from,
            To = unit.GridPosition
        });

        // 이동 완료 후 공격 가능 범위 갱신은 ActionHandler에서 처리한다
        EventBus.Instance.Publish(new UnitMoveFinishedEvent { Unit = unit });
    }
}
