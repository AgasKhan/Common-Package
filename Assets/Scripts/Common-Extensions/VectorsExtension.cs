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
     *
     * sqrMagnitud entre 2 vectores HECHO
     * IsInRadius: recibe una posicion la cual comparara con la poscion pasada, y devolvera true en caso de que este en el radio (utilizar sqrMagnitud para determinar si esta en el radio) HECHO
     * aproxDir: utilizar logica sencilla para determinar la direccion normalizada (vector de largo 1) aprozimada, sin usar ninguna funcion de normalizacion
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
    public static Vector2 Vec3To2XY(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.y);
    }

    /// <summary>
    /// Used to convert a vector3 to a vector2 with x and z taking the values of x and y.
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static Vector2 Vec3To2XZ(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.z);
    }



    /// <summary>
    /// Used to set all values of a vector2 to 0.
    /// </summary>
    /// <param name="vec"></param>
    public static void SetZero(ref this Vector2 vec)
    {
        vec.x = 0;
        vec.y = 0;
    }


    /// <summary>
    /// Used to set all values of a vector3 to 0.
    /// </summary>
    /// <param name="vec"></param>
    public static void SetZero(ref this Vector3 vec)
    {
        vec.x = 0;
        vec.y = 0;
        vec.z = 0;
    }


    /// <summary>
    /// Used to set x of a vector2 to any number.
    /// </summary>
    /// <param name="vec"></param>
    public static ref Vector2 SetX(ref this Vector2 vec, float value)
    {
        vec.x = value;

        return ref vec;
    }

    /// <summary>
    /// Used to set x of a vector3 to any number.
    /// </summary>
    /// <param name="vec"></param>
    public static ref Vector3 SetX(ref this Vector3 vec, float value)
    {
        vec.x = value;

        return ref vec;
    }
    

    /// <summary>
    /// Used to set y of a vector2 to any number.
    /// </summary>
    /// <param name="vec"></param>
    public static ref Vector2 SetY(ref this Vector2 vec, float value)
    {
        vec.y = value;

        return ref vec;
    }

    /// <summary>
    /// Used to set y of a vector3 to any number.
    /// </summary>
    /// <param name="vec"></param>
    public static ref Vector3 SetY(ref this Vector3 vec, float value)
    {
        vec.y = value;
        
        return ref vec;
    }

    

    /// <summary>
    /// Used to set z of a vector3 to any number.
    /// </summary>
    /// <param name="vec"></param>
    public static ref Vector3 SetZ(ref this Vector3 vec, float value)
    {
        vec.z = value;

        return ref vec;
    }

    /// <summary>
    /// Used to... what is it used for again?
    /// </summary>
    /// <param name="vec1"></param>
    /// <param name="vec2"></param>
    public static float Vector2SQRMagnitudBetween(this Vector2 vec1, Vector2 vec2)
    {
        Vector2 difference = vec1 - vec2;

        return difference.sqrMagnitude;
    }

    /// <summary>
    /// Used to... what is it used for again?
    /// </summary>
    /// <param name="vec1"></param>
    /// <param name="vec2"></param>
    public static float Vector3SQRMagnitudBetween(this Vector3 vec1, Vector3 vec2)
    {
        Vector3 difference = vec1 - vec2;

        return difference.sqrMagnitude;
    }

    /// <summary>
    /// Used to... what is it used for again?
    /// </summary>
    /// <param name="vec1"></param>
    /// <param name="vec2"></param>
    public static bool Vector2IsInRadius(this Vector2 vec1, Vector2 vec2, float closeDistance, bool isinRadius)
    {
            float sqrLen = Vector2SQRMagnitudBetween(vec1, vec2);
            if(sqrLen < closeDistance * closeDistance)
            {
            isinRadius = true;
            }
            else
            {
            isinRadius = false;
            }
        return isinRadius;
    }

    /// <summary>
    /// Used to... what is it used for again?
    /// </summary>
    /// <param name="vec1"></param>
    /// <param name="vec2"></param>
    public static bool Vector3IsInRadius(this Vector3 vec1, Vector3 vec2, float closeDistance, bool isinRadius)
    {
        float sqrLen = Vector3SQRMagnitudBetween(vec1, vec2);
        if (sqrLen < closeDistance * closeDistance)
        {
            isinRadius = true;
        }
        else
        {
            isinRadius = false;
        }
        return isinRadius;
    }

    /// <summary>
    /// Used to... what is it used for again?
    /// </summary>
    /// <param name="vec"></param>
    public static Vector2 AproxDir(Vector2 vec)
    {
        if (vec == Vector2.zero)
            return Vector2.zero;

        float approxMagnitude = Mathf.Abs(vec.x) + Mathf.Abs(vec.y);

        return new Vector2(vec.x / approxMagnitude, vec.y / approxMagnitude);
    }

    /// <summary>
    /// Used to... what is it used for again?
    /// </summary>
    /// <param name="vec"></param>
    public static Vector3 AproxDir(Vector3 vec)
    {
        if (vec == Vector3.zero)
            return Vector3.zero;

        float approxMagnitude = Mathf.Abs(vec.x) + Mathf.Abs(vec.y) + Mathf.Abs(vec.z);

        return new Vector3(vec.x / approxMagnitude, vec.y / approxMagnitude, vec.z / approxMagnitude);
    }
}
