using System;
using System.Collections;
using System.Collections.Generic;
using GPUInstancing;
using UnityEngine;

public class GridSpawn : MonoBehaviour
{
    [SerializeField]
    private Vector3Int gridSize;

    [SerializeField]
    private Vector3 spacing;

    [SerializeField]
    private Vector3 offset;

    [SerializeField]
    private GameObject prefab;
    
    [SerializeField]
    private GPUInstancingComponent gpuInstancingComponent;

    [SerializeField]
    private bool showPreviusly;

    [SerializeField]
    private Vector3 count;
    
    
    static Vector3 mousePosition;

    static float dt;
    
    private void Awake()
    {
        GameManager.LoadManager.RoutineQueue = MyStart();
    }

    IEnumerator MyStart()
    {
        yield return null;
        
        var manager = GPUInstancingManager.Instance;

        var hash = gpuInstancingComponent.name.GetHashCode();
        
        count.x = gridSize.x * gridSize.y * gridSize.z;
        
        for (int z = 0; z < gridSize.z; z++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {

                    var element = gpuInstancingComponent.GetGPUInstancingElement(
                        offset + new Vector3(spacing.x * x, spacing.y * y, spacing.z * z), Quaternion.identity,
                        Vector3.one, UpdateFunc);
                    
                    manager.AddMoveElement(hash,element);
                    
                    
                    //Instantiate(prefab, offset + new Vector3(spacing.x * x, spacing.y * y, spacing.z * z), Quaternion.identity);

                    count.y++;

                    count.z = (Time.timeSinceLevelLoad / count.y) * (count.x - count.y);

                    count.z = count.z / 60;

                    if (EngineUpdate.BelowLowFrameRate)
                        yield return null;
                }
            }
        }
    }

    
    private void Update()
    {
        mousePosition = Input.mousePosition;
        dt = Time.deltaTime;
    }
    

    static void UpdateFunc(GPUInstancingElement obj)
    {
        var dir = (new Vector3(mousePosition.x, obj.Position.y, obj.Position.z) - obj.Position);
        
        obj.Position += Vector3.ClampMagnitude(dir,  Mathf.Max(Mathf.Abs(obj.GetHashCode())%1000 / 100f, 1) ) * dt;
    }

    private void OnDrawGizmosSelected()
    {
        if(prefab==null)
            return;

        System.Action<int, int, int> action;
        
        if(showPreviusly)
        {
            Mesh mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
            
            action = (x, y, z)=> Gizmos.DrawWireMesh( mesh, offset + new Vector3(spacing.x * x, spacing.y * y, spacing.z * z),prefab.transform.rotation, prefab.transform.lossyScale);
        }
        else
        {
            action = (x, y, z) =>
                Gizmos.DrawWireSphere(offset + new Vector3(spacing.x * x, spacing.y * y, spacing.z * z), 0.5f);
        }
        
        for (int z = 0; z < gridSize.z; z++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    action(x, y, z);
                }
            }
        }
    }
}
