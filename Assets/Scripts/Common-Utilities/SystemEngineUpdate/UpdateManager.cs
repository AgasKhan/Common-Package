using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SystemEngineUpdate
{
    public class DataOrientedUpdate : IUpdate, IDataOrientedUpdateManager
    {
        public abstract class Data : IUpdate, IDisposable
        {
            public int Index { get; set; }
        
            public abstract bool Enable { get; }
            
            public abstract void Add(IIndexed Object);
            public abstract void Remove(IIndexed Object);

            public abstract void MyUpdate();
            
            public abstract void Dispose();
        }
        
        public class Data<T> : Data where T : IIndexed
        {
            public override bool Enable => _updateList.Count > 0;

            private FPSTreholdSelector gmFPSTrehold;
            
            private RotatingList<T> _updateList = new();

            private IDataOrientedUpdateManager.Delegate<T> _delegate;

            private System.Action _update;

            public Data(IDataOrientedUpdateManager.Delegate<T> action , FPSTreholdSelector? gmFPSTrehold)
            {
                _delegate = action;
                
                Debug.Log(gmFPSTrehold.HasValue);
                
                if (gmFPSTrehold.HasValue)
                {
                    this.gmFPSTrehold = gmFPSTrehold.Value;
                    _update = DeferredUpdate;
                }
                else
                    _update = NormalUpdate;
            }

            public override void Add(IIndexed Object)
            {
                ((T)Object).AddTo(_updateList);
            }

            public override void Remove(IIndexed Object)
            {
                ((T)Object).RemoveToAtSwapBack(_updateList);
            }
            
            public override void MyUpdate()
            {
                _update();
            }

            void DeferredUpdate()
            {
                for (int i = 0; i < _updateList.Count; i++)
                {
                    _delegate(ref _updateList.GetNext());

                    if (gmFPSTrehold)
                    {
                        break;
                    }
                }
            }
            
            void NormalUpdate()
            {
                for (int i = 0; i < _updateList.Count; i++)
                {
                    _delegate(ref _updateList.GetValue(i));
                }
            }
            
            public override void Dispose()
            {
                _updateList.Clear();
                _updateList = null;
                _delegate = null;
                _update = null;
            }
        }
        
        private Dictionary<System.Delegate, Data> _dataOriented = new ();

        public bool Enable => _dataOriented.Count > 0;
        
        public int Index { get; set; }

        public void Add<T>(T Object, IDataOrientedUpdateManager.Delegate<T> update, FPSTreholdSelector? gmFPSTrehold = null) where T : IIndexed
        {
            if (_dataOriented.TryGetValue(update, out var value))
            {
                value.Add(Object);
            }
            else
            {
                var newUpdates = new Data<T>(update,gmFPSTrehold);
                
                newUpdates.Add(Object);
                
                _dataOriented.Add(update, newUpdates);
            }
        }

        public void Remove<T>(T Object, IDataOrientedUpdateManager.Delegate<T> update) where T : IIndexed
        {
            if (_dataOriented.TryGetValue(update, out var value))
            {
                value.Remove(Object);
            }
        }
        
        public void MyUpdate()
        {
            foreach (var keyValue in _dataOriented)
            {
                if(keyValue.Value.Enable)
                    keyValue.Value.MyUpdate();
            }
        }

        public void Clear()
        {
            foreach (var keyValue in _dataOriented)
            {
                keyValue.Value.Dispose();
            }
            _dataOriented.Clear();
        }
    }
        
    public class DeferredUpdateManager : IDeferredUpdateManager
    {
        public FPSTreholdSelector gmFPSTrehold;
        
        private RotatingList<IDeferredUpdate> _updateList = new();
        
        public void Add(IDeferredUpdate deferredUpdate)
        {
            deferredUpdate.AddTo(_updateList);
        }
        
        public void Remove(IDeferredUpdate deferredUpdate)
        {
            deferredUpdate.RemoveToAtSwapBack(_updateList);
        }

        public void Clear()
        {
            _updateList.Clear();
        }

        public void MyUpdate()
        {
            if(_updateList.Count==0)
                return;

            for (int i = _updateList.Count - 1; i >= 0; i--)
            {
                _updateList.GetNext().MyDeferredUpdate();
                
                if (gmFPSTrehold)
                {
                    break;
                }
            }
        }
    }
}



