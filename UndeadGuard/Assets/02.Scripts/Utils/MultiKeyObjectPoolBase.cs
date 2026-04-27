using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public abstract class MultiKeyObjectPoolBase<TMainKey, TSubKey, T> where T : Component
{
    protected readonly Dictionary<(TMainKey Main, TSubKey Sub), ObjectPool<T>> pools = new();
    protected readonly Dictionary<(TMainKey Main, TSubKey Sub), GameObject> prefabs = new();

    private readonly int defaultCapacity;
    private readonly int maxSize;
    private readonly bool collectionCheck;

    protected MultiKeyObjectPoolBase(int defaultCapacity = 10, int maxSize = 100, bool collectionCheck = false)
    {
        this.defaultCapacity = defaultCapacity;
        this.maxSize = maxSize;
        this.collectionCheck = collectionCheck;
    }

    protected void RegisterPrefab(TMainKey mainKey, TSubKey subKey, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[MultiKeyObjectPoolBase] Prefab is null for ({mainKey}, {subKey}).");
            return;
        }

        var key = (mainKey, subKey);
        prefabs[key] = prefab;

        if (pools.ContainsKey(key))
            return;

        pools[key] = new ObjectPool<T>(
            createFunc: () => CreateObject(mainKey, subKey),
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroy,
            collectionCheck: collectionCheck,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize);
    }

    protected virtual T CreateObject(TMainKey mainKey, TSubKey subKey)
    {
        var key = (mainKey, subKey);
        if (!prefabs.TryGetValue(key, out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"[MultiKeyObjectPoolBase] Prefab is not registered for ({mainKey}, {subKey}).");
            return null;
        }

        if (prefab.GetComponent<T>() == null)
        {
            Debug.LogError($"[MultiKeyObjectPoolBase] Component {typeof(T).Name} not found on prefab {prefab.name}.");
            return null;
        }

        GameObject instance = Object.Instantiate(prefab);
        T component = instance.GetComponent<T>();
        return component;
    }

    protected virtual void OnGet(T obj)
    {
        if (obj != null)
            obj.gameObject.SetActive(true);
    }

    protected virtual void OnRelease(T obj)
    {
        if (obj != null)
            obj.gameObject.SetActive(false);
    }

    protected virtual void OnDestroy(T obj)
    {
        // Intentionally empty. We do not force runtime destruction from pooling base.
    }

    public virtual T Get(TMainKey mainKey, TSubKey subKey)
    {
        if (pools.TryGetValue((mainKey, subKey), out ObjectPool<T> pool))
            return pool.Get();

        Debug.LogError($"[MultiKeyObjectPoolBase] Pool not found for ({mainKey}, {subKey}).");
        return null;
    }

    public virtual void Release(TMainKey mainKey, TSubKey subKey, T obj)
    {
        if (obj == null)
            return;

        if (pools.TryGetValue((mainKey, subKey), out ObjectPool<T> pool))
        {
            pool.Release(obj);
            return;
        }

        Debug.LogWarning($"[MultiKeyObjectPoolBase] Pool not found for ({mainKey}, {subKey}). Object will be deactivated only.");
        obj.gameObject.SetActive(false);
    }

    public bool HasPool(TMainKey mainKey, TSubKey subKey)
    {
        return pools.ContainsKey((mainKey, subKey));
    }

    public void Clear(TMainKey mainKey, TSubKey subKey)
    {
        if (pools.TryGetValue((mainKey, subKey), out ObjectPool<T> pool))
            pool.Clear();
    }

    public void ClearAll()
    {
        foreach (ObjectPool<T> pool in pools.Values)
            pool.Clear();
    }
}
