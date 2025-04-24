using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UpdateManager;

[DefaultExecutionOrder(-1)]
public partial class GameManager : MonoBehaviour, ISuperUpdateManager
{
    public static GameManager instance { get; private set; }

    public static readonly IGameManagerState GamePlayManager = new GameManagerStateStatic(AddToLoadIfChargeEventQueue,AddToLoadIfChargeRoutineQueue);
    
    public static readonly IGameManagerState LoadManager = new GameManagerStateStatic(AddDirectEventQueue,AddDirectRoutineQueue);
    
    public static IDeferredUpdate DeferredUpdates
    {
        set => _deferredUpdateManager.Add(value);
    }
    
    private static readonly DeferredUpdateManager _deferredUpdateManager = new ();
    
    #region events

    public static event UnityAction OnAwakeEvnt;
    
    public static event UnityAction OnStartEvnt;
    
    public static event UnityAction OnUpdateEvnt;
    
    public static event UnityAction OnLateUpdateEvnt;
    
    public static event UnityAction OnFixedUpdateEvnt;
    
    public static event UnityAction OnDestroyEvnt;
    
    public static event UnityAction OnEndUpdateEvnt;
    
    public static event UnityAction OnPlay
    {
        add
        {
            instance?._fsmGameManager.gamePlay.onEnter.AddListener(value);
        }
        remove
        {
            instance?._fsmGameManager.gamePlay.onEnter.RemoveListener(value);
        }
    }
    
    public static event UnityAction OnPause
    {
        add
        {
            instance?._fsmGameManager.gamePlay.onExit.AddListener(value);
        }
        remove
        {
            instance?._fsmGameManager.gamePlay.onExit.RemoveListener(value);
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
    
    event UnityAction IEndUpdateManager.OnEndUpdateEvnt
    {
        add => OnEndUpdateEvnt += value;
        remove => OnEndUpdateEvnt -= value;
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
    private UnityEvent onEndUpdate = new UnityEvent();
    
    [SerializeField]
    private UnityEvent onFixedUpdate = new UnityEvent();
    
    [SerializeField]
    private UnityEvent onDestroy = new UnityEvent();
    
    #endregion
    

    [SerializeField]
    private GmFPSTrehold deferredUpdateTrehold;
    
    [SerializeField]
    private FSMGameManager _fsmGameManager;

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
    
    void IDeferredUpdateManager.Add(IDeferredUpdate deferredUpdate)
    {
        _deferredUpdateManager.Add(deferredUpdate);
    }

    void IDeferredUpdateManager.Remove(IDeferredUpdate deferredUpdate)
    {
        _deferredUpdateManager.Remove(deferredUpdate);
    }
    
    void IDataOrientedUpdateManager.Add<T>(T Object, IDataOrientedUpdateManager.Delegate<T> update, GmFPSTrehold? gmFPSTrehold)
    {
        GamePlayManager.Add(Object, update, gmFPSTrehold);
    }

    void IDataOrientedUpdateManager.Remove<T>(T Object, IDataOrientedUpdateManager.Delegate<T> update)
    {
        GamePlayManager.Remove(Object, update);
    }

    
    private void Awake()
    {
        instance = this;
        
        onAwake.AddListener(()=> OnAwakeEvnt?.Invoke());
        onStart.AddListener(()=> OnStartEvnt?.Invoke());
        onUpdate.AddListener(()=> OnUpdateEvnt?.Invoke());
        onLateUpdate.AddListener(()=> OnLateUpdateEvnt?.Invoke());
        onEndUpdate.AddListener(()=> OnEndUpdateEvnt?.Invoke());
        onFixedUpdate.AddListener(()=> OnFixedUpdateEvnt?.Invoke());
        onDestroy.AddListener(()=> OnDestroyEvnt?.Invoke());
        

        
        _fsmGameManager.gamePlay.Init(GamePlayManager);
        
        _fsmGameManager.load.Init(LoadManager);

        _fsmGameManager.Init(this);

        StartCoroutine(EndOfFrameUpdate());

        _deferredUpdateManager.gmFPSTrehold = deferredUpdateTrehold;
        
        onAwake.Invoke();
    }
    
    private IEnumerator EndOfFrameUpdate()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            EndUpdate();
        }
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
        
        _deferredUpdateManager.MyUpdate();
        
        _fsmGameManager.LateUpdate();
    }

    private void EndUpdate()
    {
        onEndUpdate.Invoke();
        
        _fsmGameManager.EndUpdate();
    }

    private void FixedUpdate()
    {
        onFixedUpdate.Invoke();
        
        _fsmGameManager.FixedUpdate();
    }

    private void OnGUI()
    {
        GUI.Label(new Rect (25, 25, 500, 25), _fsmGameManager.Current.GetType().Name);
        
        EngineUpdate.OnGuiFPS(new Rect (25, 50, 500, 50));
    }

    private void OnDestroy()
    {
        onDestroy.Invoke();
        
        _fsmGameManager.Destroy();
        
        OnAwakeEvnt = null;
        OnStartEvnt = null;
        OnUpdateEvnt = null;
        OnLateUpdateEvnt = null;
        OnEndUpdateEvnt = null;
        OnFixedUpdateEvnt = null;
        OnDestroyEvnt = null;
        
        _deferredUpdateManager.Clear();
    }
}


