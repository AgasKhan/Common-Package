using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// rutina que ejecutara una accion desp de que termine el tiemer
/// </summary>
[System.Serializable]
public class TimedAction : Timer
{
    protected Action end;
    
    public override float SubsDeltaTime(/*int index*/)
    {
        var aux = base.SubsDeltaTime(/*index*/);
        if (aux <= 0)
        {
            end?.Invoke();
        }

        return aux;
    }

    /// <summary>
    /// Aniade un evento al evento end
    /// </summary>
    /// <param name="end"></param>
    /// <returns></returns>
    public TimedAction AddToEnd(Action end)
    {
        this.end += end;

        return this;
    }

    /// <summary>
    /// Quita un evento del end
    /// </summary>
    /// <param name="end"></param>
    /// <returns></returns>
    public TimedAction SubstractToEnd(Action end)
    {
        this.end -= end;

        return this;
    }

    public override void Destroy()
    {
        base.Destroy();
        end = null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="timer"></param>
    /// <param name="action"></param>
    public TimedAction(float timer, Action action) : base(timer)
    {
        this.end = action;
    }
}
