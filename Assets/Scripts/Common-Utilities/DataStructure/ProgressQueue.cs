using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressQueue<T>
{
    private Queue<T> queue = new();
    public float Progress => QueueCount == 0 ? 1 : ProgressCount / (float)QueueCount;

    private int ProgressCount { get; set; }

    private int QueueCount { get; set; }
    
    public void Enqueue(T value)
    {
        queue.Enqueue(value);
        QueueCount++;
    }

    public bool TryDequeue(out T value)
    {
        if (queue.TryDequeue(out value))
        {
            ProgressCount++;
            return true;
        }
        else
        {
            QueueCount = 0;
            ProgressCount = 0;
        }

        return false;
    }

    public void Clear()
    {
        QueueCount = 0;
        ProgressCount = 0;
        queue.Clear();
    }
}