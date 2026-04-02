using System.Collections.Generic;
using UnityEngine;

// 유닛이 이동 가능한 타일 목록을 계산한다
public static class MovementRangeCalculator
{
    // 상하좌우 4방향 이동 방향 벡터
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    // 시작 위치에서 이동 범위 안에 있는 모든 타일 좌표를 반환한다
    public static List<Vector2Int> Calculate(Vector2Int start, int moveRange, GridManager grid)
    {
        var reachable = new List<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        // 큐에 현재 위치와 남은 이동 횟수를 함께 저장한다
        var queue = new Queue<(Vector2Int pos, int remaining)>();

        visited.Add(start);
        queue.Enqueue((start, moveRange));

        while (queue.Count > 0)
        {
            var (current, remaining) = queue.Dequeue();

            // 시작 위치는 제외하고 이동 가능 목록에 추가한다
            if (current != start)
                reachable.Add(current);

            if (remaining <= 0) continue;

            foreach (var dir in Directions)
            {
                var next = current + dir;

                if (visited.Contains(next)) continue;
                if (!grid.IsWalkable(next)) continue;

                visited.Add(next);
                queue.Enqueue((next, remaining - 1));
            }
        }

        return reachable;
    }
}
