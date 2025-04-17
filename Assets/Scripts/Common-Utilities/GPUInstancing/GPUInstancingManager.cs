using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;

namespace GPUInstancing
{
    public class GPUInstancingManager : MonoBehaviour
    {
        abstract class RenderData : IDisposable
        {
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
        
        class RenderDatasFixedElement<TElement> : RenderData<TElement> where TElement : IGPUInstancingElement
        {
            protected override bool AutomaticInstanceDatas => false;

            private bool isChanged;
            
            private ParallelLoopResult task;

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
                        unsafe
                        {
                            InstanceData* ptr = (InstanceData*)instanceDatas.GetUnsafePtr();
                    
                            task = Parallel.For(0, gpuInstancingComponents.Count, (i, state) =>
                            {
                                ptr[i] = gpuInstancingComponents[i].InstanceData;
                            });
                        }

                        isChanged = true;
                    }
                }
            }

            protected override void InternalLateUpdate()
            {
                if(isChanged)
                {
                    if (instanceDatas.Length < jobTrehold)
                    {
                    }
                    else
                    {
                        while (!task.IsCompleted)
                        {
                        }
                    }

                    isChanged = false;
                }

                for (int i = 0; i < rp.Length; i++)
                {
                    Graphics.RenderMeshInstanced(rp[i], mesh, i, instanceDatas);
                }
            }
        }
        
        class RenderDatasMoveElement<TElement> : RenderData<TElement>  where TElement : IGPUInstancingElement
        {
            private ParallelLoopResult task;
            
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
                    unsafe
                    {
                        InstanceData* ptr = (InstanceData*)instanceDatas.GetUnsafePtr();
                    
                        task = Parallel.For(0, gpuInstancingComponents.Count, (i, state) =>
                        {
                            ptr[i] = gpuInstancingComponents[i].InstanceData;
                        });
                    }
                }
            }

            protected override void InternalLateUpdate()
            {
                if (instanceDatas.Length < jobTrehold)
                {
                }
                else
                {
                    while (!task.IsCompleted)
                    {
                    }
                }

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
                
                transformAccessArray.Dispose();
            }
        }
        
        
        
        
    #if UNITY_EDITOR
        
        [UnityEditor.Callbacks.DidReloadScripts]
        static void EditorReloadScript()
        {
            CreateInScene();
        }

        public static GPUInstancingManager CreateInScene()
        {
    #if HasGamemanager
           return GameManager.CreateManagerInScene<GPUInstancingManager>();
    #else
            var aux = FindObjectOfType<GPUInstancingManager>();
            
            if (aux != null)
            {
                return aux;
            }
            
            GameObject go = new GameObject("GameManager");
            var newGm = go.AddComponent<GPUInstancingManager>();
            
            Debug.LogWarning("Se creo un Gamemanager en la escena que contiene el GPUInstancingManager", newGm);
            
            return newGm;
    #endif
        }

    #endif

        private Dictionary<int, RenderData> _renderDatas = new();

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
            if(!_renderDatas.ContainsKey(hash))
                return;
            
            _renderDatas[hash].Remove(element);
        }
        
        private void Update()
        {
            foreach (var keyValue in _renderDatas)
            {
                keyValue.Value.Update();
            }
        }

        private void LateUpdate()
        {
            foreach (var keyValue in _renderDatas)
            {
                keyValue.Value.LateUpdate();
            }
        }

        private void OnDestroy()
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
        public IGPUInstancingElement GetGPUInstancingElement();

        public IGPUInstancingElement GetGPUInstancingElement(Transform transform);

        public IGPUInstancingElement GetGPUInstancingElement(Vector3 position, Quaternion rotation, Vector3 lossyscale);
    }

    public interface IGPUInstancingElement : IGPUInstancingData, IIndexed
    {
        public InstanceData InstanceData { get; }
    }

    /*
    public interface IGPUInstancingElement<out TJobGpu>: IGPUInstancingElement where TJobGpu : struct, IJobGpuInstancing
    {
        
    }
    */
    
    public interface IGPUInstancingComponent<out TJobGpu> : IGPUInstancingElement  where TJobGpu : struct, IJobGpuInstancing
    {
        public TJobGpu Job { get; }
        
        public Transform transform { get; }
    }
}




