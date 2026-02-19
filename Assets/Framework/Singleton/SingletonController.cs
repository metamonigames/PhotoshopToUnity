using System.Collections.Generic;
using UnityEngine;

public class SingletonController : MonoBehaviour
{
    private static readonly Dictionary<System.Type, object> _singletonDic = new Dictionary<System.Type, object>();
    private static bool _applicationIsQuitting;
    private static bool _isInitialized;
    private static SingletonController _instance;

    /// <summary>
    /// RuntimeInitializeLoadType.BeforeSceneLoad 씬 로드 전에 자동으로 실행
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        GameObject go = new GameObject("SingletonController");
        _instance = go.AddComponent<SingletonController>();
        DontDestroyOnLoad(go);

        _isInitialized = true;
    }

    private void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    private static T Create<T>()
    {
        if (_applicationIsQuitting)
        {
            return default;
        }
        System.Type type = typeof(T);
        if (!_singletonDic.ContainsKey(type))
        {
            if (type.IsSubclassOf(typeof(Component)))
            {
                GameObject go = new GameObject { name = $"(Singleton) {type.Name}" };
                DontDestroyOnLoad(go);
                _singletonDic.Add(type, go.AddComponent(type));
            }
            else
            {
                _singletonDic.Add(type, System.Activator.CreateInstance<T>());
            }
        }
        return (T)_singletonDic[type];
    }

    public static T Get<T>()
    {
        System.Type type = typeof(T);
        return _singletonDic.ContainsKey(type) ? (T)_singletonDic[type] : Create<T>();
    }

    public static void Remove<T>()
    {
        Remove(typeof(T));
    }

    private static void Remove(System.Type inType)
    {
        System.Type type = inType;
        if (_singletonDic.ContainsKey(type) && type.IsSubclassOf(typeof(Component)))
        {
            Destroy(((Component)_singletonDic[type]).gameObject);
        }
        _singletonDic.Remove(type);
    }

    public static void RemoveAll()
    {
        foreach (var type in _singletonDic.Keys)
        {
            Remove(type);
        }
        _singletonDic.Clear();
    }
}