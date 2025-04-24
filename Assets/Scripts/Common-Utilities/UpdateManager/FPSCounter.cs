using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public interface IFPSCounter
{
    /// <summary>
    /// Below to 165 fps
    /// </summary>
    public bool BelowUltraFrameRate { get; }
    
    /// <summary>
    /// Below to 120 fps
    /// </summary>
    public bool BelowHightFrameRate { get; }

    /// <summary>
    /// Below to 60 fps
    /// </summary>
    public bool BelowMediumFrameRate { get; }

    /// <summary>
    /// Below to 30 fps
    /// </summary>
    public bool BelowLowFrameRate { get; }

    /// <summary>
    /// Below to 15 fps
    /// </summary>
    public bool BelowVeryLowFrameRate { get; }

    /// <summary>
    /// Below to 1 fps
    /// </summary>
    public bool BelowWatchDogFrameRate { get; }
}


[Serializable]
public class FPSCounter: IFPSCounter
{
    [Serializable]
    public enum FPSTrehold
    {
        BelowWatchDogFrameRate,
        BelowVeryLowFrameRate,
        BelowLowFrameRate,
        BelowMediumFrameRate,
        BelowHightFrameRate,
        BelowUltraFrameRate
    }
    
    private const int FPS165 = (1000 / 160);
    private const int FPS120 = (1000 / 120);
    private const int FPS60 = (1000 / 60);
    private const int FPS30 = (1000 / 30);
    private const int FPS15 = (1000 / 15);

    /// <summary>
    /// Below to 165 fps
    /// </summary>
    public bool BelowUltraFrameRate => ElapsedMilliseconds > FPS165 + tolerance;

    /// <summary>
    /// Below to 120 fps
    /// </summary>
    public bool BelowHightFrameRate => ElapsedMilliseconds > FPS120 + tolerance;

    /// <summary>
    /// Below to 60 fps
    /// </summary>
    public bool BelowMediumFrameRate => ElapsedMilliseconds > FPS60 + tolerance;

    /// <summary>
    /// Below to 30 fps
    /// </summary>
    public bool BelowLowFrameRate => ElapsedMilliseconds > FPS30 + tolerance;

    /// <summary>
    /// Below to 15 fps
    /// </summary>
    public bool BelowVeryLowFrameRate => ElapsedMilliseconds > FPS15 + tolerance;

    /// <summary>
    /// Below to 1 fps
    /// </summary>
    public bool BelowWatchDogFrameRate => ElapsedMilliseconds > 1000;

    private long ElapsedMilliseconds => _offset + _stopwatch.ElapsedMilliseconds;
    
    public long AverageRenderTiming { get; private set; }
    public long AverageUpdateTiming { get; private set; }
    public long AverageTiming { get; private set; }
    
    [SerializeField]
    private int tolerance = -2;
    
    [SerializeField]
    private int averageTime = 100;
    
    private long _toInitTiming;
    
    private long _toUpdateTiming;
    
    private long _toEndUpdate;
    
    
    
    private long _offset;
    

    private Average<(long main, long render)> _average;

    private readonly System.Diagnostics.Stopwatch _stopwatch = new ();

    public FPSCounter()
    {
        _average = new Average<(long main, long render)>(OnAddToCalc, Result);
    }
    
    private int OnAddToCalc((long main, long render) arg1, NativeList<(long main, long render)> list)
    {
        if (!list.IsCreated)
            return 0; 
        
        list.Add(arg1);

        long total = 0;

        int i;
        
        for (i = list.Length - 1; i >= 0; i--)
        {
            if(total >= averageTime)
                break;
            
            total += list[i].main + list[i].render;
        }

        return i;
    }

    private static (long main, long render) Result(NativeList<(long main, long render)> list)
    {
        (long main, long render) calculate = new ();

        int total = list.Length != 0 ? list.Length : 1;
        
        for (int i = 0; i < list.Length; i++)
        {
            calculate.main += list[i].main;
            calculate.render += list[i].render;
        }

        calculate.main /= total;
        calculate.render /= total;

        return calculate;
    }

    public void Resume()
    {
        _stopwatch.Restart();
    }

    public void Stop()
    {
        _stopwatch.Reset();
    }
    
    public void Update()
    {
        _toInitTiming = _stopwatch.ElapsedMilliseconds - (_toUpdateTiming + _toEndUpdate);
        
        
        _average.AddToCalc((_toUpdateTiming + _toEndUpdate, _toInitTiming));
        
        var aux = _average.Calc();

        AverageUpdateTiming = aux.main;
        AverageRenderTiming = aux.render;
        
        _offset = AverageRenderTiming + (AverageUpdateTiming - _toUpdateTiming);

        AverageTiming = AverageRenderTiming + AverageUpdateTiming;
        
        _stopwatch.Restart();
    }
    
    public void LateUpdate()
    {
        _toUpdateTiming = _stopwatch.ElapsedMilliseconds;
        
        _offset -= _toEndUpdate;
    }
    
    public void EndUpdate()
    {
        _toEndUpdate = _stopwatch.ElapsedMilliseconds - _toUpdateTiming;
        _offset = 0;
    }

    public void Destroy()
    {
        _stopwatch.Stop();
        _average.Dispose();
    }

    public void OnGui(Rect rect)
    {
        GUI.Label(rect, $"Updates: {AverageUpdateTiming}Ms\t Render:{AverageRenderTiming}Ms\nTotal:{AverageTiming}Ms \tFps:{(int)(1/(AverageTiming/1000f))}");
    }
}

[System.Serializable]
public struct GmFPSTrehold
{
    [FormerlySerializedAs("_trehold")]
    public FPSCounter.FPSTrehold trehold;

    public static implicit operator bool(GmFPSTrehold gmFPSTrehold) => gmFPSTrehold.trehold.IsBelow();
}

public static class FPScounterExtension
{
    public static bool IsBelow(this FPSCounter.FPSTrehold fpsTrehold)
    {
        switch (fpsTrehold)
        {
            case FPSCounter.FPSTrehold.BelowWatchDogFrameRate:
                return EngineUpdate.BelowWatchDogFrameRate;
                
            case FPSCounter.FPSTrehold.BelowVeryLowFrameRate:
                return EngineUpdate.BelowVeryLowFrameRate;
            
            case FPSCounter.FPSTrehold.BelowLowFrameRate:
                return EngineUpdate.BelowLowFrameRate;
            
            case FPSCounter.FPSTrehold.BelowMediumFrameRate:
                return EngineUpdate.BelowMediumFrameRate;
            
            case FPSCounter.FPSTrehold.BelowHightFrameRate:
                return EngineUpdate.BelowHightFrameRate;
            
            case FPSCounter.FPSTrehold.BelowUltraFrameRate:
                return EngineUpdate.BelowUltraFrameRate;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(fpsTrehold), fpsTrehold, null);
        }
    }
    
    public static bool IsBelow(this FPSCounter.FPSTrehold trehold, IFPSCounter fpsCounter)
    {
        return fpsCounter.IsBelow(trehold);
    }
    
    public static bool IsBelow(this IFPSCounter fpsCounter, FPSCounter.FPSTrehold fpsTrehold)
    {
        switch (fpsTrehold)
        {
            case FPSCounter.FPSTrehold.BelowWatchDogFrameRate:
                return fpsCounter.BelowWatchDogFrameRate;
                
            case FPSCounter.FPSTrehold.BelowVeryLowFrameRate:
                return fpsCounter.BelowVeryLowFrameRate;
            
            case FPSCounter.FPSTrehold.BelowLowFrameRate:
                return fpsCounter.BelowLowFrameRate;
            
            case FPSCounter.FPSTrehold.BelowMediumFrameRate:
                return fpsCounter.BelowMediumFrameRate;
            
            case FPSCounter.FPSTrehold.BelowHightFrameRate:
                return fpsCounter.BelowHightFrameRate;
            
            case FPSCounter.FPSTrehold.BelowUltraFrameRate:
                return fpsCounter.BelowUltraFrameRate;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(fpsTrehold), fpsTrehold, null);
        }
    }
}

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