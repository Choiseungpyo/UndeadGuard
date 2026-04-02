using System.Collections.Generic;
using UnityEngine;

// 법사 스킬: 마법 공격
// 지정한 타일 중심 3x3 범위(Chebyshev 거리 1)의 모든 적에게 마법 피해를 준다
public class MagicAttackSkill : SkillBase
{
    // 범위 반경 (중심으로부터 1칸 = 3x3 정사각형)
    private const int BlastRadius = 1;

    public override void Execute(Vector2Int targetPosition)
    {
        if (owner == null) return;

        owner.UnitAnimator?.TriggerAttack();

        List<Vector2Int> blastPositions = GetBlastPositions(targetPosition);
        UnitBase[] allUnits = Object.FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
        bool hitAny = false;

        foreach (Vector2Int pos in blastPositions)
        {
            foreach (UnitBase unit in allUnits)
            {
                if (unit.Team != TeamType.Enemy || unit.IsDead) continue;
                if (unit.GridPosition != pos) continue;

                // 마법 공격은 MagicAttack 수치를 사용한다
                int damage = owner.Stats.MagicAttack;
                unit.TakeDamage(damage);

                EventBus.Instance.Publish(new UnitAttackedEvent
                {
                    Attacker = owner,
                    Target = unit,
                    Damage = damage
                });

                hitAny = true;
            }
        }

        if (!hitAny)
            Debug.Log("마법 공격: 범위 내 적이 없습니다.");
    }

    // 중심 위치에서 BlastRadius 안에 있는 3x3 범위 좌표 목록을 반환한다
    private List<Vector2Int> GetBlastPositions(Vector2Int center)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        for (int dx = -BlastRadius; dx <= BlastRadius; dx++)
        {
            for (int dy = -BlastRadius; dy <= BlastRadius; dy++)
            {
                positions.Add(center + new Vector2Int(dx, dy));
            }
        }

        return positions;
    }
}
