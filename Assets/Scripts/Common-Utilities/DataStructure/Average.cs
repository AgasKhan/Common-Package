using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Average<T> : IDisposable where T : unmanaged
{
    private NativeList<T> elements = new(Allocator.Persistent);
    
    System.Func<T,NativeList<T>, int> onAddToCalc;

    System.Func<NativeList<T>,T> result;

    private NativeArray<T> _nativeArray;
    
    private bool _disposed;


    public Average(Func<T, NativeList<T>, int> onAddToCalc, Func<NativeList<T>,T> result)
    {
        this.onAddToCalc = onAddToCalc;
        this.result = result;
    }

    public T Calc()
    {
        return result.Invoke(elements);
    }
    
    public void AddToCalc(T number)
    {
        int elementsToRemove = onAddToCalc.Invoke(number, elements);
        
        if(elementsToRemove<=0)
            return;

        _nativeArray = new NativeArray<T>(elements.Length - elementsToRemove, Allocator.Temp);
        
        elements.MemCpy(elementsToRemove, _nativeArray, 0 ,_nativeArray.Length);

        elements.Length -= elementsToRemove;
        
        _nativeArray.MemCpy(0, elements, 0, _nativeArray.Length);

        _nativeArray.Dispose();
    }
    

    public void Dispose()
    {
        if (_disposed)
            return;

        if (elements.IsCreated)
            elements.Dispose();

        _disposed = true;
        //GC.SuppressFinalize(this);
    }
    
    ~Average()
    {
        Dispose();
    }
}
