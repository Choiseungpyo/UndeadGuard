using System.Collections.Generic;

/// <summary>
/// 유닛의 이동 가능 범위를 계산하는 전투 서비스.
/// 현재 위치, 이동력, 점유 상태를 기준으로 도달 가능한 칸을 구한다.
/// </summary>
public sealed class GridRangeService
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

                if (!state.Grid.IsInside(next))
                {
                    continue;
                }

                if (visited.Contains(next))
                {
                    continue;
                }

                visited.Add(next);

                CellData cell = state.Grid.GetCell(next);

                if (!cell.IsWalkable)
                {
                    continue;
                }

                if (cell.IsOccupied)
                {
                    continue;
                }

                result.Add(next);
                queue.Enqueue(new SearchNode(next, current.cost + 1));
            }
        }

        return result;
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