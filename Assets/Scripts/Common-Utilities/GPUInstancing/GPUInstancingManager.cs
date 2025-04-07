using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

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

        abstract class RenderData<TElement ,TJobGpu> : RenderData where TJobGpu : struct, IJobGpuInstancing where TElement : IGPUInstancingElement<TJobGpu>
        {
            protected override bool Set => gpuInstancingComponents != null;

            protected override bool HasElements => Set && gpuInstancingComponents.Count != 0;

            protected override int CountElements => gpuInstancingComponents.Count;

            protected TJobGpu job;
            
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
                
                element.Index = gpuInstancingComponents.Count;
            
                gpuInstancingComponents.Add(element);
            }

            protected virtual void InternalRemove(TElement element, int index)
            {
                element.Index = -1;
            
                gpuInstancingComponents.RemoveAtSwapBack(index);

                if(gpuInstancingComponents.Count > 0 && index != gpuInstancingComponents.Count)
                {
                    var aux = gpuInstancingComponents[index];
                    aux.Index = index;
                    gpuInstancingComponents[index] = aux;
                }
            }

            protected virtual void Create(TElement element)
            {
                gpuInstancingComponents = new();
                
                mesh = element.Mesh;

                rp = new RenderParams[element.SharedMaterials.Length];

                job = element.Job;

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
                
                gpuInstancingComponents.Clear();
                
                gpuInstancingComponents = null;
                
                job.Dispose();
            }
        }
        
        class RenderDatasFixedElement<TElement, TJobGpu> : RenderData<TElement,TJobGpu> where TJobGpu : struct, IJobGpuInstancingFor<TElement> where TElement : IGPUInstancingElement<TJobGpu>
        {
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
                        job.Create(instanceDatas,gpuInstancingComponents);
                    
                        jobHandle = job.Schedule(instanceDatas.Length, Mathf.Max(instanceDatas.Length / jobTrehold,1));
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
                    jobHandle.Complete();

                    instanceDatas = job.Results();
                }

                for (int i = 0; i < rp.Length; i++)
                {
                    Graphics.RenderMeshInstanced(rp[i], mesh, i, instanceDatas);
                }
            }
        }
        
        class RenderDatasMoveElement<TElement, TJobGpu> : RenderData<TElement,TJobGpu> where TJobGpu : struct, IJobGpuInstancingFor<TElement> where TElement : IGPUInstancingElement<TJobGpu>
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
                    job.Create(instanceDatas,gpuInstancingComponents);
                    
                    jobHandle = job.Schedule(instanceDatas.Length, Mathf.Max(instanceDatas.Length / jobTrehold,1));
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
        }
        
        class RenderDatasTransform<TElement, TJobGpu> : RenderData<TElement,TJobGpu> where TJobGpu : struct, IJobGpuInstancingForTransform<TElement> where TElement : IGPUInstancingComponent<TJobGpu>
        {
            protected TransformAccessArray transformAccessArray;

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
                    
                    jobHandle = job.Schedule(transformAccessArray);
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

        public void AddFixedElement<TElement, TJobGpu>(int hash, TElement element) where TJobGpu : struct, IJobGpuInstancingFor<TElement> where TElement : IGPUInstancingElement<TJobGpu> 
        {
            if (!_renderDatas.ContainsKey(hash))
            {
                _renderDatas.Add(hash, new RenderDatasFixedElement<TElement, TJobGpu>());
            }
                
            _renderDatas[hash].Add(element);
        }
        
        public void AddMoveElement<TElement, TJobGpu>(int hash, TElement element) where TJobGpu : struct, IJobGpuInstancingFor<TElement> where TElement : IGPUInstancingElement<TJobGpu> 
        {
            if (!_renderDatas.ContainsKey(hash))
            {
                _renderDatas.Add(hash, new RenderDatasMoveElement<TElement, TJobGpu>());
            }
                
            _renderDatas[hash].Add(element);
        }
        
        public void AddComponentElement<TElement, TJobGpu>(int hash, TElement element) where TJobGpu : struct, IJobGpuInstancingForTransform<TElement> where TElement : IGPUInstancingComponent<TJobGpu> 
        {
            if (!_renderDatas.ContainsKey(hash))
            {
                _renderDatas.Add(hash,new RenderDatasTransform<TElement, TJobGpu>());
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
        public uint renderingLayerMask;
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

    public interface IGPUInstancingElement
    {
        public Material[] SharedMaterials { get; }

        public Mesh Mesh { get; }

        public int Index { get; set; }
        
        public uint RenderingLayerMask { get; }
        
        public UnityEngine.Rendering.ShadowCastingMode ShadowCastingMode { get; }

        public bool ReceiveShadows { get; }
        
        public InstanceData InstanceData { get; }
    }

    public interface IGPUInstancingElement<out TJobGpu>: IGPUInstancingElement where TJobGpu : struct, IJobGpuInstancing
    {
        public TJobGpu Job { get; }
    }
    
    public interface IGPUInstancingComponent<out TJobGpu> : IGPUInstancingElement<TJobGpu>  where TJobGpu : struct, IJobGpuInstancing
    {
        public Transform transform { get; }
    }
}




