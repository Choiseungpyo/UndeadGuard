using System.Collections.Generic;
using UnityEngine;

// 전사 스킬: 칼로 베기
// 목표 방향 기준 전방 부채형 3칸(전방 + 좌전방 + 우전방)의 적에게 물리 피해를 준다
public class SlashSkill : SkillBase
{
    public override void Execute(Vector2Int targetPosition)
    {
        if (owner == null) return;

        Vector2Int origin = owner.GridPosition;
        Vector2Int direction = GetPrimaryDirection(origin, targetPosition);

        // 부채형 범위: 전방 + 전방 좌측 + 전방 우측
        List<Vector2Int> hitPositions = GetFanPositions(origin, direction);

        bool hitAny = false;
        UnitBase[] allUnits = Object.FindObjectsByType<UnitBase>(FindObjectsSortMode.None);

        foreach (Vector2Int pos in hitPositions)
        {
            foreach (UnitBase unit in allUnits)
            {
                if (unit.Team != TeamType.Enemy || unit.IsDead) continue;
                if (unit.GridPosition != pos) continue;

                unit.TakeDamage(owner.Stats.PhysicalAttack);

                EventBus.Instance.Publish(new UnitAttackedEvent
                {
                    Attacker = owner,
                    Target = unit,
                    Damage = owner.Stats.PhysicalAttack
                });

                hitAny = true;
            }
        }

        owner.UnitAnimator?.TriggerAttack();

        if (!hitAny)
            Debug.Log("칼로 베기: 범위 내 적이 없습니다.");
    }

    // 출발지에서 목표 방향의 부채형 3칸 좌표를 반환한다
    // 전방 1칸, 전방 좌측 1칸(45도), 전방 우측 1칸(45도)
    private List<Vector2Int> GetFanPositions(Vector2Int origin, Vector2Int dir)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        // 전방 칸
        positions.Add(origin + dir);

        // 전방 대각선 두 칸 (4방향 그리드에서 좌전방/우전방)
        if (dir.x != 0)
        {
            // 좌우 방향이면 위아래 대각선
            positions.Add(origin + new Vector2Int(dir.x, 1));
            positions.Add(origin + new Vector2Int(dir.x, -1));
        }
        else
        {
            // 상하 방향이면 좌우 대각선
            positions.Add(origin + new Vector2Int(1, dir.y));
            positions.Add(origin + new Vector2Int(-1, dir.y));
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
