using UnityEngine;

// 적 진영 유닛의 기반 클래스
// 좀비 등 모든 적 유닛이 이 클래스를 상속받는다
public abstract class EnemyUnit : UnitBase
{
    // 이 적의 종류
    [SerializeField] private EnemyType enemyType;

    // 처치 시 지급하는 암흑 에너지 양
    [SerializeField] private int darkEnergyReward = 1;

    public EnemyType EnemyType => enemyType;

    // 도발 상태 여부
    private bool isTaunted;

    // 도발을 건 유닛 (보통 방패병)
    private UnitBase tauntSource;

    // 남은 도발 지속 횟수
    private int tauntTurnsRemaining;

    public int DarkEnergyReward => darkEnergyReward;

    // 도발 중 여부를 반환한다. 도발원이 사망하면 도발이 해제된다
    public bool IsTaunted => isTaunted && tauntSource != null && !tauntSource.IsDead && tauntTurnsRemaining > 0;

    public UnitBase TauntSource => tauntSource;

    // 도발 상태를 부여한다
    public void SetTaunted(UnitBase source, int durationTurns = 2)
    {
        isTaunted = true;
        tauntSource = source;
        tauntTurnsRemaining = durationTurns;
    }

    // 도발 지속 횟수를 1 감소시킨다. 0이 되면 도발을 해제한다
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

    // 도발을 즉시 해제한다
    public void ClearTaunt()
    {
        isTaunted = false;
        tauntSource = null;
        tauntTurnsRemaining = 0;
    }

    // 적 유닛 사망 시 암흑 에너지 보상 정보를 함께 발행한다
    public override void Die()
    {
        base.Die();

        // WaveManager와 ResourceManager가 이 이벤트를 구독하여 처리한다
        EventBus.Instance.Publish(new EnemyDiedEvent
        {
            Unit = this,
            DarkEnergyReward = darkEnergyReward
        });
    }
}
