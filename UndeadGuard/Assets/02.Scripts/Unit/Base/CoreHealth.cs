using UnityEngine;

public class CoreHealth : Singleton<CoreHealth>, IDamageable
{
    [SerializeField] private int maxHp = 100;

    private int currentHp;
    private bool isDestroyed;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => currentHp <= 0;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        currentHp = maxHp;
        isDestroyed = false;
    }

    private void Start()
    {
        EventBus.Instance.Publish(new CoreHealthChangedEvent
        {
            CurrentHp = currentHp,
            MaxHp = maxHp
        });
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(int amount, string attackerName)
    {
        if (isDestroyed || currentHp <= 0) return;

        int previousHp = currentHp;
        currentHp = Mathf.Max(0, currentHp - amount);
        int actualDamage = Mathf.Max(0, previousHp - currentHp);

        if (actualDamage > 0)
        {
            EventBus.Instance.Publish(new CoreDamagedEvent
            {
                Core = this,
                DamageAmount = actualDamage,
                CurrentHp = currentHp,
                MaxHp = maxHp,
                AttackerName = string.IsNullOrWhiteSpace(attackerName) ? "알 수 없음" : attackerName
            });
        }

        EventBus.Instance.Publish(new DamageTakenEvent
        {
            Target = this,
            TargetBehaviour = this,
            Damage = actualDamage,
            CurrentHp = currentHp,
            MaxHp = maxHp
        });

        EventBus.Instance.Publish(new CoreHealthChangedEvent
        {
            CurrentHp = currentHp,
            MaxHp = maxHp
        });

        if (currentHp <= 0 && !isDestroyed)
        {
            isDestroyed = true;
            EventBus.Instance.Publish(new CoreDestroyedEvent
            {
                Core = this,
                CoreBehaviour = this,
                WorldPosition = transform.position
            });
        }
    }
}
