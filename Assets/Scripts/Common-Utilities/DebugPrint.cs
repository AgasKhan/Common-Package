//Comentar para desactivar el testeo
#define TESTING
//////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using System.Text;
using UnityEditor;

[UnityEditor.InitializeOnLoad]
#endif
public static class DebugPrint
{
    #if UNITY_EDITOR
    
        static DebugPrint()
        {
            UnityEditor.EditorApplication.update -= UpdateHighlightTimes;
            UnityEditor.EditorApplication.update += UpdateHighlightTimes;
            
            EditorApplication.hierarchyWindowItemOnGUI -= HierarchyHighlight_OnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyHighlight_OnGUI;
        }
        
        private static Dictionary<int, (System.DateTime date, string message)> highlightEndTimes = new ();
        private static HashSet<Object> toSelect = new();
        
        static PrintF debug = new PrintF(Debug.Log);
        static PrintF warning = new PrintF(Debug.LogWarning);
        static PrintF error = new PrintF(Debug.LogError);
        
        private static bool Chk => debug.LenghtChk || error.LenghtChk || warning.LenghtChk;

        static event UnityEditor.EditorApplication.CallbackFunction ExecuteInNextFrame
        {
            add
            {
                UnityEditor.EditorApplication.delayCall += value;
            }
            remove
            {
                UnityEditor.EditorApplication.delayCall -= value;
            }
        }
        
        private static void HierarchyHighlight_OnGUI(int instanceID, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint) return;
                
            if (DebugPrint.ShouldHighlight(instanceID, out string message))
            {
                Color BKCol = Color.green;
                Color TextCol = Color.red;
                Rect BackgroundOffset = new Rect(selectionRect.position, selectionRect.size);
        
                EditorGUI.DrawRect(BackgroundOffset, BKCol);

                EditorGUI.LabelField(new Rect(selectionRect.position + new Vector2(2f, 0f), selectionRect.size), 
                    message.Replace("\n", " - "), 
                    new GUIStyle { normal = new GUIStyleState() { textColor = TextCol } });

                EditorApplication.RepaintHierarchyWindow();
            }
        }
        
        private static void Select(Object obj)
        {
            if (toSelect.Count == 0)
                ExecuteInNextFrame += Select;

            toSelect.Add(obj);
        }
        
        private static void Select()
        {
            var arraySelected = toSelect.ToArray();

            UnityEditor.Selection.objects = arraySelected;

            foreach (var selected in arraySelected)
            {
                UnityEditor.EditorGUIUtility.PingObject(selected);
            }
            
            
            toSelect.Clear();
        }
        
        private static void UpdateHighlightTimes()
        {
            System.DateTime now = System.DateTime.UtcNow;

            List<int> expiredIDs = new();
            foreach (var kvp in highlightEndTimes)
            {
                if (kvp.Value.Item1 <= now)
                    expiredIDs.Add(kvp.Key);
            }

            foreach (var id in expiredIDs)
            {
                highlightEndTimes.Remove(id);
            }
        }

        static void AndSelect(object obj, Component component = null, float duration = 2f)
        {
            if (component != null)
            {
                int instanceID = component.gameObject.GetInstanceID();
                highlightEndTimes[instanceID] = (System.DateTime.UtcNow.AddSeconds(duration), obj.ToString());  // Almacena el ID con 100 frames de duración
                
                Select(component.gameObject);
            }
        }
        
        static void AndOpen(object obj, Component component = null, float duration = 2f)
        {
            if (component != null)
            {
                int instanceID = component.gameObject.GetInstanceID();
                highlightEndTimes[instanceID] = (System.DateTime.UtcNow.AddSeconds(duration), obj.ToString());  // Almacena el ID con 100 frames de duración
                
                Select(component.gameObject);
                UnityEditor.EditorUtility.OpenPropertyEditor(component.gameObject);
            }
        }

        private static void PrintCombinado()
        {
            string str = string.Empty;
            
            error.Print(ref str);
            warning.Print(ref str);
            debug.Print(ref str);
            
            Debug.Log(str);
        }

        public static bool ShouldHighlight(int instanceID, out string message)
        {
            var b = highlightEndTimes.TryGetValue(instanceID, out var value);
            message = value.message;
            return b;
        }
    
    #endif
    

    #region No Editor

        static string FormatComponent(Component component, object t)
        {
            #if TESTING
                return $"{t}\n\t-{component}: {component?.GetInstanceID()}";
            #else
                return string.Empty;
            #endif
        }
        
        /// <summary>
        /// Realiza el LogError y vincula el objeto al mensaje de consola
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="component"></param>
        /// <param name="duration"></param>
        public static void Error(this Component component, object obj)
        {
            #if TESTING
                Debug.LogError(FormatComponent(component, obj), component);
            #endif        
        }
        
        /// <summary>
        /// Realiza el LogWarning y vincula el objeto al mensaje de consola
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="component"></param>
        /// <param name="duration"></param>
        public static void Warning(this Component component, object obj)
        {
            #if TESTING
                Debug.LogWarning(FormatComponent(component, obj), component);
            #endif     
        }
        
        /// <summary>
        /// Realiza el Log y vincula el objeto al mensaje de consola
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="component"></param>
        /// <param name="duration"></param>
        public static void Log(object obj, Component component = null)
        {
            #if TESTING
                Debug.Log(FormatComponent(component, obj), component);
            #endif     
        }
        
        /// <summary>
        /// Realiza el Log y vincula el objeto al mensaje de consola
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="component"></param>
        /// <param name="duration"></param>
        public static void Log(this Component component, object obj)
        {
            #if TESTING
                Log(obj, component);
            #endif
        }
        

        /// <summary>
        /// Realiza el log y selecciona el objeto
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="component"></param>
        /// <param name="duration"></param>
        public static void LogAndSelect(object obj, Component component = null, float duration = 2f)
        {
            #if TESTING
                #if UNITY_EDITOR
                    AndSelect(obj, component, duration);
                #endif
                
                Log(obj, component);
            #endif
        }
        
        /// <summary>
        /// Realiza el Log y vincula el objeto al mensaje de consola ademas selecciona el objeto
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="component"></param>
        /// <param name="duration"></param>
        public static void LogAndSelect(this Component component, object obj,  float duration = 2f)
        {
            LogAndSelect(FormatComponent(component, obj),component ,duration);
        }
        
        
        
        /// <summary>
        /// Realiza el log y selecciona y abre las propiedades del objeto
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="component"></param>
        /// <param name="duration"></param>
        public static void LogAndOpen(object obj, Component component = null, float duration = 2f)
        {
            #if TESTING
            
                #if UNITY_EDITOR
                    AndOpen(obj, component, duration);
                #endif
                
                Log(obj, component);
            
            #endif
        }
        
        /// <summary>
        /// Realiza el Log y vincula el objeto al mensaje de consola ademas selecciona y abre el objeto
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="component"></param>
        /// <param name="duration"></param>

        public static void LogAndOpen(this Component component, object obj, float duration = 2f)
        {
            LogAndOpen(FormatComponent(component, obj), component, duration);
        }
        
        
        
        /// <summary>
        /// Logs consecutivos, que se acumulan por frame
        /// </summary>
        /// <param name="component"></param>
        /// <param name="t"></param>
        public static void ConsecutiveLog(object t)
        {
            #if TESTING
                #if UNITY_EDITOR
                    if (!Chk)
                        ExecuteInNextFrame += PrintCombinado;
                    
                    debug.Add(t.ToString());
                #else
                    Debug.Log(t);
                #endif
            #endif
        }
        
        /// <summary>
        /// Logs consecutivos, que se acumulan por frame
        /// </summary>
        /// <param name="component"></param>
        /// <param name="t"></param>
        public static void ConsecutiveLog(this Component component, object t)
        {
            ConsecutiveLog(FormatComponent(component, t));
        }
        
        

        /// <summary>
        /// Logs consecutivos, que se acumulan por frame
        /// </summary>
        /// <param name="component"></param>
        /// <param name="t"></param>
        public static void ConsecutiveWarning(object t)
        {        
            #if TESTING
                #if UNITY_EDITOR
                    if (!Chk)
                        ExecuteInNextFrame += PrintCombinado;
                    
                    warning.Add($"<color=yellow>{t.ToString()}</color>");
                #else
                    Debug.LogWarning(t);
                #endif
            #endif
        }
        
        /// <summary>
        /// Logs consecutivos, que se acumulan por frame
        /// </summary>
        /// <param name="component"></param>
        /// <param name="t"></param>
        public static void ConsecutiveWarning(this Component component, object t)
        {        
            ConsecutiveWarning(FormatComponent(component, t));
        }

        
        /// <summary>
        /// Logs consecutivos, que se acumulan por frame
        /// </summary>
        /// <param name="component"></param>
        /// <param name="t"></param>
        public static void ConsecutiveError(object t)
        {
            #if TESTING
                #if UNITY_EDITOR
                    if (!Chk)
                        ExecuteInNextFrame += PrintCombinado;
                    
                    error.Add("<color=red>" + t.ToString() + "</color>");
                #else
                    Debug.LogError(t);
                #endif
            #endif
        }
        
        /// <summary>
        /// Logs consecutivos, que se acumulan por frame
        /// </summary>
        /// <param name="component"></param>
        /// <param name="t"></param>
        public static void ConsecutiveError(this Component component, object t)
        {
            ConsecutiveError(FormatComponent(component, t));
        }

    #endregion
}

#if UNITY_EDITOR
    struct PrintF
    {
        private StringBuilder pantalla;

        private System.Action<object> print;
        
        public bool LenghtChk => pantalla.Length > 0 ? true : false;

        public PrintF(Action<object> print) : this()
        {
            pantalla = new StringBuilder();
            this.print = print;
        }

        public void Add(string palabra)
        {
            if (pantalla.Length!=0)
                pantalla.Append("\n" + palabra);
            else
                pantalla.Append(palabra);
        }
        
        public void Print(ref string str)
        {        
            if(!LenghtChk)
                return;
            
            str += pantalla;
            Clear();
        }

        public void Print()
        {
            if(!LenghtChk)
                return;
            
            print.Invoke(pantalla);
            Clear();
        }

        public void Clear()
        {
            pantalla.Clear();
        }
    }
#endif