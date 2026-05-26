using System.Collections.Generic;
using UnityEngine;

public interface IUnitAction
{
    string ActionId { get; }
    string DisplayName { get; }
    string Description { get; }

    bool CanUse();
    IReadOnlyCollection<Vector2Int> GetTargetTiles();
    bool CanTarget(Vector2Int targetPosition);
    UnitBase GetPrimaryTarget(Vector2Int targetPosition);
    List<Transform> GetCameraTargets(Vector2Int targetPosition);
    void Execute(Vector2Int targetPosition);
}
