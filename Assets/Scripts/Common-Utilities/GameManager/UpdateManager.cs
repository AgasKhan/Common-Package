using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IEnable
{
    bool Enable {get;}
}

public interface IUpdate : IEnable, IIndexed
{
    public void MyUpdate();
}

public interface ILateUpdate : IEnable, IIndexed
{
    public void MyLateUpdate();
    
}

public interface IEndUpdate : IEnable, IIndexed
{
    public void MyEndUpdate();
}

public interface IDeferredUpdate : IEnable, IIndexed
{
    public void MyDeferredUpdate();
}

public interface IFixedUpdate : IEnable, IIndexed
{
    public void MyFixedUpdate();
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

public interface IEndUpdateManager
{
    public static IEndUpdateManager operator + (IEndUpdateManager lvalue, IEndUpdate rvalue)
    {
        lvalue.OnEndUpdateEvnt += rvalue.MyEndUpdate;
        return lvalue;
    }
    
    public static IEndUpdateManager operator - (IEndUpdateManager lvalue, IEndUpdate rvalue)
    {
        lvalue.OnEndUpdateEvnt -= rvalue.MyEndUpdate;
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
    
    public void Add<T>(T Object, Delegate<T> update, GmFPSTrehold? gmFPSTrehold = null) where T : IIndexed;

    public void Remove<T>(T Object, Delegate<T> update) where T : IIndexed;
}

public interface ISuperUpdateManager : IUpdateManager, IFixedUpdateManager, ILateUpdateManager, IEndUpdateManager, IDeferredUpdateManager, IDataOrientedUpdateManager
{
}

namespace UpdateManager
{
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
    

    public class DataOrientedUpdate : IUpdate, IDataOrientedUpdateManager
    {
        public abstract class Data : IUpdate, IDisposable
        {
            public int Index { get; set; }
        
            public abstract bool Enable { get; }
            
            public abstract void Add(IIndexed Object);
            public abstract void Remove(IIndexed Object);

            public abstract void MyUpdate();
            
            public abstract void Dispose();
        }
        
        public class Data<T> : Data where T : IIndexed
        {
            public override bool Enable => _updateList.Count > 0;

            private GmFPSTrehold gmFPSTrehold;
            
            private RotatingList<T> _updateList = new();

            private IDataOrientedUpdateManager.Delegate<T> _delegate;

            private System.Action _update;

            public Data(IDataOrientedUpdateManager.Delegate<T> action , GmFPSTrehold? gmFPSTrehold)
            {
                _delegate = action;
                
                Debug.Log(gmFPSTrehold.HasValue);
                
                if (gmFPSTrehold.HasValue)
                {
                    this.gmFPSTrehold = gmFPSTrehold.Value;
                    _update = DeferredUpdate;
                }
                else
                    _update = NormalUpdate;
            }

            public override void Add(IIndexed Object)
            {
                ((T)Object).AddTo(_updateList);
            }

            public override void Remove(IIndexed Object)
            {
                ((T)Object).RemoveToAtSwapBack(_updateList);
            }
            
            public override void MyUpdate()
            {
                _update();
            }

            void DeferredUpdate()
            {
                for (int i = 0; i < _updateList.Count; i++)
                {
                    _delegate(ref _updateList.GetNext());

                    if (gmFPSTrehold)
                    {
                        break;
                    }
                }
            }
            
            void NormalUpdate()
            {
                for (int i = 0; i < _updateList.Count; i++)
                {
                    _delegate(ref _updateList.GetValue(i));
                }
            }
            
            public override void Dispose()
            {
                _updateList.Clear();
                _updateList = null;
                _delegate = null;
                _update = null;
            }
        }
        
        private Dictionary<System.Delegate, Data> _dataOriented = new ();

        public bool Enable => _dataOriented.Count > 0;
        
        public int Index { get; set; }

        public void Add<T>(T Object, IDataOrientedUpdateManager.Delegate<T> update, GmFPSTrehold? gmFPSTrehold = null) where T : IIndexed
        {
            if (_dataOriented.TryGetValue(update, out var value))
            {
                value.Add(Object);
            }
            else
            {
                var newUpdates = new Data<T>(update,gmFPSTrehold);
                
                newUpdates.Add(Object);
                
                _dataOriented.Add(update, newUpdates);
            }
        }

        public void Remove<T>(T Object, IDataOrientedUpdateManager.Delegate<T> update) where T : IIndexed
        {
            if (_dataOriented.TryGetValue(update, out var value))
            {
                value.Remove(Object);
            }
        }
        
        public void MyUpdate()
        {
            foreach (var keyValue in _dataOriented)
            {
                if(keyValue.Value.Enable)
                    keyValue.Value.MyUpdate();
            }
        }

        public void Clear()
        {
            foreach (var keyValue in _dataOriented)
            {
                keyValue.Value.Dispose();
            }
            _dataOriented.Clear();
        }
    }
        
    public class DeferredUpdateManager : IDeferredUpdateManager
    {
        public GmFPSTrehold gmFPSTrehold;
        
        private RotatingList<IDeferredUpdate> _updateList = new();
        
        public void Add(IDeferredUpdate deferredUpdate)
        {
            deferredUpdate.AddTo(_updateList);
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
            if(_updateList.Count==0)
                return;
            
            foreach (var updateDeferred in _updateList)
            {
                if(updateDeferred.Enable)
                    updateDeferred.MyDeferredUpdate();

                if (gmFPSTrehold)
                {
                    break;
                }
            }
        }
    }
}



