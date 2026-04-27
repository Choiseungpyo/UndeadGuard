using System.Collections.Generic;
using UnityEngine;

public class SlashSkill : SkillBase
{
    public override void Execute(Vector2Int targetPosition)
    {
        if (owner == null) return;

        Vector2Int origin = owner.GridPosition;
        Vector2Int direction = GetPrimaryDirection(origin, targetPosition);

        List<Vector2Int> hitPositions = GetFanPositions(origin, direction);
        UnitBase[] allUnits = Object.FindObjectsByType<UnitBase>(FindObjectsSortMode.None);

        bool hitAny = false;
        for (int i = 0; i < hitPositions.Count; i++)
        {
            Vector2Int pos = hitPositions[i];
            for (int j = 0; j < allUnits.Length; j++)
            {
                UnitBase unit = allUnits[j];
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
        PlaySkillParticle(targetPosition);

        if (!hitAny)
            Debug.Log("SlashSkill: no enemy in fan area.");
    }

    private List<Vector2Int> GetFanPositions(Vector2Int origin, Vector2Int dir)
    {
        List<Vector2Int> positions = new List<Vector2Int>
        {
            origin + dir
        };

        if (dir.x != 0)
        {
            positions.Add(origin + new Vector2Int(dir.x, 1));
            positions.Add(origin + new Vector2Int(dir.x, -1));
        }
        else
        {
            positions.Add(origin + new Vector2Int(1, dir.y));
            positions.Add(origin + new Vector2Int(-1, dir.y));
        }

        return positions;
    }

    private Vector2Int GetPrimaryDirection(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);

        return new Vector2Int(0, delta.y > 0 ? 1 : -1);
    }
}
