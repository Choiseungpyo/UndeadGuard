using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private EnemyUnit unit;

    private void Awake()
    {
        unit = GetComponent<EnemyUnit>();
    }

    public Vector2Int GetTargetGridPosition(List<UnitBase> allUnits)
    {
        if (unit == null)
            return Vector2Int.zero;

        if (unit.IsTaunted && unit.TauntSource != null && !unit.TauntSource.IsDead)
        {
            if (IsTargetInBasicRange(unit.TauntSource.GridPosition))
                return unit.GridPosition;

            if (TryGetNearestReachableAttackPosition(unit.TauntSource.GridPosition, out var tauntAttackPos))
                return tauntAttackPos;

            return unit.GridPosition;
        }

        if (HasAttackableTargetInRange(allUnits))
            return unit.GridPosition;

        if (TryGetClosestMovementTargetPosition(allUnits, out var targetPos))
            return targetPos;

        return unit.GridPosition;
    }

    public IEnumerator ExecuteMove(List<UnitBase> allUnits)
    {
        if (unit == null || unit.IsDead)
            yield break;

        if (HasAttackableTargetInRange(allUnits))
            yield break;

        Vector2Int targetPos = GetTargetGridPosition(allUnits);
        if (targetPos == unit.GridPosition)
            yield break;

        List<Vector2Int> path = Pathfinder.FindPath(unit.GridPosition, targetPos, GridManager.Instance, unit);
        if (path.Count <= 1)
            yield break;

        int steps = Mathf.Min(unit.Stats.MoveRange, path.Count - 1);
        yield return StartCoroutine(unit.MoveAlongPath(path.GetRange(0, steps + 1)));
    }

    public bool ExecuteAttack(List<UnitBase> allUnits)
    {
        if (unit == null || unit.IsDead)
            return false;

        if (unit.IsTaunted && unit.TauntSource != null && !unit.TauntSource.IsDead)
        {
            if (IsTargetInBasicRange(unit.TauntSource.GridPosition))
            {
                unit.PerformAttack(unit.TauntSource);
                unit.DecrementTaunt();
                return true;
            }
        }

        if (IsCoreInRange())
        {
            PerformCoreAttack();
            return true;
        }

        UnitBase attackTarget = FindAttackTargetInRange(allUnits);
        if (attackTarget != null)
        {
            unit.PerformAttack(attackTarget);
            unit.DecrementTaunt();
            return true;
        }

        unit.DecrementTaunt();
        return false;
    }

    private bool HasAttackableTargetInRange(List<UnitBase> allUnits)
    {
        if (unit == null)
            return false;

        if (unit.IsTaunted && unit.TauntSource != null && !unit.TauntSource.IsDead)
        {
            if (IsTargetInBasicRange(unit.TauntSource.GridPosition))
                return true;
        }

        if (IsCoreInRange())
            return true;

        if (FindAttackTargetInRange(allUnits) != null)
            return true;

        return false;
    }

    private bool TryGetClosestMovementTargetPosition(List<UnitBase> allUnits, out Vector2Int result)
    {
        result = unit.GridPosition;

        List<Vector2Int> candidateTargets = new List<Vector2Int>();

        foreach (UnitBase u in allUnits)
        {
            if (u == null || u.Team != TeamType.Undead || u.IsDead)
                continue;

            candidateTargets.Add(u.GridPosition);
        }

        var coreCells = GridManager.Instance.MapDefinition.GetPositions(StructureType.Core);
        for (int i = 0; i < coreCells.Count; i++)
            candidateTargets.Add(coreCells[i]);

        int bestPathLength = int.MaxValue;
        bool found = false;

        for (int i = 0; i < candidateTargets.Count; i++)
        {
            Vector2Int target = candidateTargets[i];

            if (!TryGetNearestReachableAttackPosition(target, out var attackPos))
                continue;

            List<Vector2Int> path = Pathfinder.FindPath(unit.GridPosition, attackPos, GridManager.Instance, unit);
            if (path.Count == 0)
                continue;

            if (path.Count < bestPathLength)
            {
                bestPathLength = path.Count;
                result = attackPos;
                found = true;
            }
        }

        return found;
    }

    private UnitBase FindAttackTargetInRange(List<UnitBase> allUnits)
    {
        UnitBase nearest = null;
        int minDist = int.MaxValue;

        foreach (UnitBase u in allUnits)
        {
            if (u == null || u.Team != TeamType.Undead || u.IsDead)
                continue;

            if (!IsTargetInBasicRange(u.GridPosition))
                continue;

            int dist = Manhattan(unit.GridPosition, u.GridPosition);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = u;
            }
        }

        return nearest;
    }

    private void PerformCoreAttack()
    {
        Vector2Int effectTarget = unit.GridPosition;
        var coreCells = GridManager.Instance.MapDefinition.GetPositions(StructureType.Core);
        if (coreCells.Count > 0)
        {
            Vector2Int nearestCore = coreCells[0];
            int minDist = Manhattan(unit.GridPosition, nearestCore);

            for (int i = 1; i < coreCells.Count; i++)
            {
                int d = Manhattan(unit.GridPosition, coreCells[i]);
                if (d < minDist)
                {
                    minDist = d;
                    nearestCore = coreCells[i];
                }
            }

            effectTarget = nearestCore;
            unit.FaceToward(nearestCore);
        }

        unit.UnitAnimator?.TriggerAttack();
        AttackEffectService.Play(unit, effectTarget, AttackActionIds.BasicAttack);

        CoreHealth coreHealth = FindFirstObjectByType<CoreHealth>();
        if (coreHealth != null)
            coreHealth.TakeDamage(unit.Stats.PhysicalAttack);
    }

    private bool TryGetNearestReachableAttackPosition(Vector2Int targetPos, out Vector2Int result)
    {
        result = unit.GridPosition;

        List<Vector2Int> candidates = AttackPatternResolver.GetAttackOriginCandidates(
            unit,
            targetPos,
            AttackActionIds.BasicAttack,
            unit.Stats.AttackRange);

        if (candidates.Count == 0)
            return false;

        int bestPathLength = int.MaxValue;
        bool found = false;

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2Int candidate = candidates[i];

            if (candidate == unit.GridPosition)
            {
                result = candidate;
                return true;
            }

            if (!GridManager.Instance.IsWalkableIgnoring(candidate, unit))
                continue;

            List<Vector2Int> path = Pathfinder.FindPath(unit.GridPosition, candidate, GridManager.Instance, unit);
            if (path.Count == 0)
                continue;

            if (path.Count < bestPathLength)
            {
                bestPathLength = path.Count;
                result = candidate;
                found = true;
            }
        }

        return found;
    }

    private bool IsCoreInRange()
    {
        var coreCells = GridManager.Instance.MapDefinition.GetPositions(StructureType.Core);
        for (int i = 0; i < coreCells.Count; i++)
        {
            if (IsTargetInBasicRange(coreCells[i]))
                return true;
        }

        return false;
    }

    private bool IsTargetInBasicRange(Vector2Int targetGrid)
    {
        return AttackPatternResolver.IsTargetInRange(
            unit,
            targetGrid,
            AttackActionIds.BasicAttack,
            unit.Stats.AttackRange);
    }

    private int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
