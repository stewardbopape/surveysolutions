﻿using System;
using System.Collections.Generic;
using System.Net;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.SystemData;
using Ncqrs;
using Ncqrs.Eventing.Storage;
using Newtonsoft.Json;
using Ninject;
using Ninject.Modules;
using WB.Core.Infrastructure.Storage.EventStore.Implementation;

namespace WB.Core.Infrastructure.Storage.EventStore
{
    public class EventStoreWriteSideModule : NinjectModule
    {
        private readonly EventStoreConnectionSettings settings;

        public EventStoreWriteSideModule(EventStoreConnectionSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            this.settings = settings;
        }

        public override void Load()
        {
            this.AddEventStoreProjections();
            NcqrsEnvironment.SetGetter<IStreamableEventStore>(this.GetEventStore);
            NcqrsEnvironment.SetGetter<IEventStore>(this.GetEventStore);
            this.Kernel.Bind<IStreamableEventStore>().ToMethod(_ => this.GetEventStore()).InSingletonScope();
            this.Kernel.Bind<IEventStore>().ToMethod(_ => this.GetEventStore()).InSingletonScope();
           
        }

        private IStreamableEventStore GetEventStore()
        {
            return new EventStoreWriteSide(this.settings);
        }

        private void AddEventStoreProjections()
        {
            var logger = Kernel.Get<GenericSubdomains.Logging.ILogger>();
            var httpEndPoint = new IPEndPoint(IPAddress.Parse(settings.ServerIP), settings.ServerHttpPort);
            var manager = new ProjectionsManager(new EventStoreLogger(logger), httpEndPoint, TimeSpan.FromSeconds(2));

            var userCredentials = new UserCredentials(this.settings.Login, this.settings.Password);
            try
            {
                var status = JsonConvert.DeserializeAnonymousType(manager.GetStatusAsync("$by_category").Result, new { status = "" });
                if (status.status != "Running")
                {
                    manager.EnableAsync("$by_category", userCredentials);
                }
                manager.GetStatusAsync("ToAllEvents", userCredentials).Wait();
            }
            catch (AggregateException)
            {
                string projectionQuery = @"fromCategory('" + EventStoreWriteSide.EventsCategory + @"') 
                                                .when({        
                                                    $any: function (s, e) {
                                                        linkTo('" + EventStoreWriteSide.AllEventsStream + @"', e)
                                                    }
                                                })";
                manager.CreateContinuousAsync("ToAllEvents", projectionQuery, userCredentials);
            }
        }
    }
}