using UnityEngine;

public class ThrustSkill : SkillBase
{
    private const int PierceLength = 3;

    public override void Execute(Vector2Int targetPosition)
    {
        if (owner == null) return;

        Vector2Int origin = owner.GridPosition;
        Vector2Int direction = GetPrimaryDirection(origin, targetPosition);

        owner.UnitAnimator?.TriggerAttack();
        PlaySkillParticle(targetPosition);

        UnitBase[] allUnits = Object.FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
        bool hitAny = false;

        for (int i = 1; i <= PierceLength; i++)
        {
            Vector2Int pos = origin + direction * i;

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

        if (!hitAny)
            Debug.Log("ThrustSkill: no enemy in line.");
    }

    private Vector2Int GetPrimaryDirection(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);

        return new Vector2Int(0, delta.y > 0 ? 1 : -1);
    }
}
