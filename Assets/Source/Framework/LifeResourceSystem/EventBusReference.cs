using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LifeResourceSystem
{
    [CreateAssetMenu(fileName = "EventBus", menuName = "Systems/Life Resource System/Event Bus")]
    public class EventBusReference : ScriptableObject
    {
        private Dictionary<Type, List<object>> subscribers = new Dictionary<Type, List<object>>();
        
        public void Subscribe<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (!subscribers.ContainsKey(type))
            {
                subscribers[type] = new List<object>();
            }
            subscribers[type].Add(callback);
        }

        public void Unsubscribe<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (subscribers.ContainsKey(type))
            {
                subscribers[type].Remove(callback);
            }
        }

        public void Publish<T>(T eventData)
        {
            var type = typeof(T);
            if (subscribers.ContainsKey(type))
            {
                foreach (var subscriber in subscribers[type].ToList())
                {
                    ((Action<T>)subscriber).Invoke(eventData);
                }
            }
        }
    }
}