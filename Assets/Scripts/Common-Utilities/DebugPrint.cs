using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using System.Text;
using UnityEditor;

[InitializeOnLoad]
#endif
public static class DebugPrint
{
    #if UNITY_EDITOR
        struct PrintF
        {
            private const char newLine = '\n';
            
            private StringBuilder pantalla;

            private System.Action<object> print;
            
            public bool LenghtChk => pantalla.Length > 0;

            public PrintF(Action<object> print) : this()
            {
                pantalla = new StringBuilder();
                this.print = print;
            }

            public void Add(string palabra)
            {
                if (pantalla.Length != 0)
                {
                    pantalla.Append(newLine);
                    pantalla.Append(palabra);
                }
                else
                    pantalla.Append(palabra);
            }
            
            public void Print(ref StringBuilder str)
            {        
                if(!LenghtChk)
                    return;
                
                str.Append(pantalla);
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
        
        private static Dictionary<int, (DateTime date, string message)> _highlightEndTimes = new ();
        private static HashSet<Object> _toSelect = new();
        
        private static PrintF _debug = new(Debug.Log);
        private static PrintF _warning = new(Debug.LogWarning);
        private static PrintF _error = new(Debug.LogError);

        private static StringBuilder _stringBuilder = new();
        
        private static bool Chk => _debug.LenghtChk || _error.LenghtChk || _warning.LenghtChk;
        
        static DebugPrint()
        {
            EditorApplication.update -= UpdateHighlightTimes;
            EditorApplication.update += UpdateHighlightTimes;
            
            EditorApplication.hierarchyWindowItemOnGUI -= HierarchyHighlight_OnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyHighlight_OnGUI;
        }

        private static event EditorApplication.CallbackFunction ExecuteInNextFrame
        {
            add => EditorApplication.delayCall += value;
            remove => EditorApplication.delayCall -= value;
        }
        
        private static void HierarchyHighlight_OnGUI(int instanceID, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint) return;
                
            if (ShouldHighlight(instanceID, out string message))
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
            if (_toSelect.Count == 0)
                ExecuteInNextFrame += Select;

            _toSelect.Add(obj);
        }
        
        private static void Select()
        {
            var arraySelected = _toSelect.ToArray();

            Selection.objects = arraySelected;

            foreach (var selected in arraySelected)
            {
                EditorGUIUtility.PingObject(selected);
            }
            
            
            _toSelect.Clear();
        }
        
        private static void UpdateHighlightTimes()
        {
            DateTime now = DateTime.UtcNow;

            List<int> expiredIDs = new();
            foreach (var kvp in _highlightEndTimes)
            {
                if (kvp.Value.Item1 <= now)
                    expiredIDs.Add(kvp.Key);
            }

            foreach (var id in expiredIDs)
            {
                _highlightEndTimes.Remove(id);
            }
        }

        private static void AndSelect(object obj, Component component = null, float duration = 2f)
        {
            if (component != null)
            {
                int instanceID = component.gameObject.GetInstanceID();
                _highlightEndTimes[instanceID] = (DateTime.UtcNow.AddSeconds(duration), obj.ToString());  // Almacena el ID con 100 frames de duración
                
                Select(component.gameObject);
            }
        }
        
        private static void AndOpen(object obj, Component component = null, float duration = 2f)
        {
            if (component != null)
            {
                int instanceID = component.gameObject.GetInstanceID();
                _highlightEndTimes[instanceID] = (DateTime.UtcNow.AddSeconds(duration), obj.ToString());  // Almacena el ID con 100 frames de duración
                
                Select(component.gameObject);
                EditorUtility.OpenPropertyEditor(component.gameObject);
            }
        }

        private static void PrintCombinado()
        {
            _error.Print(ref _stringBuilder);
            _warning.Print(ref _stringBuilder);
            _debug.Print(ref _stringBuilder);
            
            Debug.Log(_stringBuilder);
            _stringBuilder.Clear();
        }

        private static bool ShouldHighlight(int instanceID, out string message)
        {
            var b = _highlightEndTimes.TryGetValue(instanceID, out var value);
            message = value.message;
            return b;
        }
    
    #endif

    #region Runtime
        struct LogRegistry
        {
            public string message;
            public string stackTrace;
            public LogType LogType;
        }
    
        private static bool _enableRegister;

        private static ConcurrentQueue<LogRegistry> _logRegistries = new();
        
        public static bool EnableRegister
        {
            get => _enableRegister;
            set
            {
                if(value==_enableRegister)
                    return;

                _enableRegister = value;

                if (_enableRegister)
                {
                    LogMessageReceivedThreaded += OnLogMessageReceivedThreaded;
                }
                else
                {
                    LogMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
                }
            }
        }
         
        /// <summary>
        /// Lo mismo que Application.logMessageReceivedThreaded
        /// </summary>
        public static event Application.LogCallback LogMessageReceivedThreaded
        {
            add
            {
                Application.logMessageReceivedThreaded += value;
            }
            remove
            {
                Application.logMessageReceivedThreaded -= value;
            }
        }
        
        private static void OnLogMessageReceivedThreaded(string logString, string stacktrace, LogType type)
        {
            _logRegistries.Enqueue(new LogRegistry(){message = logString, stackTrace = stacktrace, LogType = type});    
        }
    
        private static string FormatComponent(Component component, object t)
        {
            if(component!=null)
                return $"{t}\n\t-{component}: {component?.GetInstanceID()}";
            
            return t.ToString();
        }

        /// <summary>
        /// Funcion pensada para cuando se desea obtener el output de la consola
        /// <br/>
        /// Primero debe habilitarse el EnableRegistry para funcionar
        /// </summary>
        /// <param name="message"></param>
        /// <param name="stackTrace"></param>
        /// <param name="logType"></param>
        /// <returns>En caso que exista informacion que obtener retorna verdadero</returns>
        public static bool TryDequeueLogRegistry(out string message, out string stackTrace, out LogType logType)
        {
            var ret = _logRegistries.TryDequeue(out var result);

            message = result.message;
            stackTrace = result.stackTrace;
            logType = result.LogType;

            return ret;
        }
        
        /// <summary>
        /// Realiza el LogError y vincula el objeto al mensaje de consola
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="component"></param>
        /// <param name="duration"></param>
        public static void Error(this Component component, object obj)
        {
            #if DEBUG 
                Debug.LogError(FormatComponent(component, obj), component);
            #else
            if (EnableRegister)
                OnLogMessageReceivedThreaded(FormatComponent(component, obj), string.Empty, LogType.Error);
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
            #if DEBUG
                Debug.LogWarning(FormatComponent(component, obj), component);
            #else
            if (EnableRegister)
                OnLogMessageReceivedThreaded(FormatComponent(component, obj), string.Empty, LogType.Warning);
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
            #if DEBUG
                Debug.Log(FormatComponent(component, obj), component);
            #else
             if (EnableRegister)
                OnLogMessageReceivedThreaded(FormatComponent(component, obj), string.Empty, LogType.Log);
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
            Log(obj, component);
        }
        

        /// <summary>
        /// Realiza el log y selecciona el objeto
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="component"></param>
        /// <param name="duration"></param>
        public static void LogAndSelect(object obj, Component component = null, float duration = 2f)
        {
            #if UNITY_EDITOR
                AndSelect(obj, component, duration);
            #endif
            
            Log(obj, component);
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
            #if UNITY_EDITOR
                AndOpen(obj, component, duration);
            #endif
            
            Log(obj, component);
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
        public static void ConsecutiveLog(object obj, Component component = null)
        {
                #if UNITY_EDITOR
                    if (!Chk)
                        ExecuteInNextFrame += PrintCombinado;
                    
                    _debug.Add(FormatComponent(component, obj));
                #else
                    Log(obj, component);
                #endif
        }
        
        /// <summary>
        /// Logs consecutivos, que se acumulan por frame
        /// </summary>
        /// <param name="component"></param>
        /// <param name="t"></param>
        public static void ConsecutiveLog(this Component component, object t)
        {
            ConsecutiveLog(t, component);
        }

        /// <summary>
        /// Logs consecutivos, que se acumulan por frame
        /// </summary>
        /// <param name="component"></param>
        /// <param name="t"></param>
        public static void ConsecutiveWarning(object obj, Component component = null)
        {        
            #if DEBUG
                #if UNITY_EDITOR
                    if (!Chk)
                        ExecuteInNextFrame += PrintCombinado;
                    
                    _warning.Add($"<color=yellow>{FormatComponent(component, obj)}</color>");
                #else
                    Warning(component, obj);
                #endif
            #endif
        }
        
        /// <summary>
        /// Logs consecutivos, que se acumulan por frame
        /// </summary>
        /// <param name="component"></param>
        /// <param name="t"></param>
        public static void ConsecutiveWarning(this Component component, object obj)
        {        
            ConsecutiveWarning(obj, component);
        }

        
        /// <summary>
        /// Logs consecutivos, que se acumulan por frame
        /// </summary>
        /// <param name="component"></param>
        /// <param name="t"></param>
        public static void ConsecutiveError(object obj, Component component = null)
        {
            #if DEBUG
                #if UNITY_EDITOR
                    if (!Chk)
                        ExecuteInNextFrame += PrintCombinado;
                    
                    _error.Add($"<color=red>{FormatComponent(component, obj)}</color>");
                #else
                    Error(component, obj);
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
            ConsecutiveError(t, component);
        }
    #endregion
}
