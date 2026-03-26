/// <summary>
/// 그리드 한 칸의 현재 상태를 저장하는 클래스.
/// 바닥 타입, 고정 오브젝트, 점유 중인 유닛 정보를 관리한다.
/// </summary>
public sealed class CellData
{
    public GridPosition Position { get; }

    public TileType TileType { get; private set; }
    public CellObjectType ObjectType { get; private set; }

    public int OccupantUnitId { get; private set; }

    public bool IsOccupied => OccupantUnitId >= 0;

    public bool IsWalkable
    {
        get
        {
            if (TileType == TileType.Wall)
            {
                return false;
            }

            if (ObjectType == CellObjectType.DefensePoint)
            {
                return false;
            }

            return true;
        }
    }

    public CellData(GridPosition position, TileType tileType, CellObjectType objectType = CellObjectType.None)
    {
        Position = position;
        TileType = tileType;
        ObjectType = objectType;
        OccupantUnitId = -1;
    }

    public void SetTileType(TileType tileType)
    {
        TileType = tileType;
    }

    public void SetObjectType(CellObjectType objectType)
    {
        ObjectType = objectType;
    }

    public void SetOccupant(int unitId)
    {
        OccupantUnitId = unitId;
    }

    public void ClearOccupant()
    {
        OccupantUnitId = -1;
    }
}