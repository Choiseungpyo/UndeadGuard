using System;
using System.Collections.Generic;
using UnityEngine;

// 시스템 간 느슨한 결합을 위한 중앙 이벤트 버스
// 발행자와 구독자가 서로를 직접 참조하지 않고 이벤트를 주고받을 수 있게 한다
public class EventBus
{
    private static EventBus instance;

    // 싱글턴 인스턴스를 반환한다
    public static EventBus Instance
    {
        get
        {
            if (instance == null)
                instance = new EventBus();
            return instance;
        }
    }

    // 이벤트 타입별 리스너 목록
    private readonly Dictionary<Type, List<Delegate>> listeners = new Dictionary<Type, List<Delegate>>();

    // 특정 이벤트 타입을 구독한다
    public void Subscribe<T>(Action<T> callback)
    {
        if (callback == null)
            return;

        var type = typeof(T);
        if (!listeners.TryGetValue(type, out List<Delegate> listenerList))
        {
            listenerList = new List<Delegate>();
            listeners[type] = listenerList;
        }

        if (listenerList.Contains(callback))
        {
            Debug.LogWarning($"[EventBus] Duplicate subscribe ignored: {GetEventTypeName(type)}");
            return;
        }

        listenerList.Add(callback);
    }

    // 특정 이벤트 타입 구독을 해제한다
    public void Unsubscribe<T>(Action<T> callback)
    {
        if (callback == null)
            return;

        var type = typeof(T);
        if (!listeners.TryGetValue(type, out List<Delegate> listenerList))
            return;

        // Remove all matching callbacks to clean up legacy duplicates safely.
        for (int i = listenerList.Count - 1; i >= 0; i--)
        {
            if (Equals(listenerList[i], callback))
                listenerList.RemoveAt(i);
        }

        if (listenerList.Count == 0)
            listeners.Remove(type);
    }

    // 이벤트를 발행하여 구독 중인 모든 리스너에게 전달한다
    public void Publish<T>(T eventData)
    {
        var type = typeof(T);
        if (!listeners.ContainsKey(type))
            return;

        // 순회 중 리스너가 변경될 수 있으므로 복사본을 사용한다
        var snapshot = new List<Delegate>(listeners[type]);
        foreach (var listener in snapshot)
            (listener as Action<T>)?.Invoke(eventData);
    }

    private static string GetEventTypeName(Type type)
    {
        return type != null ? type.Name : "UnknownEvent";
    }
}
