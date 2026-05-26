using System.Collections.Generic;
using UnityEngine;

public class TauntAction : UnitActionBase
{
    [SerializeField] private int tauntDuration = 2;

    public override void Execute(Vector2Int targetPosition)
    {
        if (owner == null) return;

        Vector2Int origin = owner.GridPosition;
        Vector2Int direction = GetPrimaryDirection(origin, targetPosition);
        FaceOwnerToward(origin + direction);

        owner.UnitAnimator?.TriggerAttack();
        PlayActionParticle(targetPosition);

        bool hitAny = false;
        var targetTiles = GetTauntArea(origin, direction);
        LimitToResolvedActionTargetTiles(targetTiles);
        var enemies = GetLiveEnemiesInTiles(targetTiles);
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i] as EnemyUnit;
            if (enemy == null)
                continue;

            enemy.SetTaunted(owner, tauntDuration);
            hitAny = true;
        }

        if (!hitAny)
            Debug.Log("TauntAction: no enemy in action range.");
    }

    private static HashSet<Vector2Int> GetTauntArea(Vector2Int origin, Vector2Int direction)
    {
        HashSet<Vector2Int> positions = new HashSet<Vector2Int>();
        Vector2Int perpendicular = new Vector2Int(-direction.y, direction.x);

        for (int forward = 1; forward <= 3; forward++)
        {
            for (int side = -1; side <= 1; side++)
                positions.Add(origin + direction * forward + perpendicular * side);
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
