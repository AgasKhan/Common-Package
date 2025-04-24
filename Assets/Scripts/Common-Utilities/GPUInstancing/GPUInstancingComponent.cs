using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;

namespace GPUInstancing
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GPUInstancingComponent : MonoBehaviour, IGPUInstancingComponent<GPUInstancingComponent.JobComponent>, IGetGPUInstancingElement
    {
        [BurstCompile]
        public struct JobComponent : IJobGpuInstancingForTransform<GPUInstancingComponent>
        {
            [WriteOnly]
            private NativeArray<InstanceData> instances;

            public void Create(NativeArray<InstanceData> instances, List<GPUInstancingComponent> list)
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
            
            public void Dispose()
            {
            }
        }
        
        [SerializeField]
        private MeshFilter _meshFilter;

        [SerializeField]
        private MeshRenderer _meshRenderer;

        [SerializeField]
        private GPUInstancingManager manager;
        
        public int Index { get; set; } = -1;

        public JobComponent Job => new JobComponent();

        public Material[] SharedMaterials => _meshRenderer.sharedMaterials;

        public Material[] Materials
        {
            get
            {
                ChangeToInstanceMaterial();
                
                return _meshRenderer.materials;
            }
        }

        public Mesh Mesh => _meshFilter.sharedMesh;
        
        public uint RenderingLayerMask => _meshRenderer.renderingLayerMask;

        public ShadowCastingMode ShadowCastingMode => _meshRenderer.shadowCastingMode;

        public bool ReceiveShadows => _meshRenderer.receiveShadows;

        public InstanceData InstanceData
        {
            get
            {
                return new() { objectToWorld =Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale)}; 
            }
        }
        
        [HideInInspector]
        public int hash;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += DelayCallEditor;
        }

        private void DelayCallEditor()
        {
            if(this == null)
                return;
            
            _meshFilter = GetComponent<MeshFilter>();

            _meshRenderer = GetComponent<MeshRenderer>();

            foreach (var material in SharedMaterials)
            {
                material.enableInstancing = true;
            }
        }
#endif

        public void ChangeToGpuInstancing()
        {
            enabled = true;
        }

        public void ChangeToInstanceMaterial()
        {
            enabled = false;
        }

        public IGPUInstancingElement GetGPUInstancingElement(System.Action<GPUInstancingElement> updateFunc = null)
        {
            return GetGPUInstancingElement(transform, updateFunc);
        }
        
        public IGPUInstancingElement GetGPUInstancingElement(Transform transform, System.Action<GPUInstancingElement> updateFunc = null)
        {
            return GetGPUInstancingElement(transform.position, transform.rotation, transform.lossyScale, updateFunc);
        }
        
        public IGPUInstancingElement GetGPUInstancingElement(Vector3 position, Quaternion rotation, Vector3 lossyscale, System.Action<GPUInstancingElement> updateFunc = null)
        {
            if (updateFunc == null)
                return new GPUInstancingElement(this)
                {
                    Index = 0,
                    Position = position,
                    Rotation = rotation,
                    LossyScale = lossyscale
                };

            return new GPUInstancingElement(this, updateFunc)
            {
                Index = 0,
                Position = position,
                Rotation = rotation,
                LossyScale = lossyscale
            };
        }
        
        private void Awake()
        {
            hash = name.GetHashCode();
            
            if(manager==null)
                manager = GPUInstancingManager.Instance;
        }
        
        private void OnEnable()
        {
            manager.AddComponentElement<GPUInstancingComponent, JobComponent>(hash,this);
            _meshRenderer.enabled = false;
        }

        private void OnDisable()
        {
            manager.Remove(hash,this);
            _meshRenderer.enabled = true;
        }
    }

    [Serializable]
    public class GPUInstancingData : IGPUInstancingData, IGetGPUInstancingElement
    {
        [field:SerializeField]
        public Material[] SharedMaterials { get; set; }
        
        [field:SerializeField]
        public Mesh Mesh { get; set; }
        
        [field:SerializeField]
        public uint RenderingLayerMask { get; set; }
        
        [field:SerializeField]
        public ShadowCastingMode ShadowCastingMode { get; set; }
        
        [field:SerializeField]
        public bool ReceiveShadows { get; set; }

        public IGPUInstancingElement GetGPUInstancingElement(System.Action<GPUInstancingElement> updateFunc = null)
        {
            if (updateFunc == null)
                return new GPUInstancingElement(this);

            return new GPUInstancingElement(this, updateFunc);
        }
        
        public IGPUInstancingElement GetGPUInstancingElement(Transform transform, System.Action<GPUInstancingElement> updateFunc = null)
        {
            return GetGPUInstancingElement(transform.position, transform.rotation, transform.lossyScale);
        }
        
        public IGPUInstancingElement GetGPUInstancingElement(Vector3 position, Quaternion rotation, Vector3 lossyscale, System.Action<GPUInstancingElement> updateFunc = null)
        {
            if (updateFunc == null)
                return new GPUInstancingElement(this)
                {
                    Index = 0,
                    Position = position,
                    Rotation = rotation,
                    LossyScale = lossyscale
                };

            return new GPUInstancingElement(this, updateFunc)
            {
                Index = 0,
                Position = position,
                Rotation = rotation,
                LossyScale = lossyscale
            };
        }
    }
    
    [Serializable]
    public class GPUInstancingElement : IGPUInstancingElement
    {
        [BurstCompile, Serializable]
        public struct DataTransform
        {
            public Vector3 position;
    
            public Quaternion rotation;
    
            public Vector3 lossyScale;
        }
        
        public int Index { get; set; }
        
        public Material[] SharedMaterials => _gpuInstancingData.SharedMaterials;

        public Mesh Mesh => _gpuInstancingData.Mesh;

        public uint RenderingLayerMask => _gpuInstancingData.RenderingLayerMask;

        public ShadowCastingMode ShadowCastingMode => _gpuInstancingData.ShadowCastingMode;

        public bool ReceiveShadows => _gpuInstancingData.ReceiveShadows;

        private IGPUInstancingData _gpuInstancingData;
        
        public event System.Action<GPUInstancingElement> OnRenderParallelUpdate
        {
            add
            {
                if (_onParallelUpdate == null)
                    _update = ExecuteAction;
                
                _onParallelUpdate += value;
            }
            remove
            {
                _onParallelUpdate -= value;
                
                if (_onParallelUpdate == null)
                    _update = VoidAction;
            }
        }
        
        [field:SerializeField]
        public DataTransform transform;
        
        System.Action<GPUInstancingElement> _onParallelUpdate;

        System.Action<GPUInstancingElement> _update;

        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }
    
        public Quaternion Rotation
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }
    
        public Vector3 LossyScale
        {
            get => transform.lossyScale;
            set => transform.lossyScale = value;
        }

        public InstanceData InstanceData
        {
            get
            {
                _update(this);
                return new() { objectToWorld =Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale)};       
            }
        }
        
        public GPUInstancingElement(IGPUInstancingData gpuInstancingData)
        {
            _gpuInstancingData = gpuInstancingData;
            _update = VoidAction;
        }
        
        public GPUInstancingElement(IGPUInstancingData gpuInstancingData,  System.Action<GPUInstancingElement> updateFunc )
        {
            _gpuInstancingData = gpuInstancingData;
            _update = ExecuteAction;
            _onParallelUpdate = updateFunc;
        }
        
        static void VoidAction(GPUInstancingElement gpuInstancingElement)
        {}

        static void ExecuteAction(GPUInstancingElement gpuInstancingElement)
        {
            gpuInstancingElement._onParallelUpdate(gpuInstancingElement);
        }
    }
}