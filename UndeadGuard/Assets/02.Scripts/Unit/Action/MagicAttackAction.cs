using System.Collections.Generic;
using UnityEngine;

public class MagicAttackAction : UnitActionBase
{
    private const int BlastRadius = 1;

    public override void Execute(Vector2Int targetPosition)
    {
        if (owner == null) return;

        int damage = owner.Stats != null ? owner.Stats.MagicAttack : 0;
        if (damage <= 0)
            return;

        FaceOwnerToward(targetPosition);

        bool hitAny = false;
        var targetTiles = GetBlastPositions(targetPosition);
        LimitToResolvedActionTargetTiles(targetTiles);
        var enemies = GetLiveEnemiesInTiles(targetTiles);
        for (int i = 0; i < enemies.Count; i++)
        {
            UnitBase enemy = enemies[i];
            enemy.TakeDamage(damage);
            PublishUnitAttacked(enemy, damage);
            hitAny = true;
        }

        owner.UnitAnimator?.TriggerAttack();
        PlayActionParticle(targetPosition);

        if (!hitAny)
            Debug.Log("MagicAttackAction: no enemy in action range.");
    }

    private static HashSet<Vector2Int> GetBlastPositions(Vector2Int center)
    {
        HashSet<Vector2Int> positions = new HashSet<Vector2Int>();
        for (int dx = -BlastRadius; dx <= BlastRadius; dx++)
        {
            for (int dy = -BlastRadius; dy <= BlastRadius; dy++)
                positions.Add(center + new Vector2Int(dx, dy));
        }

        return positions;
    }
}
