using System.Collections;
using System.Collections.Generic;
using SystemEngineUpdate;
using UnityEngine;
using UnityEngine.Events;

public class SectionMap : MonoBehaviour, IUpdateManager, ILateUpdateManager, IFixedUpdateManager
{
    public event UnityAction OnUpdateEvnt;
    public event UnityAction OnLateUpdateEvnt;
    public event UnityAction OnFixedUpdateEvnt;

    // Update is called once per frame
    void Update()
    {
        OnUpdateEvnt?.Invoke();
    }
}
