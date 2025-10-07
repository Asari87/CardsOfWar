
using UnityEngine;

public class GenericSingelton<T> : MonoBehaviour where T : GenericSingelton<T>
{
    [SerializeField] bool _dontDestroyOnLoad = true;
    public static T Instance {get; private set;}
    public virtual void Awake()
    {
        if (Instance is not null)
            Destroy(gameObject);
        Instance = (T)this;
        if(_dontDestroyOnLoad)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }

    public virtual void OnDestroy()
    {
        if(Instance == this)
            Instance = null;    
    }
}
