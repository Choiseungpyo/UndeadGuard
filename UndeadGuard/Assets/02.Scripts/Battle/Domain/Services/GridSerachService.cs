using System.Collections.Generic;

/// <summary>
/// 그리드 상의 이동 가능 범위와 실제 경로를 탐색하는 전투 서비스.
/// 상하좌우 기준 BFS 탐색을 공통으로 사용한다.
/// 구조물의 길막 여부는 BattleState의 구조물 상태를 통해 판단한다.
/// </summary>
public sealed class GridSearchService
{
    private static readonly GridPosition[] Directions =
    {
        new GridPosition(1, 0),
        new GridPosition(-1, 0),
        new GridPosition(0, 1),
        new GridPosition(0, -1)
    };

    public HashSet<GridPosition> GetReachablePositions(BattleState state, int unitId)
    {
        HashSet<GridPosition> result = new HashSet<GridPosition>();

        if (!state.TryGetUnit(unitId, out BattleUnit unit))
        {
            return result;
        }

        if (!unit.IsAlive)
        {
            return result;
        }

        if (unit.HasMovedThisTurn)
        {
            return result;
        }

        Queue<SearchNode> queue = new Queue<SearchNode>();
        HashSet<GridPosition> visited = new HashSet<GridPosition>();

        queue.Enqueue(new SearchNode(unit.Position, 0));
        visited.Add(unit.Position);

        while (queue.Count > 0)
        {
            SearchNode current = queue.Dequeue();

            if (current.cost >= unit.MoveRange)
            {
                continue;
            }

            for (int i = 0; i < Directions.Length; i++)
            {
                GridPosition next = current.position.Add(Directions[i]);

                if (!CanEnterForRange(state, next, visited))
                {
                    continue;
                }

                visited.Add(next);
                result.Add(next);
                queue.Enqueue(new SearchNode(next, current.cost + 1));
            }
        }

        return result;
    }

    public List<GridPosition> FindPath(BattleState state, GridPosition start, GridPosition goal)
    {
        List<GridPosition> emptyPath = new List<GridPosition>();

        if (!state.Grid.IsInside(start) || !state.Grid.IsInside(goal))
        {
            return emptyPath;
        }

        if (start == goal)
        {
            emptyPath.Add(start);
            return emptyPath;
        }

        Queue<GridPosition> queue = new Queue<GridPosition>();
        Dictionary<GridPosition, GridPosition> cameFrom = new Dictionary<GridPosition, GridPosition>();
        HashSet<GridPosition> visited = new HashSet<GridPosition>();

        queue.Enqueue(start);
        visited.Add(start);

        bool found = false;

        while (queue.Count > 0)
        {
            GridPosition current = queue.Dequeue();

            if (current == goal)
            {
                found = true;
                break;
            }

            for (int i = 0; i < Directions.Length; i++)
            {
                GridPosition next = current.Add(Directions[i]);

                if (!CanEnterForPath(state, next, goal, visited))
                {
                    continue;
                }

                visited.Add(next);
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!found)
        {
            return emptyPath;
        }

        List<GridPosition> path = new List<GridPosition>();
        GridPosition step = goal;
        path.Add(step);

        while (step != start)
        {
            step = cameFrom[step];
            path.Add(step);
        }

        path.Reverse();
        return path;
    }

    private bool CanEnterForRange(
        BattleState state,
        GridPosition position,
        HashSet<GridPosition> visited)
    {
        if (!state.Grid.IsInside(position))
        {
            return false;
        }

        if (visited.Contains(position))
        {
            return false;
        }

        if (state.HasBlockingStructureAtPosition(position))
        {
            return false;
        }

        CellData cell = state.Grid.GetCell(position);

        if (!cell.IsWalkable)
        {
            return false;
        }

        if (cell.IsOccupied)
        {
            return false;
        }

        return true;
    }

    private bool CanEnterForPath(
        BattleState state,
        GridPosition position,
        GridPosition goal,
        HashSet<GridPosition> visited)
    {
        if (!state.Grid.IsInside(position))
        {
            return false;
        }

        if (visited.Contains(position))
        {
            return false;
        }

        if (state.HasBlockingStructureAtPosition(position))
        {
            return false;
        }

        CellData cell = state.Grid.GetCell(position);
        bool isGoal = position == goal;

        if (!cell.IsWalkable)
        {
            return false;
        }

        if (cell.IsOccupied && !isGoal)
        {
            return false;
        }

        return true;
    }

    private readonly struct SearchNode
    {
        public readonly GridPosition position;
        public readonly int cost;

        public SearchNode(GridPosition position, int cost)
        {
            this.position = position;
            this.cost = cost;
        }
    }
}