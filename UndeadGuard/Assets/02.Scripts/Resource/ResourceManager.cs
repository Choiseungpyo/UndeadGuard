using UnityEngine;

public class ResourceManager : Singleton<ResourceManager>
{
    [SerializeField] private int initialDarkEnergy = 5;
    [SerializeField] private int commandPointsPerTurn = 3;
    [SerializeField] private int maxCommandPoints = 5;

    private int darkEnergy;
    private int commandPoints;
    private int totalSpentDarkEnergy;

    public int DarkEnergy => darkEnergy;
    public int CommandPoints => commandPoints;
    public int TotalSpentDarkEnergy => totalSpentDarkEnergy;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
    }

    private void Start()
    {
        darkEnergy = initialDarkEnergy;
        commandPoints = commandPointsPerTurn;
        totalSpentDarkEnergy = 0;
        PublishChange();
    }

    private void OnTurnChanged(TurnChangedEvent e)
    {
        if (e.CurrentTurn != TurnType.Player)
            return;

        commandPoints = Mathf.Min(commandPoints + commandPointsPerTurn, maxCommandPoints);
        PublishChange();
    }

    public void AddDarkEnergy(int amount)
    {
        darkEnergy += amount;
        PublishChange();
    }

    public bool TrySpendDarkEnergy(int amount)
    {
        if (darkEnergy < amount)
            return false;

        darkEnergy -= amount;
        totalSpentDarkEnergy += Mathf.Max(0, amount);
        PublishChange();
        return true;
    }

    public bool TrySpendCommandPoints(int amount)
    {
        if (commandPoints < amount)
            return false;

        commandPoints -= amount;
        PublishChange();
        return true;
    }

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
