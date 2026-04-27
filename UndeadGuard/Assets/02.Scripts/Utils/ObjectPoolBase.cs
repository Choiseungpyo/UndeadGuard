using UnityEngine.Pool;

public abstract class ObjectPoolBase<T> where T : class
{
    private readonly ObjectPool<T> pool;

    protected ObjectPoolBase(int defaultCapacity = 10, int maxSize = 100, bool collectionCheck = false)
    {
        pool = new ObjectPool<T>(
            createFunc: CreateObject,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroy,
            collectionCheck: collectionCheck,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize);
    }

    protected abstract T CreateObject();
    protected virtual void OnGet(T obj) { }
    protected virtual void OnRelease(T obj) { }
    protected virtual void OnDestroy(T obj) { }

    public T Get() => pool.Get();
    public void Release(T obj) => pool.Release(obj);
    public void Clear() => pool.Clear();
}
