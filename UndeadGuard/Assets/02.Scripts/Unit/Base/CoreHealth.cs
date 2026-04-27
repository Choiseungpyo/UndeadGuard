using UnityEngine;

public class CoreHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHp = 100;

    private int currentHp;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => currentHp <= 0;

    private void Awake()
    {
        currentHp = maxHp;
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
        if (currentHp <= 0) return;

        int previousHp = currentHp;
        currentHp = Mathf.Max(0, currentHp - amount);
        int actualDamage = Mathf.Max(0, previousHp - currentHp);

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

        if (currentHp <= 0)
            EventBus.Instance.Publish(new CoreDestroyedEvent());
    }
}
