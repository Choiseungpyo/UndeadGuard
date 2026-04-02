using System.Collections.Generic;
using UnityEngine;

// 준비 단계에서 언데드 유닛을 코어 근처에 배치할 위치를 계산한다
// 우선순위: 코어 앞(z 작은 쪽) > 코어 옆 > 코어 뒤(z 큰 쪽) > 거리 순
public static class PreparationSpawnPositioner
{
    // 코어 기준 우선순위와 거리 순으로 정렬된 스폰 위치 목록을 반환한다
    // PlayerSpawn 구역에 해당하며 이동 가능한 칸만 포함한다
    public static List<Vector2Int> GetSpawnPositions(Vector2Int corePos, int count, GridManager grid)
    {
        var candidates = grid.MapDefinition.GetSpawnZonePositions(SpawnZoneType.PlayerSpawn);

        // 이동 불가 칸(벽, 코어 등)은 제외한다
        // 스폰 전 호출이므로 점유 유닛은 없다고 가정한다
        candidates.RemoveAll(pos =>
        {
            var cell = grid.MapDefinition.GetCell(pos.x, pos.y);
            return cell.objectType == StructureType.Wall || cell.objectType == StructureType.Core;
        });

        // 우선순위 오름차순, 같은 우선순위 내에서는 코어와의 거리 오름차순으로 정렬한다
        candidates.Sort((a, b) =>
        {
            int pa = GetPriority(a, corePos);
            int pb = GetPriority(b, corePos);
            if (pa != pb) return pa.CompareTo(pb);
            return Manhattan(a, corePos).CompareTo(Manhattan(b, corePos));
        });

        return candidates.GetRange(0, Mathf.Min(count, candidates.Count));
    }

    // 코어와의 상대적 위치로 우선순위를 결정한다 (숫자가 작을수록 우선)
    private static int GetPriority(Vector2Int pos, Vector2Int core)
    {
        // 1순위: 코어 앞 (같은 x, z가 작은 쪽)
        if (pos.x == core.x && pos.y < core.y) return 1;

        // 2순위: 코어 옆 (같은 z, 다른 x)
        if (pos.y == core.y && pos.x != core.x) return 2;

        // 3순위: 코어 뒤 (같은 x, z가 큰 쪽)
        if (pos.x == core.x && pos.y > core.y) return 3;

        // 4순위: 나머지 (거리 순으로 처리됨)
        return 4;
    }

    private static int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
