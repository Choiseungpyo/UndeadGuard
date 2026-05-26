using UnityEngine;

public abstract class EnemyUnit : UnitBase
{
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private int darkEnergyReward = 1;

    private bool isTaunted;
    private UnitBase tauntSource;
    private int tauntTurnsRemaining;

    public EnemyType EnemyType => enemyType;
    public int DarkEnergyReward => darkEnergyReward;
    public bool IsTaunted => isTaunted && tauntSource != null && !tauntSource.IsDead && tauntTurnsRemaining > 0;
    public UnitBase TauntSource => tauntSource;

    public void SetTaunted(UnitBase source, int durationTurns = 2)
    {
        isTaunted = true;
        tauntSource = source;
        tauntTurnsRemaining = durationTurns;
    }

    public void DecrementTaunt()
    {
        if (!isTaunted) return;

        tauntTurnsRemaining--;
        if (tauntTurnsRemaining <= 0)
        {
            isTaunted = false;
            tauntSource = null;
        }
    }

    public void ClearTaunt()
    {
        isTaunted = false;
        tauntSource = null;
        tauntTurnsRemaining = 0;
    }

    public override void Die()
    {
        Vector3 deathWorldPosition = transform.position;

        base.Die();

        EventBus.Instance.Publish(new EnemyDiedEvent
        {
            Unit = this,
            DarkEnergyReward = darkEnergyReward,
            WorldPosition = deathWorldPosition
        });
    }
}
