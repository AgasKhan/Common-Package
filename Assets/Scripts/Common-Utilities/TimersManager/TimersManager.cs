using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SystemEngineUpdate;
using UnityEngine.SceneManagement;

public class TimersManager : MySystem<TimersManager>, IPreUpdate, ILoadScene
{
    public static List<Timer> timersList
    {
        get
        {
            if (instance == null)
            {
                instance = new TimersManager();
            }

            return instance._timersList;
        }
    }
    
    readonly List<Timer> _timersList = new();

    /// <summary>
    /// Crea un timer que se almacena en una lista para restarlos de forma automatica
    /// </summary>
    /// <param name="totTime2">el tiempo que dura el contador</param>
    /// <param name="m">el multiplicador del contador</param>
    /// <returns>Devuelve la referencia del contador creado</returns>
    public static Timer Create(float totTime2 = 10)
    {
        Timer newTimer = new Timer(totTime2);
        return newTimer;
    }

    /// <summary>
    /// Crea una rutina que ejecutara una funcion al cabo de un tiempo
    /// </summary>
    /// <param name="totTime">el tiempo total a esperar</param>
    /// <param name="action">la funcion que se ejecutara</param>
    /// <param name="loop">En caso de ser false se quita de la cola, y en caso de ser true se auto reinicia</param>
    /// <returns>retorna la rutina creada</returns>
    public static TimedAction Create(float totTime, Action action)
    {
        TimedAction newTimer = new TimedAction(totTime, action);
        return newTimer;
    }

    /// <summary>
    /// Crea una rutina completa, la cual ejecutara una funcion al comenzar/reiniciar, en el update, y al finalizar
    /// </summary>
    /// <param name="totTime"></param>
    /// <param name="update"></param>
    /// <param name="end"></param>
    /// <param name="loop">En caso de ser false se quita de la cola, y en caso de ser true se auto reinicia</param>
    /// <param name="unscaled"></param>
    /// <returns></returns>
    public static TimedCompleteAction Create(float totTime, Action update, Action end)
    {
        TimedCompleteAction newTimer = new TimedCompleteAction(totTime, update, end);
        return newTimer;
    }

    #region lerps

    static public TimedLerp<T> Create<T>(T original, T final, float seconds, System.Func<T, T, float, T> Lerp, System.Action<T> save)
    {
        return Create(() => original, () => final, seconds, Lerp, save);
    }

    static public TimedLerp<T> Create<T>(T original, System.Func<T> final, float seconds, System.Func<T, T, float, T> Lerp, System.Action<T> save)
    {
        return Create(() => original, final, seconds, Lerp, save);
    }

    static public TimedLerp<T> Create<T>(System.Func<T> original, T final, float seconds, System.Func<T, T, float, T> Lerp, System.Action<T> save)
    {
        return Create(original, () => final, seconds, Lerp, save);
    }

    static public TimedLerp<T> Create<T>(System.Func<T> original, System.Func<T> final, float seconds, System.Func<T, T, float, T> Lerp, System.Action<T> save)
    {
        return new TimedLerp<T>(original, final, seconds, Lerp, save);
    }

    static public TimedCompleteAction LerpWithCompare<T>(T original, T final, float velocity, System.Func<T, T, float, T> Lerp, System.Func<T, T, bool> compare, System.Action<T> save)
    {
        TimedCompleteAction tim = null;

        System.Action

        update = () =>
        {
            original = Lerp(original, final, Time.deltaTime * velocity);
            save(original);
            if (compare(original, final))
                tim.Set(0);
            else
                tim.Reset();
        }
        ,
        end = () =>
        {
            save(final);

        };

        tim = Create(1, update, end);

        return tim;
    }

    #endregion

    public void PreUpdate()
    {
        /*
        foreach (var item in timersList)
        {
            item.SubsDeltaTime();
        }
        */
        
        for (int i = timersList.Count - 1; i >= 0; i--)
        {
            timersList[i].SubsDeltaTime();
            
            while (i >= timersList.Count)
                i--;
        }
    }

    public void OnLoadScene(Scene arg0, LoadSceneMode loadSceneMode)
    {
        if(loadSceneMode == LoadSceneMode.Single)
            timersList.Clear();
    }
}


