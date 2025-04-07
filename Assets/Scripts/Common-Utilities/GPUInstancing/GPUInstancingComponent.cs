using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


namespace GPUInstancing
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GPUInstancingComponent : MonoBehaviour, IGPUInstancingComponent<GPUInstancingComponent.JobComponent>, IDeferredUpdate
    {
        [BurstCompile]
        public struct JobComponent : IJobGpuInstancingForTransform<GPUInstancingComponent>
        {
            [WriteOnly]
            private NativeArray<InstanceData> instances;
            
            private NativeArray<Stats> stats;

            private Vector3 mousePosition;

            public void Create(NativeArray<InstanceData> instances, List<GPUInstancingComponent> list)
            {
                this.instances = instances;

                mousePosition = Input.mousePosition;
                
                if(stats.IsCreated && stats.Length == list.Count)
                    return;

                if (stats.IsCreated)
                    stats.Dispose();

                stats = new NativeArray<Stats>(list.Count, Allocator.Persistent);
                
                unsafe
                {
                    Stats* ptr = (Stats*)stats.GetUnsafePtr();
                    
                    var task = Parallel.For(0, list.Count, index =>
                    {
                        ptr[index] = list[index].stats;
                    });

                    while (!task.IsCompleted)//bloqueo de hilo hasta que termine c:
                    {
                    }
                }
            }

            public NativeArray<InstanceData> Results()
            {
                return instances;
            }

            public void Execute(int index, TransformAccess transform)
            {
                var myStats = stats[index];
                
                myStats.vectorVelocity = (mousePosition - transform.position);

                myStats.vectorVelocity.z = 0;
                myStats.vectorVelocity.y = 0;

                myStats.vectorVelocity = Vector3.ClampMagnitude(myStats.vectorVelocity, myStats.velocity);
                
                transform.position += myStats.vectorVelocity;
                
                instances[index] = new() { objectToWorld =Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale)};
            }
            
            public void Dispose()
            {
                stats.Dispose();
            }
        }
        
        [Serializable]
        public struct Stats
        {
            public float velocity;
            public Vector3 vectorVelocity;
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

        public Stats stats;

        public InstanceData InstanceData
        {
            get
            {
                transform.position += stats.vectorVelocity;
                
                return new() { objectToWorld =Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale), renderingLayerMask = RenderingLayerMask}; 
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
            _meshFilter = GetComponent<MeshFilter>();

            _meshRenderer = GetComponent<MeshRenderer>();

            _meshRenderer.enabled = false;

            manager = GPUInstancingManager.CreateInScene();

            foreach (var material in SharedMaterials)
            {
                material.enableInstancing = true;
            }
        }
#endif

        public void ChangeToGpuInstancing()
        {
            _meshRenderer.enabled = false;

            enabled = true;
        }

        public void ChangeToInstanceMaterial()
        {
            _meshRenderer.enabled = true;

            enabled = false;
        }
        
        public void MyDeferredUpdate()
        {
            Materials[0].color = Color.red;
            
            if (Random.Range(0, 100)  != 0)
            {
                GameManager.GamePlayManager.EventQueue += ChangeToGpuInstancing;  
            }
            else
            {
                GameManager.GamePlayManager.EventQueue += ()=> Materials[0].color = Random.ColorHSV();
            }
        }
        
        private void Awake()
        {
            hash = name.GetHashCode();
            
            if(manager==null)
                manager = GPUInstancingManager.CreateInScene();
            
            
            stats.velocity = Random.Range(1f, 10f);

            GameManager.DeferredUpdates = this;
        }
        
        private void OnEnable()
        {
            manager.AddComponentElement<GPUInstancingComponent, JobComponent>(hash,this);
        }

        private void OnDisable()
        {
            manager.Remove(hash,this);
        }
    }
}

