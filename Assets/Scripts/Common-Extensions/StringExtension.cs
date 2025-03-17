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

    /// <summary>
    /// Sets tags to make the string bold
    /// </summary>
    /// <param name="str"></param>
    /// <returns><b>string</b></returns>
    public static string Bold(this string str)
    {
        return "<b>" + str + "</b>";
    }

    /// <summary>
    /// Sets tags to make the string italic
    /// </summary>
    /// <param name="str"></param>
    /// <returns><i>string</i></returns>
    public static string Italic(this string str)
    {
        return "<i>" + str + "</i>";
    }

    /// <summary>
    /// Sets tags to make the string underlined
    /// </summary>
    /// <param name="str"></param>
    /// <returns><u>string</u></returns>
    public static string Underline(this string str)
    {
        return "<u>" + str + "</u>";
    }

    /// <summary>
    /// Sets tags to give the text a color
    /// </summary>
    /// <param name="str"></param>
    /// <param name="color"></param>
    /// <returns><color=#"color">string</returns>
    public static string Color(this string str, string color)
    {
        return "<color=#" + color + ">" + str;
    }

    /// <summary>
    /// Sets tags to change the size of the string
    /// </summary>
    /// <param name="str"></param>
    /// <param name="size"></param>
    /// <returns><size="size"px>string</returns>
    public static string Size(this string str, float size)
    {
        return "<size=" + size + "px>" + str;
    }
}
