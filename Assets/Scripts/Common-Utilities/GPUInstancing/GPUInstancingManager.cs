using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SystemEngineUpdate;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GPUInstancing
{
    public class GPUInstancingManager : MySystem<GPUInstancingManager>, IPostUpdate, IPostLateUpdate, ILoadScene ,IQuit
    {
        abstract class RenderData : IDisposable
        {
            protected static readonly ParallelOptions parallelOptions = new ParallelOptions {  MaxDegreeOfParallelism = Environment.ProcessorCount };
            
            protected const int jobTrehold = 500;

            protected RenderParams[] rp;

            protected Mesh mesh;

            protected NativeArray<InstanceData> instanceDatas;

            protected JobHandle jobHandle;

            protected abstract bool Set { get; }
            
            protected abstract bool HasElements { get; }

            protected abstract int CountElements { get; }

            protected virtual bool AutomaticInstanceDatas => true;
            
            protected abstract void InternalUpdate();
            protected abstract void InternalLateUpdate();

            public abstract void Add(IGPUInstancingElement element);

            public abstract void Remove(IGPUInstancingElement element);
            
            public void Update()
            {
                if(!HasElements)
                    return;
                
                if(AutomaticInstanceDatas)
                    instanceDatas = new NativeArray<InstanceData>(CountElements, Allocator.TempJob);
                
                InternalUpdate();
            }
            
            public void LateUpdate()
            {
                if(!instanceDatas.IsCreated)
                    return;
                
                InternalLateUpdate();
                
                if(AutomaticInstanceDatas)
                    instanceDatas.Dispose();
            }

            public virtual void Dispose()
            {
                if (instanceDatas.IsCreated)
                {
                    instanceDatas.Dispose();
                }
            }
        }

        abstract class RenderData<TElement> : RenderData where TElement : IGPUInstancingElement
        {
            protected override bool Set => gpuInstancingComponents != null;

            protected override bool HasElements => Set && gpuInstancingComponents.Count != 0;

            protected override int CountElements => gpuInstancingComponents.Count;
            
            protected List<TElement> gpuInstancingComponents;
            
            public override void Add(IGPUInstancingElement element)
            {
                if (element is not TElement elementChild)
                    throw new Exception("Isnt sime type");

                InternalAdd(elementChild);
            }
            
            public override void Remove(IGPUInstancingElement element)
            {
                if (element is not TElement elementChild)
                    throw new Exception("Isnt sime type");
                
                if (!HasElements)
                {
                    return;   
                }
                
                InternalRemove(elementChild, elementChild.Index);
            }

            protected virtual void InternalAdd(TElement element)
            {
                if (!Set)
                {
                    Create(element);
                }
                
                element.AddTo(gpuInstancingComponents);
            }

            protected virtual void InternalRemove(TElement element, int index)
            {
                element.RemoveToAtSwapBack(gpuInstancingComponents);
            }

            protected virtual void Create(TElement element)
            {
                gpuInstancingComponents = new();
                
                mesh = element.Mesh;

                rp = new RenderParams[element.SharedMaterials.Length];


                for (int i = 0; i < element.SharedMaterials.Length; i++)
                {
                    rp[i] = new RenderParams(element.SharedMaterials[i])
                    {
                        receiveShadows = element.ReceiveShadows,
                        renderingLayerMask = element.RenderingLayerMask,
                        shadowCastingMode = element.ShadowCastingMode
                    };
                }
            }


            public override void Dispose()
            {
                base.Dispose();
                
                //gpuInstancingComponents.Clear();
                
                gpuInstancingComponents = null;

            }
        }

        abstract class RenderDatasParallelElement<TElement> : RenderData<TElement> where TElement : IGPUInstancingElement
        {
            private Task<ParallelLoopResult> _task;

            private unsafe InstanceData* _ptr;

            protected void RunParellelTask()
            {
                _task = Task.Run(ParallelFor);
            }
            
            protected void BlockThreatUntilTaskFinish()
            {
                if (instanceDatas.Length < jobTrehold) 
                    return;

                while (!_task.IsCompleted || !_task.Result.IsCompleted)
                {
                }
            }

            private unsafe ParallelLoopResult ParallelFor()
            {
                _ptr = (InstanceData*)instanceDatas.GetUnsafePtr();
                return Parallel.For(0, gpuInstancingComponents.Count, parallelOptions, For);
            }

            private unsafe void For(int i)
            {
                _ptr[i] = gpuInstancingComponents[i].InstanceData;
            }
        }
        
        class RenderDatasFixedElement<TElement> : RenderDatasParallelElement<TElement> where TElement : IGPUInstancingElement
        {
            private bool _isChanged;

            protected override bool AutomaticInstanceDatas => false;
            
            protected override void InternalUpdate()
            {
                if (gpuInstancingComponents.Count != instanceDatas.Length)
                {
                    if (instanceDatas.IsCreated)
                        instanceDatas.Dispose();
                    
                    instanceDatas = new NativeArray<InstanceData>(CountElements, Allocator.Persistent);
                    
                    if (gpuInstancingComponents.Count<jobTrehold)
                    {
                        for (int j = 0; j < gpuInstancingComponents.Count; j++)
                        {
                            instanceDatas[j] = gpuInstancingComponents[j].InstanceData;
                        }
                    }
                    else
                    {
                        RunParellelTask();
                    }
                    
                    _isChanged = true;
                }
            }

            protected override void InternalLateUpdate()
            {
                if(_isChanged)
                {
                    BlockThreatUntilTaskFinish();

                    _isChanged = false;
                }

                for (int i = 0; i < rp.Length; i++)
                {
                    Graphics.RenderMeshInstanced(rp[i], mesh, i, instanceDatas);
                }
            }
        }
        
        class RenderDatasMoveElement<TElement> : RenderDatasParallelElement<TElement>  where TElement : IGPUInstancingElement
        {
            protected override void InternalUpdate()
            {
                if (gpuInstancingComponents.Count < jobTrehold)
                {
                    for (int j = 0; j < gpuInstancingComponents.Count; j++)
                    {
                        instanceDatas[j] = gpuInstancingComponents[j].InstanceData;
                    }
                }
                else
                {
                    RunParellelTask();
                }
            }

            protected override void InternalLateUpdate()
            {
                BlockThreatUntilTaskFinish();

                for (int i = 0; i < rp.Length; i++)
                {
                    Graphics.RenderMeshInstanced(rp[i], mesh, i, instanceDatas);
                }
            }
        }
        
        class RenderDatasTransform<TElement, TJobGpu> : RenderData<TElement> where TJobGpu : struct, IJobGpuInstancingForTransform<TElement> where TElement : IGPUInstancingComponent<TJobGpu>
        {
            protected TJobGpu job;
            
            protected TransformAccessArray transformAccessArray;

            public bool isReadOnly;

            protected override void InternalAdd(TElement element)
            {
                base.InternalAdd(element);
                transformAccessArray.Add(element.transform);
            }

            protected override void InternalRemove(TElement element, int index)
            {
                base.InternalRemove(element, index);
                transformAccessArray.RemoveAtSwapBack(index);
            }

            protected override void Create(TElement element)
            {
                transformAccessArray = new TransformAccessArray(4);
                
                job = element.Job;
                
                base.Create(element);
            }

            protected override void InternalUpdate()
            {
                if (gpuInstancingComponents.Count < jobTrehold)
                {
                    for (int j = 0; j < gpuInstancingComponents.Count; j++)
                    {
                        instanceDatas[j] = gpuInstancingComponents[j].InstanceData;
                    }
                }
                else
                {
                    job.Create(instanceDatas,gpuInstancingComponents);
                    
                    if(isReadOnly)
                        jobHandle = job.ScheduleReadOnlyByRef(transformAccessArray,64);
                    else
                        jobHandle = job.ScheduleByRef(transformAccessArray);
                }
            }

            protected override void InternalLateUpdate()
            {
                if (instanceDatas.Length < jobTrehold)
                {
                }
                else
                {
                    jobHandle.Complete();

                    instanceDatas = job.Results();
                }

                for (int i = 0; i < rp.Length; i++)
                {
                    Graphics.RenderMeshInstanced(rp[i], mesh, i, instanceDatas);
                }
            }

            public override void Dispose()
            {
                base.Dispose();
                
                job.Dispose();
                
                if(transformAccessArray.isCreated)
                    transformAccessArray.Dispose();
            }
        }

        public static GPUInstancingManager Instance => instance;

        private readonly Dictionary<int, RenderData> _renderDatas = new();

        public bool Enable { get; set; } = true;

        public void AddFixedElement<TElement>(int hash, TElement element)  where TElement : IGPUInstancingElement
        {
            if (!_renderDatas.ContainsKey(hash))
            {
                _renderDatas.Add(hash, new RenderDatasFixedElement<TElement>());
            }
                
            _renderDatas[hash].Add(element);
        }
        
        public void AddMoveElement<TElement>(int hash, TElement element) where TElement : IGPUInstancingElement
        {
            if (!_renderDatas.ContainsKey(hash))
            {
                _renderDatas.Add(hash, new RenderDatasMoveElement<TElement>());
            }
                
            _renderDatas[hash].Add(element);
        }
        
        public void AddComponentElement<TElement, TJobGpu>(int hash, TElement element, bool isTransformJobReadOnly = false) where TJobGpu : struct, IJobGpuInstancingForTransform<TElement> where TElement : IGPUInstancingComponent<TJobGpu> 
        {
            if (!_renderDatas.ContainsKey(hash))
            {
                _renderDatas.Add(hash,new RenderDatasTransform<TElement, TJobGpu>(){isReadOnly = isTransformJobReadOnly});
            }
                
            _renderDatas[hash].Add(element);
        }
        
        public void Remove(int hash, IGPUInstancingElement element)
        {
            if(!_renderDatas.TryGetValue(hash, out var data))
                return;
            
            data.Remove(element);
        }
        
        public void PostUpdate()
        {
            if(Enable)
                foreach (var keyValue in _renderDatas)
                {
                    keyValue.Value.Update();
                }
        }

        public void PostLateUpdate()
        {
            if(Enable)
                foreach (var keyValue in _renderDatas)
                {
                    keyValue.Value.LateUpdate();
                }
        }

        public void OnLoadScene(Scene arg0, LoadSceneMode loadSceneMode)
        {
            if(loadSceneMode == LoadSceneMode.Additive)
                return;
            
            foreach (var keyValue in _renderDatas)
            {
                keyValue.Value.Dispose();
            }
        }

        public void Quit()
        {
            foreach (var keyValue in _renderDatas)
            {
                keyValue.Value.Dispose();
            }
        }
    }

    [BurstCompile]
    public struct InstanceData 
    {
        public Matrix4x4 objectToWorld;
    }
    
    public interface IJobGpuInstancing 
    {
        public NativeArray<InstanceData> Results();

        public void Dispose();
    }

    public interface IJobGpuInstancingForTransform<TElement> : IJobGpuInstancing, IJobParallelForTransform where TElement : IGPUInstancingElement
    {
        public void Create(NativeArray<InstanceData> instances, List<TElement> list);
    }
    
    public interface IJobGpuInstancingFor<TElement> : IJobGpuInstancing, IJobParallelFor where TElement : IGPUInstancingElement
    {
        public void Create(NativeArray<InstanceData> instances, List<TElement> list);
    }

    public interface IGPUInstancingData
    {
        public Material[] SharedMaterials { get; }

        public Mesh Mesh { get; }
        
        public uint RenderingLayerMask { get; }
        
        public UnityEngine.Rendering.ShadowCastingMode ShadowCastingMode { get; }

        public bool ReceiveShadows { get; }
    }
    
    public interface IGetGPUInstancingElement
    {
        public IGPUInstancingElement GetGPUInstancingElement(System.Action<GPUInstancingElement> updateFunc = null);

        public IGPUInstancingElement GetGPUInstancingElement(Transform transform, System.Action<GPUInstancingElement> updateFunc = null);

        public IGPUInstancingElement GetGPUInstancingElement(Vector3 position, Quaternion rotation, Vector3 lossyscale, System.Action<GPUInstancingElement> updateFunc = null);
    }

    public interface IGPUInstancingElement : IGPUInstancingData, IIndexed
    {
        public InstanceData InstanceData { get; }
    }
    
    public interface IGPUInstancingComponent<out TJobGpu> : IGPUInstancingElement  where TJobGpu : struct, IJobGpuInstancing
    {
        public TJobGpu Job { get; }
        
        public Transform transform { get; }
    }
}




