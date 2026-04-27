using UnityEngine;

// 그리드 좌표와 월드 좌표 사이의 변환을 담당하는 관리자
// MapSceneBuilder와 동일한 타일 간격 4f를 기준으로 한다
public class GridManager : Singleton<GridManager>
{
    [SerializeField] private MapDefinition mapDefinition;

    public const float TileSpacing = 4f;

    public MapDefinition MapDefinition => mapDefinition;

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / TileSpacing),
            Mathf.FloorToInt(worldPos.z / TileSpacing));
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            (gridPos.x + 0.5f) * TileSpacing,
            0f,
            (gridPos.y + 0.5f) * TileSpacing);
    }

    // 해당 그리드 좌표가 이동 가능한 칸인지 확인한다
    public bool IsWalkable(Vector2Int pos)
    {
        if (!IsInBounds(pos))
            return false;

        var cell = mapDefinition.GetCell(pos.x, pos.y);

        // 코어 칸과 벽 칸은 이동 불가
        if (cell.objectType == StructureType.Wall || cell.objectType == StructureType.Core)
            return false;

        // 이미 다른 유닛이 서 있는 칸도 이동 불가
        if (IsOccupied(pos))
            return false;

        return true;
    }

    // 해당 칸에 살아있는 유닛이 있는지 확인한다
    public bool IsOccupied(Vector2Int pos)
    {
        return IsOccupiedIgnoring(pos, null);
    }

    // ignore를 제외하고 해당 칸에 살아있는 유닛이 있는지 확인한다
    public bool IsOccupiedIgnoring(Vector2Int pos, UnitBase ignore)
    {
        foreach (UnitBase unit in UnitRegistry.Instance.GetAllUnits())
        {
            if (unit == null || unit.IsDead || unit == ignore) continue;
            if (unit.GridPosition == pos) return true;
        }
        return false;
    }

    // ignore를 제외하고 이동 가능한 칸인지 확인한다
    public bool IsWalkableIgnoring(Vector2Int pos, UnitBase ignore)
    {
        if (!IsInBounds(pos)) return false;

        var cell = mapDefinition.GetCell(pos.x, pos.y);
        if (cell.objectType == StructureType.Wall || cell.objectType == StructureType.Core)
            return false;

        return !IsOccupiedIgnoring(pos, ignore);
    }

    public bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < mapDefinition.Width
            && pos.y >= 0 && pos.y < mapDefinition.Height;
    }
}