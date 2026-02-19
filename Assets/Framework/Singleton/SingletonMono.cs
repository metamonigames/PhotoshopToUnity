using UnityEngine;
using System.Collections.Generic;

public class SingletonManager
{
    private static List<MonoBehaviour> _singletons = new List<MonoBehaviour>();

    public static void Register(MonoBehaviour singleton)
    {
        if (!_singletons.Contains(singleton))
        {
            _singletons.Add(singleton);
        }
    }

    public static void Unregister(MonoBehaviour singleton)
    {
        _singletons.Remove(singleton);
    }

    public static void DestroyAll()
    {
        for (int i = _singletons.Count - 1; i >= 0; i--)
        {
            if (_singletons[i] != null)
            {
                Object.DestroyImmediate(_singletons[i].gameObject);
            }
        }
        _singletons.Clear();
    }
}

public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting == true)
            {
                _instance = null;
                return null;
            }
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (T)FindAnyObjectByType(typeof(T));
                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = typeof(T).ToString();  // 자바스크립트에서 부를 수 있기 때문에 이름 그대로 넣어줘야 함.
                        singleton.hideFlags = HideFlags.None;
                        DontDestroyOnLoad(singleton);
                    }
                    _instance.hideFlags = HideFlags.None;
                    SingletonManager.Register(_instance);
                }
                return _instance;
            }
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
        _instance = null;
    }

    protected virtual void Awake()
    {
        _applicationIsQuitting = false;
    }

    protected virtual void OnDestroy()
    {
        SingletonManager.Unregister(this);
    }
}