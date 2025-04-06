using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

public class ObjectPoolManager : MonoBehaviour
{
    [System.Serializable]
    public class Category 
#if UNITY_EDITOR 
        : ISerializationCallbackReceiver
#endif
    {
        [Header("Category name"), HideInInspector]
        public string name;

        [SerializeField]
        ObjectPoolData objectPool;
        
        [SerializeField]
        public PoolObjectsScene[] poolObjects;
        
#if UNITY_EDITOR
        public void OnBeforeSerialize()
        {
            if(objectPool == null)
                return;
            
            name = objectPool.name;

            if (poolObjects == null || poolObjects.Length != objectPool.poolObjects.Length)
            {
                poolObjects = new PoolObjectsScene[objectPool.poolObjects.Length];

                for (int i = 0; i < poolObjects.Length; i++)
                {
                    poolObjects[i] = new PoolObjectsScene();
                }    
            }
            
            for (int i = 0; i < poolObjects.Length; i++) 
                poolObjects[i].poolObjectsData = objectPool.poolObjects[i];
        }

        public void OnAfterDeserialize()
        {
        }
#endif

    }
    
    [System.Serializable]
    public class ObjectRefence
    {
        public GameObject Obj;
        public Object[] auxiliarReference;

        public ObjectRefence(GameObject prefab, Transform parent, Object[] utilityRefence)
        {
            Obj = Instantiate(prefab, parent);

            auxiliarReference = new Object[utilityRefence.Length];

            for (int i = 0; i < utilityRefence.Length; i++)
            {
                auxiliarReference[i] = Obj.GetComponent(utilityRefence[i].GetType());
            }

            Obj.SetActive(false);
        }
    }
    
    [System.Serializable]
    public class PoolObjectsScene
    {
        [FormerlySerializedAs("_poolObjectsData")]
        [SerializeReference]
        public ObjectPoolData.PoolObjectsData poolObjectsData;
        
        [SerializeField]
        private Transform defaultParent;

        [Header("Internal")]
        int _index = 0;

        [SerializeReference, HideInInspector]
        ObjectRefence[] pool;

        public GameObject prefab => poolObjectsData.prefab;

        public int index
        {
            get
            {
                int aux = _index;
                _index++;
                if (_index >= pool.Length)
                    _index = 0;

                return aux;
            }
        }

        public Transform SpawnPoolObj<T>(out T go) where T : Object
        {
            var aux = pool[index];
            go = default;

            foreach (var item in aux.auxiliarReference)
            {
                if (item is T)
                {
                    go = (T)item;
                    break;
                }
            }
            return aux.Obj.transform;
        }

        public Transform SpawnPoolObj()
        {
            return pool[index].Obj.transform;
        }

        public void Init()
        {
            pool = new ObjectRefence[poolObjectsData.amount];

            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = new ObjectRefence(poolObjectsData.prefab, defaultParent, poolObjectsData.utilityRefence);
            }
        }
    }
    
    [Header("Active generation")]
    public bool eneabled;

    public static bool Instanced => instance != null;

    static ObjectPoolManager instance;
    
    [SerializeField]
    private Category[] categoriesOfPool;
    
#if UNITY_EDITOR
    
    public static int CategoriesCount => instance.categoriesOfPool?.Length ?? 0;

    public static int[] PoolObjectsCount;
    
    [UnityEditor.Callbacks.DidReloadScripts]
    static void EditorReloadScript()
    {
#if HasGamemanager
        GameManager.CreateManagerInScene<ObjectPoolManager>();
#else
        var aux = FindObjectOfType<ObjectPoolManager>();
        
        if (aux != null)
        {
            aux.OnValidate();
            return;
        }
        
        GameObject go = new GameObject("GameManager");
        var newGm = go.AddComponent<ObjectPoolManager>();
        
        Debug.LogWarning("Se creo un Gamemanager en la escena que contiene el PoolManager", newGm);
#endif
    }
    
    
    private void OnValidate()
    {
        EditorReloadScript();
        
        instance = this;
        PoolObjectsCount = new int[CategoriesCount];

        for (int i = 0; i < CategoriesCount; i++)
        {
            PoolObjectsCount[i] = categoriesOfPool[i].poolObjects.Length;
        }
    }
#endif

    #region busqueda por categoria

    /// <summary>
    /// Devuelve los indices de la categoria y el objeto del pool
    /// </summary>
    /// <param name="type">nombre de la clase/categoria del objeto</param>
    /// <param name="powerObject">nombre del prefab del objeto</param>
    /// <returns></returns>
    public static Vector2Int SrchInCategory(string type, string powerObject)
    {
        return SrchInCategory(SrchInCategory(type), powerObject);   
    }

    /// <summary>
    /// devuelve el indice de la categoria dentro del pool
    /// </summary>
    /// <param name="word">nombre de la clase/categoria del objeto</param>
    /// <returns></returns>
    public static int SrchInCategory(string word)
    {
        for (int i = 0; i < instance.categoriesOfPool.Length; i++)
        {
            if (instance.categoriesOfPool[i].name == word)
            {
                return i;
            }
        }
        Debug.LogWarning("Error categoria no encontrada: " + word);
        return -1;
    }

    /// <summary>
    /// devuelve el indice de la categoria dentro del pool
    /// </summary>
    /// <param name="index">indice de la categoria</param>
    /// <param name="powerObject">nombre del prefab del objeto</param>
    /// <returns></returns>
    public static Vector2Int SrchInCategory(int index, string powerObject)
    {
        Vector2Int indexsFind = new Vector2Int(index, -1);

        for (int ii = 0; ii < instance.categoriesOfPool[index].poolObjects.Length; ii++)
        {
            if (instance.categoriesOfPool[index].poolObjects[ii].prefab.name == powerObject)
            {
                indexsFind.y = ii;
                return indexsFind;
            }
        }
        Debug.LogWarning("No se encontro el objeto: " + powerObject);
        return indexsFind;

    }


    /// <summary>
    /// devuelve el indice de la categoria dentro del pool
    /// </summary>
    /// <param name="index">indice de la categoria</param>
    /// <param name="powerObject">nombre del prefab del objeto</param>
    /// <returns></returns>
    public static void SrchInCategory(Vector2Int indexsFind, out string category, out GameObject powerObject)
    {
        category = instance.categoriesOfPool[indexsFind.x].name;

        powerObject = instance.categoriesOfPool[indexsFind.x].poolObjects[indexsFind.y].prefab;
    }

    #endregion

    #region "Spawn" pool objects

    public static Transform SpawnPoolObject(Vector2Int indexs, Vector3? pos = null, Quaternion? angles = null, Transform padre = null)
    {

        var poolObject = InternalSpawnPoolObject(indexs);

        var transformObject = poolObject.SpawnPoolObj();

        SetTransform(transformObject, poolObject.prefab.transform, pos, angles, padre);

        return transformObject;
    }

    public static Transform SpawnPoolObject<T>(Vector2Int indexs, out T reference, Vector3? pos = null, Quaternion? angles = null, Transform padre = null, bool active=true) where T : Object
    {
        var poolObject = InternalSpawnPoolObject(indexs);

        var transformObject = poolObject.SpawnPoolObj(out reference);

        SetTransform(transformObject, poolObject.prefab.transform, pos, angles, padre, active);

        return transformObject;
    }

    static PoolObjectsScene InternalSpawnPoolObject(Vector2Int indexs)
    {
        if (indexs.x < 0)
        {
            Debug.LogWarning("categoria no encontrada");
            return null;
        }
        else if (indexs.y < 0)
        {
            Debug.LogWarning("Objeto no encontrado");
            return null;
        }

        return instance.categoriesOfPool[indexs.x].poolObjects[indexs.y];
    }

    static void SetTransform(Transform transform, Transform original, Vector3? pos = null, Quaternion? angles = null, Transform padre = null, bool active = true)
    {
        transform.SetActiveGameObject(false);

        if (padre != null)
        {
            transform.SetParent(null, true);

            transform.localScale = original.localScale;
        }
        else
        {
            var aux = transform.parent;

            transform.SetParent(null, true);

            transform.localScale = original.localScale;

            transform.SetParent(aux, true);
        }

        if(padre != null)
        {
            transform.SetParent(padre, true);
        }

        if (pos != null)
            transform.localPosition = (Vector3)pos;

        if (angles != null)
            transform.localRotation = (Quaternion)angles;

        transform.gameObject.SetActive(active);
    }
    #endregion


    IEnumerator Start()
    {
        if (!eneabled)
            yield break;
        
        foreach (var item in categoriesOfPool)
        {
            foreach (var subitem in item.poolObjects)
            {
                subitem.Init();
                yield return null;
            }
        }
        
        instance = this;
    }

}