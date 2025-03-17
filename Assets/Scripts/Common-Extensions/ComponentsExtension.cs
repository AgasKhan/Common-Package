using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ComponentsExtension
{
    /// <summary>
    /// Prendo y apago un behaviour, no un gameObject
    /// </summary>
    /// <param name="behaviour"></param>
    /// <param name="active"></param>
    public static void SetActive(this Behaviour behaviour, bool active)
    {
        behaviour.enabled = active;
    }

    /// <summary>
    /// Prendo o apago un GameObject
    /// </summary>
    /// <param name="component"></param>
    /// <param name="active"></param>
    public static void SetActiveGameObject(this Component component, bool active)
    {
        component.gameObject.SetActive(active);
    }
    
    
    public static bool IsInRadius(this Component component, Component toCompare, float radius)
    {
        return false;
    }
    
    public static bool IsInRadius(this Component component, Vector3 toCompare, float radius)
    {
        return false;
    }
    
    public static bool IsInRadius(this Vector3 pos, Component toCompare, float radius)
    {
        return false;
    }
}
