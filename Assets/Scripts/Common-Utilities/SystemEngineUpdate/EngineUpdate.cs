using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using FixedUpdate = UnityEngine.PlayerLoop.FixedUpdate;
using Update = UnityEngine.PlayerLoop.Update;

namespace SystemEngineUpdate
{
     public static class EngineUpdate
     {
          class InternalUpdate<TLoopSystem>
          {
               protected PlayerLoopSystem playerLoopSystem;
               
               protected System.Action loopList;
               public InternalUpdate(ref PlayerLoopSystem currentPlayerLoop, int index) : base()
               {
                    playerLoopSystem = new PlayerLoopSystem()
                    {
                         type = typeof(InternalUpdate<TLoopSystem>),
                         updateDelegate = Loop
                    };
                    
                    if (!InsertSystem<TLoopSystem>(ref currentPlayerLoop, playerLoopSystem, index)) 
                    {
                         Debug.LogWarning("Unable to register into the loop.");
                         return;
                    }
                    
                    PlayerLoop.SetPlayerLoop(currentPlayerLoop);
                    
     #if UNITY_EDITOR
                    onQuitInEditor += OnPlayModeState;
                 
                    void OnPlayModeState() 
                    {
                         PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
                              
                         RemoveSystem(ref currentPlayerLoop, playerLoopSystem);

                         PlayerLoop.SetPlayerLoop(currentPlayerLoop);
                    }
     #endif
               }

               public void AddLoop(System.Action loop)
               {
                    loopList += loop;
               }

               public void RemoveLoop(System.Action loop)
               {
                    loopList -= loop;
               }
               
               void Loop()
               {
                    loopList?.Invoke();
               }
          }

          class OnGuiHandler : MonoBehaviour
          {
               public System.Action onGUI;
               
               public System.Action onDrawGizmos;
               
               private void OnGUI()
               {
                    onGUI.Invoke();
               }
               
               private void OnDrawGizmos()
               {
                    onDrawGizmos.Invoke();
               }

               private void Awake()
               {
                    DontDestroyOnLoad(gameObject);
               }
          }
          
          public static bool BelowUltraFrameRate => framesPerSecond.BelowUltraFrameRate;
          public static bool BelowHightFrameRate => framesPerSecond.BelowHightFrameRate;
          public static bool BelowMediumFrameRate => framesPerSecond.BelowMediumFrameRate;
          public static bool BelowLowFrameRate => framesPerSecond.BelowLowFrameRate;
          public static bool BelowVeryLowFrameRate => framesPerSecond.BelowVeryLowFrameRate;
          public static bool BelowWatchDogFrameRate => framesPerSecond.BelowWatchDogFrameRate;

          #region AuxiliarUpdates

          public static event System.Action startUpdate
          {
               add => inInitialization.AddLoop(value);
               remove => inInitialization.RemoveLoop(value);
          }
          
          public static event System.Action endUpdate
          {
               add => inEndUpdate.AddLoop(value);
               remove => inEndUpdate.RemoveLoop(value);
          }

          #endregion
          
          #region Update
          
          public static event System.Action preUpdate
          {
               add => inPreUpdate.AddLoop(value);
               remove => inPreUpdate.RemoveLoop(value);
          }

          public static event System.Action update
          {
               add => inUpdate.AddLoop(value);
               remove => inUpdate.RemoveLoop(value);
          }
          
          public static event System.Action postUpdate
          {
               add => inPostUpdate.AddLoop(value);
               remove => inPostUpdate.RemoveLoop(value);
          }

          #endregion

          #region FixedUpdate
          
          public static event System.Action preFixedUpdate
          {
               add => inPreFixedUpdate.AddLoop(value);
               remove => inPreFixedUpdate.RemoveLoop(value);
          }
          
          public static event System.Action fixedUpdate
          {
               add => inFixedUpdate.AddLoop(value);
               remove => inFixedUpdate.RemoveLoop(value);
          }
          
          public static event System.Action PostFixedUpdate
          {
               add => inPostFixedUpdate.AddLoop(value);
               remove => inPostFixedUpdate.RemoveLoop(value);
          }
          
          #endregion
          
          #region LateUpdate
          
          public static event System.Action preLateUpdate
          {
               add => inPreLateUpdate.AddLoop(value);
               remove => inPreLateUpdate.RemoveLoop(value);
          }
          
          public static event System.Action lateUpdate
          {
               add => inLateUpdate.AddLoop(value);
               remove => inLateUpdate.RemoveLoop(value);
          }
          
          public static event System.Action postLateUpdate
          {
               add => inPostLateUpdate.AddLoop(value);
               remove => inPostLateUpdate.RemoveLoop(value);
          }
          
          #endregion
          
          public static event System.Action onQuit;
          public static event System.Action onGUI;
          public static event System.Action onDrawGizmos;
          
          #if UNITY_EDITOR
          public static event System.Action onQuitInEditor;
          #endif
          
          private static PlayerLoopSystem playerLoopSystem;

          private static InternalUpdate<Initialization> inInitialization;
          
          private static InternalUpdate<Update> inPreUpdate;
          
          private static InternalUpdate<Update.ScriptRunBehaviourUpdate> inUpdate;
          
          private static InternalUpdate<Update> inPostUpdate;
          
          private static InternalUpdate<FixedUpdate> inPreFixedUpdate;
          
          private static InternalUpdate<FixedUpdate.ScriptRunBehaviourFixedUpdate> inFixedUpdate;
          
          private static InternalUpdate<FixedUpdate> inPostFixedUpdate;
          
          private static InternalUpdate<PreLateUpdate> inPreLateUpdate;
          
          private static InternalUpdate<PreLateUpdate.ScriptRunBehaviourLateUpdate> inLateUpdate;
          
          private static InternalUpdate<PreLateUpdate> inPostLateUpdate;
          
          private static InternalUpdate<PostLateUpdate> inEndUpdate;
          
          private static FPSCounter framesPerSecond;

          [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
          static void InitAfterAssemblies()
          {
               StringBuilder stringBuilder = new StringBuilder();
               
               playerLoopSystem = PlayerLoop.GetCurrentPlayerLoop();
               
               stringBuilder.AppendLine("UnityPlayer loop");

               foreach (var subSystem in playerLoopSystem.subSystemList)
               {
                    PrintSubsystem(subSystem, stringBuilder, 0);
               }
               
               Debug.Log(stringBuilder.ToString());

               InitMyUpdatesWrappers();
               
               Application.quitting -= ApplicationOnquitting;
               Application.quitting += ApplicationOnquitting;
               
               stringBuilder.Clear();
               
               stringBuilder.AppendLine("Post UnityPlayer loop");

               foreach (var subSystem in playerLoopSystem.subSystemList)
               {
                    PrintSubsystem(subSystem, stringBuilder, 0);
               }
               
               Debug.Log(stringBuilder.ToString());
               
     #if UNITY_EDITOR
               UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeState;
               UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeState;
                 
               void OnPlayModeState(UnityEditor.PlayModeStateChange state) 
               {
                    if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) 
                    {
                         onQuitInEditor?.Invoke();
                         ApplicationOnquitting();
                    }
               }
     #endif
          }

          [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
          static void InitAfterLoad()
          {
               InitMySystems();
               var go = new GameObject("OnGuiHandlerGO");
               var guiHandler = go.AddComponent<OnGuiHandler>();
               guiHandler.onGUI = OnGUI;
               guiHandler.onDrawGizmos = OnDrawGizmos;
          }
          static void InitMyUpdatesWrappers()
          {
               inInitialization = new(ref playerLoopSystem, 0);
               
               
               inPreUpdate = new (ref playerLoopSystem, 0);

               inUpdate = new(ref playerLoopSystem, 0);
               
               inPostUpdate = new (ref playerLoopSystem, 2);
               
               
               inPreFixedUpdate = new (ref playerLoopSystem,  4);

               inFixedUpdate = new(ref playerLoopSystem, 0);
               
               inPostFixedUpdate = new (ref playerLoopSystem,  6);
               
               
               inPreLateUpdate = new(ref playerLoopSystem,  -2);

               inLateUpdate = new(ref playerLoopSystem, 0);
               
               inPostLateUpdate = new(ref playerLoopSystem, -1);
               
               
               inEndUpdate = new (ref playerLoopSystem, -1);
               
               /*

               inInitialization.AddLoop(()=>Debug.Log("Initialization loop"));

               inPreUpdate.AddLoop(()=>Debug.Log("PreUpdate loop"));

               inPostUpdate.AddLoop(()=>Debug.Log("PostUpdate loop"));

               inPreLateUpdate.AddLoop(()=>Debug.Log("PreLateUpdate loop"));

               inPostLateUpdate.AddLoop(()=>Debug.Log("PostLateUpdate loop"));

               inPreFixedUpdate.AddLoop(()=>Debug.Log("PreFixedUpdate loop"));

               inPostFixedUpdate.AddLoop(()=>Debug.Log("PostFixedUpdate loop"));

               inEndUpdate.AddLoop(()=>Debug.Log("End loop"));

               */
          }
          
          static void InitMySystems()
          {
               var childs = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(types =>typeof(MySystem).IsAssignableFrom(types) && !types.IsAbstract);

               foreach (var child in childs)
               {
                    Activator.CreateInstance(child);
               }

               framesPerSecond = FPSCounterSystem.FPSCounter;
          }

          private static void OnDrawGizmos()
          {
               onDrawGizmos?.Invoke();
          }

          private static void OnGUI()
          {
               onGUI?.Invoke();
          }

          private static void ApplicationOnquitting()
          {
               onQuit?.Invoke();
          }

          // Remove a system from the player loop
          public static void RemoveSystem(ref PlayerLoopSystem loopSystem, in PlayerLoopSystem systemToRemove) 
          {
               if (loopSystem.subSystemList == null) 
                    return;
                 
               var playerLoopSystemList = new List<PlayerLoopSystem>(loopSystem.subSystemList);
               
               for (int i = 0; i < playerLoopSystemList.Count; ++i) 
               {
                    if (playerLoopSystemList[i].type == systemToRemove.type && playerLoopSystemList[i].updateDelegate == systemToRemove.updateDelegate) 
                    {
                         playerLoopSystemList.RemoveAt(i);
                         loopSystem.subSystemList = playerLoopSystemList.ToArray();
                         return;
                    }
               }
                 
               HandleSubSystemLoopForRemoval(ref loopSystem, systemToRemove);
          }

          static void HandleSubSystemLoopForRemoval(ref PlayerLoopSystem loopSystem, PlayerLoopSystem systemToRemove) 
          {
               if (loopSystem.subSystemList == null) 
                    return;

               for (int i = 0; i < loopSystem.subSystemList.Length; ++i) 
               {
                    RemoveSystem(ref loopSystem.subSystemList[i], systemToRemove);
               }
          }

          
          /*
          public static bool InsertSystemBeforeSubSystem<T1, T2>(ref PlayerLoopSystem loopSystem,
               PlayerLoopSystem systemToInsert)
          {
               if (loopSystem.type != typeof(T1)) 
                    return HandleSubSystemLoop<T1>(ref loopSystem, systemToInsert, index);
               
               if (loopSystem.subSystemList != null)
                    for (int i = 0; i < loopSystem.subSystemList.Length; ++i)
                    {
                         if (loopSystem.subSystemList[i].type == typeof(T2))
                         {
                              InsertSystem<T1>(ref loopSystem, systemToInsert, i+1);
                              return true;
                         }
                    }
               
               return false;
          }
          */
          
          // Insert a system into the player loop
          public static bool InsertSystem<T>(ref PlayerLoopSystem loopSystem, in PlayerLoopSystem systemToInsert, int index) 
          {
               if (loopSystem.type != typeof(T)) 
                    return HandleSubSystemLoop<T>(ref loopSystem, systemToInsert, index);
                 
               var playerLoopSystemList = new List<PlayerLoopSystem>();
               
               if (loopSystem.subSystemList != null) 
                    playerLoopSystemList.AddRange(loopSystem.subSystemList);
               
               if(index<0)
                    index = playerLoopSystemList.Count + index + 1;
               
               playerLoopSystemList.Insert(index, systemToInsert);
               
               loopSystem.subSystemList = playerLoopSystemList.ToArray();
               
               return true;
          }
          
          static bool HandleSubSystemLoop<T>(ref PlayerLoopSystem loopSystem, in PlayerLoopSystem systemToInsert, int index) 
          {
               if (loopSystem.subSystemList == null) 
                    return false;

               for (int i = 0; i < loopSystem.subSystemList.Length; ++i) 
               {
                    if (!InsertSystem<T>(ref loopSystem.subSystemList[i], in systemToInsert, index)) 
                         continue;
                    
                    return true;
               }
                 
               return false;
          }

          static void PrintSubsystem(PlayerLoopSystem loopSystem, StringBuilder stringBuilder, int level) 
          {
               stringBuilder.Append('\t', level * 2).AppendLine(loopSystem.type.ToString());
               
               if (loopSystem.subSystemList == null || loopSystem.subSystemList.Length == 0) 
                    return;

               foreach (PlayerLoopSystem subSystem in loopSystem.subSystemList) 
               {
                    PrintSubsystem(subSystem, stringBuilder, level + 1);
               }
          }
     }

     public abstract class MySystem
     {
     }

     public abstract class MySystem<TChild> : MySystem where TChild : MySystem<TChild>, new()
     {
          protected static TChild instance;
          
          protected MySystem()
          {
               instance = (TChild)this;
               
               Debug.Log("Init System " + instance.GetType().Name);
               
               if (instance is IStartUpdate startUpdate)
                    EngineUpdate.startUpdate += startUpdate.StartUpdate;
               
               if (instance is IEndUpdate endUpdate)
                    EngineUpdate.endUpdate += endUpdate.EndUpdate;
               
               if (instance is IPreUpdate preUpdate)
                    EngineUpdate.preUpdate += preUpdate.PreUpdate;
               
               if (instance is IUpdate update)
                    EngineUpdate.update += update.MyUpdate;
               
               if (instance is IPostUpdate postUpdate)
                    EngineUpdate.postUpdate += postUpdate.PostUpdate;
               
               if(instance is IPreLateUpdate preLateUpdate)
                    EngineUpdate.preLateUpdate+=preLateUpdate.PreLateUpdate;
               
               if (instance is ILateUpdate lateUpdate)
                    EngineUpdate.lateUpdate += lateUpdate.MyLateUpdate;
               
               if(instance is IPostLateUpdate postLateUpdate)
                    EngineUpdate.postLateUpdate += postLateUpdate.PostLateUpdate;
               
               if(instance is IPreFixedUpdate preFixedUpdate)
                    EngineUpdate.preFixedUpdate += preFixedUpdate.PreFixedUpdate;
               
               if (instance is IFixedUpdate fixedUpdate)
                    EngineUpdate.fixedUpdate += fixedUpdate.MyFixedUpdate;
               
               if(instance is IPostFixedUpdate postFixedUpdate)
                    EngineUpdate.preFixedUpdate += postFixedUpdate.PostFixedUpdate;

               if (instance is ILoadScene loadScene)
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded += loadScene.OnLoadScene;
               
               if (instance is IUnloadScene unloadScene)
                    UnityEngine.SceneManagement.SceneManager.sceneUnloaded += unloadScene.OnUnloadScene;
               
               if (instance is IActiveSceneChange activeSceneChange)
                    UnityEngine.SceneManagement.SceneManager.activeSceneChanged += activeSceneChange.OnActiveSceneChange;
               
               if (instance is IOnGUI onGUI)
                    EngineUpdate.onGUI += onGUI.OnGUI;

               if (instance is IOnDrawGizmos onDrawGizmos)
                    EngineUpdate.onDrawGizmos += onDrawGizmos.OnDrawGizmos;
               
               if (instance is IQuit quit)
                    EngineUpdate.onQuit += quit.Quit;
          }
     }
}