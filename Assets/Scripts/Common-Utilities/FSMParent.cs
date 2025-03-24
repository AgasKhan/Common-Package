using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//estado
public interface IState
{
    public void OnEnter();
    public void OnStay();
    public void OnExit();
}

public interface IState<in T>
{
    public void OnEnter(T param);
    public void OnStay(T param);
    public void OnExit(T param);
}

public abstract class FSMClassic<TChild, TContext> : FSMParent<TChild, TContext, IState<TChild>> where TChild : FSMClassic<TChild, TContext>
{
}


public abstract class FSMParent<TChild, TContext, TState> where TChild : FSMParent<TChild, TContext, TState> where TState : IState<TChild>
{
    public TContext Context { get; private set; }
    
    private TChild myChild => (TChild)this; 
    
    public TState Current
    {
        get => _current;
        set
        {
            _current.OnExit(myChild);

            _current = value;

            _current.OnEnter(myChild);
        }
    }
    
    private TState _current;
    

    public void Init(TState initState, TContext context)
    {
        Context = context;
        _current = initState;
        _current.OnEnter(myChild);
    }
    
    public void Update()
    {
        Current.OnStay(myChild);
    }
}

