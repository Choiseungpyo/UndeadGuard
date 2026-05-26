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
        if (!TryCreateMovePlan(allUnits, null, out EnemyMovePlan plan))
            yield break;

        yield return StartCoroutine(ExecuteMovePlan(plan));
    }

    public bool TryCreateMovePlan(
        IReadOnlyList<UnitBase> allUnits,
        ISet<Vector2Int> reservedDestinations,
        out EnemyMovePlan plan)
    {
        plan = null;
        if (unit == null || unit.IsDead || allUnits == null)
            return false;

        // Taunt has the highest movement priority: pursue taunt source first.
        if (unit.IsTaunted && unit.TauntSource != null && !unit.TauntSource.IsDead)
        {
            UnitBase tauntTarget = unit.TauntSource;
            if (IsTargetInBasicRange(tauntTarget.GridPosition))
                return false;

            if (TryGetNearestReachableAttackPath(tauntTarget.GridPosition, reservedDestinations, out List<Vector2Int> tauntPath))
            {
                Vector2Int tauntDestination = tauntPath[tauntPath.Count - 1];
                plan = new EnemyMovePlan(
                    this,
                    unit,
                    tauntTarget,
                    tauntTarget.transform,
                    tauntDestination,
                    tauntPath);
                return true;
            }

            return false;
        }

        if (HasAttackableTargetInRange(allUnits))
            return false;

        if (!TryGetClosestMovementTargetPath(
                allUnits,
                reservedDestinations,
                out object targetKey,
                out Transform targetTransform,
                out List<Vector2Int> path))
            return false;

        Vector2Int destination = path[path.Count - 1];
        plan = new EnemyMovePlan(
            this,
            unit,
            targetKey,
            targetTransform,
            destination,
            path);
        return true;
    }

    public IEnumerator ExecuteMovePlan(EnemyMovePlan plan)
    {
        if (plan == null || unit == null || unit.IsDead)
            yield break;

        if (plan.EnemyAI != this || plan.MovingUnit != unit)
            yield break;

        List<Vector2Int> path = plan.Path;
        if (path == null || path.Count <= 1)
            yield break;

        yield return StartCoroutine(unit.MoveAlongPath(path));
    }

    public bool ExecuteAttack(List<UnitBase> allUnits)
    {
        if (unit == null || unit.IsDead)
            return false;

        if (!TryCreateAttackPlan(allUnits, out EnemyAttackPlan plan))
        {
            HandleNoAttackSideEffects();
            return false;
        }

        return ExecuteAttackPlan(plan);
    }

    public void HandleNoAttackSideEffects()
    {
        if (unit == null || unit.IsDead)
            return;

        unit.DecrementTaunt();
    }

    public bool TryCreateAttackPlan(IReadOnlyList<UnitBase> allUnits, out EnemyAttackPlan plan)
    {
        plan = null;
        if (unit == null || unit.IsDead)
            return false;

        if (unit.IsTaunted && unit.TauntSource != null && !unit.TauntSource.IsDead)
        {
            UnitBase tauntTarget = unit.TauntSource;
            if (IsTargetInBasicRange(tauntTarget.GridPosition))
            {
                plan = new EnemyAttackPlan(
                    this,
                    unit,
                    tauntTarget,
                    tauntTarget.transform,
                    isCoreTarget: false,
                    consumeTauntOnSuccess: true);
                return true;
            }
        }

        if (TryGetCoreTargetInRange(out CoreHealth coreTarget))
        {
            plan = new EnemyAttackPlan(
                this,
                unit,
                coreTarget,
                coreTarget.transform,
                isCoreTarget: true,
                consumeTauntOnSuccess: false);
            return true;
        }

        UnitBase attackTarget = FindAttackTargetInRange(allUnits);
        if (attackTarget == null || attackTarget.IsDead)
            return false;

        plan = new EnemyAttackPlan(
            this,
            unit,
            attackTarget,
            attackTarget.transform,
            isCoreTarget: false,
            consumeTauntOnSuccess: true);
        return true;
    }

    public bool ExecuteAttackPlan(EnemyAttackPlan plan)
    {
        if (plan == null || unit == null || unit.IsDead)
            return false;

        if (plan.EnemyAI != this)
            return false;

        if (plan.IsCoreTarget)
        {
            CoreHealth coreTarget = plan.Target as CoreHealth;
            if (coreTarget == null || coreTarget.IsDead)
                return false;

            PerformCoreAttack(coreTarget);
            return true;
        }

        UnitBase targetUnit = plan.Target as UnitBase;
        if (targetUnit == null || targetUnit.IsDead)
        {
            if (plan.ConsumeTauntOnSuccess)
                unit.DecrementTaunt();
            return false;
        }

        unit.PerformAttack(targetUnit);

        if (plan.ConsumeTauntOnSuccess)
            unit.DecrementTaunt();

        return true;
    }

    private bool HasAttackableTargetInRange(IReadOnlyList<UnitBase> allUnits)
    {
        if (unit == null)
            return false;

        if (unit.IsTaunted && unit.TauntSource != null && !unit.TauntSource.IsDead)
        {
            if (IsTargetInBasicRange(unit.TauntSource.GridPosition))
                return true;
        }

        if (TryGetCoreTargetInRange(out _))
            return true;

        if (FindAttackTargetInRange(allUnits) != null)
            return true;

        return false;
    }

    private bool TryGetClosestMovementTargetPosition(List<UnitBase> allUnits, out Vector2Int result)
    {
        result = unit.GridPosition;
        int bestPathLength = int.MaxValue;
        bool bestIsCore = false;
        bool found = false;

        for (int i = 0; i < allUnits.Count; i++)
        {
            UnitBase candidateUnit = allUnits[i];
            if (candidateUnit == null || candidateUnit.Team != TeamType.Undead || candidateUnit.IsDead)
                continue;

            if (!TryGetNearestReachableAttackPosition(candidateUnit.GridPosition, out Vector2Int attackPos))
                continue;

            List<Vector2Int> path = Pathfinder.FindPath(unit.GridPosition, attackPos, GridManager.Instance, unit);
            if (path.Count == 0)
                continue;

            bool isBetter = path.Count < bestPathLength;
            if (!isBetter)
                continue;

            bestPathLength = path.Count;
            bestIsCore = false;
            result = attackPos;
            found = true;
        }

        var coreCells = GridManager.Instance.MapDefinition.GetPositions(StructureType.Core);
        for (int i = 0; i < coreCells.Count; i++)
        {
            Vector2Int coreCell = coreCells[i];
            if (!TryGetNearestReachableAttackPosition(coreCell, out Vector2Int attackPos))
                continue;

            List<Vector2Int> path = Pathfinder.FindPath(unit.GridPosition, attackPos, GridManager.Instance, unit);
            if (path.Count == 0)
                continue;

            bool isBetter = path.Count < bestPathLength
                || (path.Count == bestPathLength && !bestIsCore);
            if (!isBetter)
                continue;

            bestPathLength = path.Count;
            bestIsCore = true;
            result = attackPos;
            found = true;
        }

        return found;
    }

    private UnitBase FindAttackTargetInRange(IReadOnlyList<UnitBase> allUnits)
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

    private void PerformCoreAttack(CoreHealth coreTarget)
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
        AttackEffectService.Play(unit, effectTarget, UnitActionIds.DefaultAction);

        if (coreTarget != null && !coreTarget.IsDead)
            coreTarget.TakeDamage(unit.Stats.PhysicalAttack, unit != null ? unit.name : null);
    }

    private bool TryGetNearestReachableAttackPosition(Vector2Int targetPos, out Vector2Int result)
    {
        result = unit.GridPosition;

        List<Vector2Int> candidates = AttackPatternResolver.GetAttackOriginCandidates(
            unit,
            targetPos,
            UnitActionIds.DefaultAction,
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

    private bool TryGetClosestMovementTargetPath(
        IReadOnlyList<UnitBase> allUnits,
        ISet<Vector2Int> reservedDestinations,
        out object targetKey,
        out Transform targetTransform,
        out List<Vector2Int> path)
    {
        targetKey = null;
        targetTransform = null;
        path = null;
        int bestTargetDistance = int.MaxValue;
        bool bestIsCore = false;
        for (int i = 0; i < allUnits.Count; i++)
        {
            UnitBase candidateUnit = allUnits[i];
            if (candidateUnit == null || candidateUnit.Team != TeamType.Undead || candidateUnit.IsDead)
                continue;

            if (!TryGetNearestReachableAttackPath(
                    candidateUnit.GridPosition,
                    reservedDestinations,
                    out List<Vector2Int> candidatePath,
                    out int candidateDistance))
                continue;

            bool isBetter = candidateDistance < bestTargetDistance;
            if (!isBetter)
                continue;

            bestTargetDistance = candidateDistance;
            bestIsCore = false;
            path = candidatePath;
            targetKey = candidateUnit;
            targetTransform = candidateUnit.transform;
        }

        CoreHealth coreTarget = CoreHealth.Instance;
        if (coreTarget != null && !coreTarget.IsDead)
        {
            var coreCells = GridManager.Instance.MapDefinition.GetPositions(StructureType.Core);
            for (int i = 0; i < coreCells.Count; i++)
            {
                if (!TryGetNearestReachableAttackPath(
                        coreCells[i],
                        reservedDestinations,
                        out List<Vector2Int> candidatePath,
                        out int candidateDistance))
                    continue;

                bool isBetter = candidateDistance < bestTargetDistance
                    || (candidateDistance == bestTargetDistance && !bestIsCore);
                if (!isBetter)
                    continue;

                bestTargetDistance = candidateDistance;
                bestIsCore = true;
                path = candidatePath;
                targetKey = coreTarget;
                targetTransform = coreTarget.transform;
            }
        }

        return path != null && path.Count > 1;
    }

    private bool TryGetNearestReachableAttackPath(
        Vector2Int targetPos,
        ISet<Vector2Int> reservedDestinations,
        out List<Vector2Int> movePath)
    {
        return TryGetNearestReachableAttackPath(targetPos, reservedDestinations, out movePath, out _);
    }

    private bool TryGetNearestReachableAttackPath(
        Vector2Int targetPos,
        ISet<Vector2Int> reservedDestinations,
        out List<Vector2Int> movePath,
        out int totalPathLength)
    {
        movePath = null;
        totalPathLength = int.MaxValue;

        List<Vector2Int> candidates = AttackPatternResolver.GetAttackOriginCandidates(
            unit,
            targetPos,
            UnitActionIds.DefaultAction,
            unit.Stats.AttackRange);

        if (candidates.Count == 0)
            return false;

        int bestProgressDistance = int.MaxValue;
        int bestPathLength = int.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2Int candidate = candidates[i];
            List<Vector2Int> fullPath;

            if (candidate == unit.GridPosition)
            {
                fullPath = new List<Vector2Int> { unit.GridPosition };
            }
            else
            {
                if (!GridManager.Instance.IsWalkableIgnoring(candidate, unit))
                    continue;

                fullPath = Pathfinder.FindPath(unit.GridPosition, candidate, GridManager.Instance, unit);
                if (fullPath.Count == 0)
                    continue;
            }

            int steps = Mathf.Min(unit.Stats.MoveRange, fullPath.Count - 1);
            if (steps <= 0)
                continue;

            Vector2Int destination = fullPath[steps];
            if (reservedDestinations != null && reservedDestinations.Contains(destination))
                continue;

            int progressDistance = Manhattan(destination, targetPos);
            if (progressDistance < bestProgressDistance
                || (progressDistance == bestProgressDistance && fullPath.Count < bestPathLength))
            {
                bestProgressDistance = progressDistance;
                bestPathLength = fullPath.Count;
                movePath = fullPath.GetRange(0, steps + 1);
                totalPathLength = fullPath.Count;
            }
        }

        return movePath != null && movePath.Count > 1;
    }

    private bool TryGetCoreTargetInRange(out CoreHealth coreTarget)
    {
        coreTarget = null;
        CoreHealth foundCore = CoreHealth.Instance;
        if (foundCore == null || foundCore.IsDead)
            return false;

        var coreCells = GridManager.Instance.MapDefinition.GetPositions(StructureType.Core);
        for (int i = 0; i < coreCells.Count; i++)
        {
            if (IsTargetInBasicRange(coreCells[i]))
            {
                coreTarget = foundCore;
                return true;
            }
        }

        return false;
    }

    private bool IsTargetInBasicRange(Vector2Int targetGrid)
    {
        return AttackPatternResolver.IsTargetInRange(
            unit,
            targetGrid,
            UnitActionIds.DefaultAction,
            unit.Stats.AttackRange);
    }

    private int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
