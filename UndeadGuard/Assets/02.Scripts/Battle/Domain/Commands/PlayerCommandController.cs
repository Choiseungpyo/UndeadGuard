using System;
using System.Collections.Generic;

/// <summary>
/// 입력 요청을 전투 명령으로 해석하는 플레이어 조작 컨트롤러.
/// 유닛 선택, 이동 요청을 받아 도메인 로직에 전달한다.
/// 실제 이동 연출은 UnitActor가 수행하고, 컨트롤러는 검증과 상태만 관리한다.
/// </summary>
public sealed class PlayerCommandController : IDisposable, IMoveCompletionReceiver
{
    private readonly BattleState state;
    private readonly BattleActionService actionService;
    private readonly GridSearchService gridSearchService;
    private readonly BattleCommandExecutor commandExecutor;

    private int selectedUnitId = -1;
    private HashSet<GridPosition> currentReachable = new HashSet<GridPosition>();
    private List<GridPosition> currentPreviewPath = new List<GridPosition>();
    private bool isActing;
    private bool isPlayerTurn;

    public event Action<int, GridPosition> UnitSelected;
    public event Action SelectionCleared;
    public event Action<IReadOnlyCollection<GridPosition>> MoveRangeChanged;
    public event Action<IReadOnlyList<GridPosition>> PathPreviewChanged;
    public event Action<int, IReadOnlyList<GridPosition>> UnitMoveRequested;

    public PlayerCommandController(
        BattleState state,
        GridSearchService gridSearchService,
        BattleActionService actionService,
        BattleCommandExecutor commandExecutor)
    {
        this.state = state;
        this.gridSearchService = gridSearchService;
        this.actionService = actionService;
        this.commandExecutor = commandExecutor;

        this.actionService.UnitMoved += HandleUnitMoved;
    }

    public bool IsActing => isActing;

    public void Dispose()
    {
        actionService.UnitMoved -= HandleUnitMoved;
    }

    public void SetPlayerTurn(bool isPlayerTurn)
    {
        this.isPlayerTurn = isPlayerTurn;
        isActing = false;
        ClearSelection();
    }

    public void HandleUnitClick(int unitId)
    {
        if (!isPlayerTurn || isActing)
        {
            return;
        }

        if (!state.TryGetUnit(unitId, out BattleUnit unit))
        {
            return;
        }

        if (unit.Team != TeamType.Player)
        {
            return;
        }

        SelectUnit(unit);
    }

    public void HandleGroundHover(GridPosition position)
    {
        if (!isPlayerTurn || isActing)
        {
            return;
        }

        if (selectedUnitId < 0)
        {
            ClearPathPreview();
            return;
        }

        if (!currentReachable.Contains(position))
        {
            ClearPathPreview();
            return;
        }

        if (!state.TryGetUnit(selectedUnitId, out BattleUnit unit))
        {
            ClearPathPreview();
            return;
        }

        List<GridPosition> path = gridSearchService.FindPath(state, unit.Position, position);

        if (path == null || path.Count == 0)
        {
            ClearPathPreview();
            return;
        }

        currentPreviewPath = path;
        PathPreviewChanged?.Invoke(currentPreviewPath);
    }

    public void HandleGroundClick(GridPosition position)
    {
        if (!isPlayerTurn || isActing)
        {
            return;
        }

        if (state.TryGetAliveUnitAtPosition(position, out BattleUnit clickedUnit) &&
            clickedUnit.Team == TeamType.Player)
        {
            SelectUnit(clickedUnit);
            return;
        }

        if (selectedUnitId < 0)
        {
            return;
        }

        if (!state.TryGetUnit(selectedUnitId, out BattleUnit selectedUnit))
        {
            return;
        }

        if (!currentReachable.Contains(position))
        {
            ClearPathPreview();
            return;
        }

        List<GridPosition> path = gridSearchService.FindPath(state, selectedUnit.Position, position);

        MoveCommand moveCommand = new MoveCommand(
            state,
            actionService,
            selectedUnitId,
            position,
            currentReachable,
            path);

        bool moved = commandExecutor.Execute(moveCommand);

        if (!moved)
        {
            ClearPathPreview();
            return;
        }

        isActing = true;
        currentReachable = new HashSet<GridPosition>();

        MoveRangeChanged?.Invoke(currentReachable);
        ClearPathPreview();
    }

    public void ClearSelection()
    {
        selectedUnitId = -1;
        currentReachable = new HashSet<GridPosition>();
        isActing = false;

        ClearPathPreview();

        SelectionCleared?.Invoke();
        MoveRangeChanged?.Invoke(currentReachable);
    }

    public void NotifyUnitMoveFinished(int unitId)
    {
        if (!isActing)
        {
            return;
        }

        if (selectedUnitId != unitId)
        {
            return;
        }

        isActing = false;

        if (!state.TryGetUnit(unitId, out BattleUnit movedUnit))
        {
            ClearSelection();
            return;
        }

        ClearPathPreview();

        UnitSelected?.Invoke(movedUnit.Id, movedUnit.Position);

        currentReachable = new HashSet<GridPosition>();
        MoveRangeChanged?.Invoke(currentReachable);
    }

    private void SelectUnit(BattleUnit unit)
    {
        selectedUnitId = unit.Id;
        currentReachable = gridSearchService.GetReachablePositions(state, unit.Id);

        ClearPathPreview();

        UnitSelected?.Invoke(unit.Id, unit.Position);
        MoveRangeChanged?.Invoke(currentReachable);
    }

    private void ClearPathPreview()
    {
        currentPreviewPath = new List<GridPosition>();
        PathPreviewChanged?.Invoke(currentPreviewPath);
    }

    private void HandleUnitMoved(int unitId, IReadOnlyList<GridPosition> path)
    {
        UnitMoveRequested?.Invoke(unitId, path);
    }

    public void NotifyMoveFinished(int unitId)
    {
        if (!isActing)
        {
            return;
        }

        if (selectedUnitId != unitId)
        {
            return;
        }

        isActing = false;

        if (!state.TryGetUnit(unitId, out BattleUnit movedUnit))
        {
            ClearSelection();
            return;
        }

        ClearPathPreview();

        UnitSelected?.Invoke(movedUnit.Id, movedUnit.Position);

        currentReachable = new HashSet<GridPosition>();
        MoveRangeChanged?.Invoke(currentReachable);
    }
}