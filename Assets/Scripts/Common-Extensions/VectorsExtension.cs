using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorsExtension
{
    /*
     * Vec2To3XY: pide la z
     * Vec2To3XZ: pide la y
     * Vec3To2XY:
     * Vec3To2XZ:
     * SetZero: trabaja con ref, y setea en 0 los vectors2 y 3
     * setX: 2y3 ref y retorna ref
     * SetY: 2y3
     * SetZ: 3
     * Vec3To2XY:
     * Vec3To2XZ:
     */
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static Vector3 Vec2To3(this Vector2 vec, float z)
    {
        return new Vector3(vec.x, vec.y, z);
    }
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="vec"></param>
    public static void SetZero(ref this Vector3 vec)
    {
        vec.x = 0;
        vec.y = 0;
        vec.z = 0;
    }
    
}
