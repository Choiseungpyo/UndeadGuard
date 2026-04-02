using System.Collections.Generic;
using UnityEngine;

// 그리드 위에서 두 지점 사이의 최단 경로를 탐색한다
public static class Pathfinder
{
    // 상하좌우 4방향 이동 방향 벡터
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    // 시작 위치에서 목표 위치까지의 최단 경로를 반환한다
    // 반환값은 시작 위치 포함. 경로가 없으면 빈 리스트를 반환한다
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, GridManager grid)
    {
        var openSet = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        // 시작점에서 각 노드까지의 실제 이동 비용
        var gScore = new Dictionary<Vector2Int, int>();
        gScore[start] = 0;

        // 예상 총 비용 (g + h)
        var fScore = new Dictionary<Vector2Int, int>();
        fScore[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            var current = GetLowestFScore(openSet, fScore);

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            foreach (var dir in Directions)
            {
                var neighbor = current + dir;

                if (!grid.IsWalkable(neighbor)) continue;

                int tentativeG = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // 경로를 찾지 못한 경우
        return new List<Vector2Int>();
    }

    // 맨해튼 거리를 휴리스틱으로 사용한다 (4방향 이동이므로 적합)
    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // 열린 목록에서 f 비용이 가장 낮은 노드를 반환한다
    private static Vector2Int GetLowestFScore(List<Vector2Int> openSet, Dictionary<Vector2Int, int> fScore)
    {
        var lowest = openSet[0];
        int lowestF = fScore.ContainsKey(lowest) ? fScore[lowest] : int.MaxValue;

        for (int i = 1; i < openSet.Count; i++)
        {
            int f = fScore.ContainsKey(openSet[i]) ? fScore[openSet[i]] : int.MaxValue;
            if (f < lowestF)
            {
                lowestF = f;
                lowest = openSet[i];
            }
        }

        return lowest;
    }

    // 목표에서 출발점까지 역추적하여 순서대로 정렬된 경로를 반환한다
    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        return path;
    }
}
