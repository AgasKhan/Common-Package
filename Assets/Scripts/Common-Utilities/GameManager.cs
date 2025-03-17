using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour, IUpdateManager, ILateUpdateManager, IFixedUpdateManager
{
    public static event UnityAction OnAwakeEvnt;
    
    public static event UnityAction OnStartEvnt;
    
    public static event UnityAction OnUpdateEvnt;
    
    public static event UnityAction OnLateUpdateEvnt;
    
    public static event UnityAction OnFixedUpdateEvnt;
    
    public static event UnityAction OnDestroyEvnt;
    
    
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

    public static GameManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateInScene()
    {
        var aux = FindObjectOfType<GameManager>();
        if(aux!=null)
            return;

        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }
    
    private void Awake()
    {
        instance = this;
        
        onAwake.AddListener(()=> OnAwakeEvnt?.Invoke());
        onStart.AddListener(()=> OnStartEvnt?.Invoke());
        onUpdate.AddListener(()=> OnUpdateEvnt?.Invoke());
        onLateUpdate.AddListener(()=> OnLateUpdateEvnt?.Invoke());
        onFixedUpdate.AddListener(()=> OnFixedUpdateEvnt?.Invoke());
        onDestroy.AddListener(()=> OnDestroyEvnt?.Invoke());
        
        onAwake.Invoke();
    }

    private void Start()
    {
        onStart.Invoke();
    }

    private void Update()
    {
        onUpdate.Invoke();   
    }

    private void LateUpdate()
    {
        onLateUpdate.Invoke();
    }

    private void FixedUpdate()
    {
        onFixedUpdate.Invoke();
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

