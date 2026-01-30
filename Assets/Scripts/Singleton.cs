using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    [Header("Singleton Settings")]
    [SerializeField]public bool dontDestroyOnLoad = true;

    public static T Instance
    {
        get
        {

            if (instance == null)
            {
                instance = FindAnyObjectByType<T>();

                if (instance == null)
                {
                    CreateInstance();
                }
            }
            return instance;
            
        }
    }

    private static void CreateInstance()
    {
        GameObject singletonObject = new GameObject();
        singletonObject.name = $"{typeof(T).Name} (Singleton)";
        instance = singletonObject.AddComponent<T>();

        Debug.Log($"[Singleton] Created new instance of {typeof(T)}");
    }

    public static bool HasInstance => instance != null;

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            InitializeSingleton();
        }
        else if (instance != this)
        {

            Destroy(gameObject);
        }
    }

    protected virtual void InitializeSingleton()
    {
        if (dontDestroyOnLoad)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    public static void DestroyInstance()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
    }
}