﻿using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Machine.Specifications;
using Ncqrs.Eventing;
using Ncqrs.Eventing.Storage;
using WB.Infrastructure.Native.Storage.Postgre;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;
using WB.UI.Designer.Providers.CQRS.Accounts.Events;
using WB.Infrastructure.Native.Storage;
using It = Machine.Specifications.It;

namespace WB.Tests.Integration.PostgreSQLEventStoreTests
{
    public class when_stroring_event : with_postgres_db
    {
        Establish context = () =>
        {
            eventSourceId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");

            int sequenceCounter = 1;
            var eventTypeResolver = new EventTypeResolver();
            eventTypeResolver.RegisterEventDataType(typeof(AccountRegistered));
            eventTypeResolver.RegisterEventDataType(typeof(AccountConfirmed));
            eventTypeResolver.RegisterEventDataType(typeof(AccountLocked));

            events = new UncommittedEventStream(Guid.NewGuid(), null);

            events.Append(new UncommittedEvent(Guid.NewGuid(),
                eventSourceId,
                sequenceCounter++,
                0,
                DateTime.UtcNow,
                new AccountRegistered { ApplicationName = "App",
                    ConfirmationToken = "token",
                    Email = "test@test.com" }));

            events.Append(new UncommittedEvent(Guid.NewGuid(),
                eventSourceId,
                sequenceCounter++,
                0,
                DateTime.UtcNow,
                new AccountConfirmed()));
            events.Append(new UncommittedEvent(Guid.NewGuid(),
                eventSourceId,
                sequenceCounter++,
                0,
                DateTime.UtcNow,
                new AccountLocked()));

            eventStore = new PostgresEventStore(
                new PostgreConnectionSettings { ConnectionString = connectionStringBuilder.ConnectionString }, 
                eventTypeResolver,
                Mock.Of<IEventSerializerSettingsFactory>());
        };

        Because of = () => eventStore.Store(events);

        It should_read_stored_events = () =>
        {
            var eventStream = eventStore.ReadFrom(eventSourceId, 0, int.MaxValue);
            eventStream.IsEmpty.ShouldBeFalse();
            eventStream.Count().ShouldEqual(3);

            var firstEvent = eventStream.First();
            firstEvent.Payload.ShouldBeOfExactType<AccountRegistered>();
            var accountRegistered = (AccountRegistered)firstEvent.Payload;

            accountRegistered.Email.ShouldEqual("test@test.com");
        };

        It should_persist_items_with_global_sequence_set = () =>
        {
            var eventStream = eventStore.ReadFrom(eventSourceId, 0, int.MaxValue);
            eventStream.Select(x => x.GlobalSequence).SequenceEqual(new [] {1L, 2, 3}).ShouldBeTrue();
        };
        
        It should_count_stored_events = () => eventStore.CountOfAllEvents().ShouldEqual(3);

        It should_be_able_to_read_all_events = () =>
        {
            var committedEvents = eventStore.GetAllEvents().ToList();
            committedEvents.Count.ShouldEqual(3);
            committedEvents[0].Payload.ShouldBeOfExactType(typeof(AccountRegistered));
            committedEvents[1].Payload.ShouldBeOfExactType(typeof(AccountConfirmed));
            committedEvents[2].Payload.ShouldBeOfExactType(typeof(AccountLocked));
        };

        It should_be_able_to_count_events_after_position = () => eventStore.GetEventsCountAfterPosition(new EventPosition(0, 0, eventSourceId, 1)).ShouldEqual(2);

        It should_be_able_to_get_events_after_position = () =>
        {
            List<CommittedEvent> eventsAfterPosition = eventStore.GetEventsAfterPosition(new EventPosition(0, 0, eventSourceId, 1)).ToList()[0].ToList();
            eventsAfterPosition[0].Payload.ShouldBeOfExactType(typeof(AccountConfirmed));
            eventsAfterPosition[1].Payload.ShouldBeOfExactType(typeof(AccountLocked));
        };

        static PostgresEventStore eventStore;
        static UncommittedEventStream events;
        static Guid eventSourceId;
    }
}