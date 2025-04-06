using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class CollectionsExtension
{
    public static void Swap<T>(this T[] collection, int indexA, int indexB)
    {
        if (indexA == indexB)
            return;
        
        (collection[indexA], collection[indexB]) = (collection[indexB], collection[indexA]);
    }
    
    public static void Swap<T>(this IList<T> collection, int indexA, int indexB)
    {
        if (indexA == indexB)
            return;
        
        (collection[indexA], collection[indexB]) = (collection[indexB], collection[indexA]);
    }
    
    public static void RemoveAtSwapBack<T>(this IList<T> collection, int index)
    {
        int lastIndex = collection.Count - 1;

        if (index != lastIndex)
        {
            collection.Swap(index, lastIndex);
        }

        collection.RemoveAt(lastIndex);
    }
}



    
