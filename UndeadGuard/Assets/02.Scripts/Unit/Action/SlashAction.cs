using System.Collections.Generic;
using UnityEngine;

public class SlashAction : UnitActionBase
{
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
        var targetTiles = GetFanPositions(origin, direction);
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
            Debug.Log("SlashAction: no enemy in action range.");
    }

    private static HashSet<Vector2Int> GetFanPositions(Vector2Int origin, Vector2Int direction)
    {
        HashSet<Vector2Int> positions = new HashSet<Vector2Int>
        {
            origin + direction
        };

        if (direction.x != 0)
        {
            positions.Add(origin + new Vector2Int(direction.x, 1));
            positions.Add(origin + new Vector2Int(direction.x, -1));
        }
        else
        {
            positions.Add(origin + new Vector2Int(1, direction.y));
            positions.Add(origin + new Vector2Int(-1, direction.y));
        }

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
