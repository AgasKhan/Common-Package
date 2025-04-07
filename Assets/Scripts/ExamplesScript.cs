using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExamplesScript : MonoBehaviour, IUpdate
{
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
