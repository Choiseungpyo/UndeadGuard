using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AttackEffectService : Singleton<AttackEffectService>
{
    private Transform poolRoot;
    private AttackVfxPoolManager poolManager;

    public static void Play(UnitBase attacker, Vector2Int targetGridPosition, string actionId)
    {
        AttackEffectService instance = EnsureInstance();
        if (instance == null)
            return;

        instance.PlayInternal(attacker, targetGridPosition, actionId);
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        DontDestroyOnLoad(gameObject);
        EnsurePoolInfrastructure();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        poolManager?.ClearAll();
    }

    private void PlayInternal(UnitBase attacker, Vector2Int targetGridPosition, string actionId)
    {
        if (attacker == null)
            return;

        if (!TryResolvePattern(attacker, actionId, out AttackPatternEntry pattern) || pattern == null)
            return;

        if (pattern.ParticlePrefab == null)
            return;

        Vector3 spawnPosition = ResolveSpawnPosition(attacker, targetGridPosition);
        spawnPosition += pattern.ParticleWorldOffset;

        float spawnDelay = Mathf.Max(0f, pattern.ParticleSpawnDelay);
        if (spawnDelay > 0f)
        {
            StartCoroutine(PlayAfterDelay(pattern.ParticlePrefab, spawnPosition, spawnDelay, pattern.ParticleAutoDestroyDelay));
            return;
        }

        SpawnFromPool(pattern.ParticlePrefab, spawnPosition, pattern.ParticleAutoDestroyDelay);
    }

    private IEnumerator PlayAfterDelay(GameObject prefab, Vector3 spawnPosition, float delaySeconds, float autoReturnDelay)
    {
        yield return new WaitForSeconds(delaySeconds);
        SpawnFromPool(prefab, spawnPosition, autoReturnDelay);
    }

    private void SpawnFromPool(GameObject prefab, Vector3 spawnPosition, float autoReturnDelay)
    {
        EnsurePoolInfrastructure();

        int prefabId = prefab.GetInstanceID();
        if (!poolManager.HasParticlePool(prefabId))
            poolManager.RegisterParticlePrefab(prefabId, prefab);

        PooledAttackEffect effect = poolManager.GetParticleEffect(prefabId);
        if (effect == null)
            return;

        effect.Spawn(spawnPosition, pooled => poolManager.ReleaseParticleEffect(prefabId, pooled));

        float returnDelay = autoReturnDelay > 0f
            ? autoReturnDelay
            : effect.EstimateLifetime(1f);

        effect.ScheduleReturn(returnDelay);
    }

    private void EnsurePoolInfrastructure()
    {
        if (poolManager != null)
            return;

        GameObject root = new GameObject("AttackPatternVfxPoolRoot");
        root.transform.SetParent(transform, false);
        poolRoot = root.transform;
        poolManager = new AttackVfxPoolManager(poolRoot, this);
    }

    private static AttackEffectService EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        AttackEffectService found = FindFirstObjectByType<AttackEffectService>();
        if (found != null)
            return found;

        GameObject go = new GameObject(nameof(AttackEffectService));
        return go.AddComponent<AttackEffectService>();
    }

    private static bool TryResolvePattern(UnitBase attacker, string actionId, out AttackPatternEntry pattern)
    {
        if (AttackPatternResolver.TryGetPattern(attacker, actionId, out pattern))
            return true;

        if (string.Equals(actionId, AttackActionIds.BasicAttack, StringComparison.OrdinalIgnoreCase))
            return false;

        return AttackPatternResolver.TryGetPattern(attacker, AttackActionIds.BasicAttack, out pattern);
    }

    private static Vector3 ResolveSpawnPosition(UnitBase attacker, Vector2Int targetGridPosition)
    {
        GridManager gridManager = GridManager.Instance;
        if (gridManager == null)
            return attacker.transform.position;

        return gridManager.IsInBounds(targetGridPosition)
            ? gridManager.GridToWorld(targetGridPosition)
            : attacker.transform.position;
    }
}
