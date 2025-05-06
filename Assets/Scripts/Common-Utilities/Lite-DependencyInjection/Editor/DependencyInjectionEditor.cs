using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(DefaultDependencyAttribute), true)]
public class DependencyInjectionEditor : PropertyDrawer
{
    private static HashSet<Object> objectToBind = new();

    private static bool inQueue;
    
    private void DelayCall()
    {
        foreach (var obj in objectToBind)
        {
            string sceneName = null;
            
            if (obj is Component go)
                sceneName = go.gameObject.scene.name;
            
            BrainDependencyInjection.ApplyBindingsToObject(obj, sceneName);
        }
        
        objectToBind.Clear();
        
        inQueue = false;
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        objectToBind.Add(property.serializedObject.targetObject);

        if (!inQueue)
        {
            EditorApplication.delayCall += DelayCall;
            inQueue = true;
        }
        
        property.serializedObject.ApplyModifiedProperties();

        EditorGUI.PropertyField(position, property, label);
    }
}
