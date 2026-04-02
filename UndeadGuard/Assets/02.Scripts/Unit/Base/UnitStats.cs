using UnityEngine;

// 유닛의 수치 데이터를 담는 클래스
// SerializeField를 통해 인스펙터에서 초기값을 설정할 수 있다
[System.Serializable]
public class UnitStats
{
    // 최대 체력
    [SerializeField] private int maxHp;

    // 현재 체력
    private int currentHp;

    // 물리 공격력
    [SerializeField] private int physicalAttack;

    // 마법 공격력
    [SerializeField] private int magicAttack;

    // 방어력
    [SerializeField] private int defensePower;

    // 공격 사거리 (타일 단위)
    [SerializeField] private int attackRange;

    // 이동 범위 (타일 단위)
    [SerializeField] private int moveRange;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public int PhysicalAttack => physicalAttack;
    public int MagicAttack => magicAttack;
    public int DefensePower => defensePower;
    public int AttackRange => attackRange;
    public int MoveRange => moveRange;

    // 최대 체력으로 초기화한다
    public void Initialize()
    {
        currentHp = maxHp;
    }

    // 피해를 받아 현재 체력을 감소시킨다. 0 미만으로 내려가지 않는다
    public void ApplyDamage(int damage)
    {
        int actualDamage = Mathf.Max(0, damage - defensePower);
        currentHp = Mathf.Max(0, currentHp - actualDamage);
    }

    // 체력을 지정한 수치만큼 회복한다. 최대 체력을 초과하지 않는다
    public void Heal(int amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    // 체력이 0인지 확인한다
    public bool IsEmpty => currentHp <= 0;
}
