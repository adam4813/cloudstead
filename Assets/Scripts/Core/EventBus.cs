using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    public static void Subscribe<T>(Action<T> callback)
    {
        var type = typeof(T);
        if (!_subscribers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _subscribers[type] = list;
        }
        list.Add(callback);
    }

    public static void Unsubscribe<T>(Action<T> callback)
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out var list))
            list.Remove(callback);
    }

    public static void Publish<T>(T eventData)
    {
        var type = typeof(T);
        if (!_subscribers.TryGetValue(type, out var list))
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            try
            {
                ((Action<T>)list[i])?.Invoke(eventData);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    public static void Clear()
    {
        _subscribers.Clear();
    }
}
