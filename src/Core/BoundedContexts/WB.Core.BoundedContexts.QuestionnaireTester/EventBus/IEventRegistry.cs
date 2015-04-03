﻿using System;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.BoundedContexts.QuestionnaireTester.EventBus.Implementation;

namespace WB.Core.BoundedContexts.QuestionnaireTester.EventBus
{
    public interface IEventRegistry
    {
        void Subscribe<TEvent>(Action<TEvent> handler);
        void Subscribe<TEvent>(IEventBusEventHandler<TEvent> handler);
        void SubscribeAll(object obj);

        void Unsubscribe<TEvent>(Action<TEvent> handler);
        void Unsubscribe<TEvent>(IEventBusEventHandler<TEvent> handler);
        void UnsubscribeAll(object obj);

        IEventSubscription<TEvent> GetSubscription<TEvent>();
    }
}