using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RotatingList<T> : RefList<T>
{
    public int Actual { get; protected set; }
    
    public override IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            var aux = array[Actual];
            Actual = (Actual + 1) % Count;
            yield return aux;
        }
    }
    
    public ref T GetNext()
    {
        if (Count == 0)
            throw new InvalidOperationException("The list is empty");
        
        ref T item = ref array[Actual];
        Actual = (Actual + 1) % Count;
        return ref item;
    }
    
}

public class CompositeRotatingList<T> : IList<T>, IList
{
    private List<RotatingList<T>> _lists = new List<RotatingList<T>>();
    private readonly object _syncRoot = new object();

    // La cuenta global es la suma de las cuentas de cada lista interna.
    public int Count => _lists.Sum(cl => cl.Count);
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public bool IsSynchronized => false;
    public object SyncRoot => _syncRoot;

    public T this[int index]
    {
        get
        {
            var (list, localIndex) = GetListAndLocalIndex(index);
            return list[localIndex];
        }
        set
        {
            var (list, localIndex) = GetListAndLocalIndex(index);
            list[localIndex] = value;
        }
    }

    object IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T)value;
    }
    
    public static CompositeRotatingList<T> operator +(CompositeRotatingList<T> composite, RotatingList<T> cl)
    {
        composite.AddCircularList(cl);
        return composite;
    }

    public static CompositeRotatingList<T> operator -(CompositeRotatingList<T> composite, RotatingList<T> cl)
    {
        composite._lists.Remove(cl);
        return composite;
    }

    // Devuelve la lista interna y el índice local correspondiente a un índice global.
    private (RotatingList<T> list, int localIndex) GetListAndLocalIndex(int index)
    {
        if (index < 0 || index >= Count)
            throw new IndexOutOfRangeException();

        int cumulative = 0;
        foreach (var cl in _lists)
        {
            if (index < cumulative + cl.Count)
            {
                // Ajustamos el índice según la posición Actual de la lista interna.
                int offset = index - cumulative;
                int localIndex = (cl.Actual + offset) % cl.Count;
                return (cl, localIndex);
            }
            cumulative += cl.Count;
        }
        throw new IndexOutOfRangeException();
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var cl in _lists)
        {
            int localCount = cl.Count;
            for (int i = 0; i < localCount; i++)
                yield return cl.GetNext();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Permite agregar un RotatingList<T> al composite.
    public void AddCircularList(RotatingList<T> cl)
    {
        if (cl == null)
            throw new ArgumentNullException(nameof(cl));
        _lists.Add(cl);
    }

    // Agrega un elemento a la primera lista interna o crea una nueva si no existe.
    public void Add(T item)
    {
        if (_lists.Count == 0)
        {
            var newList = new RotatingList<T>();
            newList.Add(item);
            _lists.Add(newList);
        }
        else
        {
            _lists[0].Add(item);
        }
    }

    public void Clear()
    {
        _lists.Clear();
    }

    public bool Contains(T item)
    {
        return _lists.Any(cl => cl.Contains(item));
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (var cl in _lists)
        {
            cl.CopyTo(array, arrayIndex);
            arrayIndex += cl.Count;
        }
    }

    // Implementación de IList.CopyTo(Array, int)
    public void CopyTo(Array array, int index)
    {
        if (array is T[] typedArray)
        {
            CopyTo(typedArray, index);
        }
        else
        {
            // Si no es del tipo T[], se hace una copia elemento a elemento.
            foreach (var item in this)
            {
                array.SetValue(item, index++);
            }
        }
    }

    public int IndexOf(T item)
    {
        int cumulative = 0;
        foreach (var cl in _lists)
        {
            int idx = cl.IndexOf(item);
            if (idx != -1)
                return cumulative + idx;
            cumulative += cl.Count;
        }
        return -1;
    }

    public void Insert(int index, T item)
    {
        var (list, localIndex) = GetListAndLocalIndex(index);
        list.Insert(localIndex, item);
    }

    public bool Remove(T item)
    {
        foreach (var cl in _lists)
        {
            if (cl.Remove(item))
                return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        var (list, localIndex) = GetListAndLocalIndex(index);
        list.RemoveAt(localIndex);
    }

    // Implementaciones explícitas para IList (objetos)
    int IList.Add(object value)
    {
        Add((T)value);
        return Count - 1;
    }

    bool IList.Contains(object value) => Contains((T)value);
    int IList.IndexOf(object value) => IndexOf((T)value);
    void IList.Insert(int index, object value) => Insert(index, (T)value);
    void IList.Remove(object value) => Remove((T)value);
}
