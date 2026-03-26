/// <summary>
/// 플레이어가 사용하는 전투 자원 상태를 관리하는 클래스.
/// 현재는 암흑 에너지의 보유량, 획득, 소비를 담당한다.
/// </summary>
public sealed class PlayerResourceState
{
    public int DarkEnergy { get; private set; }

    public PlayerResourceState(int startDarkEnergy)
    {
        DarkEnergy = startDarkEnergy;
    }

    public void AddDarkEnergy(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        DarkEnergy += amount;
    }

    public bool TrySpendDarkEnergy(int amount)
    {
        if (amount < 0)
        {
            return false;
        }

        if (DarkEnergy < amount)
        {
            return false;
        }

        DarkEnergy -= amount;
        return true;
    }
}