using System;
using System.Collections;
using System.Collections.Generic;
using SystemEngineUpdate;
using UnityEngine;

public class ExamplesScript : MonoBehaviour, IUpdate
{
    public int Index { get; set; }
    public bool Enable {get; set;}

    private IUpdateManager _updateManager;
    
    private void Awake()
    {
        _updateManager = GetComponentInParent<IUpdateManager>();

        if (_updateManager == null)
            _updateManager = GameManager.GamePlayManager;

        _updateManager += this;
        
        GameManager.OnPause += GameManagerOnPause;
    }

    private void GameManagerOnPause()
    {
        
    }

    public void MyUpdate()
    {
        //Debug.Log("Update Manager -> " + (_updateManager as Component)?.name);
    }
}
