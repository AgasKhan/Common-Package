using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Timer : ClampedValue, IIndexed
{
    public int Index { get; set; }
    
    protected bool _unscaled;

    protected bool loop;

    protected float multiply = 1;

    bool _freeze = true; //por defecto no esta agregado

    /// <summary>
    /// delta time seleccionado del timer
    /// </summary>
    public float DeltaTime
    {
        get
        {
            return _unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        }
    }

    public float Multiply { get => multiply; protected set => multiply = value; }

    /// <summary>
    /// Propiedad que sirve para agregar o quitar de la cola para la resta
    /// </summary>
    public bool Freeze
    {
        get => _freeze;
        set
        {
            if (value == _freeze)
                return;

            if (value)
            {
                this.RemoveTo(TimersManager.timersList);
            }
            else
            {
                this.AddTo(TimersManager.timersList);
            }

            _freeze = value;
        }
    }

    /// <summary>
    /// Chequea si el contador llego a su fin
    /// </summary>
    /// <returns>Devuelve true si llego a 0</returns>
    public bool Chck
    {
        get
        {
            return _current <= 0;
        }
    }

    /// <summary>
    /// Modifica el numero que multiplica la constante temporal, y asi acelerar o disminuir el timer
    /// </summary>
    /// <param name="m">Por defecto es 1</param>
    public Timer SetMultiply(float m)
    {
        Multiply = m;

        return this;
    }

    /// <summary>
    /// En caso de que el contador este detenido lo reanuda
    /// </summary>
    public Timer Start()
    {
        Freeze = false;

        return this;
    }

    /// <summary>
    /// Frena el contador, no resetea ni modifica el contador actual
    /// </summary>
    public Timer Stop()
    {
        Freeze = true;

        return this;
    }

    /// <summary>
    /// Setea el current, sin hacer trigger del evento
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    public Timer SetCurrent(float f)
    {
        _current = f;
        return this;
    }

    /// <summary>
    /// Setea si el timer debe reiniciarse de forma automatica o no
    /// </summary>
    /// <param name="l"></param>
    /// <returns></returns>
    public Timer SetLoop(bool l)
    {
        loop = l;

        return this;
    }

    /// <summary>
    /// Setea el contador, y comienza la cuenta (si se quiere) desde ese numero
    /// </summary>
    /// <param name="totalTim">El numero a contar</param>
    /// <param name="f">Si arranca a contar o no</param>
    public Timer Set(float totalTim, bool f = true)
    {
        base.Set(totalTim);
        Freeze = !f;

        return this;
    }


    /// <summary>
    /// Setea si utiliza el time.deltatime o el Time.unscaledDeltaTime
    /// </summary>
    /// <param name="u"></param>
    /// <returns></returns>
    public Timer SetUnscaled(bool u)
    {
        _unscaled = u;

        return this;
    }

    /// <summary>
    /// vuelve el valor current el maximo, y vuelve a poner al contador en la cola
    /// </summary>
    /// <returns></returns>
    public override ClampedValue Reset()
    {
        base.Reset();
        Start();
        return this;
    }


    /// <summary>
    /// Realiza la resta automatica asi como las funciones necesarias dentro del TimerManager y recibe el indice dentro del manager
    /// </summary>
    /// <returns></returns>
    public virtual float SubsDeltaTime(/*int index*/)
    {
        var aux = Substract(DeltaTime * Multiply);

        if (aux <= 0)
        {
            if (loop)
            {
                Reset();
                return 0;
            }

            else
                Stop();
            //StopWithIndex(index);
        }

        return aux;
    }

    public override void Destroy()
    {
        base.Destroy();
        Stop();
    }

    /// <summary>
    /// Configura el timer para su uso
    /// </summary>
    /// <param name="totTim">valor por defecto a partir de donde se va a contar</param>
    /// <param name="m">Modifica el multiplicador del timer, por defecto 0</param>
    public Timer(float totTim) : base(totTim)
    {
        Start();
    }
}
