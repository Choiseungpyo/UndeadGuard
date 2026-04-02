using System.Collections.Generic;
using UnityEngine;

// 창병 스킬: 찌르기
// 목표 방향으로 직선 3칸을 관통하여 모든 적에게 물리 피해를 준다
public class ThrustSkill : SkillBase
{
    // 관통 거리 (타일 수)
    private const int PierceLength = 3;

    public override void Execute(Vector2Int targetPosition)
    {
        if (owner == null) return;

        Vector2Int origin = owner.GridPosition;
        Vector2Int direction = GetPrimaryDirection(origin, targetPosition);

        owner.UnitAnimator?.TriggerAttack();

        UnitBase[] allUnits = Object.FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
        bool hitAny = false;

        // 전방 직선 PierceLength칸 모두 판정한다
        for (int i = 1; i <= PierceLength; i++)
        {
            Vector2Int pos = origin + direction * i;

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

        if (!hitAny)
            Debug.Log("찌르기: 직선 범위 내 적이 없습니다.");
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
