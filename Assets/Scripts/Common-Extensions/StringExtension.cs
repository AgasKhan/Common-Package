using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StringExtension
{
    /*
     * RichText: una tag que se colocara para hacer efectivo el uso de esa etiqueta
     * Bold: aplica la etiqueta Bold
     * Italic: idem italic
     * Color: aplica un color por richText
     * Size: 
     *
     *
     * 
     */
    
    
    /// <summary>
    /// Checks if the string is empty
    /// </summary>
    /// <param name="str"></param>
    /// <returns>true for empty strings</returns>
    public static bool IsEmpty(this string str)
    {
        return str.Length == 0;
    }
}
