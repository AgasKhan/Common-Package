using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TimedLerp<T> : TimedCompleteAction
{
    System.Func<T> original;
    System.Func<T> final;
    System.Func<T, T, float, T> lerp;
    public event System.Action<T> save;

    public TimedLerp<T> AddToSave(System.Action<T> save)
    {
        this.save += save;

        return this;
    }

    public TimedLerp<T> SubstractToSave(System.Action<T> save)
    {
        this.save -= save;

        return this;
    }
    void Update()
    {
        save(lerp(original(), final(), InversePercentage));
    }

    void End()
    {
        save(final());
    }

    public override void Destroy()
    {
        base.Destroy();
        original = null;
        final = null;
        lerp = null;
        save= null;
    }


    public TimedLerp(Func<T> original, Func<T> final, float timer, Func<T, T, float, T> lerp, Action<T> save) : base(timer, null, null)
    {
        this.original = original;
        this.final = final;
        this.lerp = lerp;
        this.save = save;

        update = Update;

        end = End;
    }
}
