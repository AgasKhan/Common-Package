using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour, IUpdateManager, ILateUpdateManager, IFixedUpdateManager
{
    interface IGameManagerState : IState<FSMGameManager>, ISuperUpdateManager
    {
        public void OnFixed(FSMGameManager gameManager);

        public void OnLate(FSMGameManager gameManager);
    }
    
    
    [System.Serializable]
    class FSMGameManager : FSMParent<FSMGameManager,GameManager,IGameManagerState>
    {
        public GamePlayState gamePlay = new GamePlayState();
        public LoadState load = new LoadState();

        public void FixedUpdate()
        {
            Current.OnFixed(this);
        }

        public void LateUpdate()
        {
            Current.OnLate(this);
        }
        
        public void Init(GameManager gameManager)
        {
            Init(load, gameManager);
        }
    }
    
    
    [System.Serializable]
    class GamePlayState : IGameManagerState
    {
        [SerializeField]
        public UnityEvent onPlay = new UnityEvent();
        
        public event UnityAction OnUpdateEvnt;
        public event UnityAction OnLateUpdateEvnt;
        public event UnityAction OnFixedUpdateEvnt;
        
        [SerializeField]
        public UnityEvent onPause = new UnityEvent();
        
        public void OnEnter(FSMGameManager context)
        {
            onPlay.Invoke();
        }

        public void OnStay(FSMGameManager context)
        {
            OnUpdateEvnt?.Invoke();
        }
        
        public void OnLate(FSMGameManager gameManager)
        {
            OnLateUpdateEvnt?.Invoke();
        }
        
        public void OnFixed(FSMGameManager gameManager)
        {
            OnFixedUpdateEvnt?.Invoke();
        }

        public void OnExit(FSMGameManager context)
        {
            onPause.Invoke();
        }
    }
    
    
    [System.Serializable]
    class LoadState : IGameManagerState
    {
        public event UnityAction OnUpdateEvnt;
        public event UnityAction OnLateUpdateEvnt;
        public event UnityAction OnFixedUpdateEvnt;
        
        public void OnEnter(FSMGameManager context)
        {
            
        }

        public void OnStay(FSMGameManager context)
        {
            
        }
        
        public void OnFixed(FSMGameManager gameManager)
        {
            
        }

        public void OnLate(FSMGameManager gameManager)
        {
            
        }

        public void OnExit(FSMGameManager context)
        {
            
        }
    }
    
    public static GameManager instance { get; private set; }
    
    #region events

    public static event UnityAction OnAwakeEvnt;
    
    public static event UnityAction OnStartEvnt;
    
    public static event UnityAction OnUpdateEvnt;
    
    public static event UnityAction OnLateUpdateEvnt;
    
    public static event UnityAction OnFixedUpdateEvnt;
    
    public static event UnityAction OnDestroyEvnt;
    
    public static event UnityAction OnPlay
    {
        add
        {
            instance._fsmGameManager.gamePlay.onPlay.AddListener(value);
        }
        remove
        {
            instance._fsmGameManager.gamePlay.onPlay.RemoveListener(value);
        }
    }
    
    public static event UnityAction OnPause
    {
        add
        {
            instance._fsmGameManager.gamePlay.onPause.AddListener(value);
        }
        remove
        {
            instance._fsmGameManager.gamePlay.onPause.RemoveListener(value);
        }
    }

    public static ISuperUpdateManager GamePlay => instance._fsmGameManager.gamePlay;
    
    public static ISuperUpdateManager Load => instance._fsmGameManager.load;
    
    event UnityAction IUpdateManager.OnUpdateEvnt
    {
        add => OnUpdateEvnt += value;
        remove => OnUpdateEvnt -= value;
    }
    
    event UnityAction ILateUpdateManager.OnLateUpdateEvnt
    {
        add => OnLateUpdateEvnt += value;
        remove => OnLateUpdateEvnt -= value;
    }

    event UnityAction IFixedUpdateManager.OnFixedUpdateEvnt
    {
        add => OnFixedUpdateEvnt += value;
        remove => OnFixedUpdateEvnt -= value;
    }
    
    
    [SerializeField]
    private UnityEvent onAwake = new UnityEvent();
    
    [SerializeField]
    private UnityEvent onStart = new UnityEvent();
    
    [SerializeField]
    private UnityEvent onUpdate = new UnityEvent();
    
    [SerializeField]
    private UnityEvent onLateUpdate = new UnityEvent();
    
    [SerializeField]
    private UnityEvent onFixedUpdate = new UnityEvent();
    
    [SerializeField]
    private UnityEvent onDestroy = new UnityEvent();

    
    
    #endregion

    #region FrameRate threshold

    public static bool BelowHightFrameRate => instance?.framesPerSecond.BelowHightFrameRate ?? false;

    public static bool BelowMediumFrameRate => instance?.framesPerSecond.BelowMediumFrameRate ?? false;

    public static bool BelowLowFrameRate => instance?.framesPerSecond.BelowLowFrameRate ?? false;

    public static bool BelowVeryLowFrameRate => instance?.framesPerSecond.BelowVeryLowFrameRate ?? false;

    public static bool BelowWatchDogFrameRate => instance?.framesPerSecond.BelowWatchDogFrameRate ?? false;
    
    private FPSCounter framesPerSecond;
    
    #endregion

    [SerializeField]
    private FSMGameManager _fsmGameManager;
    
    
#if UNITY_EDITOR
    [UnityEditor.Callbacks.DidReloadScripts]
    static void CreateInScene()
    {
        var aux = FindObjectOfType<GameManager>();
        if(aux!=null)
            return;

        GameObject go = new GameObject("GameManager");
        var newGm = go.AddComponent<GameManager>();
        
        Debug.LogWarning("Se creo un nuevo GameManager para la escena", newGm);
    }
#endif
    
    private void Awake()
    {
        instance = this;
        
        onAwake.AddListener(()=> OnAwakeEvnt?.Invoke());
        onStart.AddListener(()=> OnStartEvnt?.Invoke());
        onUpdate.AddListener(()=> OnUpdateEvnt?.Invoke());
        onLateUpdate.AddListener(()=> OnLateUpdateEvnt?.Invoke());
        onFixedUpdate.AddListener(()=> OnFixedUpdateEvnt?.Invoke());
        onDestroy.AddListener(()=> OnDestroyEvnt?.Invoke());
        
        framesPerSecond = new(this);

        _fsmGameManager.Init(this);
        
        onAwake.Invoke();
    }

    private void Start()
    {
        onStart.Invoke();
    }

    private void Update()
    {
        onUpdate.Invoke();   
        
        _fsmGameManager.Update();
    }

    private void LateUpdate()
    {
        onLateUpdate.Invoke();
        
        _fsmGameManager.LateUpdate();
    }

    private void FixedUpdate()
    {
        onFixedUpdate.Invoke();
        
        _fsmGameManager.FixedUpdate();
    }

    private void OnDestroy()
    {
        onDestroy.Invoke();
        OnAwakeEvnt = null;
        OnStartEvnt = null;
        OnUpdateEvnt = null;
        OnLateUpdateEvnt = null;
        OnFixedUpdateEvnt = null;
        OnDestroyEvnt = null;
    }
}


public interface IUpdateManager
{
    public static IUpdateManager operator + (IUpdateManager lvalue, IUpdate rvalue)
    {
        lvalue.OnUpdateEvnt += rvalue.MyUpdate;
        return lvalue;
    }

    public event UnityAction OnUpdateEvnt;
}

public interface ILateUpdateManager
{
    public event UnityAction OnLateUpdateEvnt;
}

public interface IFixedUpdateManager
{
    public event UnityAction OnFixedUpdateEvnt;
}

public interface ISuperUpdateManager : IUpdateManager, IFixedUpdateManager, ILateUpdateManager { }

public interface IUpdate
{
    public void MyUpdate();
}





/*
public interface IFPSCounter
{
    /// <summary>
    /// Below to 120 fps
    /// </summary>
    public bool BelowHightFrameRate { get; }


    /// <summary>
    /// Below to 60 fps
    /// </summary>
    public bool BelowMediumFrameRate { get; }

    /// <summary>
    /// Below to 30 fps
    /// </summary>
    public bool BelowLowFrameRate { get; }

    /// <summary>
    /// Below to 15 fps
    /// </summary>
    public bool BelowVeryLowFrameRate { get; }

    /// <summary>
    /// Below to 1 fps
    /// </summary>
    public bool BelowWatchDogFrameRate { get; }
}
*/

public class FPSCounter //: IFPSCounter
{
    private const int FPS120 = 1000 / 120;
    private const int FPS60 = 1000 / 60;
    private const int FPS30 = 1000 / 30;
    private const int FPS15 = 1000 / 15;

    /// <summary>
    /// Below to 120 fps
    /// </summary>
    public bool BelowHightFrameRate => ElapsedMilliseconds > FPS120;

    /// <summary>
    /// Below to 60 fps
    /// </summary>
    public bool BelowMediumFrameRate => ElapsedMilliseconds > FPS60;

    /// <summary>
    /// Below to 30 fps
    /// </summary>
    public bool BelowLowFrameRate => ElapsedMilliseconds > FPS30;

    /// <summary>
    /// Below to 15 fps
    /// </summary>
    public bool BelowVeryLowFrameRate => ElapsedMilliseconds > FPS15;

    /// <summary>
    /// Below to 1 fps
    /// </summary>
    public bool BelowWatchDogFrameRate => ElapsedMilliseconds > 1000;

    private long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    private readonly System.Diagnostics.Stopwatch _stopwatch = new ();

    private IUpdateManager updateManager;

    public FPSCounter(IUpdateManager updateManager)
    {
        this.updateManager = updateManager;
        updateManager.OnUpdateEvnt += Update;
    }

    void Update()
    {
        _stopwatch.Restart();
    }

    public void Destroy()
    {
        updateManager.OnUpdateEvnt -= Update;
        _stopwatch.Stop();
    }
}


