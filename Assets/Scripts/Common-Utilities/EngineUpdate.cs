using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;


public static class EngineUpdate
{
     class InternalUpdate<TUpdate, TLoopSystem> where TUpdate : IIndexed, IEnable
     {
          public delegate void Execute(ref TUpdate t);
          
          protected PlayerLoopSystem playerLoopSystem;
          
          protected RefList<TUpdate> loopList = new();
          
          protected Execute execute;

          public InternalUpdate(ref PlayerLoopSystem currentPlayerLoop, Execute execute, int index) : base()
          {
               this.execute = execute;
               
               playerLoopSystem = new PlayerLoopSystem()
               {
                    type = typeof(InternalUpdate<TUpdate, TLoopSystem>),
                    updateDelegate = Loop
               };
               
               if (!InsertSystem<TLoopSystem>(ref currentPlayerLoop, playerLoopSystem, index)) 
               {
                    Debug.LogWarning("Unable to register into the loop.");
                    return;
               }
               
               PlayerLoop.SetPlayerLoop(currentPlayerLoop);
               
#if UNITY_EDITOR
               UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeState;
               UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeState;
            
               void OnPlayModeState(UnityEditor.PlayModeStateChange state) 
               {
                    if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) 
                    {
                         PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
                         
                         RemoveSystem(ref currentPlayerLoop, playerLoopSystem);

                         PlayerLoop.SetPlayerLoop(currentPlayerLoop);
                    }
               }
#endif
          }

          public void AddLoop(TUpdate loop)
          {
               loop.AddTo(loopList);
          }

          public void RemoveLoop(TUpdate loop)
          {
               loop.RemoveToAtSwapBack(loopList);
          }
          
          void Loop()
          {
               for (int i = 0; i < loopList.Count; i++)
               {
                    if(!loopList[i].Enable)
                         continue;
                    
                    execute(ref loopList.GetValue(i));
               }
          }
     }
     
     private static PlayerLoopSystem playerLoopSystem;

     private static InternalUpdate<IUpdate, Update> inPreUpdate;
     
     private static InternalUpdate<IUpdate, Update> inPostUpdate;
     
     private static InternalUpdate<IFixedUpdate, FixedUpdate> inPreFixedUpdate;
     
     private static InternalUpdate<ILateUpdate, PreLateUpdate> inPreLateUpdate;

     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
     static void Init()
     {
          StringBuilder stringBuilder = new StringBuilder();
          
          playerLoopSystem = PlayerLoop.GetCurrentPlayerLoop();
          
          stringBuilder.AppendLine("UnityPlayer loop");

          foreach (var subSystem in playerLoopSystem.subSystemList)
          {
               PrintSubsystem(subSystem, stringBuilder, 0);
          }
          
          Debug.Log(stringBuilder.ToString());
          
          inPreUpdate = new (ref playerLoopSystem, (ref IUpdate a) => a.MyUpdate(), 0);
          
          inPostUpdate = new (ref playerLoopSystem, (ref IUpdate a) => a.MyUpdate(), 2);
          
          inPreFixedUpdate = new (ref playerLoopSystem, (ref IFixedUpdate a) => a.MyFixedUpdate(), 4);
          
          inPreLateUpdate = new(ref playerLoopSystem, (ref ILateUpdate a) => a.MyLateUpdate(), -2);

          stringBuilder.Clear();
          
          stringBuilder.AppendLine("Post UnityPlayer loop");

          foreach (var subSystem in playerLoopSystem.subSystemList)
          {
               PrintSubsystem(subSystem, stringBuilder, 0);
          }
          
          Debug.Log(stringBuilder.ToString());
     }


     public static void AddPreUpdate(IUpdate update)
     {
          inPreUpdate.AddLoop(update);
     }

     public static void RemovePreUpdate(IUpdate update)
     {
          inPreUpdate.RemoveLoop(update);
     }
     
     
     public static void AddPostUpdate(IUpdate update)
     {
          inPostUpdate.AddLoop(update);
     }

     public static void RemovePostUpdate(IUpdate update)
     {
          inPostUpdate.RemoveLoop(update);
     }
     
     
     public static void AddPreFixedUpdate(IFixedUpdate update)
     {
          inPreFixedUpdate.AddLoop(update);
     }
     
     public static void RemovePreFixedUpdate(IFixedUpdate update)
     {
          inPreFixedUpdate.RemoveLoop(update);
     }
     
     
     public static void AddPreLateUpdate(ILateUpdate update)
     {
          inPreLateUpdate.AddLoop(update);
     }

     public static void RemovePreLateUpdate(ILateUpdate update)
     {
          inPreLateUpdate.RemoveLoop(update);
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
