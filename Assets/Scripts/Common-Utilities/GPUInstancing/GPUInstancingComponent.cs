using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


namespace GPUInstancing
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GPUInstancingComponent : MonoBehaviour, IGPUInstancingComponent, IDeferredUpdate
    {
        [SerializeField]
        private MeshFilter _meshFilter;

        [SerializeField]
        private MeshRenderer _meshRenderer;

        [SerializeField]
        private GPUInstancingManager manager;
        
        public int Index { get; set; } = -1;


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
        
        public InstanceData InstanceData => new() { objectToWorld =Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale), renderingLayerMask = RenderingLayerMask};
        
        [HideInInspector]
        public int hash;
        
#if UNITY_EDITOR
        private void OnValidate()
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

            GameManager.DeferredUpdates = this;
        }
        
        private void OnEnable()
        {
            manager.Add(hash,this);
        }

        private void OnDisable()
        {
            manager.Remove(hash,this);
        }
    }
}

