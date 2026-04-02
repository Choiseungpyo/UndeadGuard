using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 적 유닛의 AI를 담당한다
// 타겟 우선순위: 도발 중인 방패병 > 언데드 핵 > 가장 가까운 언데드
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float tileMoveDuration = 0.2f;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private EnemyUnit unit;

    private void Awake()
    {
        unit = GetComponent<EnemyUnit>();
    }

    // 이 적이 이번 턴 목표로 삼을 그리드 위치를 반환한다
    // 대상 칸 자체가 아니라 공격 사거리가 닿는 인접 칸을 반환한다
    public Vector2Int GetTargetGridPosition(List<UnitBase> allUnits)
    {
        if (unit == null)
            return Vector2Int.zero;

        // 도발 중인 방패병이 공격 범위 안에 있으면 제자리 유지
        if (unit.IsTaunted && unit.TauntSource != null && !unit.TauntSource.IsDead)
        {
            if (Manhattan(unit.GridPosition, unit.TauntSource.GridPosition) <= unit.Stats.AttackRange)
                return unit.GridPosition;

            if (TryGetNearestReachableAttackPosition(unit.TauntSource.GridPosition, out var tauntAttackPos))
                return tauntAttackPos;
        }

        // 이미 코어 공격 가능 위치에 있으면 제자리 유지
        if (IsCoreInRange())
            return unit.GridPosition;

        // 코어를 공격할 수 있는 칸 찾기
        if (GridManager.Instance.MapDefinition.TryGetCorePosition(out var corePos))
        {
            if (TryGetNearestReachableAttackPosition(corePos, out var coreAttackPos))
                return coreAttackPos;
        }

        // 가장 가까운 언데드 유닛을 공격할 수 있는 칸 찾기
        UnitBase nearestUndead = GetNearestUndeadUnit(allUnits);
        if (nearestUndead != null)
        {
            if (Manhattan(unit.GridPosition, nearestUndead.GridPosition) <= unit.Stats.AttackRange)
                return unit.GridPosition;

            if (TryGetNearestReachableAttackPosition(nearestUndead.GridPosition, out var unitAttackPos))
                return unitAttackPos;
        }

        return unit.GridPosition;
    }

    // 이동과 공격을 포함한 이번 턴 행동을 실행한다
    public IEnumerator ExecuteTurn(List<UnitBase> allUnits)
    {
        if (unit == null || unit.IsDead)
            yield break;

        // 1. 도발 중인 방패병이 공격 범위 안에 있으면 먼저 공격
        if (unit.IsTaunted && unit.TauntSource != null && !unit.TauntSource.IsDead)
        {
            if (Manhattan(unit.GridPosition, unit.TauntSource.GridPosition) <= unit.Stats.AttackRange)
            {
                PerformAttack(unit.TauntSource);
                unit.DecrementTaunt();
                yield break;
            }
        }

        // 2. 공격 가능한 언데드가 있으면 먼저 공격
        UnitBase attackTarget = FindAttackTargetInRange(allUnits);
        if (attackTarget != null)
        {
            PerformAttack(attackTarget);
            yield break;
        }

        // 3. 코어가 공격 범위면 바로 공격
        if (IsCoreInRange())
        {
            PerformCoreAttack();
            yield break;
        }

        // 4. 공격 가능한 위치로 이동
        Vector2Int targetPos = GetTargetGridPosition(allUnits);

        if (targetPos != unit.GridPosition)
        {
            List<Vector2Int> path = Pathfinder.FindPath(unit.GridPosition, targetPos, GridManager.Instance);

            if (path.Count > 1)
            {
                int steps = Mathf.Min(unit.Stats.MoveRange, path.Count - 1);
                List<Vector2Int> movePath = path.GetRange(0, steps + 1);
                yield return StartCoroutine(MoveAlongPath(movePath));
            }
        }

        // 5. 이동 후 도발 대상 공격 체크
        if (unit.IsTaunted && unit.TauntSource != null && !unit.TauntSource.IsDead)
        {
            if (Manhattan(unit.GridPosition, unit.TauntSource.GridPosition) <= unit.Stats.AttackRange)
            {
                PerformAttack(unit.TauntSource);
                unit.DecrementTaunt();
                yield break;
            }
        }

        // 6. 이동 후 다시 언데드 공격 체크
        attackTarget = FindAttackTargetInRange(allUnits);
        if (attackTarget != null)
        {
            PerformAttack(attackTarget);
            yield break;
        }

        // 7. 이동 후 코어 공격 체크
        if (IsCoreInRange())
        {
            PerformCoreAttack();
        }

        unit.DecrementTaunt();
    }

    // 공격 범위 내 언데드 유닛을 찾는다
    // 우선순위: 도발 중인 방패병 > 가장 가까운 언데드
    private UnitBase FindAttackTargetInRange(List<UnitBase> allUnits)
    {
        UnitBase nearest = null;
        int minDist = int.MaxValue;

        foreach (UnitBase u in allUnits)
        {
            if (u == null || u.Team != TeamType.Undead || u.IsDead)
                continue;

            int dist = Manhattan(unit.GridPosition, u.GridPosition);
            if (dist <= unit.Stats.AttackRange && dist < minDist)
            {
                minDist = dist;
                nearest = u;
            }
        }

        return nearest;
    }

    private void PerformAttack(UnitBase target)
    {
        unit.UnitAnimator?.TriggerAttack();
        target.TakeDamage(unit.Stats.PhysicalAttack);

        EventBus.Instance.Publish(new UnitAttackedEvent
        {
            Attacker = unit,
            Target = target,
            Damage = unit.Stats.PhysicalAttack
        });
    }

    private void PerformCoreAttack()
    {
        unit.UnitAnimator?.TriggerAttack();

        CoreHealth coreHealth = FindFirstObjectByType<CoreHealth>();
        if (coreHealth != null)
        {
            coreHealth.TakeDamage(unit.Stats.PhysicalAttack);
        }
    }

    private UnitBase GetNearestUndeadUnit(List<UnitBase> allUnits)
    {
        UnitBase nearest = null;
        int minDist = int.MaxValue;

        foreach (UnitBase u in allUnits)
        {
            if (u == null || u.Team != TeamType.Undead || u.IsDead)
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

    // 대상 칸에서 공격 사거리 안에 있는 칸 중 실제로 갈 수 있는 가장 가까운 칸을 찾는다
    // 공격 사거리가 1보다 크면 사거리 내 모든 칸을 후보로 고려한다
    private bool TryGetNearestReachableAttackPosition(Vector2Int targetPos, out Vector2Int result)
    {
        result = unit.GridPosition;

        int attackRange = unit.Stats.AttackRange;
        List<Vector2Int> candidates = new List<Vector2Int>();

        // 공격 사거리 내 모든 칸을 후보로 수집한다 (타겟 칸 제외)
        for (int dx = -attackRange; dx <= attackRange; dx++)
        {
            for (int dy = -attackRange; dy <= attackRange; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > attackRange) continue;
                if (dx == 0 && dy == 0) continue;

                Vector2Int candidate = targetPos + new Vector2Int(dx, dy);

                // 현재 내 위치는 점유 중이어도 후보로 인정한다
                if (candidate == unit.GridPosition)
                {
                    result = candidate;
                    return true;
                }

                if (!GridManager.Instance.IsWalkable(candidate)) continue;

                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
            return false;

        int bestPathLength = int.MaxValue;
        bool found = false;

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2Int candidate = candidates[i];
            List<Vector2Int> path = Pathfinder.FindPath(unit.GridPosition, candidate, GridManager.Instance);

            if (path.Count == 0) continue;

            if (path.Count < bestPathLength)
            {
                bestPathLength = path.Count;
                result = candidate;
                found = true;
            }
        }

        return found;
    }

    private IEnumerator MoveAlongPath(List<Vector2Int> path)
    {
        unit.UnitAnimator?.SetWalking(true);

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int targetPos = path[i];
            Vector3 startWorldPos = unit.transform.position;
            Vector3 gridCenter = GridManager.Instance.GridToWorld(targetPos);
            Vector3 endWorldPos = new Vector3(gridCenter.x, unit.transform.position.y, gridCenter.z);

            float elapsed = 0f;
            while (elapsed < tileMoveDuration)
            {
                elapsed += Time.deltaTime;
                unit.transform.position = Vector3.Lerp(
                    startWorldPos,
                    endWorldPos,
                    Mathf.Clamp01(elapsed / tileMoveDuration));
                yield return null;
            }

            unit.transform.position = endWorldPos;
            unit.SetGridPosition(targetPos);
        }

        unit.UnitAnimator?.SetWalking(false);
    }

    private bool IsCoreInRange()
    {
        if (!GridManager.Instance.MapDefinition.TryGetCorePosition(out var corePos))
            return false;

        return Manhattan(unit.GridPosition, corePos) <= unit.Stats.AttackRange;
    }

    private int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
