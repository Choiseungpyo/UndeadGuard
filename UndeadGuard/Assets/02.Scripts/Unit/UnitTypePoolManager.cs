using UnityEngine;

public sealed class UnitTypePoolManager : MultiKeyObjectPoolBase<TeamType, int, UnitBase>
{
    private readonly Transform poolRoot;

    public UnitTypePoolManager(Transform poolRoot, int defaultCapacity)
        : base(defaultCapacity: Mathf.Max(1, defaultCapacity), maxSize: int.MaxValue, collectionCheck: false)
    {
        this.poolRoot = poolRoot;
    }

    public void RegisterUndeadPrefab(UndeadType undeadType, UnitBase prefab)
    {
        if (prefab == null)
            return;

        RegisterPrefab(TeamType.Undead, (int)undeadType, prefab.gameObject);
    }

    public void RegisterEnemyPrefab(EnemyType enemyType, UnitBase prefab)
    {
        if (prefab == null)
            return;

        RegisterPrefab(TeamType.Enemy, (int)enemyType, prefab.gameObject);
    }

    public UnitBase GetUndead(UndeadType undeadType)
    {
        return Get(TeamType.Undead, (int)undeadType);
    }

    public UnitBase GetEnemy(EnemyType enemyType)
    {
        return Get(TeamType.Enemy, (int)enemyType);
    }

    public void ReleaseEnemy(EnemyType enemyType, UnitBase unit)
    {
        Release(TeamType.Enemy, (int)enemyType, unit);
    }

    protected override UnitBase CreateObject(TeamType mainKey, int subKey)
    {
        if (!prefabs.TryGetValue((mainKey, subKey), out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"[UnitTypePoolManager] Prefab not registered for ({mainKey}, {subKey}).");
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, poolRoot);
        UnitBase unit = instance.GetComponent<UnitBase>();
        if (unit == null)
        {
            Debug.LogError($"[UnitTypePoolManager] UnitBase not found on prefab {prefab.name}.");
            instance.SetActive(false);
            return null;
        }

        return unit;
    }

    protected override void OnGet(UnitBase obj)
    {
        if (obj != null)
            obj.gameObject.SetActive(true);
    }

    protected override void OnRelease(UnitBase obj)
    {
        if (obj != null)
            obj.gameObject.SetActive(false);
    }
}
