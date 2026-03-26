using System.Collections.Generic;

/// <summary>
/// 유닛 이동 행동을 표현하는 명령 객체.
/// 이동할 유닛, 목표 칸, 이동 가능 칸, 실제 이동 경로를 바탕으로 이동을 실행한다.
/// </summary>
public sealed class MoveCommand : IBattleCommand
{
    private readonly BattleState state;
    private readonly BattleActionService actionService;
    private readonly int unitId;
    private readonly GridPosition targetPosition;
    private readonly HashSet<GridPosition> allowedPositions;
    private readonly IReadOnlyList<GridPosition> path;

    public MoveCommand(
        BattleState state,
        BattleActionService actionService,
        int unitId,
        GridPosition targetPosition,
        HashSet<GridPosition> allowedPositions,
        IReadOnlyList<GridPosition> path)
    {
        this.state = state;
        this.actionService = actionService;
        this.unitId = unitId;
        this.targetPosition = targetPosition;
        this.allowedPositions = allowedPositions;
        this.path = path;
    }

    public bool CanExecute()
    {
        if (state == null || actionService == null)
        {
            return false;
        }

        if (allowedPositions == null || path == null)
        {
            return false;
        }

        if (!allowedPositions.Contains(targetPosition))
        {
            return false;
        }

        if (path.Count == 0)
        {
            return false;
        }

        if (path[path.Count - 1] != targetPosition)
        {
            return false;
        }

        return state.TryGetUnit(unitId, out _);
    }

    public bool Execute()
    {
        return actionService.TryMove(state, unitId, targetPosition, allowedPositions, path);
    }
}