using System.Collections.Generic;
using UnityEngine;

public class TauntSkill : SkillBase
{
    [SerializeField] private int tauntDuration = 2;

    public override void Execute(Vector2Int targetPosition)
    {
        if (owner == null) return;

        Vector2Int origin = owner.GridPosition;
        Vector2Int direction = GetPrimaryDirection(origin, targetPosition);
        List<Vector2Int> tauntPositions = GetTauntArea(origin, direction);

        owner.UnitAnimator?.TriggerAttack();
        PlaySkillParticle(targetPosition);

        EnemyUnit[] allEnemies = Object.FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        bool hitAny = false;

        for (int i = 0; i < tauntPositions.Count; i++)
        {
            Vector2Int pos = tauntPositions[i];
            for (int j = 0; j < allEnemies.Length; j++)
            {
                EnemyUnit enemy = allEnemies[j];
                if (enemy.IsDead) continue;
                if (enemy.GridPosition != pos) continue;

                enemy.SetTaunted(owner, tauntDuration);
                hitAny = true;
            }
        }

        if (!hitAny)
            Debug.Log("TauntSkill: no enemy in taunt area.");
    }

    private List<Vector2Int> GetTauntArea(Vector2Int origin, Vector2Int dir)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        Vector2Int perpendicular = new Vector2Int(-dir.y, dir.x);

        for (int forward = 1; forward <= 3; forward++)
        {
            for (int side = -1; side <= 1; side++)
            {
                Vector2Int pos = origin + dir * forward + perpendicular * side;
                positions.Add(pos);
            }
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
