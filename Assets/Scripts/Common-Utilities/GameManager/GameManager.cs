using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-1)]
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

        public void EndUpdate()
        {
            Current.OnEnd(this);
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
            gameManagerStateStatic.OnFixed(param, FrameRateTreshold);
        }

        public virtual void OnLate(FSMGameManager param)
        {
            gameManagerStateStatic.OnLate(param, FrameRateTreshold);
        }
        
        public virtual void OnEnd(FSMGameManager param)
        {
            gameManagerStateStatic.OnEnd(param, FrameRateTreshold);
        }

        public virtual void OnExit(FSMGameManager param)
        {
            onExit.Invoke();
            gameManagerStateStatic.OnExit(param, FrameRateTreshold);
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
        public event UnityAction OnEndUpdateEvnt;
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
        
        public  ProgressQueue<IEnumerator> routineQueue = new();
        
        public UnityAction<UnityAction, ProgressQueue<UnityAction>> onAddEventQueue;
        
        public UnityAction<IEnumerator, ProgressQueue<IEnumerator>> onAddRoutineQueue;

        private RotatingList<IDeferredUpdate> _deferredUpdates = new();
        
        private Coroutine _coroutine;

        private bool _isEnter;
        
        public GameManagerStateStatic(UnityAction<UnityAction, ProgressQueue<UnityAction>> onAddEventQueue, UnityAction<IEnumerator, ProgressQueue<IEnumerator>> onAddRoutineQueue)
        {
            this.onAddEventQueue = onAddEventQueue;
            this.onAddRoutineQueue = onAddRoutineQueue;
        }
        
        void IDeferredUpdateManager.Add(IDeferredUpdate deferredUpdate)
        {
            _deferredUpdates.Add(deferredUpdate);
        }

        void IDeferredUpdateManager.Remove(IDeferredUpdate deferredUpdate)
        {
            _deferredUpdates.Remove(deferredUpdate);
        }
        
        public void OnEnter(FSMGameManager context, System.Func<bool> FrameRateTreshold)
        {
            _isEnter = true;
            _deferredUpdateManager.Add(_deferredUpdates);
            _coroutine ??= context.Context.StartCoroutine(Routine(FrameRateTreshold));//Ejecuto la corrutina si no se estaba ejecutando
        }

        public void OnStay(FSMGameManager context, System.Func<bool> FrameRateTreshold)
        {
            OnUpdateEvnt?.Invoke();
        }
        
        public void OnLate(FSMGameManager gameManager, System.Func<bool> FrameRateTreshold)
        {
            OnLateUpdateEvnt?.Invoke();
            
            do
            {
                if(eventQueue.TryDequeue(out var result))
                    result.Invoke();
                else
                    break;
                
            } while (FrameRateTreshold());
        }
        
        public void OnFixed(FSMGameManager gameManager, System.Func<bool> FrameRateTreshold)
        {
            OnFixedUpdateEvnt?.Invoke();
        }

        public void OnEnd(FSMGameManager gameManager, System.Func<bool> FrameRateTreshold)
        {
            OnEndUpdateEvnt?.Invoke();
        }

        public void OnExit(FSMGameManager context, System.Func<bool> FrameRateTreshold)
        {
            _isEnter = false;
            _deferredUpdateManager.Remove(_deferredUpdates);
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

            _deferredUpdates.Clear();
            
            _deferredUpdateManager.Remove(_deferredUpdates);
        }
    }
    
    private class DeferredUpdateManager : IDeferredUpdateManager
    {
        private CompositeRotatingList<IDeferredUpdate> _updateList = new CompositeRotatingList<IDeferredUpdate>();

        public void Add(RotatingList<IDeferredUpdate> listUpdates)
        {
            _updateList += listUpdates;
        }
        
        public void Add(IDeferredUpdate deferredUpdate)
        {
            deferredUpdate.AddTo(_updateList);
        }

        public void Remove(RotatingList<IDeferredUpdate> listUpdates)
        {
            _updateList -= listUpdates;
        }
        
        public void Remove(IDeferredUpdate deferredUpdate)
        {
            deferredUpdate.AddTo(_updateList);
        }

        public void Clear()
        {
            _updateList.Clear();
        }

        public void MyUpdate()
        {
            foreach (var updateDeferred in _updateList)
            {
                updateDeferred.MyDeferredUpdate();

                if (BelowHightFrameRate)
                {
                    break;
                }
            }
        }
    }
    
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

    
    #region FrameRate threshold
    public static bool BelowHightFrameRate => instance?.framesPerSecond.BelowHightFrameRate ?? false;

    public static bool BelowMediumFrameRate => instance?.framesPerSecond.BelowMediumFrameRate ?? false;
    public static bool BelowLowFrameRate => instance?.framesPerSecond.BelowLowFrameRate ?? false;

    public static bool BelowVeryLowFrameRate => instance?.framesPerSecond.BelowVeryLowFrameRate ?? false;

    public static bool BelowWatchDogFrameRate => instance?.framesPerSecond.BelowWatchDogFrameRate ?? false;
    
    private FPSCounter<GameManager> framesPerSecond;
    
    #endregion

    [SerializeField]
    private FSMGameManager _fsmGameManager;
    
    
#if UNITY_EDITOR
    static UnityEditor.Build.NamedBuildTarget CurrentNamedBuildTarget
    {
        get
        {
#if UNITY_SERVER
            return NamedBuildTarget.Server;
#else
            UnityEditor.BuildTarget buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            UnityEditor.BuildTargetGroup targetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(buildTarget);
            UnityEditor.Build.NamedBuildTarget namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            return namedBuildTarget;
#endif
        }
    }
    
    static GameManager CreateInScene()
    {
        if (instance != null)
            return instance;
        
        var aux = FindObjectOfType<GameManager>();
        if(aux!=null)
            return aux;

        GameObject go = new GameObject("GameManagers");
        var newGm = go.AddComponent<GameManager>();
        
        Debug.LogWarning("Se creo un nuevo GameManager para la escena", newGm);

        return newGm;
    }
    
    [UnityEditor.Callbacks.DidReloadScripts]
    static void EditorReloadScript()
    {
        const string defineStr = "HasGamemanager";

        var currentNameBuildTarget = CurrentNamedBuildTarget;
        
        CreateInScene();
        
        UnityEditor.PlayerSettings.GetScriptingDefineSymbols(currentNameBuildTarget, out string[] defines);
        
        List<string> definesList = new List<string>(defines);
        
        if(definesList.Contains(defineStr))
            return;

        definesList.Add(defineStr);
        
        UnityEditor.PlayerSettings.SetScriptingDefineSymbols(currentNameBuildTarget, definesList.ToArray());
    }

    public static T CreateManagerInScene<T>() where T : MonoBehaviour
    {
        var gm = CreateInScene();

        if (gm.gameObject.TryGetComponent(out T component))
        {
            return component;
        }

        return gm.gameObject.AddComponent<T>();
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
        onEndUpdate.AddListener(()=> OnEndUpdateEvnt?.Invoke());
        onFixedUpdate.AddListener(()=> OnFixedUpdateEvnt?.Invoke());
        onDestroy.AddListener(()=> OnDestroyEvnt?.Invoke());
        
        framesPerSecond = new(this);
        
        _fsmGameManager.gamePlay.Init(GamePlayManager);
        
        _fsmGameManager.load.Init(LoadManager);

        _fsmGameManager.Init(this);

        StartCoroutine(EndOfFrameUpdate());
        
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
        
        _fsmGameManager.LateUpdate();
        
        _deferredUpdateManager.MyUpdate();
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

    private void OnDestroy()
    {
        framesPerSecond.Destroy();
        
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

    void IDeferredUpdateManager.Add(IDeferredUpdate deferredUpdate)
    {
        _deferredUpdateManager.Add(deferredUpdate);
    }

    void IDeferredUpdateManager.Remove(IDeferredUpdate deferredUpdate)
    {
        _deferredUpdateManager.Remove(deferredUpdate);
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

public class RotatingList<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
{
    public int Actual { get; protected set; }
    
    public bool IsFixedSize => ((IList)_list).IsFixedSize;
    
    public int Count => _list.Count;

    public bool IsSynchronized => ((ICollection)_list).IsSynchronized;

    public object SyncRoot => ((ICollection)_list).SyncRoot;

    public bool IsReadOnly => ((IList<T>)_list).IsReadOnly;
    
    
    public T this[int index]
    {
        get => _list[index];
        set => _list[index] = value;
    }
    
    object IList.this[int index]
    {
        get => ((IList)_list)[index];
        set => ((IList)_list)[index] = value;
    }
    
    private List<T> _list = new List<T>();
    
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return _list[Actual];

            Actual = (Actual + 1) % Count;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_list).GetEnumerator();
    }

    public void Add(T item)
    {
        _list.Add(item);
    }

    public int Add(object value)
    {
        return ((IList)_list).Add(value);
    }
    
    public void Clear()
    {
        _list.Clear();
    }

    void IList.Clear()
    {
        _list.Clear();
    }

    public bool Contains(object value)
    {
        return ((IList)_list).Contains(value);
    }

    public int IndexOf(object value)
    {
        return ((IList)_list).IndexOf(value);
    }

    public void Insert(int index, object value)
    {
        ((IList)_list).Insert(index, value);
    }
    
    public bool Remove(T item)
    {
        return _list.Remove(item);
    }

    public void Remove(object value)
    {
        ((IList)_list).Remove(value);
    }

    public void RemoveAt(int index)
    {
        _list.RemoveAt(index);
    }
    
    void IList.RemoveAt(int index)
    {
        _list.RemoveAt(index);
    }
    
    public bool Contains(T item)
    {
        return _list.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    public void CopyTo(Array array, int index)
    {
        ((ICollection)_list).CopyTo(array, index);
    }
    
    public int IndexOf(T item)
    {
        return _list.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        _list.Insert(index, item);
    }
    
    public T GetNext()
    {
        if (Count == 0)
            throw new InvalidOperationException("The list is empty");
        T item = _list[Actual];
        Actual = (Actual + 1) % Count;
        return item;
    }
}

public class CompositeRotatingList<T> : IList<T>, IList
{
    private List<RotatingList<T>> _lists = new List<RotatingList<T>>();
    private readonly object _syncRoot = new object();

    // La cuenta global es la suma de las cuentas de cada lista interna.
    public int Count => _lists.Sum(cl => cl.Count);
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public bool IsSynchronized => false;
    public object SyncRoot => _syncRoot;

    public T this[int index]
    {
        get
        {
            var (list, localIndex) = GetListAndLocalIndex(index);
            return list[localIndex];
        }
        set
        {
            var (list, localIndex) = GetListAndLocalIndex(index);
            list[localIndex] = value;
        }
    }

    object IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T)value;
    }
    
    public static CompositeRotatingList<T> operator +(CompositeRotatingList<T> composite, RotatingList<T> cl)
    {
        composite.AddCircularList(cl);
        return composite;
    }

    public static CompositeRotatingList<T> operator -(CompositeRotatingList<T> composite, RotatingList<T> cl)
    {
        composite._lists.Remove(cl);
        return composite;
    }

    // Devuelve la lista interna y el índice local correspondiente a un índice global.
    private (RotatingList<T> list, int localIndex) GetListAndLocalIndex(int index)
    {
        if (index < 0 || index >= Count)
            throw new IndexOutOfRangeException();

        int cumulative = 0;
        foreach (var cl in _lists)
        {
            if (index < cumulative + cl.Count)
            {
                // Ajustamos el índice según la posición Actual de la lista interna.
                int offset = index - cumulative;
                int localIndex = (cl.Actual + offset) % cl.Count;
                return (cl, localIndex);
            }
            cumulative += cl.Count;
        }
        throw new IndexOutOfRangeException();
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var cl in _lists)
        {
            int localCount = cl.Count;
            for (int i = 0; i < localCount; i++)
                yield return cl.GetNext();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Permite agregar un RotatingList<T> al composite.
    public void AddCircularList(RotatingList<T> cl)
    {
        if (cl == null)
            throw new ArgumentNullException(nameof(cl));
        _lists.Add(cl);
    }

    // Agrega un elemento a la primera lista interna o crea una nueva si no existe.
    public void Add(T item)
    {
        if (_lists.Count == 0)
        {
            var newList = new RotatingList<T>();
            newList.Add(item);
            _lists.Add(newList);
        }
        else
        {
            _lists[0].Add(item);
        }
    }

    public void Clear()
    {
        _lists.Clear();
    }

    public bool Contains(T item)
    {
        return _lists.Any(cl => cl.Contains(item));
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (var cl in _lists)
        {
            cl.CopyTo(array, arrayIndex);
            arrayIndex += cl.Count;
        }
    }

    // Implementación de IList.CopyTo(Array, int)
    public void CopyTo(Array array, int index)
    {
        if (array is T[] typedArray)
        {
            CopyTo(typedArray, index);
        }
        else
        {
            // Si no es del tipo T[], se hace una copia elemento a elemento.
            foreach (var item in this)
            {
                array.SetValue(item, index++);
            }
        }
    }

    public int IndexOf(T item)
    {
        int cumulative = 0;
        foreach (var cl in _lists)
        {
            int idx = cl.IndexOf(item);
            if (idx != -1)
                return cumulative + idx;
            cumulative += cl.Count;
        }
        return -1;
    }

    public void Insert(int index, T item)
    {
        var (list, localIndex) = GetListAndLocalIndex(index);
        list.Insert(localIndex, item);
    }

    public bool Remove(T item)
    {
        foreach (var cl in _lists)
        {
            if (cl.Remove(item))
                return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        var (list, localIndex) = GetListAndLocalIndex(index);
        list.RemoveAt(localIndex);
    }

    // Implementaciones explícitas para IList (objetos)
    int IList.Add(object value)
    {
        Add((T)value);
        return Count - 1;
    }

    bool IList.Contains(object value) => Contains((T)value);
    int IList.IndexOf(object value) => IndexOf((T)value);
    void IList.Insert(int index, object value) => Insert(index, (T)value);
    void IList.Remove(object value) => Remove((T)value);
}
