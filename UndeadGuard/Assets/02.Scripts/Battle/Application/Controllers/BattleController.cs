using System;
using System.Collections.Generic;

/// <summary>
/// 입력 요청을 전투 명령으로 해석하는 애플리케이션 계층의 컨트롤러.
/// 유닛 선택, 이동 요청, 턴 종료 요청을 받아 도메인 로직에 전달한다.
/// 실제 이동 연출은 UnitActor가 수행하고, 컨트롤러는 검증과 상태만 관리한다.
/// </summary>
public sealed class BattleController : IDisposable
{
    private readonly BattleState state;
    private readonly GridRangeService rangeService;
    private readonly BattleActionService actionService;
    private readonly TurnSystem turnSystem;
    private readonly BattleCommandExecutor commandExecutor;
    private readonly PathFinder pathFinder;

    private int selectedUnitId = -1;
    private HashSet<GridPosition> currentReachable = new HashSet<GridPosition>();
    private List<GridPosition> currentPreviewPath = new List<GridPosition>();
    private bool isActing;

    public event Action<int, GridPosition> UnitSelected;
    public event Action SelectionCleared;
    public event Action<IReadOnlyCollection<GridPosition>> MoveRangeChanged;
    public event Action<IReadOnlyList<GridPosition>> PathPreviewChanged;
    public event Action<int, IReadOnlyList<GridPosition>> UnitMoveRequested;
    public event Action<TeamType, int> TurnStarted;

    public BattleController(
        BattleState state,
        GridRangeService rangeService,
        BattleActionService actionService,
        TurnSystem turnSystem,
        BattleCommandExecutor commandExecutor,
        PathFinder pathFinder)
    {
        this.state = state;
        this.rangeService = rangeService;
        this.actionService = actionService;
        this.turnSystem = turnSystem;
        this.commandExecutor = commandExecutor;
        this.pathFinder = pathFinder;

        this.actionService.UnitMoved += HandleUnitMoved;
        this.turnSystem.TurnStarted += HandleTurnStarted;
    }

    public void Dispose()
    {
        actionService.UnitMoved -= HandleUnitMoved;
        turnSystem.TurnStarted -= HandleTurnStarted;
    }

    public void StartBattle()
    {
        turnSystem.StartBattle(state);
    }

    public void HandleUnitClick(int unitId)
    {
        if (turnSystem.CurrentTurn != TeamType.Player || isActing)
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
        if (turnSystem.CurrentTurn != TeamType.Player || isActing)
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

        List<GridPosition> path = pathFinder.FindPath(state, unit.Position, position);

        if (path.Count == 0)
        {
            ClearPathPreview();
            return;
        }

        currentPreviewPath = path;
        PathPreviewChanged?.Invoke(currentPreviewPath);
    }

    public void HandleGroundClick(GridPosition position)
    {
        if (turnSystem.CurrentTurn != TeamType.Player || isActing)
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

        List<GridPosition> path = pathFinder.FindPath(state, selectedUnit.Position, position);

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

    public void EndPlayerTurn()
    {
        if (turnSystem.CurrentTurn != TeamType.Player || isActing)
        {
            return;
        }

        ClearSelection();

        EndTurnCommand endTurnCommand = new EndTurnCommand(state, turnSystem);
        commandExecutor.Execute(endTurnCommand);
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

        // 이동 후에도 선택 유지
        // 아웃라인을 유지하기 위해 다시 선택 이벤트를 보낸다.
        UnitSelected?.Invoke(movedUnit.Id, movedUnit.Position);

        currentReachable = new HashSet<GridPosition>();
        MoveRangeChanged?.Invoke(currentReachable);
    }

    private void SelectUnit(BattleUnit unit)
    {
        selectedUnitId = unit.Id;
        currentReachable = rangeService.GetReachablePositions(state, unit.Id);

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

    private void HandleTurnStarted(TeamType team, int round)
    {
        isActing = false;
        ClearSelection();
        TurnStarted?.Invoke(team, round);
    }
}