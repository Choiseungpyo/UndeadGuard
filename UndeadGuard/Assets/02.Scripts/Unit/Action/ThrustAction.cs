using System.Collections.Generic;
using UnityEngine;

public class ThrustAction : UnitActionBase
{
    private const int PierceLength = 3;

    public override void Execute(Vector2Int targetPosition)
    {
        if (owner == null) return;

        int damage = owner.Stats != null ? owner.Stats.PhysicalAttack : 0;
        if (damage <= 0)
            return;

        Vector2Int origin = owner.GridPosition;
        Vector2Int direction = GetPrimaryDirection(origin, targetPosition);
        FaceOwnerToward(origin + direction);

        bool hitAny = false;
        var targetTiles = GetLinePositions(origin, direction);
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
            Debug.Log("ThrustAction: no enemy in action range.");
    }

    private static HashSet<Vector2Int> GetLinePositions(Vector2Int origin, Vector2Int direction)
    {
        HashSet<Vector2Int> positions = new HashSet<Vector2Int>();
        for (int i = 1; i <= PierceLength; i++)
            positions.Add(origin + direction * i);

        return positions;
    }

    private static Vector2Int GetPrimaryDirection(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);

        return new Vector2Int(0, delta.y > 0 ? 1 : -1);
    }
}
