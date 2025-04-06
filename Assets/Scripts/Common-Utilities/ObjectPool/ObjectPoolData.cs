using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CommonPackage/ObjectPoolData")]
public class ObjectPoolData : ScriptableObject
{
    [System.Serializable]
    public class PoolObjectsData
    {
        public GameObject prefab;
        public Object[] utilityRefence;
        public int amount;
    }
    
    public PoolObjectsData[] poolObjects;
}
