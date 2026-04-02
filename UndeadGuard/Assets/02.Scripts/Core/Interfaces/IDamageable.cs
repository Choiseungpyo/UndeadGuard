// 피해를 받을 수 있는 객체가 구현해야 하는 인터페이스
public interface IDamageable
{
    // 피해량을 받아 체력을 감소시킨다
    void TakeDamage(int amount);

    // 현재 살아있는지 여부를 반환한다
    bool IsDead { get; }
}
