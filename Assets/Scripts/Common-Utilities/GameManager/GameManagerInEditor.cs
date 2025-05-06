using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
public partial class  GameManager
{
    static UnityEditor.Build.NamedBuildTarget CurrentNamedBuildTarget
    {
        get
        {
#if UNITY_SERVER
            return NamedBuildTarget.Server;
#else
            UnityEditor.BuildTarget buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            UnityEditor.BuildTargetGroup targetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(buildTarget);
            UnityEditor.Build.NamedBuildTarget namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            return namedBuildTarget;
#endif
        }
    }
    
    static GameManager CreateInScene()
    {
        if (instance != null)
            return instance;
        
        var aux = FindObjectOfType<GameManager>();
        if(aux!=null)
            return aux;

        GameObject go = new GameObject("GameManagers");
        var newGm = go.AddComponent<GameManager>();
        
        Debug.LogWarning("Se creo un nuevo GameManager para la escena", newGm);

        return newGm;
    }
    
    [UnityEditor.InitializeOnLoadMethod]
    [UnityEditor.Callbacks.DidReloadScripts]
    static void EditorReloadScript()
    {
        const string defineStr = "HasGamemanager";

        var currentNameBuildTarget = CurrentNamedBuildTarget;
        
        CreateInScene();

        UnityEditor.PlayerSettings.allowUnsafeCode = true;
        
        UnityEditor.PlayerSettings.GetScriptingDefineSymbols(currentNameBuildTarget, out string[] defines);
        
        List<string> definesList = new List<string>(defines);
        
        if(definesList.Contains(defineStr))
            return;

        definesList.Add(defineStr);
        
        UnityEditor.PlayerSettings.SetScriptingDefineSymbols(currentNameBuildTarget, definesList.ToArray());
    }

    public static T CreateManagerInScene<T>() where T : MonoBehaviour
    {
        var gm = CreateInScene();

        if (gm.gameObject.TryGetComponent(out T component))
        {
            return component;
        }

        return gm.gameObject.AddComponent<T>();
    }
}
#endif
