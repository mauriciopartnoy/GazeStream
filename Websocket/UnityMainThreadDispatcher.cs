using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : Singleton<UnityMainThreadDispatcher>
{
    private readonly Queue<Action> _executionQueue = new Queue<Action>();


    private void Update()
    {
        while (_executionQueue.Count > 0)
        {
            var action = _executionQueue.Dequeue();
            action();
        }
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}