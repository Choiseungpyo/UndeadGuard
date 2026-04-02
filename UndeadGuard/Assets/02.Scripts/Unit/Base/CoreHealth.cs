using UnityEngine;

// 언데드 핵의 체력을 관리한다
// 핵의 체력이 0이 되면 게임 오버 이벤트를 발행한다
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

    // 피해를 받아 핵 체력을 감소시킨다
    public void TakeDamage(int amount)
    {
        if (currentHp <= 0) return;

        currentHp = Mathf.Max(0, currentHp - amount);

        EventBus.Instance.Publish(new CoreHealthChangedEvent
        {
            CurrentHp = currentHp,
            MaxHp = maxHp
        });

        if (currentHp <= 0)
        {
            EventBus.Instance.Publish(new CoreDestroyedEvent());
        }
    }
}
