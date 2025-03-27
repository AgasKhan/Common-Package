using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour, ISuperUpdateManager
{
    public interface IGameManagerState : ISuperUpdateManager, IDelayedAction
    {}
    
    [System.Serializable]
    private class FSMGameManager : FSMParent<FSMGameManager,GameManager,GameManagerState>
    {
        public GameManagerState gamePlay = new GameManagerState();
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

        public void Destroy()
        {
            gamePlay.Destroy();
            load.Destroy();
        }
    }
    
    [System.Serializable]
    private class GameManagerState : IState<FSMGameManager>
    {
        [SerializeField]
        public UnityEvent onEnter = new UnityEvent();
        
        [SerializeField]
        public UnityEvent onExit = new UnityEvent();
        
        protected virtual bool FrameRateTreshold() => BelowHightFrameRate;
        
        protected GameManagerStateStatic gameManagerStateStatic;

        public virtual void Init(IGameManagerState gameManagerState)
        {
            gameManagerStateStatic = gameManagerState as GameManagerStateStatic;
        }
        
        public virtual void OnEnter(FSMGameManager param)
        {
            onEnter.Invoke();
            gameManagerStateStatic.OnEnter(param, FrameRateTreshold);
        }
        
        public virtual void OnStay(FSMGameManager param)
        {
            gameManagerStateStatic.OnStay(param, FrameRateTreshold);
        }
        
        public virtual void OnFixed(FSMGameManager param)
        {
            gameManagerStateStatic.OnFixed(param);
        }

        public virtual void OnLate(FSMGameManager param)
        {
            gameManagerStateStatic.OnLate(param);
        }

        public virtual void OnExit(FSMGameManager param)
        {
            onExit.Invoke();
            gameManagerStateStatic.OnExit(param);
        }

        public virtual void Destroy()
        {
            gameManagerStateStatic.Destroy();
        }
    }
    
    [System.Serializable]
    private class LoadState : GameManagerState
    {
        protected override bool FrameRateTreshold() => GameManager.BelowLowFrameRate;
        
        public float ProgressQueue => (gameManagerStateStatic.routineQueue.Progress + gameManagerStateStatic.eventQueue.Progress) / 2;

        public override void Init(IGameManagerState gameManagerState)
        {
            base.Init(gameManagerState);
            
            gameManagerStateStatic.onAddEventQueue += OnAddEventQueue;
            gameManagerStateStatic.onAddRoutineQueue += OnAddRoutineQueue;
        }
        
        public override void OnLate(FSMGameManager param)
        {
            base.OnLate(param);

            if (ProgressQueue == 1)
                param.Current = param.gamePlay;
        }

        public override void Destroy()
        {
            gameManagerStateStatic.onAddEventQueue -= OnAddEventQueue;

            gameManagerStateStatic.onAddRoutineQueue -= OnAddRoutineQueue;

            base.Destroy();
        }
        
        private void OnAddEventQueue(UnityAction arg0, ProgressQueue<UnityAction> arg1)
        {
            //Sistema de carga
        }
        
        private void OnAddRoutineQueue(IEnumerator arg0, ProgressQueue<IEnumerator> arg1)
        {
            //Sistema de carga
        }
    }
    
    private class GameManagerStateStatic : IGameManagerState
    {
        public event UnityAction OnUpdateEvnt;
        public event UnityAction OnLateUpdateEvnt;
        public event UnityAction OnFixedUpdateEvnt;
        
        public event UnityAction EventQueue
        {
            add
            {
                onAddEventQueue.Invoke(value, eventQueue);
            }
            remove{}
        }

        public IEnumerator RoutineQueue
        {
            set => onAddRoutineQueue.Invoke(value, routineQueue);
        }
        
        public ProgressQueue<UnityAction> eventQueue = new();
        
        public ProgressQueue<IEnumerator> routineQueue = new();
        
        public UnityAction<UnityAction, ProgressQueue<UnityAction>> onAddEventQueue;
        
        public UnityAction<IEnumerator, ProgressQueue<IEnumerator>> onAddRoutineQueue;

        private Coroutine _coroutine;

        private bool _isEnter;
        
        public GameManagerStateStatic(UnityAction<UnityAction, ProgressQueue<UnityAction>> onAddEventQueue, UnityAction<IEnumerator, ProgressQueue<IEnumerator>> onAddRoutineQueue)
        {
            this.onAddEventQueue = onAddEventQueue;
            this.onAddRoutineQueue = onAddRoutineQueue;
        }
        
        public void OnEnter(FSMGameManager context, System.Func<bool> FrameRateTreshold)
        {
            _isEnter = true;
            
            _coroutine ??= context.Context.StartCoroutine(Routine(FrameRateTreshold));//Ejecuto la corrutina si no se estaba ejecutando
        }

        public void OnStay(FSMGameManager context, System.Func<bool> FrameRateTreshold)
        {
            OnUpdateEvnt?.Invoke();

            do
            {
                if(eventQueue.TryDequeue(out var result))
                    result.Invoke();
                else
                    break;
                
            } while (FrameRateTreshold());
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
            _isEnter = false;
        }

        private IEnumerator Routine(System.Func<bool> FrameRateTreshold)
        {
            while (_isEnter)
            {
                do
                {
                    if(routineQueue.TryDequeue(out var routine))
                        yield return routine;
                    else
                        break;
                
                } while (FrameRateTreshold());

                yield return null;
            }

            _coroutine = null;
        }

        public void Destroy()
        {
            OnUpdateEvnt = null;
            OnLateUpdateEvnt = null;
            OnFixedUpdateEvnt = null;

            _isEnter = false;
            
            eventQueue.Clear();
            routineQueue.Clear();
        }
    }
    
    public static GameManager instance { get; private set; }

    public static readonly IGameManagerState GamePlayManager = new GameManagerStateStatic(AddToLoadIfChargeEventQueue,AddToLoadIfChargeRoutineQueue);
    
    public static readonly IGameManagerState LoadManager = new GameManagerStateStatic(AddDirectEventQueue,AddDirectRoutineQueue);
    
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
            instance._fsmGameManager.gamePlay.onEnter.AddListener(value);
        }
        remove
        {
            instance._fsmGameManager.gamePlay.onEnter.RemoveListener(value);
        }
    }
    
    public static event UnityAction OnPause
    {
        add
        {
            instance._fsmGameManager.gamePlay.onExit.AddListener(value);
        }
        remove
        {
            instance._fsmGameManager.gamePlay.onExit.RemoveListener(value);
        }
    }


    
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

    #region Static methods queue

    static void AddToLoadIfChargeEventQueue(UnityAction arg0, ProgressQueue<UnityAction> progressQueue)
    {
        if (instance?._fsmGameManager.Current == instance?._fsmGameManager.load)
            LoadManager.EventQueue += arg0;
        else
            progressQueue.Enqueue(arg0);
    }
    
    static void AddToLoadIfChargeRoutineQueue(IEnumerator enumerator, ProgressQueue<IEnumerator> queue)
    {
        if (instance?._fsmGameManager.Current == instance?._fsmGameManager.load)
            LoadManager.RoutineQueue = enumerator;
        else
            queue.Enqueue(enumerator);
    }

    static void AddDirectEventQueue(UnityAction arg0, ProgressQueue<UnityAction> progressQueue)
    {
        progressQueue.Enqueue(arg0);
    }
    
    static void AddDirectRoutineQueue(IEnumerator arg0, ProgressQueue<IEnumerator> progressQueue)
    {
        progressQueue.Enqueue(arg0);
    }

    #endregion
    
    
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
        
        _fsmGameManager.gamePlay.Init(GamePlayManager);
        
        _fsmGameManager.load.Init(LoadManager);

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
        
        _fsmGameManager.Destroy();
        
        OnAwakeEvnt = null;
        OnStartEvnt = null;
        OnUpdateEvnt = null;
        OnLateUpdateEvnt = null;
        OnFixedUpdateEvnt = null;
        OnDestroyEvnt = null;
    }
}


public interface IDelayedAction
{
    /// <summary>
    /// Cola de acciones que se ejecutaran uno a la vez y en sucecion
    /// </summary>
    public event UnityAction EventQueue;

    /// <summary>
    /// Cola de coroutines que se ejecutaran una a la vez y en sucecion
    /// </summary>
    public IEnumerator RoutineQueue { set; }
}


public interface IUpdateManager
{
    public static IUpdateManager operator + (IUpdateManager lvalue, IUpdate rvalue)
    {
        lvalue.OnUpdateEvnt += rvalue.MyUpdate;
        return lvalue;
    }
    
    public static IUpdateManager operator - (IUpdateManager lvalue, IUpdate rvalue)
    {
        lvalue.OnUpdateEvnt -= rvalue.MyUpdate;
        return lvalue;
    }

    public event UnityAction OnUpdateEvnt;
}

public interface ILateUpdateManager
{
    public static ILateUpdateManager operator + (ILateUpdateManager lvalue, ILateUpdate rvalue)
    {
        lvalue.OnLateUpdateEvnt += rvalue.MyLateUpdate;
        return lvalue;
    }
    
    public static ILateUpdateManager operator - (ILateUpdateManager lvalue, ILateUpdate rvalue)
    {
        lvalue.OnLateUpdateEvnt -= rvalue.MyLateUpdate;
        return lvalue;
    }
    
    public event UnityAction OnLateUpdateEvnt;
}

public interface IFixedUpdateManager
{
    public static IFixedUpdateManager operator + (IFixedUpdateManager lvalue, IFixedUpdate rvalue)
    {
        lvalue.OnFixedUpdateEvnt += rvalue.MyFixedUpdate;
        return lvalue;
    }
    
    public static IFixedUpdateManager operator - (IFixedUpdateManager lvalue, IFixedUpdate rvalue)
    {
        lvalue.OnFixedUpdateEvnt -= rvalue.MyFixedUpdate;
        return lvalue;
    }
    
    public event UnityAction OnFixedUpdateEvnt;
}

public interface ISuperUpdateManager : IUpdateManager, IFixedUpdateManager, ILateUpdateManager
{
    
}

public interface IUpdate
{
    public void MyUpdate();
}

public interface IFixedUpdate
{
    public void MyFixedUpdate();
}

public interface ILateUpdate
{
    public void MyLateUpdate();
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

public class ProgressQueue<T>
{
    private Queue<T> queue = new();

    public float Progress => QueueCount == 0 ? 1 : ProgressCount / (float)QueueCount;

    private int ProgressCount { get; set; }

    private int QueueCount { get; set; }
    
    public void Enqueue(T value)
    {
        queue.Enqueue(value);
        QueueCount++;
    }

    public bool TryDequeue(out T value)
    {
        if (queue.TryDequeue(out value))
        {
            ProgressCount++;
            return true;
        }
        else
        {
            QueueCount = 0;
            ProgressCount = 0;
        }

        return false;
    }

    public void Clear()
    {
        QueueCount = 0;
        ProgressCount = 0;
        queue.Clear();
    }
}
