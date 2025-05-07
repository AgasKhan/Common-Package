using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ClampedValue : IGetPercentage
{
    /// <summary>
    /// Valor maximo en el cual clampea
    /// </summary>
    [SerializeField]
    protected float _total;

    [SerializeField]
    protected float _current;

    protected bool notifyOnSuscribe = true;

    /// <summary>
    /// que hace cuando se setea el current
    /// </summary>
    protected System.Action<float> internalSetCurrent;

    /// <summary>
    /// Valor maximo
    /// </summary>
    public virtual float Total
    {
        get => _total;
        set
        {
            _total = value;

            Current = Current;
        }
    }

    /// <summary>
    /// Valor actual
    /// </summary>
    public virtual float Current
    {
        get => _current;
        set
        {
            internalSetCurrent(value - _current); //lo seteo con un delegado para asi poder quitar o agregar la funcion de ejecutar un evento al modificar
        }
    }
    
    public float Percentage => Total>0 ? Current / Total : 0;
    

    public float InversePercentage => 1 - Percentage;
    

    protected event System.Action<IGetPercentage, float> _onChange; //version interna que almacera todos los que desean ser notificados cuando se modifique el timer

    /// <summary>
    /// Evento que se ejecutara cada vez que se actualice el valor actual
    /// </summary>
    public event System.Action<IGetPercentage, float> onChange
    {
        add
        {
            if (_onChange == null)
            {
                internalSetCurrent += InternalEventSetCurrent;
            }

            _onChange += value;

            if (notifyOnSuscribe)
                InternalEventSetCurrent(0);
        }

        remove
        {
            _onChange -= value;

            if (_onChange == null)
                internalSetCurrent -= InternalEventSetCurrent;
        }
    }

    /// <summary>
    /// Reinicia el contador a su valor por defecto, para reiniciar la cuenta
    /// </summary>
    public virtual ClampedValue Reset()
    {
        Current = Total;

        return this;
    }

    /// <summary>
    /// Efectua una resta en el contador
    /// </summary>
    /// <param name="n">En caso de ser negativo(-) suma al contador, siempre y cuando no este frenado</param>
    public virtual float Substract(float n)
    {
        Current -= n;
        return Current;
    }

    /// <summary>
    /// Setea el contador
    /// </summary>
    /// <param name="totalTim">El numero a contar</param>
    public ClampedValue Set(float totalTim)
    {
        Total = totalTim;
        Reset();

        return this;
    }


    /// <summary>
    /// Si se desea ejecutar el evento onChange, cuando se suscribe un nuevo elemento
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public ClampedValue NotifyOnSuscribe(bool b)
    {
        notifyOnSuscribe = b;
        return this;
    }
    

    /// <summary>
    /// Clampeo de la variable
    /// </summary>
    /// <param name="value"></param>
    protected void InternalSetCurrent(float value)
    {
        _current += value;

        _current = Mathf.Clamp(_current, 0, Total);
    }

    /// <summary>
    /// Trigger del evento
    /// </summary>
    /// <param name="value"></param>
    protected void InternalEventSetCurrent(float value)
    {
        _onChange(this, value);
    }

    public virtual void Destroy()
    {
        _onChange = null;
        internalSetCurrent = null;
    }

    protected virtual void Create()
    {
    }

    public ClampedValue()
    {
        internalSetCurrent = InternalSetCurrent;
        Create();
    }

    public ClampedValue(float totTim)
    {
        _current = totTim;
        _total = totTim;
        internalSetCurrent = InternalSetCurrent;
        Create();
    }
}

/// <summary>
/// Interfaz para representar un porcentage en la UI o afines
/// </summary>
public interface IGetPercentage
{
    float Percentage { get; }

    float InversePercentage { get; }

    float Current { get; }

    float Total { get; }
}