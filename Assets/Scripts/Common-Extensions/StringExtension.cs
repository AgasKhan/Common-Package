using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.Text;

public static class StringExtension
{
    /*
     * RichText: una tag que se colocara para hacer efectivo el uso de esa etiqueta
     * Bold: aplica la etiqueta Bold
     * Italic: idem italic
     * Color: aplica un color por richText
     * Size: 
     * ClearRichText: Quitar todas las etiquetas
     * Length: en caso de encontrar etiquetas no las cuenta (la idea es no crear strings en el medio para que sea copado en memoria)
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
    /// Checks if the string is empty or null
    /// </summary>
    /// <param name="str"></param>
    /// <returns><b>string</b></returns>
    public static bool IsEmptyOrNull(this string str)
    {
        return str == null || str.IsEmpty();
    }

    /// <summary>
    /// Clears the Rich Text tags from a string
    /// </summary>
    /// <param name="str"></param>
    /// <returns><b>string</b></returns>
    public static string ClearRichText(this string str)
    {
        int openTag = 0;
        var outCome = new StringBuilder(str);

        for (int i = 0; i < outCome.Length; i++)
        {
            if (outCome[i] == '<')
            {
                openTag = i;
            }

            if (outCome[i] == '>')
            {
                outCome.Remove(openTag, i - openTag + 1);

                openTag = 0;
                i = 0;
            } 
        }

        return new string(outCome.ToString());
    }

    /// <summary>
    /// Measures the length of a string without the Rich Text tags
    /// </summary>
    /// <param name="str"></param>
    /// <returns><b>string</b></returns>
    public static int ClearLength(this string str)
    {
        return str.ClearRichText().Length;
    }

    /// <summary>
    /// Sets Rich Text tags to modify the aspect of the string
    /// </summary>
    /// <param name="str"></param>
    /// <returns><b>string</b></returns>
    public static string RichText(this string str, string tag, string value = null)
    {
        if(value.IsEmptyOrNull())
            return "<"+tag+">" + str + "</"+tag+">";
        
        return "<"+tag+"="+value+">" + str + "</"+tag+">";
    }

    /// <summary>
    /// Modifies the Rich Text tags to make the string bold
    /// </summary>
    /// <param name="str"></param>
    /// <returns><b>string</b></returns>
    public static string Bold(this string str)
    {
        return str.RichText("b");
    }

    /// <summary>
    /// Modifies Rich Text tags to make the string italic
    /// </summary>
    /// <param name="str"></param>
    /// <returns><i>string</i></returns>
    public static string Italic(this string str)
    {
        return str.RichText("i");
    }

    /// <summary>
    /// Modifies tags to make the string underlined
    /// </summary>
    /// <param name="str"></param>
    /// <returns><u>string</u></returns>
    public static string Underline(this string str)
    {
        return str.RichText("u");
    }

    /// <summary>
    /// Modifies Rich Text tags to give the text a color
    /// </summary>
    /// <param name="str"></param>
    /// <param name="color"></param>
    /// <returns><color=#"color">string</returns>
    public static string Color(this string str, Color color)
    {
        return str.RichText("color","#" + ColorUtility.ToHtmlStringRGBA(color));
    }

    /// <summary>
    /// Modifies Rich Text tags to change the visible size of the string
    /// </summary>
    /// <param name="str"></param>
    /// <param name="size"></param>
    /// <returns><size="size"px>string</returns>
    public static string Size(this string str, float size)
    {
        return str.RichText("size", size.ToString(CultureInfo.InvariantCulture));
    }
}
