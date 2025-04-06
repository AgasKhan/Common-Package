using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
public interface IFPSCounter
{
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
*/

public class FPSCounter<T> where T : IUpdateManager, ILateUpdateManager//: IFPSCounter
{
    private const int FPS120 = 1000 / 120;
    private const int FPS60 = 1000 / 60;
    private const int FPS30 = 1000 / 30;
    private const int FPS15 = 1000 / 15;

    /// <summary>
    /// Below to 120 fps
    /// </summary>
    public bool BelowHightFrameRate => ElapsedMilliseconds > FPS120;

    /// <summary>
    /// Below to 60 fps
    /// </summary>
    public bool BelowMediumFrameRate => ElapsedMilliseconds > FPS60;

    /// <summary>
    /// Below to 30 fps
    /// </summary>
    public bool BelowLowFrameRate => ElapsedMilliseconds > FPS30;

    /// <summary>
    /// Below to 15 fps
    /// </summary>
    public bool BelowVeryLowFrameRate => ElapsedMilliseconds > FPS15;

    /// <summary>
    /// Below to 1 fps
    /// </summary>
    public bool BelowWatchDogFrameRate => ElapsedMilliseconds > 1000;

    private long ElapsedMilliseconds => PreviusRenderTiming + _stopwatch.ElapsedMilliseconds;
    
    public int Index { get; set; }
    
    public long PreviusRenderTiming { get; private set; }
    
    public long PreviusMainTiming { get; private set; }

    private readonly System.Diagnostics.Stopwatch _stopwatch = new ();

    private T updateManager;

    public FPSCounter(T updateManager)
    {
        this.updateManager = updateManager;
        updateManager.OnUpdateEvnt += Update;
        updateManager.OnLateUpdateEvnt += LateUpdate;
    }
    
    void Update()
    {
        PreviusRenderTiming = ElapsedMilliseconds - PreviusMainTiming;
        _stopwatch.Restart();
    }
    
    private void LateUpdate()
    {
        PreviusMainTiming = ElapsedMilliseconds;
    }

    public void Destroy()
    {
        updateManager.OnUpdateEvnt -= Update;
        _stopwatch.Stop();
    }
}