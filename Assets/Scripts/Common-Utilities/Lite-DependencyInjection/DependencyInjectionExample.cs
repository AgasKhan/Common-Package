using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DependencyInjectionExample : MonoBehaviour
{
    [System.Serializable]
    class Example2
    {
        [DefaultDependency()]
        [SerializeField]
        public MonoBehaviour mono2;
        
        [DefaultDependency()]
        [SerializeField]
        public MonoBehaviour mono3;
    }
    
    [DefaultDependency()]
    [SerializeField]
    private int entero;
    
    [DefaultDependency()]
    [SerializeField]
    private int ente2;
    
    [DefaultDependency()]
    [SerializeField]
    protected string cadenaTexto;
    
    [DefaultDependency()]
    public Vector3 vector3;
    
    [DefaultDependency()]
    [field: SerializeField]
    public Vector2 vector2 { get; set; }
    
    [SerializeField]
    private Example2 exampleClass;
    
    [DefaultDependency()]
    [field: SerializeField]
    public MonoBehaviour mono { get; set; }
}
