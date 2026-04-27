using UnityEngine;

// MonoBehaviour 기반 싱글톤 공통 클래스
// 씬 내에 인스턴스가 하나만 존재하도록 보장한다
// 사용 방법: public class Foo : Singleton<Foo>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            return;
        }

        Instance = this as T;
    }
}
