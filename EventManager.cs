using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace GameCtrlManagers
{
    public static class EventManager
    {
        private static Dictionary<Type, List<IGlobalSubscriber>> s_Subscribers
            = new Dictionary<Type, List<IGlobalSubscriber>>();

        private static Dictionary<Type, List<Type>> s_CashedSubscriberTypes =
            new Dictionary<Type, List<Type>>();

        public static void Subscribe(IGlobalSubscriber subscriber)
        {
            List<Type> subscriberTypes = GetSubscribersTypes(subscriber.GetType());
            int cnt = subscriberTypes.Count;
            for (int i = 0; i < cnt; i++)
            {
                Type t = subscriberTypes[i];
                if (!s_Subscribers.ContainsKey(t))
                    s_Subscribers[t] = new List<IGlobalSubscriber>();
                s_Subscribers[t].Add(subscriber);
            }
        }

        public static void Unsubscribe(IGlobalSubscriber subcriber)
        {
            List<Type> subscriberTypes = GetSubscribersTypes(subcriber.GetType());
            for (int i = 0; i < subscriberTypes.Count; i++)
            {
                Type t = subscriberTypes[i];
                if (s_Subscribers.ContainsKey(t))
                    s_Subscribers[t].Remove(subcriber);
            }
        }

        public static List<Type> GetSubscribersTypes(Type globalSubscriber)
        {
            if (s_CashedSubscriberTypes.ContainsKey(globalSubscriber))
                return s_CashedSubscriberTypes[globalSubscriber];

            List<Type> subscriberTypes = new List<Type>();
            List<Type> allTypes = new List<Type>(globalSubscriber.GetInterfaces());
            int cnt = allTypes.Count;
            for (int i = 0; i < cnt; i++)
            {
                if (typeof(IGlobalSubscriber).IsAssignableFrom(allTypes[i]))
                {
                    subscriberTypes.Add(allTypes[i]);
                }
            }
            s_CashedSubscriberTypes[globalSubscriber] = subscriberTypes;
            return subscriberTypes;
        }

        public static void RaiseEvent<TSubscriber>(Action<TSubscriber> action)
            where TSubscriber : IGlobalSubscriber
        {
            List<IGlobalSubscriber> subscribers = s_Subscribers[typeof(TSubscriber)];
            for (int i = 0; i < subscribers.Count; i++)
            {
                IGlobalSubscriber subscriber = subscribers[i];
                try
                {
                    action.Invoke((TSubscriber)subscriber);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }
    }
}
