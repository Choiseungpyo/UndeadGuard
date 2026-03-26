using System.Collections.Generic;

/// <summary>
/// 시작 칸에서 목표 칸까지의 실제 이동 경로를 계산하는 클래스.
/// 대각선 없이 상하좌우 기준으로만 탐색하며,
/// 벽과 점유 칸을 고려한 최단 경로를 구한다.
/// </summary>
public sealed class PathFinder
{
    private static readonly GridPosition[] Directions =
    {
        new GridPosition(1, 0),
        new GridPosition(-1, 0),
        new GridPosition(0, 1),
        new GridPosition(0, -1)
    };

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

                if (!state.Grid.IsInside(next))
                {
                    continue;
                }

                if (visited.Contains(next))
                {
                    continue;
                }

                CellData cell = state.Grid.GetCell(next);

                bool isGoal = next == goal;
                bool canPass = cell.IsWalkable && (!cell.IsOccupied || isGoal);

                if (!canPass)
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
}