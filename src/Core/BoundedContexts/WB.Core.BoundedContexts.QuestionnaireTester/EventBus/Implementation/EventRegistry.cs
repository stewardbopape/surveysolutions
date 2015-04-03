﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.GenericSubdomains.Utils.Services;

namespace WB.Core.BoundedContexts.QuestionnaireTester.EventBus.Implementation
{
    public class EventRegistry: IEventRegistry
    {
        private readonly ILogger logger;

        private readonly ConcurrentDictionary<Type, IEventSubscription> subscriptions = new ConcurrentDictionary<Type, IEventSubscription>();

        public EventRegistry(ILogger logger)
        {
            this.logger = logger;
        }

        #region IMessageBus Members

        public void Subscribe<TEvent>(IEventBusEventHandler<TEvent> handler)
        {
            Subscribe<TEvent>(handler.Handle);
        }

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var subscription = (EventSubscription<TEvent>)subscriptions.GetOrAdd(
                typeof(TEvent),
                t => new EventSubscription<TEvent>());
            subscription.Subscribe(handler);
        }

        public void Unsubscribe<TEvent>(IEventBusEventHandler<TEvent> handler)
        {
            Unsubscribe<TEvent>(handler.Handle);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof (TEvent);
            IEventSubscription subscription;
            if (subscriptions.TryGetValue(eventType, out subscription))
            {
                ((EventSubscription<TEvent>) subscription).Unsubscribe(handler);
            }
            else
            {
                logger.Info("No subscribers for event {0} found.".FormatString(eventType.ToString()));
            }
        }

        private readonly string subscribeMethodName = "Subscribe";
        private readonly string unsubscribeMethodName = "Unsubscribe";
//        private readonly string subscribeMethodName = Reflect<EventRegistry>.MethodName(c => c.Subscribe(null));
//        private readonly string unsubscribeMethodName = Reflect<EventRegistry>.MethodName(c => c.Unsubscribe(null));

        public void SubscribeAll(object obj)
        {
            var eventTypes = GetEventsListToHandleFromClass(obj);
            CallMethodForEvent(subscribeMethodName, obj, eventTypes);
        }

        public void UnsubscribeAll(object obj)
        {
            var eventTypes = GetEventsListToHandleFromClass(obj);
            CallMethodForEvent(unsubscribeMethodName, obj, eventTypes);
        }

        #endregion

        private Type[] GetEventsListToHandleFromClass(object obj)
        {
            Type type = obj.GetType();
            Type[] interfaces = type.GetInterfaces();
            return interfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IEventBusEventHandler<>))
                .Select(k => k.GetGenericArguments().Single())
                .ToArray();
        }


        private static void CallMethodForEvent(string methodName, object obj, Type[] eventTypes)
        {
            MethodInfo method = typeof(EventRegistry).GetMethod(methodName);

            foreach (Type eventType in eventTypes)
            {
                MethodInfo genericMethod = method.MakeGenericMethod(eventType);
                genericMethod.Invoke(obj, new[] { obj });
            }
        }

        public IEventSubscription<TEvent> GetSubscription<TEvent>()
        {
            var eventType = typeof(TEvent);

            IEventSubscription subscription;
            if (subscriptions.TryGetValue(eventType, out subscription))
            {
                return (IEventSubscription<TEvent>)subscription;
            }

            return null;
        }
    }
}