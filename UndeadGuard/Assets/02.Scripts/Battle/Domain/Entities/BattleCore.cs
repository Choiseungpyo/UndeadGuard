/// <summary>
/// 플레이어가 방어해야 하는 핵의 전투 데이터를 관리하는 클래스.
/// 핵의 위치와 체력을 보관하며, 파괴 여부 판정의 기준이 된다.
/// </summary>
public sealed class BattleCore
{
    public GridPosition Position { get; }
    public int MaxHp { get; }
    public int Hp { get; private set; }

    public bool IsDestroyed => Hp <= 0;

    public BattleCore(GridPosition position, int maxHp)
    {
        Position = position;
        MaxHp = maxHp;
        Hp = maxHp;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            damage = 1;
        }

        Hp -= damage;

        if (Hp < 0)
        {
            Hp = 0;
        }
    }
}