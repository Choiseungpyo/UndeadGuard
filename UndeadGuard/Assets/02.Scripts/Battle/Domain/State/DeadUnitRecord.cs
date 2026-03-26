/// <summary>
/// 소멸한 플레이어 유닛의 정보를 저장하는 클래스.
/// 사망 위치와 부활 비용을 함께 보관하여 부활 시스템의 기준으로 사용한다.
/// </summary>
public sealed class DeadUnitRecord
{
    public BattleUnit Unit { get; }
    public GridPosition DeathPosition { get; }
    public int ResurrectCost { get; }

    public DeadUnitRecord(BattleUnit unit, GridPosition deathPosition, int resurrectCost)
    {
        Unit = unit;
        DeathPosition = deathPosition;
        ResurrectCost = resurrectCost;
    }
}