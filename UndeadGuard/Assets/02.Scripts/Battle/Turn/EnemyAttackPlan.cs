using UnityEngine;

public sealed class EnemyAttackPlan
{
    public EnemyAttackPlan(
        EnemyAI enemyAI,
        UnitBase attackerUnit,
        IDamageable target,
        Transform targetTransform,
        bool isCoreTarget,
        bool consumeTauntOnSuccess)
    {
        EnemyAI = enemyAI;
        AttackerUnit = attackerUnit;
        Target = target;
        TargetTransform = targetTransform;
        IsCoreTarget = isCoreTarget;
        ConsumeTauntOnSuccess = consumeTauntOnSuccess;
    }

    public EnemyAI EnemyAI { get; }
    public UnitBase AttackerUnit { get; }
    public IDamageable Target { get; }
    public Transform TargetTransform { get; }
    public bool IsCoreTarget { get; }
    public bool ConsumeTauntOnSuccess { get; }
}
