using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SystemEngineUpdate
{
    public interface IEnable
    {
        bool Enable {get;}
    }

    #region Updates

    public interface IStartUpdate
    {
        public void StartUpdate();
    }

    public interface IUpdate 
    {
        public void MyUpdate();
    }
    public interface IPreUpdate
    {
        void PreUpdate();
    }

    public interface IPostUpdate
    {
        void PostUpdate();
    }

    public interface ILateUpdate 
    {
        public void MyLateUpdate();
    }
    
    public interface IPreLateUpdate
    {
        void PreLateUpdate();
    }

    public interface IPostLateUpdate
    {
        void PostLateUpdate();
    }

    public interface IFixedUpdate
    {
        public void MyFixedUpdate();
    }
    
    
    public interface IPreFixedUpdate
    {
        void PreFixedUpdate();
    }

    public interface IPostFixedUpdate
    {
        void PostFixedUpdate();
    }
    
    public interface IEndUpdate
    {
        public void EndUpdate();
    }

    public interface IDeferredUpdate : IIndexed
    {
        public void MyDeferredUpdate();
    }
    
    #endregion

    #region UpdateManagers
    
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

    public interface IEndUpdateManager
    {
        public static IEndUpdateManager operator + (IEndUpdateManager lvalue, IEndUpdate rvalue)
        {
            lvalue.OnEndUpdateEvnt += rvalue.EndUpdate;
            return lvalue;
        }
        
        public static IEndUpdateManager operator - (IEndUpdateManager lvalue, IEndUpdate rvalue)
        {
            lvalue.OnEndUpdateEvnt -= rvalue.EndUpdate;
            return lvalue;
        }
        
        public event UnityAction OnEndUpdateEvnt;
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

    public interface IDeferredUpdateManager
    {
        public static IDeferredUpdateManager operator + (IDeferredUpdateManager lvalue, IDeferredUpdate rvalue)
        {
            lvalue.Add(rvalue);
            return lvalue;
        }
        
        public static IDeferredUpdateManager operator - (IDeferredUpdateManager lvalue, IDeferredUpdate rvalue)
        {
            lvalue.Remove(rvalue);
            return lvalue;
        }

        public void Add(IDeferredUpdate deferredUpdate);
        public void Remove(IDeferredUpdate deferredUpdate);
    }

    public interface IDataOrientedUpdateManager
    {
        public delegate void Delegate<T>(ref T obj);
        
        public void Add<T>(T Object, Delegate<T> update, FPSTreholdSelector? gmFPSTrehold = null) where T : IIndexed;

        public void Remove<T>(T Object, Delegate<T> update) where T : IIndexed;
    }
    
    #endregion
    
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

    public interface ISuperUpdateManager : IUpdateManager, IFixedUpdateManager, ILateUpdateManager, IEndUpdateManager, IDeferredUpdateManager, IDataOrientedUpdateManager
    {
    }

     public interface ILoadScene
     {
          void OnLoadScene(Scene arg0, LoadSceneMode loadSceneMode);
     }
     
     public interface IUnloadScene
     {
          void OnUnloadScene(Scene arg0);
     }

     public interface IActiveSceneChange
     {
          void OnActiveSceneChange(Scene arg0, Scene scene);
     }
     
     public interface IOnGUI
     {
          void OnGUI();
     }

     public interface IOnDrawGizmos
     {
          void OnDrawGizmos();
     }

     public interface IQuit
     {
          void Quit();
     }
}