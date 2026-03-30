/// <summary>
/// 그리드 한 칸의 현재 상태를 저장하는 클래스.
/// 셀 자체의 정보와 점유 중인 유닛 정보를 관리한다.
/// 구조물의 길막 여부는 BattleState의 구조물 상태에서 판단한다.
/// </summary>
public sealed class CellData
{
    public GridPosition Position { get; }

    public StructureType ObjectType { get; private set; }

    public int OccupantUnitId { get; private set; }

    public bool IsOccupied => OccupantUnitId >= 0;

    public bool IsWalkable => true;

    public CellData(GridPosition position, StructureType objectType = StructureType.None)
    {
        Position = position;
        ObjectType = objectType;
        OccupantUnitId = -1;
    }

    public void SetObjectType(StructureType objectType)
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