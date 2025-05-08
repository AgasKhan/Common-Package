#if UNITY_EDITOR

using UnityEditor.Callbacks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;


namespace EditorSave
{
    public static class SaveManager
    {
        private const string path = "SaveEditor.bin";

        private static Dictionary<string, object> s_keysAndValues = new();

        private static readonly IFormatter s_formatter = new BinaryFormatter();

        public static T Load<T>(string key)
        {
            if(s_keysAndValues.TryGetValue(key, out var value))
            {
                if (value is T child)
                    return child;
            }

            return default(T);
        }
        
        public static void Save<T>(string key, T value)
        {
            s_keysAndValues[key] = value;

            EditorApplication.delayCall += Save;
        }
        
        private static void Save()
        {
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            
            s_formatter.Serialize(stream, s_keysAndValues);
            
            stream.Close();
        }

        [InitializeOnLoadMethod]
        private  static void Load()
        {
            Stream readStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
            
            if(readStream.Length !=0)
                s_keysAndValues = (Dictionary<string, object>) s_formatter.Deserialize(readStream);
            
            readStream.Close();
        }
    }
}
#endif





