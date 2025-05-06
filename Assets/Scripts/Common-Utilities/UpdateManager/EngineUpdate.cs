using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using FixedUpdate = UnityEngine.PlayerLoop.FixedUpdate;
using Update = UnityEngine.PlayerLoop.Update;


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
     
     public static event System.Action onQuit;
     public static event System.Action onGUI;
     public static event System.Action onDrawGizmos;
     
     #if UNITY_EDITOR
     public static event System.Action onQuitInEditor;
     #endif
     
     private static PlayerLoopSystem playerLoopSystem;

     private static InternalUpdate<Initialization> inInitialization;
     
     private static InternalUpdate<Update> inPreUpdate;
     
     private static InternalUpdate<Update> inPostUpdate;
     
     private static InternalUpdate<FixedUpdate> inPreFixedUpdate;
     
     private static InternalUpdate<FixedUpdate> inPostFixedUpdate;
     
     private static InternalUpdate<PreLateUpdate> inPreLateUpdate;
     
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
          
          
          
          framesPerSecond = new();
          
          inInitialization = new(ref playerLoopSystem, 0);
          
          inPreUpdate = new (ref playerLoopSystem, 0);
          
          inPostUpdate = new (ref playerLoopSystem, 2);
          
          inPreFixedUpdate = new (ref playerLoopSystem,  4);
          
          inPostFixedUpdate = new (ref playerLoopSystem,  6);
          
          inPreLateUpdate = new(ref playerLoopSystem,  -2);
          
          inPostLateUpdate = new(ref playerLoopSystem, -1);
          
          inEndUpdate = new (ref playerLoopSystem, -1);
          
          Application.quitting -= ApplicationOnquitting;
          Application.quitting += ApplicationOnquitting;
          
          
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
          
          inInitialization.AddLoop(framesPerSecond.Update);
          
          inPreLateUpdate.AddLoop(framesPerSecond.LateUpdate);
          
          inEndUpdate.AddLoop(framesPerSecond.EndUpdate);
          
          onQuit += framesPerSecond.Destroy;
          
          
          
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
          var go = new GameObject("OnGuiHandlerGO");
          var guiHandler = go.AddComponent<OnGuiHandler>();
          guiHandler.onGUI = OnGUI;
          guiHandler.onDrawGizmos = OnDrawGizmos;
     }

     private static void OnDrawGizmos()
     {
          onDrawGizmos?.Invoke();
     }

     private static void OnGUI()
     {
          framesPerSecond.OnGui(new Rect (25, 50, 500, 50));
          onGUI?.Invoke();
     }

     private static void ApplicationOnquitting()
     {
          onQuit?.Invoke();
     }

     #region Update
     public static void AddPreUpdate(System.Action update)
     {
          inPreUpdate.AddLoop(update);
     }
     public static void AddPreUpdate(IUpdate update)
     {
          inPreUpdate.AddLoop(update.MyUpdate);
     }
     public static void RemovePreUpdate(System.Action update)
     {
          inPreUpdate.RemoveLoop(update);
     }
     public static void RemovePreUpdate(IUpdate update)
     {
          inPreUpdate.RemoveLoop(update.MyUpdate);
     }
     
     
     public static void AddPostUpdate(System.Action update)
     {
          inPostUpdate.AddLoop(update);
     }
     public static void AddPostUpdate(IUpdate update)
     {
          inPostUpdate.AddLoop(update.MyUpdate);
     }
     public static void RemovePostUpdate(System.Action update)
     {
          inPostUpdate.RemoveLoop(update);
     }
     public static void RemovePostUpdate(IUpdate update)
     {
          inPostUpdate.RemoveLoop(update.MyUpdate);
     }

     #endregion

     #region FixedUpdate
     public static void AddPreFixedUpdate(System.Action update)
     {
          inPreFixedUpdate.AddLoop(update);
     }
     public static void AddPreFixedUpdate(IFixedUpdate update)
     {
          inPreFixedUpdate.AddLoop(update.MyFixedUpdate);
     }
     public static void RemovePreFixedUpdate(System.Action update)
     {
          inPreFixedUpdate.RemoveLoop(update);
     }
     public static void RemovePreFixedUpdate(IFixedUpdate update)
     {
          inPreFixedUpdate.RemoveLoop(update.MyFixedUpdate);
     }
     
     
     public static void AddPostFixedUpdate(System.Action update)
     {
          inPostFixedUpdate.AddLoop(update);
     }
     public static void AddPostFixedUpdate(IFixedUpdate update)
     {
          inPostFixedUpdate.AddLoop(update.MyFixedUpdate);
     }
     public static void RemovePostFixedUpdate(System.Action update)
     {
          inPostFixedUpdate.RemoveLoop(update);
     }
     public static void RemovePostFixedUpdate(IFixedUpdate update)
     {
          inPostFixedUpdate.RemoveLoop(update.MyFixedUpdate);
     }
     #endregion
     
     #region LateUpdate
     public static void AddPreLateUpdate(System.Action update)
     {
          inPreLateUpdate.AddLoop(update);
     }
     public static void AddPreLateUpdate(ILateUpdate update)
     {
         inPreLateUpdate.AddLoop(update.MyLateUpdate);
     }
     
     public static void RemovePreLateUpdate(System.Action update)
     {
          inPreLateUpdate.RemoveLoop(update);
     }

     public static void RemovePreLateUpdate(ILateUpdate update)
     {
         inPreLateUpdate.RemoveLoop(update.MyLateUpdate);
     }
     
     
     public static void AddPostLateUpdate(System.Action update)
     {
          inPostLateUpdate.AddLoop(update);
     }
     public static void AddPostLateUpdate(ILateUpdate update)
     {
         inPostLateUpdate.AddLoop(update.MyLateUpdate);
     }
     public static void RemovePostLateUpdate(System.Action update)
     {
         inPostLateUpdate.RemoveLoop(update);
     }
     public static void RemovePostLateUpdate(ILateUpdate update)
     {
          inPostLateUpdate.RemoveLoop(update.MyLateUpdate);
     }
     #endregion

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
