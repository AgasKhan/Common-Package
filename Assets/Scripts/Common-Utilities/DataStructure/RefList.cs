using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RefList<T> : IList, IList<T>
{
    public bool IsFixedSize => false;

    public bool IsSynchronized => array.IsSynchronized;

    public object SyncRoot => ((ICollection)array).SyncRoot;

    public bool IsReadOnly => array.IsReadOnly;
    
    public T this[int index]
    {
        get => array[index];
        set => array[index] = value;
    }
    
    object IList.this[int index]
    {
        get => ((IList)array)[index];
        set => ((IList)array)[index] = value;
    }
    
    public int Count { get; private set; } = 0;

    [SerializeField]
    protected T[] array;

    public RefList()
    {
        array = new T[4];
    }
    
    public ref T GetValue(int index)
    {
        return ref array[index];
    }

    public void Add(T item)
    {
        if(Count>=array.Length)
        {
            //resize
            Resize();
        }
        
        array[Count] = item;
        Count++;
    }

    public void Clear()
    {
        Count = 0;
    }

    public bool Contains(T item)
    {
        for (int i = 0; i < Count; i++)
        {
            if (array[i].Equals(item))
                return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        for (int i = index; i < Count+1; i++)
        {
            array[i] = array[i + 1];
        }

        Count--;
    }

    public bool Remove(T item)
    {
        for (int i = 0; i < Count; i++)
        {
            if(array[i].Equals(item))
            {
                RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public T[] AsArray()
    {
        return array;
    }

    public T[] ToArray()
    {
        T[] nArray = new T[Count];
        
        Array.Copy(array, 0, nArray, 0, Count);

        return nArray;
    }
    
    public void Insert(int index, object value)
    {
        Insert(index, (T)value);
    }

    public void Insert(int index, T item)
    {
        Count++;

        if (Count>= array.Length)
            Resize();

        for (int i = Count - 1; i > index; i--)
        {
            array[i] = array[i-1];
        }
        
        array[index] = item;
    }
    
    public virtual IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return array[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(this.array,arrayIndex,array, 0, Count);
    }

    public void CopyTo(Array array, int index)
    {
        Array.Copy(this.array,index,array, 0, Count);
    }
    
    public int IndexOf(T item)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (item.Equals(array[i]))
                return i;
        }
        
        return -1;
    }
    
    int IList.Add(object value)
    {
        Add((T)value);
        return Count-1;
    }

    bool IList.Contains(object value)
    {
        return Contains((T)value);
    }

    int IList.IndexOf(object value)
    {
        return IndexOf((T)value);
    }

    void IList.Remove(object value)
    {
        Remove((T)value);
    }
    
    void Resize()
    {
        T[] arrayViejo = array;

        array = new T[array.Length * 2];
        
        Array.Copy(arrayViejo, 0, array, 0, Count);
    }
}
