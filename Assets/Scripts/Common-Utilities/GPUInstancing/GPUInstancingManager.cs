using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace GPUInstancing
{
    public class GPUInstancingManager : MonoBehaviour
    {
        [BurstCompile]
        struct Job : IJobGpuInstancing, IJobParallelForTransform
        {
            [WriteOnly]
            private NativeArray<InstanceData> instances;
            
            public void Create(NativeArray<InstanceData> instances)
            {
                this.instances = instances;
            }

            public NativeArray<InstanceData> Results()
            {
                return instances;
            }

            public void Execute(int index, TransformAccess transform)
            {
                instances[index] = new() { objectToWorld =Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale)};
            }
        }
        
        class RenderData : IDisposable
        {
            private const int jobTrehold = 500;
            
            private List<IGPUInstancingElement> gpuInstancingComponents;

            private RenderParams[] rp;

            private Mesh mesh;

            private NativeArray<InstanceData> instanceDatas;

            private TransformAccessArray transformAccessArray;

            private JobHandle jobHandle;

            private Job job;

            private bool Set => gpuInstancingComponents != null;
            
            public void Add(IGPUInstancingElement element)
            {
                if (!Set)
                {
                    Create(element);
                }
                
                element.Index = gpuInstancingComponents.Count;
            
                gpuInstancingComponents.Add(element);

                if (element is IGPUInstancingComponent component)
                {
                    transformAccessArray.Add(component.transform);    
                }
            }

            public void Remove(IGPUInstancingElement element)
            {
                if (!Set)
                {
                    return;   
                }
                
                int index = element.Index;
                
                element.Index = -1;
                
                gpuInstancingComponents.RemoveAtSwapBack(index);
            
                if(element is not IGPUInstancingComponent)
                    return;
                
                transformAccessArray.RemoveAtSwapBack(index);

                if(gpuInstancingComponents.Count > 0 && index != gpuInstancingComponents.Count)
                {
                    var aux = gpuInstancingComponents[index];
                    aux.Index = index;
                    gpuInstancingComponents[index] = aux;
                }
            }
            
            void Create(IGPUInstancingElement element)
            {
                gpuInstancingComponents = new();
                                
                transformAccessArray = new TransformAccessArray(4);
                
                mesh = element.Mesh;

                rp = new RenderParams[element.SharedMaterials.Length];

                //job = element.Job;

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
            
            public void Update()
            {
                if((gpuInstancingComponents?.Count ?? 0) == 0 )
                    return;
            
                instanceDatas = new NativeArray<InstanceData>(gpuInstancingComponents.Count,Allocator.TempJob);

                if (gpuInstancingComponents.Count < jobTrehold)
                {
                    for (int j = 0; j < gpuInstancingComponents.Count; j++)
                    {
                        instanceDatas[j] = gpuInstancingComponents[j].InstanceData;
                    }
                }
                else
                {
                    job.Create(instanceDatas);
                
                    jobHandle = job.Schedule(transformAccessArray);
                }
            }

            public void LateUpdate()
            {
                if(!instanceDatas.IsCreated)
                    return;

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

                instanceDatas.Dispose();
            }

            public void Dispose()
            {
                transformAccessArray.Dispose();

                gpuInstancingComponents.Clear();
                
                gpuInstancingComponents = null;
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

        public void Add(int hash, IGPUInstancingElement element)
        {
            if(!_renderDatas.ContainsKey(hash))
                _renderDatas.Add(hash, new RenderData());
                
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

    /*
    public static class ExtensionGpuInstancing
    {
        public static Unity.Jobs.JobHandle Shedule<T>(ref this T job, TransformAccessArray transformAccessArray) where T : struct, IJobGpuInstancing, IJobParallelForTransform
        {
            return job.Schedule(transformAccessArray);
        }
    }
    */
    
    public interface IJobGpuInstancing 
    {
        public void Create(NativeArray<InstanceData> instances);

        public NativeArray<InstanceData> Results();
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
        
        //public IJobGpuInstancing Job { get; }
    }
    
    public interface IGPUInstancingComponent : IGPUInstancingElement
    {
        public Transform transform { get; }
    }
}




