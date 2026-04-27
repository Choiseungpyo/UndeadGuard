using System.Collections.Generic;
using UnityEngine;

public class MagicAttackSkill : SkillBase
{
    private const int BlastRadius = 1;

    public override void Execute(Vector2Int targetPosition)
    {
        if (owner == null) return;

        owner.UnitAnimator?.TriggerAttack();
        PlaySkillParticle(targetPosition);

        List<Vector2Int> blastPositions = GetBlastPositions(targetPosition);
        UnitBase[] allUnits = Object.FindObjectsByType<UnitBase>(FindObjectsSortMode.None);

        bool hitAny = false;
        for (int i = 0; i < blastPositions.Count; i++)
        {
            Vector2Int pos = blastPositions[i];
            for (int j = 0; j < allUnits.Length; j++)
            {
                UnitBase unit = allUnits[j];
                if (unit.Team != TeamType.Enemy || unit.IsDead) continue;
                if (unit.GridPosition != pos) continue;

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
            Debug.Log("MagicAttackSkill: no enemy in blast area.");
    }

    private List<Vector2Int> GetBlastPositions(Vector2Int center)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int dx = -BlastRadius; dx <= BlastRadius; dx++)
        {
            for (int dy = -BlastRadius; dy <= BlastRadius; dy++)
                positions.Add(center + new Vector2Int(dx, dy));
        }

        return positions;
    }
}
