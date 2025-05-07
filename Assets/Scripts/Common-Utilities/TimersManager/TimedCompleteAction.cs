using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// rutina que ejecutara una funcion al en cada frame, y otra al finalizar
/// </summary>
[System.Serializable]
public class TimedCompleteAction : TimedAction
{
    protected Action update;

    public override float SubsDeltaTime(/*int index*/)
    {
        update();

        return base.SubsDeltaTime(/*index*/);
    }


    /// <summary>
    /// Aniade un evento al update
    /// </summary>
    /// <param name="update"></param>
    /// <returns></returns>
    public TimedCompleteAction AddToUpdate(Action update)
    {
        this.update += update;

        return this;
    }


    /// <summary>
    /// Quita un evento del Update
    /// </summary>
    /// <param name="update"></param>
    /// <returns></returns>
    public TimedCompleteAction SubstractToUpdate(Action update)
    {
        this.update -= update;

        return this;
    }

    public override void Destroy()
    {
        base.Destroy();
        update = null;
    }

    /// <summary>
    /// crea una rutina que ejecutara una funcion al comenzar/reiniciar, otra en cada frame, y otra al final
    /// </summary>
    /// <param name="timer"></param>
    /// <param name="update"></param>
    /// <param name="end"></param>
    public TimedCompleteAction(float timer, Action update, Action end) : base(timer, end)
    {
        this.update = update;
    }
}
