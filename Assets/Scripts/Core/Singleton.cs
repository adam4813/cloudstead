using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    protected static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
                Debug.LogError($"[Singleton] {typeof(T).Name} instance is null. Ensure it exists in the scene and has been initialized.");
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = (T)this;
        DontDestroyOnLoad(gameObject);
    }

    public virtual void Initialize() { }

    protected virtual void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
