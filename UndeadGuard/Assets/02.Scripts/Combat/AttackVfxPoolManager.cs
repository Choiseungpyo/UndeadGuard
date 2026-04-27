using UnityEngine;

public sealed class AttackVfxPoolManager : MultiKeyObjectPoolBase<int, int, PooledAttackEffect>
{
    private const int ParticlePoolSubKey = 0;

    private readonly Transform poolRoot;
    private readonly AttackEffectService owner;

    public AttackVfxPoolManager(Transform poolRoot, AttackEffectService owner)
        : base(defaultCapacity: 4, maxSize: int.MaxValue, collectionCheck: false)
    {
        this.poolRoot = poolRoot;
        this.owner = owner;
    }

    public void RegisterParticlePrefab(int prefabId, GameObject prefab)
    {
        RegisterPrefab(prefabId, ParticlePoolSubKey, prefab);
    }

    public bool HasParticlePool(int prefabId)
    {
        return HasPool(prefabId, ParticlePoolSubKey);
    }

    public PooledAttackEffect GetParticleEffect(int prefabId)
    {
        return Get(prefabId, ParticlePoolSubKey);
    }

    public void ReleaseParticleEffect(int prefabId, PooledAttackEffect effect)
    {
        Release(prefabId, ParticlePoolSubKey, effect);
    }

    protected override PooledAttackEffect CreateObject(int mainKey, int subKey)
    {
        if (!prefabs.TryGetValue((mainKey, subKey), out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"[AttackVfxPoolManager] Prefab not registered for key ({mainKey}, {subKey}).");
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, poolRoot);
        instance.name = prefab.name + "_Pooled";

        PooledAttackEffect effect = instance.GetComponent<PooledAttackEffect>();
        if (effect == null)
            effect = instance.AddComponent<PooledAttackEffect>();

        effect.Initialize(owner);
        effect.Despawn();
        return effect;
    }

    protected override void OnGet(PooledAttackEffect obj)
    {
        // Spawn step handles activation and particle restart.
    }

    protected override void OnRelease(PooledAttackEffect obj)
    {
        if (obj != null)
            obj.Despawn();
    }
}
