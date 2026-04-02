using UnityEngine;

// 암흑 에너지와 사령 포인트를 통합 관리한다
// 암흑 에너지는 적 처치 시 획득하고 부활 및 강화에 사용한다
// 사령 포인트는 플레이어 턴 시작 시 지급되며 사령 명령 사용 시 소모한다
public class ResourceManager : Singleton<ResourceManager>
{
    [SerializeField] private int initialDarkEnergy = 5;
    [SerializeField] private int commandPointsPerTurn = 3;
    [SerializeField] private int maxCommandPoints = 5;

    private int darkEnergy;
    private int commandPoints;

    public int DarkEnergy => darkEnergy;
    public int CommandPoints => commandPoints;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void Start()
    {
        darkEnergy = initialDarkEnergy;
        commandPoints = commandPointsPerTurn;
        PublishChange();
    }

    // 적 처치 시 암흑 에너지를 획득한다
    private void OnEnemyDied(EnemyDiedEvent e)
    {
        AddDarkEnergy(e.DarkEnergyReward);
    }

    // 플레이어 턴 시작 시 사령 포인트를 지급한다
    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (e.CurrentTurn != TurnType.Player) return;

        commandPoints = Mathf.Min(commandPoints + commandPointsPerTurn, maxCommandPoints);
        PublishChange();
    }

    // 암흑 에너지를 추가한다
    public void AddDarkEnergy(int amount)
    {
        darkEnergy += amount;
        PublishChange();
    }

    // 암흑 에너지를 소모한다. 성공 여부를 반환한다
    public bool TrySpendDarkEnergy(int amount)
    {
        if (darkEnergy < amount) return false;

        darkEnergy -= amount;
        PublishChange();
        return true;
    }

    // 사령 포인트를 소모한다. 성공 여부를 반환한다
    public bool TrySpendCommandPoints(int amount)
    {
        if (commandPoints < amount) return false;

        commandPoints -= amount;
        PublishChange();
        return true;
    }

    // 웨이브 보상으로 암흑 에너지를 추가한다
    public void AddWaveReward(int amount)
    {
        AddDarkEnergy(amount);
    }

    private void PublishChange()
    {
        EventBus.Instance.Publish(new ResourceChangedEvent
        {
            DarkEnergy = darkEnergy,
            CommandPoint = commandPoints
        });
    }
}
