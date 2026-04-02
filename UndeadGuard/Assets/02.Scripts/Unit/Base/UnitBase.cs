using UnityEngine;

// 모든 유닛의 공통 기반 클래스
// 언데드 유닛과 적 유닛 모두 이 클래스를 상속받는다
public abstract class UnitBase : MonoBehaviour, IUnit, IDamageable
{
    [SerializeField] private TeamType team;
    [SerializeField] private UnitStats stats;

    // 애니메이션 제어 컴포넌트
    private UnitAnimator unitAnimator;

    // 현재 그리드 위치
    private Vector2Int gridPosition;

    // 이번 턴 이동 완료 여부
    private bool hasMoved;

    // 이번 턴 행동 완료 여부
    private bool hasActed;

    // 사망 여부
    private bool isDead;

    // 풀 스폰으로 초기화 완료 여부. true이면 Start()의 위치 스냅을 건너뛴다
    private bool spawnInitialized;

    public TeamType Team => team;
    public Vector2Int GridPosition => gridPosition;
    public bool HasMoved => hasMoved;
    public bool HasActed => hasActed;
    public bool IsDead => isDead;
    public UnitStats Stats => stats;

    public UnitAnimator UnitAnimator => unitAnimator;

    protected virtual void Awake()
    {
        stats.Initialize();
        unitAnimator = GetComponent<UnitAnimator>();
    }

    protected virtual void Start()
    {
        // 풀 스폰으로 이미 초기화된 경우 위치 스냅을 건너뛴다
        if (spawnInitialized) return;
        if (GridManager.Instance == null) return;

        // 씬에 배치된 위치를 기준으로 그리드 좌표를 계산하고
        // X, Z를 타일 중앙으로 스냅한다. Y는 씬에 설정된 높이를 유지한다
        var pos = GridManager.Instance.WorldToGrid(transform.position);
        gridPosition = pos;
        var center = GridManager.Instance.GridToWorld(pos);
        transform.position = new Vector3(center.x, transform.position.y, center.z);
    }

    // 그리드 위치를 설정한다
    public void SetGridPosition(Vector2Int position)
    {
        gridPosition = position;
    }

    // 피해를 받아 체력을 감소시키고 사망 처리를 수행한다
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        stats.ApplyDamage(amount);
        unitAnimator?.TriggerHit();

        EventBus.Instance.Publish(new UnitAttackedEvent
        {
            Attacker = null,
            Target = this,
            Damage = amount
        });

        if (stats.IsEmpty)
            Die();
    }

    // 사망 처리를 수행한다
    public virtual void Die()
    {
        isDead = true;
        unitAnimator?.TriggerDie();

        EventBus.Instance.Publish(new UnitDiedEvent
        {
            Unit = this,
            Position = gridPosition
        });

        gameObject.SetActive(false);
    }

    // 이동 완료 상태로 표시한다
    public void MarkAsMoved()
    {
        hasMoved = true;
    }

    // 행동 완료 상태로 표시한다
    public void MarkAsActed()
    {
        hasActed = true;
    }

    // 턴 시작 시 이동 및 행동 상태를 초기화한다
    public void ResetTurnState()
    {
        hasMoved = false;
        hasActed = false;
    }

    // 부활 처리를 수행한다. 체력을 초기화하고 지정 위치에 다시 배치한다
    public void Revive(Vector2Int position)
    {
        isDead = false;
        gridPosition = position;
        stats.Initialize();
        gameObject.SetActive(true);
        unitAnimator?.TriggerRevive();

        EventBus.Instance.Publish(new UnitRevivedEvent
        {
            Unit = this,
            Position = position
        });
    }

    // 풀에서 꺼낼 때 유닛 상태를 초기화한다
    // 이벤트 없이 조용히 리셋하며 Start()의 위치 스냅 중복 실행을 막는다
    public void PrepareForSpawn(Vector2Int position, Vector3 worldPos)
    {
        spawnInitialized = true;
        isDead = false;
        gridPosition = position;
        hasMoved = false;
        hasActed = false;
        stats.Initialize();
        transform.position = new Vector3(worldPos.x, transform.position.y, worldPos.z);
    }
}
