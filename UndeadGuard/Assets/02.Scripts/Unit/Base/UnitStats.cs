using UnityEngine;

[System.Serializable]
public class UnitStats
{
    [SerializeField] private int maxHp;
    private int currentHp;

    [SerializeField] private int physicalAttack;
    [SerializeField] private int magicAttack;
    [SerializeField] private int defensePower;
    [SerializeField] private int attackRange;
    [SerializeField] private int moveRange;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public int PhysicalAttack => physicalAttack;
    public int MagicAttack => magicAttack;
    public int DefensePower => defensePower;
    public int AttackRange => attackRange;
    public int MoveRange => moveRange;

    public void Initialize()
    {
        currentHp = maxHp;
    }

    // Damage is reduced by defense, but at least 1 damage is always applied.
    public void ApplyDamage(int damage)
    {
        int actualDamage = Mathf.Max(1, damage - defensePower);
        currentHp = Mathf.Max(0, currentHp - actualDamage);
    }

    public void Heal(int amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    public bool IsEmpty => currentHp <= 0;
}
