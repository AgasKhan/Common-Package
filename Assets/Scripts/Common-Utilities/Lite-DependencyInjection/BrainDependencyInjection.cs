using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Callbacks;
using System.Reflection;

public class BrainDependencyInjection : AssetPostprocessor
{
    private const string path = "Assets/Resources/Lite-DependencyInjection/";

    private static DependencyInjectionScriptable scriptableData;
    
    [DidReloadScripts]
    static void Init()
    {
        scriptableData = Resources.Load<DependencyInjectionScriptable>("Lite-DependencyInjection/DependencyInjectionData");
        
        if (scriptableData == null)
        {
            scriptableData = ScriptableObject.CreateInstance<DependencyInjectionScriptable>();
            
            AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "Lite-DependencyInjection");
            AssetDatabase.CreateAsset(scriptableData, path + "/DependencyInjectionData.asset");    
        }
        
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        EditorSceneManager.sceneSaving += OnSceneSaving;
        
        EditorSceneManager.sceneClosing -= OnSceneClosing;
        EditorSceneManager.sceneClosing += OnSceneClosing;
        
        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes().Where(t => t.IsClass || t.IsValueType));

        var membersWithDependency = allTypes
            .SelectMany(type => type.GetMembersWithAttribute<DefaultDependencyAttribute>());

        var withDependency = membersWithDependency as MemberInfo[] ?? membersWithDependency.ToArray();
        
        var fieldsType = withDependency
            .ToFieldInfos()
            .Select(info => info.FieldType);
       
       var propertyTypes = withDependency
            .ToPropertyInfos()
            .Select(info => info.PropertyType);

       StringBuilder fieldsBuilder = new StringBuilder();
       
       StringBuilder concatBuilder = new StringBuilder();
       
       foreach (var type in fieldsType.Concat(propertyTypes).Distinct())
       {
            fieldsBuilder.Append("\n\t");
            fieldsBuilder.Append("\n\t");
            fieldsBuilder.Append("[SerializeField]");
            fieldsBuilder.Append("\n\t");
            fieldsBuilder.Append("public KeyValuePairArray<");
            fieldsBuilder.Append(type.FullName);
            fieldsBuilder.Append("> ");
            fieldsBuilder.Append(type.Name.ToUpper());
            fieldsBuilder.Append(';');

            if (concatBuilder.Length == 0)
            {
                //public IEnumerable<KeyValuePair> All => ExampleGO.Concat(etc);
                concatBuilder.Append("\tpublic IEnumerable<KeyValuePair> All => ");
                concatBuilder.Append(type.Name.ToUpper());
            }
            else
            {
                concatBuilder.Append(".Concat(");
                concatBuilder.Append(type.Name.ToUpper());
                concatBuilder.Append(')');
            }
                
           
            DebugPrint.ConsecutiveLog(type.Name);
       }

       concatBuilder.Append(';');
       
       
       File.WriteAllText(path + "DependencyInjectData.cs", $"using UnityEngine;\nusing System.Linq;\nusing System.Collections;\nusing System.Collections.Generic;\n" +
                                        $"public partial class DependencyInjectData\n{{\n{concatBuilder}\n\n{fieldsBuilder}\n}}\n\n");
       
       scriptableData.OnValidate();
    }

    private static void OnSceneSaving(Scene scene, string s)
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            ApplyBindings(go, scene.name);
        }
    }
    
    private static void OnSceneClosing(Scene scene, bool removingscene)
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            ApplyBindings(go, scene.name);
        }
    }

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if(scriptableData == null)
            return;

        foreach (var path in importedAssets)
        {
            Object assetLoaded = AssetDatabase.LoadAssetAtPath(path, typeof(Object));

            if (scriptableData == null)
                return;

            if(assetLoaded is GameObject gameObject)
            {
                ApplyBindings(gameObject.transform, null);
            }
            else
            {
                ApplyBindingsToObject(assetLoaded, null);
            }
        
            EditorUtility.SetDirty(assetLoaded);
        }
    }

    private static DependencyInjectData.KeyValuePairArray GetValuesOfType(Type type)
    {
        var fieldInjector = typeof(DependencyInjectData).GetField(type.Name.ToUpper());

        return (DependencyInjectData.KeyValuePairArray)fieldInjector.GetValue(scriptableData.data);
    }

    public static void SetValueOfMember(MemberInfo memberInfo, object original, string sceneName)
    {
        var dependency = (DefaultDependencyAttribute)memberInfo.GetCustomAttribute(typeof(DefaultDependencyAttribute), true);
            
        memberInfo.SetValue(original,  GetValuesOfType(memberInfo.GetMemberType())[dependency.index, sceneName]);
    }
    
    public static void SetValueOfMembers(IEnumerable<MemberInfo> members, object original, string sceneName)
    {
        if(members==null)
            return;
        
        foreach (var member in members)
        {
            SetValueOfMember(member, original, sceneName);
        }
    }

    public static void ApplyBindingsToObject(object obj, string sceneName)
    {
        if(obj==null)
            return;
        
        var members = obj.GetType().GetMembersWithAttribute<DefaultDependencyAttribute>();

        SetValueOfMembers(members, obj, sceneName);

        foreach (var member in obj.GetType().GetSerializableMembers())
        {
            var memberType = member.GetMemberType();
            
            if(typeof(Object).IsAssignableFrom(memberType) || memberType.IsArray || memberType.IsPrimitive)
                continue;
            
            ApplyBindingsToObject(member.GetValue(obj), sceneName);
        }
    }
    
    private static void ApplyBindings(Transform transform, string sceneName)
    {
        foreach (var obj in transform.GetComponents<Component>())
        {
            ApplyBindingsToObject(obj, sceneName);
        }
        
        foreach (Transform child in transform)
        {
            ApplyBindings(child, sceneName);
        }
    }

    private static void ApplyBindings(GameObject assetLoaded, string sceneName)
    {
        foreach (var obj in assetLoaded.GetComponents<Component>())
        {
            ApplyBindingsToObject(obj, sceneName);
        }
    }
}
#endif

[System.Serializable]
public partial class DependencyInjectData
{
    [System.Serializable]
    public struct SceneAndValue<TValue>
    {
        public SceneAsset sceneAsset;
        public TValue value;
    }
    
    [System.Serializable]
    public abstract class KeyValuePair
    {
        [HideInInspector]
        public int indexInArray;
        public string name;
        public abstract Type type { get; }
        public abstract object Value { get; }
        
        public abstract object this[string sceneName] { get; }
    }
    
    [System.Serializable]
    public class KeyValuePair<TValue> : KeyValuePair
    {
        public SceneAndValue<TValue>[] scenesAndValues = Array.Empty<SceneAndValue<TValue>>();
        
        public override Type type => typeof(TValue);

        public override object Value => this[string.Empty];

        public override object this[string sceneName]
        {
            get
            {
                if (string.IsNullOrEmpty(sceneName))
                {
                    if(scenesAndValues.Length > 0)
                        return scenesAndValues[0].value;
                    
                    return default(TValue);
                }
                
                foreach (var sceneAndValue in scenesAndValues)
                {
                    if(sceneAndValue.sceneAsset == null)
                        continue;
                    
                    if (sceneAndValue.sceneAsset.name == sceneName)
                    {
                        return sceneAndValue.value;
                    }
                }
                
                if(scenesAndValues.Length>0)
                    return scenesAndValues[0].value;
                
                return default(TValue);
            }   
        }
    }

    public abstract class KeyValuePairArray
    {
        public abstract object this[int index, string sceneName] { get; }
    }

    [System.Serializable]
    public class KeyValuePairArray<TValue> : KeyValuePairArray, ISerializationCallbackReceiver, IEnumerable<KeyValuePair>
    {
        public KeyValuePair<TValue>[] elements = Array.Empty<KeyValuePair<TValue>>();

        public int Length => elements.Length;

        public override object this[int index, string sceneName]
        {
            get
            {
                index = Mathf.Clamp(index, 0, Length - 1);

                if (index < 0)
                    return default;

                return elements[index][sceneName];
            }
        }


        public void OnBeforeSerialize()
        {
            for (int i = 0; i < elements.Length; i++)
            {
                elements[i].indexInArray = i;
            }
        }
        
        public void OnAfterDeserialize()
        {
        }

        public IEnumerator<KeyValuePair> GetEnumerator()
        {
            for (int i = 0; i < elements.Length; i++)
            {
                yield return elements[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public partial class DefaultDependencyAttribute : PropertyAttribute
{
    public int index;
    
    public DefaultDependencyAttribute()
    {
    }
    
    public DefaultDependencyAttribute(int index)
    {
        this.index = index;
    }
}




