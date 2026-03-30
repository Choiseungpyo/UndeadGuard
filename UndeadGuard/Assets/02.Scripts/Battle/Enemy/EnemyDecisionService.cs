using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyDecisionService
{
    private readonly BattleState state;
    private readonly GridSearchService gridSearchService;

    public EnemyDecisionService(BattleState state, GridSearchService gridSearchService)
    {
        this.state = state;
        this.gridSearchService = gridSearchService;
    }

    public bool TryGetMoveDecision(int unitId, out EnemyMoveDecision decision)
    {
        decision = default;

        if (!state.TryGetUnit(unitId, out BattleUnit enemyUnit))
        {
            return false;
        }

        HashSet<GridPosition> reachablePositions = gridSearchService.GetReachablePositions(state, unitId);
        if (reachablePositions == null || reachablePositions.Count == 0)
        {
            return false;
        }

        BattleUnit nearestPlayer = null;
        int nearestDistance = int.MaxValue;

        foreach (BattleUnit playerUnit in state.GetAliveUnits(TeamType.Player))
        {
            int distance = Mathf.Abs(enemyUnit.Position.x - playerUnit.Position.x)
                         + Mathf.Abs(enemyUnit.Position.z - playerUnit.Position.z);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlayer = playerUnit;
            }
        }

        if (nearestPlayer == null)
        {
            return false;
        }

        GridPosition bestPosition = enemyUnit.Position;
        int bestDistance = nearestDistance;

        foreach (GridPosition candidate in reachablePositions)
        {
            if (candidate.Equals(enemyUnit.Position))
            {
                continue;
            }

            int distance = Mathf.Abs(candidate.x - nearestPlayer.Position.x)
                         + Mathf.Abs(candidate.z - nearestPlayer.Position.z);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPosition = candidate;
            }
        }

        if (bestPosition.Equals(enemyUnit.Position))
        {
            return false;
        }

        List<GridPosition> path = gridSearchService.FindPath(state, enemyUnit.Position, bestPosition);
        if (path == null || path.Count == 0)
        {
            return false;
        }

        decision = new EnemyMoveDecision
        {
            destination = bestPosition,
            reachablePositions = reachablePositions,
            path = path
        };

        return true;
    }

    private int GetManhattanDistance(GridPosition a, GridPosition b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }
}

public struct EnemyMoveDecision
{
    public GridPosition destination;
    public HashSet<GridPosition> reachablePositions;
    public IReadOnlyList<GridPosition> path;
}