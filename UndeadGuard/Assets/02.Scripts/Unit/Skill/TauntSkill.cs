using System.Collections.Generic;
using UnityEngine;

// 방패병 스킬: 도발
// 목표 방향을 기준으로 전방 3x3 범위의 적에게 도발 상태를 부여한다
// 도발 상태의 적은 방패병을 우선 공격 대상으로 삼는다
public class TauntSkill : SkillBase
{
    // 도발 지속 시간 (적 턴 횟수 기준)
    [SerializeField] private int tauntDuration = 2;

    public override void Execute(Vector2Int targetPosition)
    {
        if (owner == null) return;

        Vector2Int origin = owner.GridPosition;
        Vector2Int direction = GetPrimaryDirection(origin, targetPosition);

        // 전방 3x3 범위: 방향 벡터 기준으로 앞쪽 3x3 영역
        List<Vector2Int> tauntPositions = GetTauntArea(origin, direction);

        owner.UnitAnimator?.TriggerAttack();

        EnemyUnit[] allEnemies = Object.FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        bool hitAny = false;

        foreach (Vector2Int pos in tauntPositions)
        {
            foreach (EnemyUnit enemy in allEnemies)
            {
                if (enemy.IsDead) continue;
                if (enemy.GridPosition != pos) continue;

                enemy.SetTaunted(owner, tauntDuration);
                hitAny = true;
            }
        }

        if (!hitAny)
            Debug.Log("도발: 범위 내 적이 없습니다.");
    }

    // 전방 3x3 범위 좌표 목록을 반환한다
    // 유닛 위치 기준으로 방향 벡터 앞쪽 3칸 x 측면 1칸씩 총 9칸
    private List<Vector2Int> GetTauntArea(Vector2Int origin, Vector2Int dir)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        // 방향 벡터의 수직 벡터를 구한다
        Vector2Int perp = new Vector2Int(-dir.y, dir.x);

        // 전방 1칸~3칸, 측면 -1~+1칸 범위 수집
        for (int forward = 1; forward <= 3; forward++)
        {
            for (int side = -1; side <= 1; side++)
            {
                Vector2Int pos = origin + dir * forward + perp * side;
                positions.Add(pos);
            }
        }

        return positions;
    }

    // 출발지에서 목표 방향으로 가장 가까운 4방향 단위 벡터를 반환한다
    private Vector2Int GetPrimaryDirection(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);
        else
            return new Vector2Int(0, delta.y > 0 ? 1 : -1);
    }
}
