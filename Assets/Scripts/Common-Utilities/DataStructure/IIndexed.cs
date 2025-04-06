using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IIndexed
{
    public int Index { get; set; }
}

public static class IndexedExtension
{
    public static void AddTo<T>(this T item, IList<T> collection ) where T : IIndexed
    {
        item.Index = collection.Count;
        collection.Add(item);
    }
    
    public static void InsertTo<T>(this T item, IList<T> collection, int index) where T : IIndexed
    {
        collection.Insert(index, item);
        item.Index = index;

        // Actualizar los índices de los elementos siguientes
        for (int i = index + 1; i < collection.Count; i++)
        {
            collection[i].Index = i;
        }
    }
    
    public static void RemoveToAtSwapBack<T>(this T item, IList<T> collection) where T : IIndexed
    {
        int lastIndex = collection.Count - 1;

        if (item.Index != lastIndex)
        {
            T lastItem = collection[lastIndex];
            collection[item.Index] = lastItem;
            lastItem.Index = item.Index;
        }

        collection.RemoveAt(lastIndex);
        item.Index = -1;
    }
    
    public static bool RemoveTo<T>(this T item, IList<T> collection) where T : IIndexed
    {
        if (item.Index < 0 || item.Index >= collection.Count || !EqualityComparer<T>.Default.Equals(collection[item.Index], item))
        {
            // No está en la lista o el índice no es válido
            return false;
        }

        RemoveAt(collection, item.Index);
        return true;
    }
    
    public static void RemoveAt<T>(this IList<T> collection, int index) where T : IIndexed
    {
        collection[index].Index = -1;
        collection.RemoveAt(index);

        // Actualizar los índices de los elementos posteriores
        for (int i = index; i < collection.Count; i++)
        {
            collection[i].Index = i;
        }
    }
    
    public static void SwapTo<T>(this T item, int indexToSwap,  IList<T> collection) where T : IIndexed
    {
        if (item.Index == indexToSwap)
            return;

        // Intercambiar elementos en la lista
        (collection[item.Index], collection[indexToSwap]) = (collection[indexToSwap], collection[item.Index]);

        // Actualizar índices internos
        collection[item.Index].Index = item.Index;
        collection[indexToSwap].Index = indexToSwap;
    }
    
    public static void SwapTo<T>(this T item, T itemToSwap,  IList<T> collection) where T : IIndexed
    {
        item.SwapTo(itemToSwap.Index, collection);
    }
}