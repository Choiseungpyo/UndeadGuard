using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UndeadPrefabEntry
{
    public UndeadType undeadType;
    public UnitBase prefab;
}

[System.Serializable]
public class EnemyPrefabEntry
{
    public EnemyType enemyType;
    public UnitBase prefab;
}

[System.Serializable]
public class UndeadSpawnConfig
{
    public UndeadType undeadType;
}

public class UnitRegistry : Singleton<UnitRegistry>
{
    [SerializeField] private List<UndeadPrefabEntry> undeadPrefabs = new List<UndeadPrefabEntry>();
    [SerializeField] private List<EnemyPrefabEntry> enemyPrefabs = new List<EnemyPrefabEntry>();
    [SerializeField] private List<UndeadSpawnConfig> initialUndeadSpawns = new List<UndeadSpawnConfig>();
    [SerializeField] private int defaultPoolSize = 5;
    [SerializeField] private float enemyReturnDelaySeconds = 1.2f;

    private readonly List<UnitBase> activeUnits = new List<UnitBase>();
    private readonly HashSet<EnemyUnit> pendingEnemyRelease = new HashSet<EnemyUnit>();
    private readonly Dictionary<EnemyUnit, Coroutine> pendingEnemyReleaseRoutines = new Dictionary<EnemyUnit, Coroutine>();
    private UnitTypePoolManager poolManager;
    private Transform poolRoot;

    protected override void Awake()
    {
        base.Awake();
        InitializePools();
    }

    private void Start()
    {
        SpawnInitialUndeadUnits();
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        foreach (var pair in pendingEnemyReleaseRoutines)
        {
            if (pair.Value != null)
                StopCoroutine(pair.Value);
        }
        pendingEnemyReleaseRoutines.Clear();
        pendingEnemyRelease.Clear();
    }

    private void InitializePools()
    {
        if (poolRoot == null)
        {
            GameObject root = new GameObject("UnitPoolRoot");
            root.transform.SetParent(transform, false);
            poolRoot = root.transform;
        }

        poolManager = new UnitTypePoolManager(poolRoot, defaultPoolSize);

        for (int i = 0; i < undeadPrefabs.Count; i++)
        {
            UndeadPrefabEntry entry = undeadPrefabs[i];
            if (entry == null || entry.prefab == null)
                continue;

            poolManager.RegisterUndeadPrefab(entry.undeadType, entry.prefab);
        }

        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            EnemyPrefabEntry entry = enemyPrefabs[i];
            if (entry == null || entry.prefab == null)
                continue;

            poolManager.RegisterEnemyPrefab(entry.enemyType, entry.prefab);
        }
    }

    public void SpawnInitialUndeadUnits()
    {
        if (!GridManager.Instance.MapDefinition.TryGetCorePosition(out Vector2Int corePos))
        {
            Debug.LogError("UnitRegistry: core position is not configured. Cannot spawn initial undead units.");
            return;
        }

        int count = initialUndeadSpawns.Count;
        List<Vector2Int> positions = PreparationSpawnPositioner.GetSpawnPositions(corePos, count, GridManager.Instance);

        for (int i = 0; i < initialUndeadSpawns.Count; i++)
        {
            if (i >= positions.Count)
            {
                Debug.LogWarning($"UnitRegistry: missing player spawn position for {initialUndeadSpawns[i].undeadType}.");
                break;
            }

            UnitBase unit = SpawnUndead(initialUndeadSpawns[i].undeadType, positions[i]);
            if (unit == null)
                Debug.LogWarning($"UnitRegistry: failed to spawn undead {initialUndeadSpawns[i].undeadType} at {positions[i]}.");
        }
    }

    public UnitBase SpawnUndead(UndeadType undeadType, Vector2Int gridPosition)
    {
        if (poolManager == null)
        {
            Debug.LogError("UnitRegistry: pool manager is not initialized.");
            return null;
        }

        UnitBase unit = poolManager.GetUndead(undeadType);
        if (unit == null)
        {
            Debug.LogError($"UnitRegistry: no undead pool found for {undeadType}.");
            return null;
        }

        Vector3 worldPos = GridManager.Instance.GridToWorld(gridPosition);
        unit.PrepareForSpawn(gridPosition, worldPos);

        RegisterUnit(unit);
        return unit;
    }

    public UnitBase SpawnEnemy(EnemyType enemyType, Vector2Int gridPosition)
    {
        if (poolManager == null)
        {
            Debug.LogError("UnitRegistry: pool manager is not initialized.");
            return null;
        }

        if (!GridManager.Instance.IsWalkable(gridPosition))
        {
            Debug.LogWarning($"UnitRegistry: cannot spawn enemy at {gridPosition}, tile is not walkable.");
            return null;
        }

        UnitBase unit = poolManager.GetEnemy(enemyType);
        if (unit == null)
        {
            Debug.LogError($"UnitRegistry: no enemy pool found for {enemyType}.");
            return null;
        }

        Vector3 worldPos = GridManager.Instance.GridToWorld(gridPosition);
        unit.PrepareForSpawn(gridPosition, worldPos);

        RegisterUnit(unit);
        return unit;
    }

    public void RegisterUnit(UnitBase unit)
    {
        if (unit == null)
            return;

        if (!activeUnits.Contains(unit))
            activeUnits.Add(unit);
    }

    private void OnEnemyDied(EnemyDiedEvent e)
    {
        if (e?.Unit == null)
            return;

        EnemyUnit enemyUnit = e.Unit;
        if (pendingEnemyRelease.Contains(enemyUnit))
            return;

        pendingEnemyRelease.Add(enemyUnit);
        Coroutine routine = StartCoroutine(ReturnEnemyToPoolAfterDelay(enemyUnit));
        pendingEnemyReleaseRoutines[enemyUnit] = routine;
    }

    private System.Collections.IEnumerator ReturnEnemyToPoolAfterDelay(EnemyUnit unit)
    {
        float delay = Mathf.Max(0f, enemyReturnDelaySeconds);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (unit != null && unit.IsDead)
            ReturnEnemyToPool(unit);

        pendingEnemyReleaseRoutines.Remove(unit);
        pendingEnemyRelease.Remove(unit);
    }

    private void ReturnEnemyToPool(EnemyUnit unit)
    {
        if (unit == null)
            return;

        pendingEnemyReleaseRoutines.Remove(unit);
        pendingEnemyRelease.Remove(unit);
        activeUnits.Remove(unit);
        poolManager?.ReleaseEnemy(unit.EnemyType, unit);
    }

    public void ClearEnemies()
    {
        for (int i = activeUnits.Count - 1; i >= 0; i--)
        {
            UnitBase unit = activeUnits[i];
            if (unit == null || unit.Team != TeamType.Enemy)
                continue;

            activeUnits.RemoveAt(i);
            EnemyUnit enemyUnit = unit as EnemyUnit;
            if (enemyUnit != null)
            {
                if (pendingEnemyReleaseRoutines.TryGetValue(enemyUnit, out Coroutine routine) && routine != null)
                    StopCoroutine(routine);

                pendingEnemyReleaseRoutines.Remove(enemyUnit);
                pendingEnemyRelease.Remove(enemyUnit);
                poolManager?.ReleaseEnemy(enemyUnit.EnemyType, enemyUnit);
            }
            else
                unit.gameObject.SetActive(false);
        }
    }

    public IReadOnlyList<UnitBase> GetAllUnits() => activeUnits;

    public List<UnitBase> GetAliveUndeadUnits()
    {
        return activeUnits.FindAll(u => u != null && u.Team == TeamType.Undead && !u.IsDead);
    }

    public List<UnitBase> GetAliveEnemyUnits()
    {
        return activeUnits.FindAll(u => u != null && u.Team == TeamType.Enemy && !u.IsDead);
    }

    public List<UnitBase> GetDeadUndeadUnits()
    {
        return activeUnits.FindAll(u => u != null && u.Team == TeamType.Undead && u.IsDead);
    }
}
