using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using System.IO;
using System.Text;
#endif

[CreateAssetMenu(menuName = "DependencyInjectionScriptable")]
public class DependencyInjectionScriptable : ScriptableObject
{
    [DefaultDependency(Dependency.PlayerLife5)]
    public int Prueba;
    
    [SerializeField]
    public DependencyInjectData data;
    
    #if UNITY_EDITOR

    [SerializeField, Min(1)]
    private float timeToReload;

    private bool inQueue;

    private DateTime _dateTime;
    
    public void OnValidate()
    {
        inQueue = false;
        EditorApplication.delayCall -= DelayCall;
        
        EditorApplication.update -= UpdateInEditor;
        EditorApplication.update += UpdateInEditor;

        _dateTime = DateTime.Now.AddSeconds(timeToReload);
    }

    private void UpdateInEditor()
    {
        if (_dateTime > DateTime.Now)
            return;
        
        if (!inQueue)
        {
            EditorApplication.delayCall += DelayCall;
            inQueue = true;
        }
    }

    private void DelayCall()
    {
        inQueue = false;
        EditorApplication.update -= UpdateInEditor;
        
        string path = AssetDatabase.GetAssetPath(this).Replace(name + ".asset", string.Empty);
        string csName = "Dependecy.cs";

        StringBuilder stringBuilder = new();
        List<string> textInFile = new();
        
        foreach (var injection in data.All)
        {
            while (injection.indexInArray>=textInFile.Count)
            {
                textInFile.Add(string.Empty);
            }

            if (!string.IsNullOrEmpty(injection.name))
            {
                textInFile[injection.indexInArray] += $"\n\t///<summary> Type: {injection.type.FullName} <br/>Actual value: {injection.Value} </summary>\n\t{injection.name}, ";
            }
        }

        for (int i = 0; i < textInFile.Count; i++)
        {
            stringBuilder.Append(textInFile[i].Substring(0, textInFile[i].Length-2));
            stringBuilder.Append('=');
            stringBuilder.Append(i);
            stringBuilder.Append(",\n");
        }
        
        File.WriteAllText(path + csName, $"using UnityEngine;\nusing System.Collections;\nusing System.Collections.Generic;\n" +
                                         $"public enum Dependency\n{{\n{stringBuilder}\n}}\n\n" +
                                         $"public partial class DefaultDependencyAttribute\n{{\n" +
                                         $"\tpublic DefaultDependencyAttribute(Dependency dependency)\n" +
                                         $"\t{{\n\t\tindex = (int)dependency;\n\t}}\n\n" +
                                         $"\tpublic DefaultDependencyAttribute(string name)\n" +
                                         $"\t{{\n\t\tindex = (int)System.Enum.Parse<Dependency>(name);\n\t}}\n}}");
    }
#endif
}