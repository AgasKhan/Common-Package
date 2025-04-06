using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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


public interface ISuperUpdateManager : IUpdateManager, IFixedUpdateManager, ILateUpdateManager, IEndUpdateManager, IDeferredUpdateManager
{
}


public interface IUpdate
{
    public void MyUpdate();
}

public interface ILateUpdate
{
    public void MyLateUpdate();
}

public interface IEndUpdate
{
    public void MyEndUpdate();
}

public interface IDeferredUpdate : IIndexed
{
    public void MyDeferredUpdate();
}

public interface IFixedUpdate
{
    public void MyFixedUpdate();
}