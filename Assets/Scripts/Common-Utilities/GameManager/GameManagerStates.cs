using System;
using System.Collections;
using System.Collections.Generic;
using SystemEngineUpdate;
using UnityEngine;
using UnityEngine.Events;
public partial class GameManager
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
        private FPSTreholdSelector frameRateEventTreshold;
        
        [SerializeField]
        private FPSTreholdSelector frameRateDeferredTreshold;
        
        [SerializeField]
        public UnityEvent onEnter = new UnityEvent();
        
        [SerializeField]
        public UnityEvent onExit = new UnityEvent();
        
        protected GameManagerStateStatic gameManagerStateStatic;

        public virtual void Init(IGameManagerState gameManagerState)
        {
            gameManagerStateStatic = gameManagerState as GameManagerStateStatic;

            gameManagerStateStatic.DeferredTrehold = frameRateDeferredTreshold;
        }
        
        public virtual void OnEnter(FSMGameManager param)
        {
            onEnter.Invoke();
            gameManagerStateStatic.OnEnter(param, frameRateEventTreshold);
        }
        
        public virtual void OnStay(FSMGameManager param)
        {
            gameManagerStateStatic.OnStay(param, frameRateEventTreshold);
        }
        
        public virtual void OnFixed(FSMGameManager param)
        {
            gameManagerStateStatic.OnFixed(param, frameRateEventTreshold);
        }

        public virtual void OnLate(FSMGameManager param)
        {
            gameManagerStateStatic.OnLate(param, frameRateEventTreshold);
        }
        
        public virtual void OnEnd(FSMGameManager param)
        {
            gameManagerStateStatic.OnEnd(param, frameRateEventTreshold);
        }

        public virtual void OnExit(FSMGameManager param)
        {
            onExit.Invoke();
            gameManagerStateStatic.OnExit(param, frameRateEventTreshold);
        }

        public virtual void Destroy()
        {
            gameManagerStateStatic.Destroy();
        }
    }
    
    [System.Serializable]
    private class LoadState : GameManagerState
    {
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

            if (ProgressQueue == 1 && !gameManagerStateStatic.hasCorutine)
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

        public FPSTreholdSelector DeferredTrehold
        {
            set => _deferredUpdates.gmFPSTrehold = value;
        }
        
        public bool hasCorutine => _routine != null;
        
        public ProgressQueue<UnityAction> eventQueue = new();
        
        public  ProgressQueue<IEnumerator> routineQueue = new();
        
        public UnityAction<UnityAction, ProgressQueue<UnityAction>> onAddEventQueue;
        
        public UnityAction<IEnumerator, ProgressQueue<IEnumerator>> onAddRoutineQueue;
        
        public DataOrientedUpdate dataOrientedUpdate = new();

        private DeferredUpdateManager _deferredUpdates = new();
        
        private Coroutine _coroutine;

        private IEnumerator _routine;

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
        
        public void Add<T>(T Object, IDataOrientedUpdateManager.Delegate<T> update, FPSTreholdSelector? gmFPSTrehold = null) where T : IIndexed
        {
            dataOrientedUpdate.Add(Object, update, gmFPSTrehold);
        }

        public void Remove<T>(T Object,IDataOrientedUpdateManager.Delegate<T> update) where T : IIndexed
        {
            dataOrientedUpdate.Remove(Object, update);
        }
        
        public void OnEnter(FSMGameManager context, FPSTreholdSelector frameRateTreshold)
        {
            _isEnter = true;
            _coroutine ??= context.Context.StartCoroutine(Routine(frameRateTreshold));//Ejecuto la corrutina si no se estaba ejecutando
        }

        public void OnStay(FSMGameManager context, FPSTreholdSelector frameRateTreshold)
        {
            OnUpdateEvnt?.Invoke();
        }
        
        public void OnLate(FSMGameManager gameManager, FPSTreholdSelector frameRateTreshold)
        {
            OnLateUpdateEvnt?.Invoke();
            
            do
            {
                if(eventQueue.TryDequeue(out var result))
                    result.Invoke();
                else
                    break;
                
            } while (!frameRateTreshold);
            
            dataOrientedUpdate.MyUpdate();
            
            _deferredUpdates.MyUpdate();
        }
        
        public void OnFixed(FSMGameManager gameManager, FPSTreholdSelector frameRateTreshold)
        {
            OnFixedUpdateEvnt?.Invoke();
        }

        public void OnEnd(FSMGameManager gameManager, FPSTreholdSelector frameRateTreshold)
        {
            OnEndUpdateEvnt?.Invoke();
        }

        public void OnExit(FSMGameManager context, FPSTreholdSelector frameRateTreshold)
        {
            _isEnter = false;
        }

        private IEnumerator Routine(FPSTreholdSelector FrameRateTreshold)
        {
            while (_isEnter)
            {
                do
                {
                    if (routineQueue.TryDequeue(out _routine))
                    {
                        yield return _routine;
                    }
                    else
                        break;
                
                } while (!FrameRateTreshold);

                _routine = null;

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
            
            dataOrientedUpdate.Clear();
        }
    }
}

