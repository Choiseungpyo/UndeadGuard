using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// 씬 내 모든 유닛을 관리하고 프리팹 기반 오브젝트 풀을 제공한다
// 언데드 유닛: 씬 시작 시 풀에서 스폰, 사망 시 비활성 유지 (부활 가능)
// 적 유닛: 웨이브마다 풀에서 꺼내 스폰, 사망 시 풀로 반환

// 언데드 유닛 종류와 프리팹을 연결하는 데이터
[System.Serializable]
public class UndeadPrefabEntry
{
    public UndeadType undeadType;
    public UnitBase prefab;
}

// 적 유닛 종류와 프리팹을 연결하는 데이터
[System.Serializable]
public class EnemyPrefabEntry
{
    public EnemyType enemyType;
    public UnitBase prefab;
}

// 플레이어가 보유한 언데드 유닛 목록 설정
// 배치 위치는 PreparationSpawnPositioner가 코어 기준으로 자동 계산한다
[System.Serializable]
public class UndeadSpawnConfig
{
    public UndeadType undeadType;
}

public class UnitRegistry : Singleton<UnitRegistry>
{
    // 인스펙터에서 언데드 유닛 종류별 프리팹을 등록한다
    [SerializeField] private List<UndeadPrefabEntry> undeadPrefabs = new List<UndeadPrefabEntry>();

    // 인스펙터에서 적 유닛 종류별 프리팹을 등록한다
    [SerializeField] private List<EnemyPrefabEntry> enemyPrefabs = new List<EnemyPrefabEntry>();

    // 인스펙터에서 게임 시작 시 배치할 언데드 유닛 목록을 설정한다
    [SerializeField] private List<UndeadSpawnConfig> initialUndeadSpawns = new List<UndeadSpawnConfig>();

    // 풀 초기 생성 크기
    [SerializeField] private int defaultPoolSize = 5;

    // 현재 전장에 등록된 활성 유닛 목록 (생존 여부와 관계없이 유지)
    private readonly List<UnitBase> activeUnits = new List<UnitBase>();

    // 언데드 초기 배치는 게임 시작 시 최초 1회만 수행한다
    private bool initialSpawnDone;

    // 언데드 유닛 오브젝트 풀
    private Dictionary<UndeadType, ObjectPool<UnitBase>> undeadPools;

    // 적 유닛 오브젝트 풀
    private Dictionary<EnemyType, ObjectPool<UnitBase>> enemyPools;

    protected override void Awake()
    {
        base.Awake();
        InitializePools();
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
    }

    // 정비 페이즈 시작 시 언데드 유닛을 최초 1회 배치한다
    private void OnPhaseChanged(PhaseChangedEvent e)
    {
        if (e.CurrentPhase != PhaseType.Preparation) return;
        if (initialSpawnDone) return;

        initialSpawnDone = true;
        SpawnInitialUndeadUnits();
    }

    private void InitializePools()
    {
        undeadPools = new Dictionary<UndeadType, ObjectPool<UnitBase>>();
        enemyPools = new Dictionary<EnemyType, ObjectPool<UnitBase>>();

        foreach (UndeadPrefabEntry entry in undeadPrefabs)
        {
            if (entry.prefab == null) continue;

            UnitBase prefab = entry.prefab;
            undeadPools[entry.undeadType] = new ObjectPool<UnitBase>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: unit => unit.gameObject.SetActive(true),
                actionOnRelease: unit => unit.gameObject.SetActive(false),
                actionOnDestroy: unit => Destroy(unit.gameObject),
                defaultCapacity: defaultPoolSize
            );
        }

        foreach (EnemyPrefabEntry entry in enemyPrefabs)
        {
            if (entry.prefab == null) continue;

            UnitBase prefab = entry.prefab;
            enemyPools[entry.enemyType] = new ObjectPool<UnitBase>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: unit => unit.gameObject.SetActive(true),
                actionOnRelease: unit => unit.gameObject.SetActive(false),
                actionOnDestroy: unit => Destroy(unit.gameObject),
                defaultCapacity: defaultPoolSize
            );
        }
    }

    // 코어 기준 우선순위 위치에 언데드 유닛을 배치한다
    // PreparationSpawnPositioner가 PlayerSpawn 구역 내에서 최적 위치를 계산한다
    public void SpawnInitialUndeadUnits()
    {
        if (!GridManager.Instance.MapDefinition.TryGetCorePosition(out var corePos))
        {
            Debug.LogError("맵에 코어 위치가 설정되지 않았습니다. 언데드 유닛을 배치할 수 없습니다.");
            return;
        }

        int count = initialUndeadSpawns.Count;
        List<Vector2Int> positions = PreparationSpawnPositioner.GetSpawnPositions(
            corePos, count, GridManager.Instance);

        for (int i = 0; i < initialUndeadSpawns.Count; i++)
        {
            if (i >= positions.Count)
            {
                Debug.LogWarning($"PlayerSpawn 구역 칸이 부족합니다. {initialUndeadSpawns[i].undeadType} 스폰 건너뜀.");
                break;
            }

            UnitBase unit = SpawnUndead(initialUndeadSpawns[i].undeadType, positions[i]);
            if (unit == null)
                Debug.LogWarning($"언데드 유닛 스폰 실패: {initialUndeadSpawns[i].undeadType} at {positions[i]}");
        }
    }

    // 언데드 유닛을 풀에서 꺼내 지정 위치에 배치하고 등록한다
    public UnitBase SpawnUndead(UndeadType undeadType, Vector2Int gridPosition)
    {
        if (!undeadPools.TryGetValue(undeadType, out var pool))
        {
            Debug.LogError($"UndeadType {undeadType}의 프리팹이 UnitRegistry에 등록되지 않았습니다.");
            return null;
        }

        Vector3 worldPos = GridManager.Instance.GridToWorld(gridPosition);
        UnitBase unit = pool.Get();
        unit.PrepareForSpawn(gridPosition, worldPos);

        RegisterUnit(unit);
        return unit;
    }

    // 적 유닛을 풀에서 꺼내 지정 위치에 배치하고 등록한다
    // 해당 칸이 이동 불가인 경우 스폰하지 않는다
    public UnitBase SpawnEnemy(EnemyType enemyType, Vector2Int gridPosition)
    {
        if (!enemyPools.TryGetValue(enemyType, out var pool))
        {
            Debug.LogError($"EnemyType {enemyType}의 프리팹이 UnitRegistry에 등록되지 않았습니다.");
            return null;
        }

        if (!GridManager.Instance.IsWalkable(gridPosition))
        {
            Debug.LogWarning($"그리드 위치 {gridPosition}에 적을 스폰할 수 없습니다. 이동 불가 칸입니다.");
            return null;
        }

        Vector3 worldPos = GridManager.Instance.GridToWorld(gridPosition);
        UnitBase unit = pool.Get();
        unit.PrepareForSpawn(gridPosition, worldPos);

        RegisterUnit(unit);
        return unit;
    }

    // 유닛을 활성 목록에 추가한다
    public void RegisterUnit(UnitBase unit)
    {
        if (!activeUnits.Contains(unit))
            activeUnits.Add(unit);
    }

    private void OnEnemyDied(EnemyDiedEvent e)
    {
        ReturnEnemyToPool(e.Unit);
    }

    // 적 유닛을 활성 목록에서 제거하고 풀로 반환한다
    private void ReturnEnemyToPool(EnemyUnit unit)
    {
        activeUnits.Remove(unit);

        if (enemyPools.TryGetValue(unit.EnemyType, out var pool))
            pool.Release(unit);
    }

    // 웨이브 시작 시 적 유닛 목록을 초기화한다
    public void ClearEnemies()
    {
        activeUnits.RemoveAll(u => u.Team == TeamType.Enemy);
    }

    // 전장에 등록된 모든 유닛 목록을 반환한다
    public IReadOnlyList<UnitBase> GetAllUnits() => activeUnits;

    // 생존한 언데드 유닛 목록을 반환한다
    public List<UnitBase> GetAliveUndeadUnits()
    {
        return activeUnits.FindAll(u => u.Team == TeamType.Undead && !u.IsDead);
    }

    // 생존한 적 유닛 목록을 반환한다
    public List<UnitBase> GetAliveEnemyUnits()
    {
        return activeUnits.FindAll(u => u.Team == TeamType.Enemy && !u.IsDead);
    }

    // 사망한 언데드 유닛 목록을 반환한다 (부활 대상 조회에 사용)
    public List<UnitBase> GetDeadUndeadUnits()
    {
        return activeUnits.FindAll(u => u.Team == TeamType.Undead && u.IsDead);
    }
}
