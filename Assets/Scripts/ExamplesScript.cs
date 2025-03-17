using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExamplesScript : MonoBehaviour
{
    private IUpdateManager _updateManager;
    
    private void Awake()
    {
        _updateManager = GetComponentInParent<IUpdateManager>();

        if (_updateManager == null)
            _updateManager = GameManager.instance;

        _updateManager.OnUpdateEvnt += MyUpdate;
    }

    private void MyUpdate()
    {
        Debug.Log("Update Manager -> " + (_updateManager as Component)?.name);
    }
}
