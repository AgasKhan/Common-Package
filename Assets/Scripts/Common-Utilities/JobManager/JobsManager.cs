using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using UnityEngine.SceneManagement;
using SystemEngineUpdate;

public class JobsManager : MySystem<JobsManager>, IPostLateUpdate , ILoadScene ,IQuit
{
    class JobsWrapper : IDoubleZeldaElement<JobsWrapper>
    {
        public JobHandle jobHandle;

        public Action<JobHandle> actionToComplete;
        public Action end;
        
        public bool delayed = false;//no lo quiero ahora
        
        public SingleZeldaList<JobsWrapper> Parent { get; set; }
        
        public JobsWrapper Next { get; set; }
        
        public JobsWrapper Previus { get; set; }
        
        public bool isCompleted => jobHandle.IsCompleted;

        private DoubleZeldaList<JobsWrapper> realParent => (DoubleZeldaList<JobsWrapper>)Parent;

        public bool LateUpdate()
        {
            if (!isCompleted && !delayed || isCompleted)
            {
                jobHandle.Complete();
                actionToComplete?.Invoke(jobHandle);
                realParent.Remove(this);
                end?.Invoke();
                return true;
            }

            return false;
        }
        
        public void Destroy()
        {
            actionToComplete = null;
            end?.Invoke();
            end=null;
        }
    }
    
    private static DoubleZeldaList<JobsWrapper> _jobHandles = new DoubleZeldaList<JobsWrapper>();

    #region Creates IJobParallelForTransform
    public static JobHandle Create<T>(ref T job, Transform[] transformArray, bool delayed ,Action<JobHandle> actionToComplete, Action dispose) where T : struct,IJobParallelForTransform
    {
        var tranformArrayAcces = new TransformAccessArray(transformArray);
        
        JobHandle _jobHandle = job.ScheduleByRef(tranformArrayAcces);

        dispose += tranformArrayAcces.Dispose;

        return Create(ref _jobHandle, delayed, actionToComplete, dispose);
    }
    
    public static JobHandle Create<T>(ref T job, Transform[] transformArray, JobHandle jobHandle, bool delayed ,Action<JobHandle> actionToComplete, Action dispose) where T : struct,IJobParallelForTransform
    {
        var tranformArrayAcces = new TransformAccessArray(transformArray);
        
        JobHandle _jobHandle = job.ScheduleByRef(tranformArrayAcces, jobHandle);

        dispose += tranformArrayAcces.Dispose;

        return Create(ref _jobHandle, delayed, actionToComplete, dispose);
    }
    
    
    public static JobHandle Create<T>(ref T job, TransformAccessArray transformArray, bool delayed ,Action<JobHandle> actionToComplete, Action dispose) where T : struct,IJobParallelForTransform
    {
        JobHandle _jobHandle = job.ScheduleByRef(transformArray);

        return Create(ref _jobHandle, delayed, actionToComplete, dispose);
    }
    
    public static JobHandle Create<T>(ref T job, TransformAccessArray transformArray, JobHandle jobHandle, bool delayed ,Action<JobHandle> actionToComplete, Action dispose) where T : struct,IJobParallelForTransform
    {
        JobHandle _jobHandle = job.ScheduleByRef(transformArray, jobHandle);

        return Create(ref _jobHandle, delayed, actionToComplete, dispose);
    }
    
    #endregion

    #region Creates IJobFor
    
    public static JobHandle Create<T>(ref T job, int lenght, JobHandle jobHandle, bool delayed, Action<JobHandle> actionToComplete, Action dispose) where T : struct,IJobFor
    {
        JobHandle _jobHandle = job.ScheduleByRef(lenght,  jobHandle);

        return Create(ref _jobHandle, delayed, actionToComplete, dispose);
    }
    
    public static JobHandle Create<T>(ref T job, int lenght, int batch, JobHandle jobHandle, bool delayed, Action<JobHandle> actionToComplete, Action dispose) where T : struct,IJobFor
    {
        JobHandle _jobHandle = job.ScheduleParallelByRef(lenght, batch, jobHandle);

        return Create(ref _jobHandle, delayed, actionToComplete, dispose);
    }
    
    #endregion

    #region  Creates IJobs,IJobParallelFor
    
    public static JobHandle Create<T>(ref T job, int lenght, int batch, bool delayed, Action<JobHandle> actionToComplete, Action dispose) where T : struct,IJobParallelFor
    {
        JobHandle _jobHandle = job.ScheduleByRef(lenght,  batch);

        return Create(ref _jobHandle, delayed, actionToComplete, dispose);
    }
    
    public static JobHandle Create<T>(ref T job, bool delayed, Action<JobHandle> actionToComplete, Action dispose) where T : struct,IJob
    {
        JobHandle _jobHandle = job.ScheduleByRef();
        
        return Create(ref _jobHandle, delayed, actionToComplete, dispose);
    }
    
    public static JobHandle Create<T>(ref T job, JobHandle jobHandle, bool delayed, Action<JobHandle> actionToComplete, Action dispose) where T : struct,IJob
    {
        JobHandle _jobHandle = job.ScheduleByRef(jobHandle);
        
        return Create(ref _jobHandle, delayed, actionToComplete, dispose);
    }
    #endregion
    
    static ref JobHandle Create(ref JobHandle jobHandle, bool delayed, Action<JobHandle> actionToComplete, Action dispose)
    {
        JobsWrapper jobsWrapper = new JobsWrapper()
        {
            jobHandle = jobHandle,
            delayed = delayed,
            actionToComplete = actionToComplete,
            end = dispose
        };
        
        _jobHandles.AddLast(jobsWrapper);
        
        return ref jobHandle;
    }
    
    public void PostLateUpdate()
    {
        foreach (var jobs in _jobHandles)
        {
            if (jobs.LateUpdate())
            {
                _jobHandles.Remove(jobs);
                jobs.Destroy();
            }
        }
    }

    public void OnLoadScene(Scene arg0, LoadSceneMode loadSceneMode)
    {
        if (loadSceneMode == LoadSceneMode.Single)
            instance.Quit();
    }

    public void Quit()
    {
        _jobHandles.Clear();
    }
}
