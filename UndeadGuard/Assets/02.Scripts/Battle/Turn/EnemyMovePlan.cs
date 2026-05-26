using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyMovePlan
{
    public EnemyMovePlan(
        EnemyAI enemyAI,
        UnitBase movingUnit,
        object targetKey,
        Transform targetTransform,
        Vector2Int destination,
        List<Vector2Int> path)
    {
        EnemyAI = enemyAI;
        MovingUnit = movingUnit;
        TargetKey = targetKey;
        TargetTransform = targetTransform;
        Destination = destination;
        Path = path;
    }

    public EnemyAI EnemyAI { get; }
    public UnitBase MovingUnit { get; }
    public object TargetKey { get; }
    public Transform TargetTransform { get; }
    public Vector2Int Destination { get; }
    public List<Vector2Int> Path { get; }
}
