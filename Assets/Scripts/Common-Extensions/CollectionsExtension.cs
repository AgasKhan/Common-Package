using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

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


public static class EfficientCopyExtensions
{
    public static void MemCpy<T>(this NativeArray<T> source, int sourceIndex, NativeList<T> destination, int destinationIndex, int length) where T : unmanaged
    {
        MemCpy(source.Slice(sourceIndex, length), destination.AsArray().Slice(destinationIndex, length));
    }
    
    public static void MemCpy<T>(this NativeList<T> source, int sourceIndex, NativeArray<T> destination, int destinationIndex, int length) where T : unmanaged
    {
        MemCpy(source.AsArray().Slice(sourceIndex, length), destination.Slice(destinationIndex, length));
    }
    
    public static void MemCpy<T>(this NativeList<T> source, int sourceIndex, NativeList<T> destination, int destinationIndex, int length) where T : unmanaged
    {
        MemCpy(source.AsArray().Slice(sourceIndex, length), destination.AsArray().Slice(destinationIndex, length));
    }
    
    public static void MemCpy<T>(this NativeArray<T> source, int sourceIndex, NativeArray<T> destination, int destinationIndex, int length) where T : unmanaged
    {
        MemCpy(source.Slice(sourceIndex, length), destination.Slice(destinationIndex, length));
    }

    public static void MemCpy<T>(this NativeSlice<T> source, int sourceIndex, NativeSlice<T> destination, int destinationIndex, int length) where T : unmanaged
    {
        MemCpy(source.Slice(sourceIndex, length), destination.Slice(destinationIndex, length));
    }

    public static unsafe void MemCpy<T>(this NativeSlice<T> source, NativeSlice<T> destination) where T : unmanaged
    {
        if (source.Length != destination.Length)
            throw new ArgumentException("Source and destination must have the same length");

        UnsafeUtility.MemCpy(
            destination.GetUnsafePtr(),
            source.GetUnsafeReadOnlyPtr(),
            source.Length * UnsafeUtility.SizeOf<T>());
    }
}

    
