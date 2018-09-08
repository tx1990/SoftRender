using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace SoftRender
{
    public class Loom : MonoBehaviour
    {
        public static int maxThreads = 8;
        private static int numThreads;

        private static Loom _current;

        private static bool initialized;

        private readonly List<Action> _actions = new List<Action>();
        private int _count;

        private readonly List<Action> _currentActions = new List<Action>();

        private readonly List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();

        private readonly List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();

        public static Loom Current
        {
            get
            {
                Initialize();
                return _current;
            }
        }

        private void Awake()
        {
            _current = this;
            initialized = true;
        }

        private static void Initialize()
        {
            if (!initialized)
            {
                if (!Application.isPlaying)
                    return;
                initialized = true;
                var g = new GameObject("Loom");
                _current = g.AddComponent<Loom>();
            }
        }

        public static void QueueOnMainThread(Action action, float time = 0f)
        {
            if (Math.Abs(time) > 0.001f)
                lock (Current._delayed)
                {
                    Current._delayed.Add(new DelayedQueueItem {time = Time.time + time, action = action});
                }
            else
                lock (Current._actions)
                {
                    Current._actions.Add(action);
                }
        }

        public static void RunAsync(Action a)
        {
            Initialize();
            while (numThreads >= maxThreads) Thread.Sleep(1);

            Interlocked.Increment(ref numThreads);
            ThreadPool.QueueUserWorkItem(RunAction, a);
        }

        private static void RunAction(object action)
        {
            try
            {
                ((Action) action)();
            }
            catch
            {
            }
            finally
            {
                Interlocked.Decrement(ref numThreads);
            }
        }


        private void OnDisable()
        {
            if (_current == this) _current = null;
        }


        // Use this for initialization  
        private void Start()
        {
        }

        // Update is called once per frame  
        private void Update()
        {
            lock (_actions)
            {
                _currentActions.Clear();
                _currentActions.AddRange(_actions);
                _actions.Clear();
            }

            foreach (var a in _currentActions) a();

            lock (_delayed)
            {
                _currentDelayed.Clear();
                _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));
                foreach (var item in _currentDelayed)
                    _delayed.Remove(item);
            }

            foreach (var delayed in _currentDelayed) delayed.action();
        }

        public struct DelayedQueueItem
        {
            public float time;
            public Action action;
        }
    }
}