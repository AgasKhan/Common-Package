using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SingleZeldaList<T> : IEnumerable<T> where T : class, ISingleZeldaElement<T>
{
    public T First { get; protected set; }
    public T Last { get; protected set; }
    public int Count { get; protected set; } = 0;

    public virtual void AddFirst(T item)
    {
        Count++;

        item.Next = null;

        if (First == null)
        {
            First = item;
            Last = item;
        }
        else
        {
            item.Next = First;

            First = item;
        }
    }

    public virtual void AddLast(T item)
    {
        Count++;

        item.Next = null;

        if (Last == null)
        {
            First = item;
            Last = item;
        }
        else
        {
            Last.Next = item;

            Last = item;
        }
    }

    public virtual T RemoveFirst()
    {
        if (Count == 0)
            return null;

        T item = First;

        First = First.Next;

        FreeItem(item);

        return item;
    }

    public void Clear()
    {
        foreach (var item in this)
        {
            FreeItem(item);
            item.Destroy();
        }

        First = null;
        Last = null;
    }

    public IEnumerator<T> GetEnumerator()
    {
        T nodeIterator = First;

        T actual = null;

        T prev = null;

        while (nodeIterator != null)
        {
            if (actual?.Parent != null) //si me antiguo actual no tiene parent, significa que lo quite de la lista, por ende no me interesa como previo
                prev = actual;

            actual = nodeIterator; //lo actualizo al nodo actual
            nodeIterator = nodeIterator.Next; //voy al siguiente

            yield return actual;            

            //si despues de retornar, el siguiente nodo no tiene padre, significa que el acual es el last o saque el nodo siguiente
            //por eso descarto que sea el last, y si poseo un previo en condiciones, me permite reobtener el nuevo siguiente
            if (nodeIterator?.Parent == null && actual != Last && prev?.Parent!=null)
            {
                nodeIterator = prev.Next;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    protected virtual void FreeItem(T item)
    {
        Count--;
        item.Next = null;
        item.Parent = null;
    }
}

public class DoubleZeldaList<T> : SingleZeldaList<T> where T : class, IDoubleZeldaElement<T>
{
    public override void AddFirst(T item)
    {
        if (item.Parent != null)
            return;

        Count++;

        item.Next = null;
        item.Previus = null;

        item.Parent = this;

        if (First==null)
        {
            First = item;
            Last = item;
        }
        else
        {
            First.Previus = item;

            item.Next = First;

            First = item;
        }
    }

    public override void AddLast(T item)
    {
        if (item.Parent != null)
            return;

        Count++;

        item.Next = null;
        item.Previus = null;

        item.Parent = this;

        if (Last == null)
        {
            First = item;
            Last = item;
        }
        else
        {
            Last.Next = item;

            item.Previus = Last;

            Last = item;
        }
    }

    public override T RemoveFirst()
    {
        if (Count == 0)
            return null;
        else if (Count == 1)
        {
            Last = null;
        }

        T item = First;

        First = First.Next;

        if(First!=null)
            First.Previus = null;

        FreeItem(item);

        return item;
    }

    public T RemoveLast()
    {
        if (Count == 0)
            return null;
        else if (Count == 1)
            First = null;

        T item = Last;

        Last = Last.Previus;

        if (Last != null)
            Last.Next = null;

        FreeItem(item);

        return item;
    }

    public void Remove(T item)
    {
        if (Count == 0 || item==null || item.Parent==null)
            return;

        if (First.Equals(item))
        {
            RemoveFirst();
        }
        else if (Last.Equals(item))
        {
            RemoveLast();
        }
        else
        {    
            var prev = item.Previus;
            var next = item.Next;

            next.Previus = prev;
            prev.Next = next;

            FreeItem(item);
        }
    }

    protected override void FreeItem(T item)
    {
        base.FreeItem(item);
        item.Previus = null;
    }

}

public interface ISingleZeldaElement<T> where T : class, ISingleZeldaElement<T>
{
    SingleZeldaList<T> Parent { get; set; }

    T Next { get; set; }

    void Destroy();
}

public interface IDoubleZeldaElement<T> : ISingleZeldaElement<T> where T : class, IDoubleZeldaElement<T>
{
    T Previus { get; set; }
}
