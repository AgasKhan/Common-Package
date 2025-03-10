using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorsExtension
{
    /*
     * Vec2To3XY: pide la z HECHO
     * Vec2To3XZ: pide la y HECHO
     * Vec3To2XY: HECHO
     * Vec3To2XZ: HECHO
     * SetZero: trabaja con ref, y setea en 0 los vectors2 y 3 HECHO
     * setX: 2y3 ref y retorna ref
     * SetY: 2y3
     * SetZ: 3
     */


    /// <summary>
    /// Used to convert a vector2 to a vector3 with x and y taking the values of x and y.
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static Vector3 Vec2To3XY(this Vector2 vec, float z)
    {
        return new Vector3(vec.x, vec.y, z);
    }


    /// <summary>
    /// Used to convert a vector2 to a vector3 with x and y taking the values of x and z.
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static Vector3 Vec2To3XZ(this Vector2 vec, float y)
    {
        return new Vector3(vec.x, y, vec.y);
    }



    /// <summary>
    /// Used to convert a vector3 to a vector2 with x and y taking the values of x and y.
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static Vector3 Vec3To2XY(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.y);
    }

    /// <summary>
    /// Used to convert a vector3 to a vector2 with x and z taking the values of x and y.
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static Vector3 Vec3To2XZ(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.z);
    }



    /// <summary>
    /// Used to set all values of a vector2 to 0.
    /// </summary>
    /// <param name="vec"></param>
    public static void SetZeroVec2(ref this Vector2 vec)
    {
        vec.x = 0;
        vec.y = 0;
    }


    /// <summary>
    /// Used to set all values of a vector3 to 0.
    /// </summary>
    /// <param name="vec"></param>
    public static void SetZeroVec3(ref this Vector3 vec)
    {
        vec.x = 0;
        vec.y = 0;
        vec.z = 0;
    }
    



    /// <summary>
    /// Used to set x of a vector2 to any number.
    /// </summary>
    /// <param name="vec"></param>
    public static void SetXVec2(ref this Vector2 vec, float value)
    {
        vec.x = value;
    }

    /// <summary>
    /// Used to set x of a vector3 to any number.
    /// </summary>
    /// <param name="vec"></param>
    public static void SetXVec3(ref this Vector3 vec, float value)
    {
        vec.x = value;
    }
    


    /// <summary>
    /// Used to set y of a vector2 to any number.
    /// </summary>
    /// <param name="vec"></param>
    public static void SetYVec2(ref this Vector2 vec, float value)
    {
        vec.y = value;
    }

    /// <summary>
    /// Used to set y of a vector3 to any number.
    /// </summary>
    /// <param name="vec"></param>
    public static void SetYVec3(ref this Vector3 vec, float value)
    {
        vec.y = value;
    }

    

    /// <summary>
    /// Used to set z of a vector3 to any number.
    /// </summary>
    /// <param name="vec"></param>
    public static void SetZVec3(ref this Vector3 vec, float value)
    {
        vec.z = value;
    }
}
